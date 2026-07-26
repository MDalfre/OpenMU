// <copyright file="MapEntryValidator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// Invokes map entry validation plugins.
/// </summary>
public static class MapEntryValidator
{
    /// <summary>
    /// Validates an entry into a target map.
    /// </summary>
    /// <param name="player">The player which enters the map.</param>
    /// <param name="targetMap">The target map definition.</param>
    /// <param name="source">The source of the map entry.</param>
    /// <returns>The validation result.</returns>
    public static async ValueTask<MapEntryValidationEventArgs> ValidateAsync(Player player, GameMapDefinition targetMap, MapEntrySource source)
    {
        var result = new MapEntryValidationEventArgs();
        if (player.GameContext.PlugInManager.GetPlugInPoint<IMapEntryValidationPlugIn>() is { } plugInPoint)
        {
            await plugInPoint.ValidateMapEntryAsync(player, targetMap, source, result).ConfigureAwait(false);
        }

        return result;
    }
}
