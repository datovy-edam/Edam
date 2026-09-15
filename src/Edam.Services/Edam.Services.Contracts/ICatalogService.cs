namespace Edam.Services.Contracts;

/// <summary>
/// BL-6.2: onboards the data-catalog surface (<c>Edam.Data.Assets</c> + <c>Edam.Data.AssetDb</c>,
/// hosted via <c>Edam.Data.Assets.Services</c>) behind an interface as a hosted service.
/// Persistence/data-store wiring lands in BL-6.6.
/// </summary>
public interface ICatalogService : IWave1Service
{
}
