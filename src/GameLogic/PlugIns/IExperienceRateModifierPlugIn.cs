// <copyright file="IExperienceRateModifierPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Allows plugins to modify the final experience rate.</summary>
[Guid("B748CFD1-52BE-4C71-B164-4363922BF40F")]
[PlugInPoint("Experience rate modifier", "Modifies the final experience rate for a player.")]
public interface IExperienceRateModifierPlugIn
{
    /// <summary>Modifies the experience calculation arguments.</summary>
    /// <param name="player">The player who receives the experience.</param>
    /// <param name="arguments">The mutable experience rate arguments.</param>
    void ModifyExperienceRate(Player player, ExperienceRateArguments arguments);

    /// <summary>Contains mutable experience calculation values.</summary>
    public sealed class ExperienceRateArguments
    {
        /// <summary>Gets or sets the relative multiplier.</summary>
        public float Multiplier { get; set; } = 1.0f;
    }
}
