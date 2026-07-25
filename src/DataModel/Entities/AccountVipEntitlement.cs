// <copyright file="AccountVipEntitlement.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Describes the source which granted VIP access to an account.
/// </summary>
public enum VipEntitlementSource
{
    /// <summary>
    /// A trial which was granted when the account was created.
    /// </summary>
    Trial,

    /// <summary>
    /// A purchased VIP period.
    /// </summary>
    Purchase,

    /// <summary>
    /// A manually granted VIP period.
    /// </summary>
    Manual,

    /// <summary>
    /// A compensation granted by the server administration.
    /// </summary>
    Compensation,
}

/// <summary>
/// Grants an account a VIP level for a defined period.
/// </summary>
public class AccountVipEntitlement
{
    /// <summary>
    /// Gets or sets the granted VIP level.
    /// </summary>
    public int VipLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the entitlement becomes active.
    /// </summary>
    public DateTime StartsAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the entitlement expires.
    /// A value of <see langword="null"/> means that the entitlement is permanent.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the source of the entitlement.
    /// </summary>
    public VipEntitlementSource Source { get; set; }

    /// <summary>
    /// Gets or sets an optional external reference, for example a payment identifier.
    /// </summary>
    public string? SourceReference { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which this entitlement was revoked.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which this entitlement was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
