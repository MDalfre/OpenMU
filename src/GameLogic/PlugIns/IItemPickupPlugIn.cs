// <copyright file="IItemPickupPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called before an item is added to a player's inventory.
/// </summary>
[Guid("F3F77D5B-B4A6-43EA-8C55-73D9F1A8513A")]
[PlugInPoint("Item pickup", "Plugins which can handle an item pickup before the default inventory operation.")]
public interface IItemPickupPlugIn
{
    /// <summary>
    /// Handles an item pickup request.
    /// </summary>
    /// <param name="player">The player which tries to pick up the item.</param>
    /// <param name="droppedItem">The dropped item.</param>
    /// <param name="pickupArguments">The arguments which control the default pickup operation.</param>
    /// <returns>A task which represents the asynchronous operation.</returns>
    ValueTask HandleItemPickupAsync(Player player, DroppedItem droppedItem, ItemPickupArguments pickupArguments);

    /// <summary>
    /// Arguments for handling an item pickup request.
    /// </summary>
    public sealed class ItemPickupArguments : CancelEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was handled by a plugin.
        /// </summary>
        public bool WasHandled
        {
            get => this.Cancel;
            set => this.Cancel = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the handled request succeeded.
        /// </summary>
        public bool Success { get; set; }
    }
}
