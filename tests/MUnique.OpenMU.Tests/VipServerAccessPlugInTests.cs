namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using NUnit.Framework;

/// <summary>
/// Tests for the <see cref="VipServerAccessPlugIn"/>.
/// </summary>
[TestFixture]
public class VipServerAccessPlugInTests
{
    [Test]
    public async ValueTask RestrictedServerRejectsAccountWithoutVipAsync()
    {
        var (player, account) = CreatePlayerAndAccount(3, 0, AccountState.Normal);
        var eventArgs = new AccountLoginValidationEventArgs(account);

        await CreatePlugIn().ValidateAccountLoginAsync(player, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.True);
        Assert.That(eventArgs.RejectionResult, Is.EqualTo(LoginResult.NoChargeInfo));
    }

    [Test]
    public async ValueTask RestrictedServerAllowsAccountWithRequiredVipAsync()
    {
        var (player, account) = CreatePlayerAndAccount(3, 1, AccountState.Normal);
        var eventArgs = new AccountLoginValidationEventArgs(account);

        await CreatePlugIn().ValidateAccountLoginAsync(player, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    [Test]
    public async ValueTask UnrestrictedServerAllowsAccountWithoutVipAsync()
    {
        var (player, account) = CreatePlayerAndAccount(1, 0, AccountState.Normal);
        var eventArgs = new AccountLoginValidationEventArgs(account);

        await CreatePlugIn().ValidateAccountLoginAsync(player, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    [Test]
    public async ValueTask RestrictedServerAllowsGameMasterAsync()
    {
        var (player, account) = CreatePlayerAndAccount(4, 0, AccountState.GameMaster);
        var eventArgs = new AccountLoginValidationEventArgs(account);

        await CreatePlugIn().ValidateAccountLoginAsync(player, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    private static VipServerAccessPlugIn CreatePlugIn()
    {
        return new VipServerAccessPlugIn
        {
            Configuration = new VipServerAccessPlugInConfiguration
            {
                MinimumVipLevel = 1,
                RestrictedServerIds = [3, 4],
                BypassGameMasters = true,
            },
        };
    }

    private static (Player Player, Account Account) CreatePlayerAndAccount(byte serverId, float vipLevel, AccountState accountState)
    {
        var persistenceProvider = new InMemoryPersistenceContextProvider();
        var persistenceContext = persistenceProvider.CreateNewContext();
        var gameConfiguration = persistenceContext.CreateNew<GameConfiguration>();
        var plugInManager = new PlugInManager([], new NullLoggerFactory(), null, null);
        var gameContext = new Mock<IGameServerContext>();
        gameContext.SetupGet(context => context.Id).Returns(serverId);
        gameContext.SetupGet(context => context.Configuration).Returns(gameConfiguration);
        gameContext.SetupGet(context => context.PersistenceContextProvider).Returns(persistenceProvider);
        gameContext.SetupGet(context => context.LoggerFactory).Returns(new NullLoggerFactory());
        gameContext.SetupGet(context => context.PlugInManager).Returns(plugInManager);

        var account = persistenceContext.CreateNew<Account>();
        account.LoginName = "test";
        account.State = accountState;
        if (vipLevel > 0)
        {
            account.Attributes.Add(persistenceContext.CreateNew<StatAttribute>(Stats.IsVip, vipLevel));
        }

        return (new Player(gameContext.Object), account);
    }
}
