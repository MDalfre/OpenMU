// <copyright file="PickupItemAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Action to pick up an item from the floor.
/// </summary>
public class PickupItemAction
{
    /// <summary>
    /// Pickups the item.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="dropId">The drop identifier.</param>
    public async ValueTask PickupItemAsync(Player player, ushort dropId)
    {
        var droppedLocateable = player.CurrentMap?.GetDrop(dropId);

        switch (droppedLocateable)
        {
            case DroppedMoney droppedMoney:
                if (!await TryPickupMoneyAsync(player, droppedMoney).ConfigureAwait(false))
                {
                    await player.InvokeViewPlugInAsync<IItemPickUpFailedPlugIn>(p => p.ItemPickUpFailedAsync(ItemPickFailReason.General)).ConfigureAwait(false);
                }

                break;
            case DroppedItem droppedItem:
                {
                    var (success, stackTarget, wasHandled) = await TryPickupItemAsync(player, droppedItem).ConfigureAwait(false);
                    if (wasHandled)
                    {
                        break;
                    }

                    if (success)
                    {
                        if (stackTarget != null)
                        {
                            await player.InvokeViewPlugInAsync<IItemPickUpFailedPlugIn>(p => p.ItemPickUpFailedAsync(ItemPickFailReason.ItemStacked)).ConfigureAwait(false);
                            await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(stackTarget, false)).ConfigureAwait(false);
                        }
                        else
                        {
                            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(droppedItem.Item)).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await player.InvokeViewPlugInAsync<IItemPickUpFailedPlugIn>(p => p.ItemPickUpFailedAsync(ItemPickFailReason.General)).ConfigureAwait(false);
                    }

                    break;
                }

            default:
                await player.InvokeViewPlugInAsync<IItemPickUpFailedPlugIn>(p => p.ItemPickUpFailedAsync(ItemPickFailReason.General)).ConfigureAwait(false);
                break;
        }
    }

    private static async ValueTask<(bool Success, Item? StackTarget)> RejectAsync(Player player, string messageKey)
    {
        await player.ShowLocalizedBlueMessageAsync(messageKey).ConfigureAwait(false);
        return (false, null);
    }

    private static bool CanPickup(Player player, ILocateable droppedLocateable)
    {
        if (!player.IsAlive)
        {
            return false;
        }

        var dist = (int)player.GetDistanceTo(droppedLocateable);
        if (dist > 3)
        {
            return false;
        }

        return true;
    }

    private static bool IsLimitReached(Player player, ItemDefinition? itemDefinition)
    {
        if (itemDefinition is null)
        {
            return true;
        }

        if (itemDefinition.StorageLimitPerCharacter == 0)
        {
            return false;
        }

        return player.Inventory?.Items.Count(item => item.Definition == itemDefinition) >= itemDefinition.StorageLimitPerCharacter;
    }

    private static bool PlayerHasActiveQuestForItem(Player player, Item item)
    {
        var questStates = player.SelectedCharacter?.QuestStates;
        if (questStates is null)
        {
            return false;
        }

        return questStates
            .Select(q => q.ActiveQuest)
            .OfType<QuestDefinition>()
            .SelectMany(q => q.RequiredItems)
            .Any(r => r.Item == item.Definition && (r.DropItemGroup?.ItemLevel is null || r.DropItemGroup.ItemLevel == item.Level));
    }

    private static async ValueTask<(bool Success, Item? StackTarget, bool WasHandled)> TryPickupItemAsync(Player player, DroppedItem droppedItem)
    {
        if (!CanPickup(player, droppedItem))
        {
            return (false, null, false);
        }

        if (player.GameContext.PlugInManager.GetPlugInPoint<IItemPickupPlugIn>() is { } plugInPoint)
        {
            var pickupArguments = new IItemPickupPlugIn.ItemPickupArguments();
            await plugInPoint.HandleItemPickupAsync(player, droppedItem, pickupArguments).ConfigureAwait(false);
            if (pickupArguments.WasHandled)
            {
                return (pickupArguments.Success, null, true);
            }
        }

        if (IsLimitReached(player, droppedItem.Item.Definition))
        {
            var itemName = string.Empty;
            using (CultureHelper.SetTemporaryCulture(player.Culture))
            {
                itemName = droppedItem.Item.Level > 0
                    ? $"{droppedItem.Item.Definition?.Name.ToString()} +{droppedItem.Item.Level.ToString()}"
                    : droppedItem.Item.Definition?.Name.ToString();
            }

            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PickupLimitReached), itemName).ConfigureAwait(false);
            return (false, null, false);
        }

        var slot = player.Inventory?.CheckInvSpace(droppedItem.Item);
        if (slot < InventoryConstants.EquippableSlotsCount)
        {
            return (false, null, false);
        }

        if (droppedItem.Item.Definition?.IsQuestItem == true
            && !PlayerHasActiveQuestForItem(player, droppedItem.Item))
        {
            var rejectionResult = await RejectAsync(player, nameof(PlayerMessage.ItemDoesNotBelongToYou)).ConfigureAwait(false);
            return (rejectionResult.Success, rejectionResult.StackTarget, false);
        }

        if (droppedItem.Item.Definition?.IsBoundToCharacter == true
            && !droppedItem.IsPlayerAnOwner(player))
        {
            var rejectionResult = await RejectAsync(player, nameof(PlayerMessage.ItemDoesNotBelongToYou)).ConfigureAwait(false);
            return (rejectionResult.Success, rejectionResult.StackTarget, false);
        }

        if (!droppedItem.IsPlayerAnOwner(player) && droppedItem.IsOwnerPickupPriorityActive)
        {
            var rejectionResult = await RejectAsync(player, nameof(PlayerMessage.ItemDoesNotBelongToYou)).ConfigureAwait(false);
            return (rejectionResult.Success, rejectionResult.StackTarget, false);
        }

        var result = await droppedItem.TryPickUpByAsync(player).ConfigureAwait(false);
        if (result.Success)
        {
            await player.OnPickedUpItemAsync(droppedItem).ConfigureAwait(false);
        }

        return (result.Success, result.StackTarget, false);
    }

    private static async ValueTask<bool> TryPickupMoneyAsync(Player player, DroppedMoney droppedMoney)
    {
        return CanPickup(player, droppedMoney) && await droppedMoney.TryPickUpByAsync(player).ConfigureAwait(false);
    }
}
