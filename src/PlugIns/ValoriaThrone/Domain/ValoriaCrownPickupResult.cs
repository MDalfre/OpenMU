// <copyright file="ValoriaCrownPickupResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Describes how a crown pickup request was handled.
/// </summary>
public enum ValoriaCrownPickupResult
{
    /// <summary>The item does not belong to the current crown execution.</summary>
    NotCrown,

    /// <summary>The item was a crown but the player was not eligible to claim it.</summary>
    Rejected,

    /// <summary>The crown was claimed by the player.</summary>
    PickedUp,
}
