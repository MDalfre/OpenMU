// <copyright file="ImperialEraPresentation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>Provides centralized player-facing names and descriptions of imperial eras.</summary>
public static class ImperialEraPresentation
{
    /// <summary>Gets the localized era name.</summary>
    public static string GetName(ImperialEra era) => era switch
    {
        ImperialEra.Ascension => "Era da Ascensão",
        ImperialEra.Fortune => "Era da Fortuna",
        ImperialEra.Freedom => "Era da Liberdade",
        ImperialEra.Luck => "Era da Sorte",
        _ => "Nenhuma",
    };

    /// <summary>Gets the configured era benefit description.</summary>
    public static string GetDescription(ImperialEra era, ValoriaImperialEraOptions options) => era switch
    {
        ImperialEra.Ascension => $"Experiência global aumentada em {FormatBonus(options.AscensionExperienceMultiplier)}.",
        ImperialEra.Fortune => $"Taxa de drop comum aumentada em {FormatBonus(options.FortuneDropMultiplier)}.",
        ImperialEra.Freedom => "Personagens PK podem utilizar teleportes normais entre mapas.",
        ImperialEra.Luck => $"Chances da Chaos Machine aumentadas em {FormatBonus(options.LuckChaosMachineMultiplier)} e das joias em {FormatBonus(options.LuckJewelMultiplier)}.",
        _ => "O Imperador ainda não proclamou uma Era.",
    };

    private static string FormatBonus(float multiplier) => $"{Math.Max(0, multiplier - 1):P0}";
}
