// <copyright file="IPvpPenaltyPolicyPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Allows plugins to suppress the normal consequences of player-versus-player combat.</summary>
[Guid("89294DD1-8C8A-4DF4-969E-C8AB30DE0624")]
[PlugInPoint("PvP penalty policy", "Determines whether PvP combat is exempt from PK and self-defense consequences.")]
public interface IPvpPenaltyPolicyPlugIn
{
    /// <summary>Evaluates the PvP penalty policy for the specified players.</summary>
    /// <param name="attacker">The attacking or killing player.</param>
    /// <param name="defender">The attacked or killed player.</param>
    /// <param name="arguments">The mutable policy decision.</param>
    void EvaluatePvpPenalty(Player attacker, Player defender, PvpPenaltyArguments arguments);

    /// <summary>Contains the mutable PvP penalty decision.</summary>
    public sealed class PvpPenaltyArguments
    {
        /// <summary>
        /// Gets or sets a value indicating whether PK escalation and self-defense consequences are suppressed.
        /// </summary>
        public bool IsPenaltySuppressed { get; set; }
    }
}
