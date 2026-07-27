// <copyright file="ImperialReign.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Represents the currently active imperial reign of Valoria.
/// </summary>
public sealed class ImperialReign
{
    /// <summary>Gets or sets the identifier of the reign.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the emperor character.</summary>
    public Guid EmperorCharacterId { get; set; }

    /// <summary>Gets or sets the emperor name retained for presentation.</summary>
    public string EmperorCharacterName { get; set; } = string.Empty;

    /// <summary>Gets or sets the current runtime identifier of the imperial guild.</summary>
    public uint ImperialGuildId { get; set; }

    /// <summary>Gets or sets the imperial guild name retained for presentation.</summary>
    public string ImperialGuildName { get; set; } = string.Empty;

    /// <summary>Gets or sets the start of the reign.</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Gets or sets the end of the reign.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the era selected for this reign.</summary>
    public ImperialEra SelectedEra { get; set; }

    /// <summary>Gets or sets the time at which the era was selected.</summary>
    public DateTimeOffset? EraSelectedAt { get; set; }
}
