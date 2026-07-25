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
    [Display(Name = "Restricted Server IDs")]
    public IList<int> RestrictedServers { get; set; } = [3, 4];

    /// <summary>
    /// Gets or sets a value indicating whether game masters bypass the restriction.
    /// </summary>
    public bool BypassGameMasters { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the expiration date is shown when the character enters the world.
    /// </summary>
    public bool ShowExpirationOnEnterWorld { get; set; } = true;

    /// <summary>
    /// Gets or sets the minutes before expiration at which warnings are shown.
    /// </summary>
    public IList<int> ExpirationWarningMinutes { get; set; } = [10, 5, 1];

    /// <summary>
    /// Gets or sets the message shown when a timed VIP entitlement is active.
    /// Placeholder 0 contains the UTC expiration date.
    /// </summary>
    public string ActiveVipMessage { get; set; } = "VIP level {0} active until {1:yyyy-MM-dd HH:mm} UTC.";

    /// <summary>
    /// Gets or sets the message shown when permanent VIP access is active.
    /// </summary>
    public string PermanentVipMessage { get; set; } = "VIP level {0} is permanently active.";

    /// <summary>
    /// Gets or sets the message shown before VIP access expires.
    /// </summary>
    public string ExpirationWarningMessage { get; set; } = "Your VIP access expires in {0} minute(s).";

    /// <summary>
    /// Gets or sets the message shown when VIP access expires on a restricted server.
    /// </summary>
    public string ExpiredMessage { get; set; } = "Your VIP access has expired. Returning to server selection.";
}
