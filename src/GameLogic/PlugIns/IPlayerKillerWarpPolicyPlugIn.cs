// <copyright file="IPlayerKillerWarpPolicyPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Allows plugins to authorize normal warp-list travel for player killers.</summary>
[Guid("D6C42076-07CE-4FC3-BE67-0DD48282F899")]
[PlugInPoint("Player killer warp policy", "Authorizes normal warp-list travel for player killers.")]
public interface IPlayerKillerWarpPolicyPlugIn
{
    /// <summary>Evaluates a player-killer warp attempt.</summary>
    /// <param name="player">The player who attempts to warp.</param>
    /// <param name="arguments">The mutable authorization arguments.</param>
    void EvaluatePlayerKillerWarp(Player player, PlayerKillerWarpArguments arguments);

    /// <summary>Contains the mutable authorization decision.</summary>
    public sealed class PlayerKillerWarpArguments
    {
        /// <summary>Gets or sets a value indicating whether the warp is allowed.</summary>
        public bool Allowed { get; set; }
    }
}
