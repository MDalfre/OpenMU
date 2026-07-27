// <copyright file="ValoriaThroneOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configures the Valoria Throne event.
/// </summary>
public sealed class ValoriaThroneOptions
{
    /// <summary>Gets or sets a value indicating whether the event is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the server which hosts the event.</summary>
    public byte? EventServerId { get; set; }

    /// <summary>Gets or sets the event map identifier.</summary>
    public byte EventMapId { get; set; } = 30;

    /// <summary>Gets or sets the fallback map identifier.</summary>
    public byte FallbackMapId { get; set; }

    /// <summary>Gets or sets the fallback position x coordinate.</summary>
    public byte FallbackPositionX { get; set; } = 142;

    /// <summary>Gets or sets the fallback position y coordinate.</summary>
    public byte FallbackPositionY { get; set; } = 126;

    /// <summary>Gets or sets the announcement duration.</summary>
    public TimeSpan AnnouncementDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the preparation duration.</summary>
    public TimeSpan PreparationDuration { get; set; }

    /// <summary>Gets or sets the registration duration.</summary>
    public TimeSpan RegistrationDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the battle duration.</summary>
    public TimeSpan BattleDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the crown phase duration.</summary>
    public TimeSpan CrownPhaseDuration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets or sets the cooldown duration.</summary>
    public TimeSpan CooldownDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets a value indicating whether late entry is allowed.</summary>
    public bool AllowLateEntry { get; set; }

    /// <summary>Gets or sets a value indicating whether game masters bypass admission restrictions.</summary>
    public bool AllowGameMasterBypass { get; set; }

    /// <summary>Gets or sets the guardian monster definition identifier.</summary>
    public short GuardianMonsterId { get; set; }

    /// <summary>Gets or sets the guardian spawn x coordinate.</summary>
    public byte GuardianSpawnX { get; set; }

    /// <summary>Gets or sets the guardian spawn y coordinate.</summary>
    public byte GuardianSpawnY { get; set; }

    /// <summary>Gets or sets the guardian direction.</summary>
    public byte GuardianDirection { get; set; }

    /// <summary>Gets or sets the monsters which support the guardian during the battle.</summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<ValoriaThroneSupportMonsterOptions> SupportMonsters { get; set; } = new List<ValoriaThroneSupportMonsterOptions>();

    /// <summary>Gets or sets the scheduling configuration.</summary>
    public ValoriaThroneScheduleOptions Schedule { get; set; } = new();

    /// <summary>Gets the safe disabled configuration.</summary>
    public static ValoriaThroneOptions Default => new();
}

/// <summary>
/// Configures automated event starts.
/// </summary>
public sealed class ValoriaThroneScheduleOptions
{
    /// <summary>Gets or sets a value indicating whether automatic scheduling is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the IANA time zone identifier.</summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";

    /// <summary>Gets or sets the scheduled days.</summary>
    public ICollection<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek> { DayOfWeek.Sunday };

    /// <summary>Gets or sets the local event start time.</summary>
    public TimeOnly StartTime { get; set; } = new(20, 0);
}
