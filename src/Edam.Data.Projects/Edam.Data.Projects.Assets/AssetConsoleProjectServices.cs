using Edam.Data.Projects.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Projects.Assets;

/// <summary>
/// The DI hook for the asset-console adapter (PE-5b): bind the legacy console as the project
/// process. Kept here (not in the project composition root) so only this project references the
/// asset libraries — <c>AddProjectServices</c> stays provider-light.
/// </summary>
public static class AssetConsoleProjectServices
{
   /// <summary>Bind <see cref="AssetConsoleProjectProcess"/> as the project <c>IProjectProcess</c>.</summary>
   public static IServiceCollection AddAssetConsoleProjectProcess(this IServiceCollection services)
      => services.AddSingleton<IProjectProcess, AssetConsoleProjectProcess>();
}
