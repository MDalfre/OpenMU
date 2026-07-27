// <copyright file="IJewelSuccessRateModifierPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Allows plugins to modify the success chance of fallible jewel applications.</summary>
[Guid("D490BFA9-3274-44F0-8E31-FD3D2EA95352")]
[PlugInPoint("Jewel success rate modifier", "Modifies the success chance of a jewel application.")]
public interface IJewelSuccessRateModifierPlugIn
{
    /// <summary>Modifies the jewel success chance.</summary>
    /// <param name="player">The player who applies the jewel.</param>
    /// <param name="jewel">The consumed jewel.</param>
    /// <param name="targetItem">The item targeted by the jewel.</param>
    /// <param name="arguments">The mutable success chance arguments.</param>
    void ModifyJewelSuccessRate(Player player, Item jewel, Item targetItem, JewelSuccessRateArguments arguments);

    /// <summary>Contains a mutable success chance between zero and one.</summary>
    public sealed class JewelSuccessRateArguments
    {
        /// <summary>Gets or sets the effective success chance.</summary>
        public double EffectiveChance { get; set; }
    }
}
