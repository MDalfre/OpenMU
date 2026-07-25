namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Restricts selected game servers to accounts with a configured VIP level.
/// </summary>
[PlugIn]
[Display(Name = "VIP Server Access", Description = "Restricts selected game server IDs to accounts with a minimum VIP level.")]
[Guid("AF1B0A1E-5250-46CF-A75C-D9C69ED114ED")]
public class VipServerAccessPlugIn : IAccountLoginValidationPlugIn, IPlayerStateChangedPlugIn, ISupportCustomConfiguration<VipServerAccessPlugInConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromDays(1);
    private readonly ConcurrentDictionary<Player, CancellationTokenSource> _expirationTasks = new();

    /// <inheritdoc />
    public VipServerAccessPlugInConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public ValueTask ValidateAccountLoginAsync(Player player, AccountLoginValidationEventArgs eventArgs)
    {
        var configuration = this.Configuration ?? CreateDefaultConfiguration();
        if (configuration.MinimumVipLevel <= 0
            || player.GameContext is not IGameServerContext gameServerContext
            || !configuration.RestrictedServers.Contains(gameServerContext.Id)
            || (configuration.BypassGameMasters && IsGameMaster(eventArgs.Account.State)))
        {
            return ValueTask.CompletedTask;
        }

        var vipStatus = VipEntitlementService.GetStatus(eventArgs.Account, DateTime.UtcNow);
        if (vipStatus.Level >= configuration.MinimumVipLevel)
        {
            return ValueTask.CompletedTask;
        }

        player.Logger.LogWarning(
            "Login of account {Account} to server {ServerId} was rejected. VIP level {VipLevel} is below the required level {MinimumVipLevel}.",
            eventArgs.Account.LoginName,
            gameServerContext.Id,
            vipStatus.Level,
            configuration.MinimumVipLevel);
        eventArgs.RejectionResult = LoginResult.ChargedChannel;
        eventArgs.Cancel = true;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState.IsDisconnectedOrFinished())
        {
            this.CancelExpirationTask(player);
            return;
        }

        if (previousState != PlayerState.CharacterSelection
            || currentState != PlayerState.EnteredWorld
            || !this.IsRestrictedServer(player, out var configuration)
            || player.Account is not { } account
            || (configuration.BypassGameMasters && IsGameMaster(account.State)))
        {
            return;
        }

        var status = VipEntitlementService.GetStatus(account, DateTime.UtcNow);
        if (!status.IsActive)
        {
            return;
        }

        if (configuration.ShowExpirationOnEnterWorld)
        {
            await this.ShowStatusAsync(player, status, configuration).ConfigureAwait(false);
        }

        if (status.ExpiresAtUtc is not null)
        {
            this.StartExpirationTask(player, status, configuration);
        }
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

    private bool IsRestrictedServer(Player player, out VipServerAccessPlugInConfiguration configuration)
    {
        configuration = this.Configuration ?? CreateDefaultConfiguration();
        return player.GameContext is IGameServerContext gameServerContext
               && configuration.RestrictedServers.Contains(gameServerContext.Id);
    }

    private void StartExpirationTask(Player player, VipStatus status, VipServerAccessPlugInConfiguration configuration)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        this._expirationTasks.AddOrUpdate(
            player,
            cancellationTokenSource,
            (_, previous) =>
            {
                previous.Cancel();
                return cancellationTokenSource;
            });

        _ = this.MonitorExpirationAsync(player, status, configuration, cancellationTokenSource);
    }

    private void CancelExpirationTask(Player player)
    {
        if (this._expirationTasks.TryRemove(player, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }
    }

    private async Task MonitorExpirationAsync(Player player, VipStatus initialStatus, VipServerAccessPlugInConfiguration configuration, CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            var status = initialStatus;
            while (status.ExpiresAtUtc is { } expiration)
            {
                foreach (var warningMinutes in configuration.ExpirationWarningMinutes.Where(minutes => minutes > 0).Distinct().OrderByDescending(minutes => minutes))
                {
                    var warningTime = expiration.AddMinutes(-warningMinutes);
                    if (warningTime <= DateTime.UtcNow)
                    {
                        continue;
                    }

                    await DelayUntilAsync(warningTime, cancellationTokenSource.Token).ConfigureAwait(false);
                    await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
                        view => view.ShowMessageAsync(string.Format(player.Culture, configuration.ExpirationWarningMessage, warningMinutes), Interfaces.MessageType.GoldenCenter)).ConfigureAwait(false);
                }

                if (expiration > DateTime.UtcNow)
                {
                    await DelayUntilAsync(expiration, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                status = await ReloadStatusAsync(player, cancellationTokenSource.Token).ConfigureAwait(false);
                if (status.Level >= configuration.MinimumVipLevel)
                {
                    if (status.ExpiresAtUtc is null || status.ExpiresAtUtc > expiration)
                    {
                        continue;
                    }
                }

                await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
                    view => view.ShowMessageAsync(configuration.ExpiredMessage, Interfaces.MessageType.GoldenCenter)).ConfigureAwait(false);
                await new LogoutAction().LogoutAsync(player, LogoutType.BackToServerSelection).ConfigureAwait(false);
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Unexpected error while monitoring VIP expiration for account {Account}.", player.Account?.LoginName);
        }
        finally
        {
            if (this._expirationTasks.TryGetValue(player, out var currentTask)
                && ReferenceEquals(currentTask, cancellationTokenSource))
            {
                this._expirationTasks.TryRemove(player, out _);
            }

            cancellationTokenSource.Dispose();
        }
    }

    private static async Task DelayUntilAsync(DateTime targetUtc, CancellationToken cancellationToken)
    {
        while (targetUtc > DateTime.UtcNow)
        {
            var remaining = targetUtc - DateTime.UtcNow;
            await Task.Delay(remaining > MaximumDelay ? MaximumDelay : remaining, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<VipStatus> ReloadStatusAsync(Player player, CancellationToken cancellationToken)
    {
        if (player.Account is not { } account)
        {
            return VipStatus.Inactive;
        }

        using var context = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration);
        var refreshedAccount = await context.GetAccountByLoginNameAsync(account.LoginName, cancellationToken).ConfigureAwait(false);
        return refreshedAccount is null
            ? VipStatus.Inactive
            : VipEntitlementService.GetStatus(refreshedAccount, DateTime.UtcNow);
    }

    private async ValueTask ShowStatusAsync(Player player, VipStatus status, VipServerAccessPlugInConfiguration configuration)
    {
        var message = status.ExpiresAtUtc is { } expiration
            ? string.Format(player.Culture, configuration.ActiveVipMessage, status.Level, expiration)
            : string.Format(player.Culture, configuration.PermanentVipMessage, status.Level);
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
            view => view.ShowMessageAsync(message, Interfaces.MessageType.BlueNormal)).ConfigureAwait(false);
    }
}
