// <copyright file="ImperialEraSelectionPolicy.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Applies the authoritative eligibility rules for an Imperial Era selection.
/// </summary>
public static class ImperialEraSelectionPolicy
{
    /// <summary>
    /// Determines whether a character may select an Era.
    /// </summary>
    public static bool CanSelect(
        ImperialReign? reign,
        Guid? characterId,
        ImperialEra era,
        ValoriaThroneEventState eventState,
        DateTimeOffset now,
        TimeSpan selectionDuration)
    {
        return Enum.IsDefined(era)
               && era != ImperialEra.None
               && reign is { SelectedEra: ImperialEra.None }
               && characterId == reign.EmperorCharacterId
               && eventState is ValoriaThroneEventState.Idle or ValoriaThroneEventState.Cooldown
               && reign.StartedAt <= now
               && reign.ExpiresAt > now
               && reign.StartedAt.Add(selectionDuration) >= now;
    }
}
