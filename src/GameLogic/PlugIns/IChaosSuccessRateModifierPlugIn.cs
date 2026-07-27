// <copyright file="IChaosSuccessRateModifierPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>Allows plugins to modify an item crafting success rate.</summary>
[Guid("FA8BB9A3-1B97-46A3-87CE-78958B4CD7A8")]
[PlugInPoint("Chaos success rate modifier", "Modifies an item crafting success rate calculated by the server.")]
public interface IChaosSuccessRateModifierPlugIn
{
    /// <summary>Modifies the success rate calculation.</summary>
    /// <param name="player">The player who owns the crafting storage.</param>
    /// <param name="handler">The authoritative crafting handler.</param>
    /// <param name="arguments">The mutable success rate arguments.</param>
    void ModifyChaosSuccessRate(Player player, IItemCraftingHandler handler, ChaosSuccessRateArguments arguments);

    /// <summary>Contains mutable success rate values in percent.</summary>
    public sealed class ChaosSuccessRateArguments
    {
        /// <summary>Gets or sets the effective success percentage.</summary>
        public double EffectiveRate { get; set; }

        /// <summary>Gets the applied modifier diagnostics.</summary>
        public ICollection<SuccessRateModifier> Modifiers { get; } = new List<SuccessRateModifier>();
    }
}
