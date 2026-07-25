// <copyright file="VipStatusChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shows the current account VIP status with the <c>/vip</c> command.
/// </summary>
[Guid("A7B8C9D0-E1F2-4A3B-8C9D-0E1F2A3B4C5D")]
[PlugIn]
[Display(Name = "VIP Status Command", Description = "Shows the current VIP level and expiration date with /vip.")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public sealed class VipStatusChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/vip";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.Account is not { } account)
        {
            return;
        }

        using var context = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration);
        var refreshedAccount = await context.GetAccountByLoginNameAsync(account.LoginName).ConfigureAwait(false);
        var status = refreshedAccount is null
            ? VipStatus.Inactive
            : VipEntitlementService.GetStatus(refreshedAccount, DateTime.UtcNow);
        var message = status switch
        {
            { IsActive: false } => "VIP is not active on this account.",
            { ExpiresAtUtc: null } => $"VIP level {status.Level} is permanently active.",
            _ => $"VIP level {status.Level} is active until {status.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC.",
        };
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
            view => view.ShowMessageAsync(message, Interfaces.MessageType.BlueNormal)).ConfigureAwait(false);
    }
}
