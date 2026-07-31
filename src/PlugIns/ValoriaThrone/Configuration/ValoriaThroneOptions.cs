// <copyright file="ValoriaThroneOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

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
    [Display(Name = "Duração do anúncio (minutos)")]
    public TimeSpan AnnouncementDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the preparation duration.</summary>
    [Display(Name = "Duração da preparação (minutos)")]
    public TimeSpan PreparationDuration { get; set; }

    /// <summary>Gets or sets the registration duration.</summary>
    [Display(Name = "Duração do registro (minutos)")]
    public TimeSpan RegistrationDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the battle duration.</summary>
    [Display(Name = "Duração da batalha (minutos)")]
    public TimeSpan BattleDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the crown phase duration.</summary>
    [Display(Name = "Duração da fase da coroa (minutos)")]
    public TimeSpan CrownPhaseDuration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets or sets the maximum time a carrier has to deliver the crown.</summary>
    public TimeSpan CrownDeliveryDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Gets or sets the delay before a lost crown returns to its original position.</summary>
    public TimeSpan CrownRespawnDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the item group used as temporary crown representation.</summary>
    public byte CrownItemGroup { get; set; } = 14;

    /// <summary>Gets or sets the item number used as temporary crown representation.</summary>
    public short CrownItemNumber { get; set; } = 13;

    /// <summary>Gets or sets the reserved item level which identifies the crown visual on supported clients.</summary>
    public byte CrownVisualItemLevel { get; set; } = 15;

    /// <summary>Gets or sets the client marker identifier used for the crown carrier.</summary>
    public byte CrownCarrierMarkerId { get; set; } = 1;

    /// <summary>Gets or sets the Senior NPC definition identifier.</summary>
    public short SeniorNpcId { get; set; } = 223;

    /// <summary>Gets or sets the time allowed for the carrier to confirm the coronation.</summary>
    [Display(Name = "Tempo para confirmar a coroação (minutos)")]
    public TimeSpan CoronationConfirmationDuration { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Gets or sets the duration of the coronation ceremony.</summary>
    [Display(Name = "Duração da coroação (minutos)")]
    public TimeSpan CoronationDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the maximum distance from the Senior during the ceremony.</summary>
    public byte CoronationRadius { get; set; } = 4;

    /// <summary>Gets or sets the duration of a reign after a successful coronation.</summary>
    [Display(Name = "Duração do reinado (minutos)")]
    public TimeSpan ReignDuration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Gets or sets the current persistent imperial reign.</summary>
    [ScaffoldColumn(false)]
    public ImperialReign? ImperialReign { get; set; }

    /// <summary>Gets or sets the Lands of Trials access configuration.</summary>
    public ValoriaLandsOfTrialsOptions LandsOfTrials { get; set; } = new();

    /// <summary>Gets or sets imperial era configuration.</summary>
    public ValoriaImperialEraOptions ImperialEras { get; set; } = new();

    /// <summary>Gets or sets the cooldown duration.</summary>
    [Display(Name = "Cooldown do evento (minutos)")]
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
