// <copyright file="MapEntrySource.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Identifies how a player attempts to enter a map.
/// </summary>
public enum MapEntrySource
{
    /// <summary>Entry requested internally by the server.</summary>
    Internal,

    /// <summary>Entry requested through a warp command.</summary>
    Warp,

    /// <summary>Entry requested through a map gate.</summary>
    Gate,

    /// <summary>Entry requested through a skill.</summary>
    Skill,

    /// <summary>Entry requested when a character is selected.</summary>
    CharacterSelection,

    /// <summary>Entry requested by a game master command.</summary>
    GameMasterCommand,
}
