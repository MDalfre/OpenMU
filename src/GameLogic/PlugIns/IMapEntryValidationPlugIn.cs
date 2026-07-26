// <copyright file="IMapEntryValidationPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which validates attempts to enter a map.
/// </summary>
[Guid("BDEA7384-B6F3-4E5C-9588-E7A68379F28D")]
[PlugInPoint("Map entry validation", "Plugins which validate a player before entering a map.")]
public interface IMapEntryValidationPlugIn
{
    /// <summary>
    /// Validates a map entry attempt.
    /// </summary>
    /// <param name="player">The player which enters the map.</param>
    /// <param name="targetMap">The target map definition.</param>
    /// <param name="source">The source of the map entry.</param>
    /// <param name="eventArgs">The mutable validation result.</param>
    ValueTask ValidateMapEntryAsync(Player player, GameMapDefinition targetMap, MapEntrySource source, MapEntryValidationEventArgs eventArgs);
}
