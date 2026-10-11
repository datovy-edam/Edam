using System;
using System.Collections.Generic;
using System.Linq;
using Edam.Data.Projects.Contracts;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>How serious an arguments-file finding is: only <see cref="Error"/> makes a document invalid.</summary>
public enum ArgumentsIssueSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>One finding about an arguments document.</summary>
/// <param name="Code">Stable code (e.g. <c>procedure-unknown</c>) so a caller may react to it.</param>
/// <param name="Severity">Information · Warning · Error.</param>
/// <param name="Message">What is wrong, in words a person can act on.</param>
/// <param name="Where">Where it is, as a JSON path (e.g. <c>Process.ProcedureName</c>).</param>
public sealed record ArgumentsValidationIssue(
   string Code, ArgumentsIssueSeverity Severity, string Message, string? Where = null);

/// <summary>The outcome of validating one arguments document.</summary>
/// <param name="Valid">True when there is no <see cref="ArgumentsIssueSeverity.Error"/> — warnings are tolerated.</param>
/// <param name="Strict">Whether unknown properties were treated as errors.</param>
/// <param name="SchemaApplied">False when no schema was available (only the semantic rules ran).</param>
/// <param name="Issues">Every finding, in the order it was found.</param>
public sealed record ArgumentsValidationResult(
   bool Valid, bool Strict, bool SchemaApplied, IReadOnlyList<ArgumentsValidationIssue> Issues);

/// <summary>
/// <b>Arguments-file validation</b> (ADR-0011 addressing; the source specification is *EDAM Studio — Understanding
/// Projects* §2.3, kept at <c>docs/reference/0010 - Edam.Studio.Understanding.Projects.v0.docx</c>).
/// <para>
/// It runs <b>two layers</b>, because a JSON schema is necessary but not sufficient:
/// <list type="number">
/// <item>the <b>published schema</b> (shape, required sections, types, the procedure enumeration) — the contract
/// any editor or CI may also use; and</item>
/// <item>the <b>semantic rules the specification states but a schema cannot express</b>: §2.3 step 4 *"the name
/// must be the same as the Project folder"*, step 5 *"valid values ... should match existing enumerator values
/// as found in <c>Edam.Data.AssetConsole.AssetConsoleProcedure</c>"*, step 2 (a domain section to identify/add),
/// and the rule the whole codebase rests on — every reference resolves <b>inside the project</b>
/// (ADR-0011 decision 6).</item>
/// </list>
/// </para>
/// <para>
/// <b>Lenient by default</b> (the chosen behaviour): unknown properties are reported, never fatal — a document
/// that runs today must validate today, and the specification leaves <c>InspectArguments</c> and
/// <c>ElementTransform</c> as TBD. Pass <c>strict: true</c> for a CI gate.
/// </para>
/// </summary>
public sealed class ArgumentsValidator
{
    private readonly JsonSchema? m_Schema;
    private readonly IReadOnlyCollection<string>? m_KnownProcedures;
    private readonly HashSet<string>? m_KnownProperties;

    /// <summary>Whether unknown properties are fatal.</summary>
    public bool Strict { get; }

    /// <summary>True when a schema was supplied (so the shape rules ran).</summary>
    public bool HasSchema => m_Schema is not null;

