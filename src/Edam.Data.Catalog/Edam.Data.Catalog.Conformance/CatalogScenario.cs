using Edam.Data.Catalog.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Catalog.Conformance;

/// <summary>
/// Shared provider-conformance scenario (ADR-0006): the SAME catalog behavior is exercised against
/// whatever <see cref="ICatalogStore"/>/<see cref="IContentStore"/> is supplied, proving a back-end
/// can be swapped (FileSystem, PostgreSQL, in-memory) without changing the scenario/callers.
/// </summary>
public static class CatalogScenario
{
    public readonly record struct Check(string Name, bool Passed, string Detail);

    public static async Task<IReadOnlyList<Check>> RunAsync(
        ICatalogStore store, IContentStore? content, CancellationToken ct = default)
    {
        var checks = new List<Check>();

        // ---- containers -----------------------------------------------------
        var container = store.EnlistContainer("conformance", "Conformance container", null, ContainerType.DataContext);
        checks.Add(New("EnlistContainer returns a container", container is not null && !string.IsNullOrEmpty(container.ContainerId), container.ContainerId));

        var byName = await store.GetContainerAsync("conformance", ct: ct);
        checks.Add(New("GetContainer by name resolves it", byName?.ContainerId == "conformance", byName?.ContainerId ?? "<null>"));

        var byId = store.GetContainer(container.Id);
        checks.Add(New("GetContainer by id resolves it", byId?.Id == container.Id, byId?.Id.ToString() ?? "<null>"));

        // ---- items ----------------------------------------------------------
        var branch = await store.CreateBranchAsync("/alpha/beta", "A branch", container.Id, ct);
        checks.Add(New("CreateBranch returns a path-addressed item", branch is not null && branch.FullPath == "/alpha/beta", branch?.FullPath ?? "<null>"));

        var byPath = await store.GetItemByPathAsync("/alpha/beta", ct);
        checks.Add(New("GetItemByPath resolves the branch", byPath?.FullPath == "/alpha/beta", byPath?.FullPath ?? "<null>"));

        var containers = await store.GetContainersAsync(ct);
        checks.Add(New("GetContainers enumerates enlisted containers", containers.Count >= 1, $"{containers.Count} container(s)"));

        // ---- item data ------------------------------------------------------
        var leaf = store.CreateDataLeaf(branch!, "note", dataValue: "hello-edam");
        checks.Add(New("CreateDataLeaf adds a leaf", leaf is not null && leaf.Name == "note", leaf?.Name ?? "<null>"));

        var got = store.GetDataByName(branch!.Id, "note");
        checks.Add(New("GetDataByName returns the leaf", got?.Value == "hello-edam", got?.Value ?? "<null>"));

        var removed = store.DeleteData(leaf!.Id);
        var absent = store.GetData(leaf.Id);
        checks.Add(New("DeleteData removes the leaf", removed && absent is null, $"removed={removed} absent={absent is null}"));

        // ---- content store (if provided) ------------------------------------
        if (content is not null)
        {
            const string path = "/docs/readme";
            var bytes = Encoding.UTF8.GetBytes("edam-content");
            await content.WriteAsync(path, new MemoryStream(bytes), ct);
            var exists = await content.ExistsAsync(path, ct);
            using var opened = await content.OpenReadAsync(path, ct);
            var roundtrip = opened is not null && ((MemoryStream)opened).ToArray().SequenceEqual(bytes);
            var deleted = await content.DeleteAsync(path, ct);
            var gone = !await content.ExistsAsync(path, ct);
            checks.Add(New("IContentStore write/exists/read", exists && roundtrip, $"exists={exists} roundtrip={roundtrip}"));
            checks.Add(New("IContentStore delete", deleted && gone, $"deleted={deleted} gone={gone}"));
        }

        // ---- path/URI addressing --------------------------------------------
        checks.Add(New("Resource addressing is path/URI-based", branch.FullPath.StartsWith("/") && byPath.FullPath == branch.FullPath, branch.FullPath));

        return checks;
    }

    private static Check New(string name, bool passed, string detail)
        => new(name, passed, detail);
}
