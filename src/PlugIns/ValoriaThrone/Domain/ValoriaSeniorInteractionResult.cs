// <copyright file="ValoriaSeniorInteractionResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Describes the result of a player interaction with the event Senior.
/// </summary>
public enum ValoriaSeniorInteractionResult
{
    /// <summary>The NPC is not the Senior of the active event map.</summary>
    NotSenior,

    /// <summary>The interaction was rejected by the server.</summary>
    Rejected,

    /// <summary>The player must confirm the coronation by talking again.</summary>
    ConfirmationRequested,

    /// <summary>The coronation has started.</summary>
    CoronationStarted,
}
