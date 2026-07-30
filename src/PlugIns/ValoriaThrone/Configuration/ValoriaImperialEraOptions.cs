// <copyright file="ValoriaImperialEraOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>Configures imperial era selection and benefits.</summary>
public sealed class ValoriaImperialEraOptions
{
    /// <summary>Gets or sets the time allowed to select an era.</summary>
    [Display(Name = "Prazo para escolher a Era (minutos)")]
    public TimeSpan SelectionDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the era selected after the deadline.</summary>
    public ImperialEra DefaultEra { get; set; } = ImperialEra.Ascension;

    /// <summary>Gets or sets the Ascension experience multiplier.</summary>
    public float AscensionExperienceMultiplier { get; set; } = 1.1f;

    /// <summary>Gets or sets the Fortune common drop multiplier.</summary>
    public float FortuneDropMultiplier { get; set; } = 1.1f;

    /// <summary>Gets or sets the Luck Chaos Machine multiplier.</summary>
    public float LuckChaosMachineMultiplier { get; set; } = 1.1f;

    /// <summary>Gets or sets the maximum effective Chaos Machine success percentage.</summary>
    public double MaximumChaosMachineSuccessRate { get; set; } = 100.0;

    /// <summary>Gets or sets a value indicating whether regular combinations receive the Luck bonus.</summary>
    public bool ApplyLuckToRegularCombinations { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether event-ticket combinations receive the Luck bonus.</summary>
    public bool ApplyLuckToEventCombinations { get; set; }

    /// <summary>Gets or sets a value indicating whether unknown custom combinations receive the Luck bonus.</summary>
    public bool ApplyLuckToCustomCombinations { get; set; }

    /// <summary>Gets or sets the Luck jewel multiplier.</summary>
    public float LuckJewelMultiplier { get; set; } = 1.1f;

    /// <summary>Gets or sets the maximum effective jewel success percentage.</summary>
    public double MaximumJewelSuccessRate { get; set; } = 100.0;

    /// <summary>Gets or sets the fallible jewels affected by the Luck era.</summary>
    public ICollection<ValoriaJewelKind> AffectedJewels { get; set; } = new List<ValoriaJewelKind>
    {
        ValoriaJewelKind.Soul,
        ValoriaJewelKind.Life,
        ValoriaJewelKind.Harmony,
    };
}
