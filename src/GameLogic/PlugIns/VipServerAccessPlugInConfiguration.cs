namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Configuration for the <see cref="VipServerAccessPlugIn"/>.
/// </summary>
public class VipServerAccessPlugInConfiguration
{
    /// <summary>
    /// Gets or sets the minimum VIP level required to connect.
    /// </summary>
    public int MinimumVipLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets the game server IDs which require VIP access.
    /// </summary>
    public byte[] RestrictedServerIds { get; set; } = [3, 4];

    /// <summary>
    /// Gets or sets a value indicating whether game masters bypass the restriction.
    /// </summary>
    public bool BypassGameMasters { get; set; } = true;
}
