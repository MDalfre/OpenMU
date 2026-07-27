// <copyright file="ValoriaCrownContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Holds the authoritative, transient state of a crown for one event execution.
/// </summary>
public sealed class ValoriaCrownContext
{
    /// <summary>Gets the event execution which owns the crown.</summary>
    public required Guid EventInstanceId { get; init; }

    /// <summary>Gets or sets the current ground object identifier.</summary>
    public ushort? GroundItemId { get; set; }

    /// <summary>Gets or sets the current crown holder character identifier.</summary>
    public Guid? HolderCharacterId { get; set; }

    /// <summary>Gets or sets the current crown holder guild identifier.</summary>
    public uint? HolderGuildId { get; set; }

    /// <summary>Gets or sets the current crown holder guild name.</summary>
    public string? HolderGuildName { get; set; }

    /// <summary>Gets or sets the current crown holder name.</summary>
    public string? HolderCharacterName { get; set; }

    /// <summary>Gets or sets the original guardian death position.</summary>
    public required Point OriginalDropPosition { get; init; }

    /// <summary>Gets or sets the time at which the crown was picked up.</summary>
    public DateTimeOffset? PickedUpAt { get; set; }

    /// <summary>Gets or sets the delivery deadline for the current holder.</summary>
    public DateTimeOffset? DeliveryDeadline { get; set; }

    /// <summary>Gets or sets the runtime identifier of the Senior used for the coronation.</summary>
    public ushort? SeniorObjectId { get; set; }

    /// <summary>Gets or sets the time at which the carrier was asked to confirm the coronation.</summary>
    public DateTimeOffset? ConfirmationRequestedAt { get; set; }

    /// <summary>Gets or sets the time at which the coronation started.</summary>
    public DateTimeOffset? CoronationStartedAt { get; set; }

    /// <summary>Gets or sets the time at which the coronation ends.</summary>
    public DateTimeOffset? CoronationEndsAt { get; set; }

    /// <summary>Clears the logical crown holder.</summary>
    public void ClearHolder()
    {
        this.HolderCharacterId = null;
        this.HolderGuildId = null;
        this.HolderGuildName = null;
        this.HolderCharacterName = null;
        this.PickedUpAt = null;
        this.DeliveryDeadline = null;
        this.SeniorObjectId = null;
        this.ConfirmationRequestedAt = null;
        this.CoronationStartedAt = null;
        this.CoronationEndsAt = null;
    }
}
