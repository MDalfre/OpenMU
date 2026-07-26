// <copyright file="AttackerExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.GameLogic;

/// <summary>
/// Provides display helpers for attackers.
/// </summary>
internal static class AttackerExtensions
{
    /// <summary>
    /// Gets a display name for an attacker.
    /// </summary>
    /// <param name="attacker">The attacker.</param>
    /// <returns>The character name, or a fallback for non-player attackers.</returns>
    public static string GetName(this IAttacker? attacker)
    {
        return attacker is Player player ? player.Name : attacker?.Id.ToString() ?? "Um aventureiro";
    }
}
