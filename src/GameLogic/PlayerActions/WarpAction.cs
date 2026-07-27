// <copyright file="WarpAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions;

using System.Diagnostics.CodeAnalysis;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Action to warp to another place.
/// </summary>
public class WarpAction
{
    /// <summary>
    /// Warps the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="warpInfo">The warp information.</param>
    public async ValueTask WarpToAsync(Player player, WarpInfo warpInfo)
    {
        if (await this.CheckRequirementsAsync(player, warpInfo).ConfigureAwait(false) is { } errorMessage)
        {
            await player.ShowBlueMessageAsync(errorMessage).ConfigureAwait(false);
        }
        else
        {
            await player.WarpToAsync(warpInfo.Gate!, true).ConfigureAwait(false);
        }
    }

    private async ValueTask<string?> CheckRequirementsAsync(Player player, WarpInfo warpInfo)
    {
        if (player.SelectedCharacter?.State >= HeroState.PlayerKiller1stStage)
        {
            var policyArguments = new IPlayerKillerWarpPolicyPlugIn.PlayerKillerWarpArguments();
            player.GameContext.PlugInManager.GetPlugInPoint<IPlayerKillerWarpPolicyPlugIn>()?.EvaluatePlayerKillerWarp(player, policyArguments);
            if (!policyArguments.Allowed)
            {
                return "Personagens PK não podem utilizar teleportes normais.";
            }
        }

        var requirement = player.SelectedCharacter?.GetEffectiveMoveLevelRequirement(warpInfo.LevelRequirement);
        if (requirement > player.Attributes?[Stats.Level])
        {
            return $"You need to be level {requirement} in order to warp";
        }

        if (warpInfo.Gate?.Map is null)
        {
            return "The warp target is not initialized";
        }

        if (warpInfo.Gate.Map.TryGetRequirementError(player, out var message))
        {
            return message;
        }

        var entryResult = await MapEntryValidator.ValidateAsync(player, warpInfo.Gate.Map, MapEntrySource.Warp).ConfigureAwait(false);
        if (entryResult.Denied)
        {
            return entryResult.Message ?? "You cannot enter this map at the moment.";
        }

        // Money check should be last to avoid getting zen when other checks failed
        if (!this.CheckMoneyRequirement(player, warpInfo))
        {
            return $"You need {warpInfo.Costs} in order to warp";
        }

        return null;
    }

    private bool CheckMoneyRequirement(Player player, WarpInfo warpInfo)
    {
        if (player.TryRemoveMoney(warpInfo.Costs))
        {
            return true;
        }

        return false;
    }
}
