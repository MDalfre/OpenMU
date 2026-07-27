// <copyright file="ICommonDropRateModifierPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Allows plugins to modify chance-based common monster drops.</summary>
[Guid("2770E992-03B2-43AE-861A-C6F422A2D061")]
[PlugInPoint("Common drop rate modifier", "Modifies chance-based common monster drops.")]
public interface ICommonDropRateModifierPlugIn
{
    /// <summary>Modifies the common drop calculation arguments.</summary>
    /// <param name="player">The player for whom the drop is generated.</param>
    /// <param name="monster">The defeated monster.</param>
    /// <param name="arguments">The mutable common drop rate arguments.</param>
    void ModifyCommonDropRate(Player player, MonsterDefinition monster, CommonDropRateArguments arguments);

    /// <summary>Contains mutable common drop calculation values.</summary>
    public sealed class CommonDropRateArguments
    {
        /// <summary>Gets or sets the relative multiplier.</summary>
        public double Multiplier { get; set; } = 1.0;
    }
}
