namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Restricts selected game servers to accounts with a configured VIP level.
/// </summary>
[PlugIn]
[Display(Name = "VIP Server Access", Description = "Restricts selected game server IDs to accounts with a minimum VIP level.")]
[Guid("AF1B0A1E-5250-46CF-A75C-D9C69ED114ED")]
public class VipServerAccessPlugIn : IAccountLoginValidationPlugIn, ISupportCustomConfiguration<VipServerAccessPlugInConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public VipServerAccessPlugInConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public ValueTask ValidateAccountLoginAsync(Player player, AccountLoginValidationEventArgs eventArgs)
    {
        var configuration = this.Configuration ?? CreateDefaultConfiguration();
        if (configuration.MinimumVipLevel <= 0
            || player.GameContext is not IGameServerContext gameServerContext
            || !configuration.RestrictedServerIds.Contains(gameServerContext.Id)
            || (configuration.BypassGameMasters && IsGameMaster(eventArgs.Account.State)))
        {
            return ValueTask.CompletedTask;
        }

        var vipLevel = eventArgs.Account.Attributes?
            .FirstOrDefault(attribute => attribute.Definition?.Id == Stats.IsVip.Id)?.Value ?? 0;
        if (vipLevel >= configuration.MinimumVipLevel)
        {
            return ValueTask.CompletedTask;
        }

        player.Logger.LogWarning(
            "Login of account {Account} to server {ServerId} was rejected. VIP level {VipLevel} is below the required level {MinimumVipLevel}.",
            eventArgs.Account.LoginName,
            gameServerContext.Id,
            vipLevel,
            configuration.MinimumVipLevel);
        eventArgs.RejectionResult = LoginResult.NoChargeInfo;
        eventArgs.Cancel = true;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig()
    {
        return CreateDefaultConfiguration();
    }

    private static VipServerAccessPlugInConfiguration CreateDefaultConfiguration()
    {
        return new VipServerAccessPlugInConfiguration();
    }

    private static bool IsGameMaster(AccountState state)
    {
        return state is AccountState.GameMaster or AccountState.GameMasterInvisible;
    }
}