    /// <param name="schemaJson">The schema document (read from the address the configuration states), or null.</param>
    /// <param name="strict">True to treat unknown properties as errors (a CI gate).</param>
    /// <param name="knownProcedures">
    /// The procedure names the runner accepts — the caller supplies them from
    /// <c>Edam.Data.AssetConsole.AssetConsoleProcedure</c>, so this type needs no dependency on it and a
    /// conformance check can compare the schema's enumeration with the code's.
    /// </param>
    public ArgumentsValidator(
       string? schemaJson, bool strict = false, IReadOnlyCollection<string>? knownProcedures = null)
    {
        Strict = strict;
        m_KnownProcedures = knownProcedures;

        if (!String.IsNullOrWhiteSpace(schemaJson))
        {
            // one synchronous parse at construction (no I/O afterwards, so Validate stays pure)
            m_Schema = JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();
            m_KnownProperties = new HashSet<string>(
               m_Schema.ActualProperties.Keys, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>The schema's own procedure enumeration — what the published contract tells other tools.</summary>
    public IReadOnlyCollection<string> SchemaProcedures
    {
        get
        {
            var property = m_Schema is null
               ? null
               : m_Schema.ActualProperties.TryGetValue("Process", out var process)
                  && process.ActualProperties.TryGetValue("ProcedureName", out var name)
                     ? name
                     : null;

            var values = property?.Enumeration;
            return values is null
               ? Array.Empty<string>()
               : values.Select(v => v?.ToString() ?? String.Empty).ToList();
        }
    }

    /// <summary>Validate one arguments document. Pure: no file system, no database, nothing thrown at the caller.</summary>
    /// <param name="argumentsJson">The document's text.</param>
    /// <param name="projectName">
    /// The project the document belongs to (its folder name), when known — §2.3 step 4 is checked against it.
    /// </param>
    public ArgumentsValidationResult Validate(string? argumentsJson, string? projectName = null)
    {
        var issues = new List<ArgumentsValidationIssue>();

        if (String.IsNullOrWhiteSpace(argumentsJson))
        {
            return new ArgumentsValidationResult(false, Strict, HasSchema, new[]
            {
                new ArgumentsValidationIssue("empty", ArgumentsIssueSeverity.Error,
                   "the arguments document is empty")
            });
        }

        JObject? root = null;
        try
        {
            root = JObject.Parse(argumentsJson!);
        }
        catch (Exception ex)
        {
            issues.Add(new ArgumentsValidationIssue("json", ArgumentsIssueSeverity.Error,
               "the document is not valid JSON: " + ex.Message));
            return new ArgumentsValidationResult(false, Strict, HasSchema, issues);
        }

        // ---- 1. the published contract: shape, required sections, types -------------------------
        if (m_Schema is not null)
        {
            try
            {
                foreach (var error in m_Schema.Validate(argumentsJson!))
                {
                    // the procedure name is re-stated below in words a person can act on (and only when the
                    // value really is a string), so the raw enumeration message is not repeated
                    if ((error.Path ?? String.Empty)
                        .IndexOf("ProcedureName", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    issues.Add(new ArgumentsValidationIssue("shape", ArgumentsIssueSeverity.Error,
                       error.ToString(), error.Path));
                }
            }
            catch (Exception ex)
            {
                // a schema that cannot be applied is reported, never fatal: the semantic rules still run
                issues.Add(new ArgumentsValidationIssue("schema-unavailable", ArgumentsIssueSeverity.Warning,
                   "the arguments schema could not be applied (" + ex.Message + ")"));
            }
        }
        else
        {
            issues.Add(new ArgumentsValidationIssue("schema-unavailable", ArgumentsIssueSeverity.Information,
               "no arguments schema was available, so only the rules a schema cannot express were checked"));
        }

        // ---- 2. the rules the specification states and a schema cannot express -------------------
        CheckProcedure(root, issues);
        CheckProjectName(root, projectName, issues);
        CheckDomain(root, issues);
        CheckReferences(root, issues);
        CheckUnknownProperties(root, issues);

        var valid = !issues.Any(i => i.Severity == ArgumentsIssueSeverity.Error);
        return new ArgumentsValidationResult(valid, Strict, HasSchema, issues);
    }

    private void CheckProcedure(JObject root, List<ArgumentsValidationIssue> issues)
    {
        const string where = "Process.ProcedureName";
        var procedure = root.SelectToken(where)?.Value<string>();

        if (String.IsNullOrWhiteSpace(procedure))
        {
            issues.Add(new ArgumentsValidationIssue("procedure-missing", ArgumentsIssueSeverity.Error,
               "the process must name the procedure to execute (§2.3 step 5)", where));
            return;
        }

        if (m_KnownProcedures is not null && m_KnownProcedures.Count > 0 &&
            !m_KnownProcedures.Contains(procedure!, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new ArgumentsValidationIssue("procedure-unknown", ArgumentsIssueSeverity.Error,
               "'" + procedure + "' is not one of the procedures this build knows (" +
               m_KnownProcedures.Count + " values of Edam.Data.AssetConsole.AssetConsoleProcedure)", where));
        }
    }

    private void CheckProjectName(JObject root, string? projectName, List<ArgumentsValidationIssue> issues)
    {
        const string where = "Project.Name";
        var stated = root.SelectToken(where)?.Value<string>();

        if (String.IsNullOrWhiteSpace(projectName))
        {
            issues.Add(new ArgumentsValidationIssue("project-name-unchecked", ArgumentsIssueSeverity.Information,
               "the project this document belongs to was not supplied, so the rule that the name must equal the " +
               "project folder could not be checked (§2.3 step 4)", where));
            return;
        }

        if (String.IsNullOrWhiteSpace(stated))
        {
            issues.Add(new ArgumentsValidationIssue("project-name-missing", ArgumentsIssueSeverity.Warning,
               "no Project.Name is stated; the specification asks for the project's name and version (§2.3 step 4)",
               where));
            return;
        }

        if (!String.Equals(stated, projectName, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ArgumentsValidationIssue("project-name-mismatch", ArgumentsIssueSeverity.Error,
               "Project.Name is '" + stated + "' but the project folder is '" + projectName +
               "' — the specification requires them to be the same (§2.3 step 4)", where));
        }
    }

    private static void CheckDomain(JObject root, List<ArgumentsValidationIssue> issues)
    {
        const string where = "Domain.DomainId";
        var domainId = root.SelectToken(where)?.Value<string>();

        if (String.IsNullOrWhiteSpace(domainId))
        {
            issues.Add(new ArgumentsValidationIssue("domain-missing", ArgumentsIssueSeverity.Warning,
               "no Domain.DomainId is stated, so the application cannot identify the asset domain (and add it " +
               "to the EDAM database when it is missing) when this document is selected (§2.3 step 2)", where));
        }
    }

    /// <summary>
    /// Every reference an arguments document holds must stay <b>inside the project</b> (ADR-0011 decision 6) —
    /// that is the rule the whole address model exists to make true, and the rule a reader relies on.
    /// </summary>
    private static void CheckReferences(JObject root, List<ArgumentsValidationIssue> issues)
    {
        var references = new List<(string Where, string? Value)>();

        var uriList = root.SelectToken("UriList") as JArray;
        if (uriList is not null)
        {
            for (var i = 0; i < uriList.Count; i++)
            {
                references.Add(("UriList[" + i + "]", uriList[i]?.Value<string>()));
            }
        }

        references.Add(("InputFile.Path", root.SelectToken("InputFile.Path")?.Value<string>()));
        references.Add(("OutputFile.Path", root.SelectToken("OutputFile.Path")?.Value<string>()));
        references.Add(("TextMapFilePath", root.SelectToken("TextMapFilePath")?.Value<string>()));

        foreach (var (where, value) in references)
        {
            if (String.IsNullOrWhiteSpace(value)) continue;
            issues.AddRange(CheckReference(where, value!));
        }

        if ((uriList is null || uriList.Count == 0) &&
            String.IsNullOrWhiteSpace(root.SelectToken("InputFile.Name")?.Value<string>()))
        {
            issues.Add(new ArgumentsValidationIssue("inputs-missing", ArgumentsIssueSeverity.Warning,
               "the document states no UriList and no InputFile, so there is nothing to read (§2.3 steps 6-7)",
               "UriList"));
        }
    }

    private static IEnumerable<ArgumentsValidationIssue> CheckReference(string where, string value)
    {
        var text = value.Trim();

        // an absolute or drive path is not an address at all (ADR-0011): it would step out of the project and
        // out of the catalog's container
        if (text.StartsWith('/') || text.StartsWith('\\') || text.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
            (text.Length > 1 && text[1] == ':'))
        {
            yield return new ArgumentsValidationIssue("path-absolute", ArgumentsIssueSeverity.Error,
               "'" + text + "' is an absolute path; a reference must be project-relative (e.g. './Archive/x.xsd')",
               where);
            yield break;
        }

        // '..' is clamped at the container root by the address model — it cannot escape the container, but it
        // does leave the PROJECT, which for an arguments document is almost always a mistake worth saying
        var segments = text.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == ".."))
        {
            yield return new ArgumentsValidationIssue("path-leaves-project", ArgumentsIssueSeverity.Warning,
               "'" + text + "' climbs out of the project with '..' (the address model keeps it inside the " +
               "container, but the specification addresses files inside the project)", where);
        }
    }

    private void CheckUnknownProperties(JObject root, List<ArgumentsValidationIssue> issues)
    {
        if (m_KnownProperties is null) return;

        foreach (var property in root.Properties())
        {
            if (m_KnownProperties.Contains(property.Name)) continue;

            issues.Add(new ArgumentsValidationIssue("unknown-property",
               Strict ? ArgumentsIssueSeverity.Error : ArgumentsIssueSeverity.Warning,
               "'" + property.Name + "' is not part of the arguments contract" +
               (Strict ? " (strict mode)" : " — tolerated, reported for the record"), property.Name));
        }
    }
}
