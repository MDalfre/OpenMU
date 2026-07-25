// <copyright file="VipEntitlementServiceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.InMemory;
using NUnit.Framework;

/// <summary>
/// Tests for the <see cref="VipEntitlementService"/>.
/// </summary>
[TestFixture]
public class VipEntitlementServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ActiveEntitlementIsReturned()
    {
        var (context, account) = CreateAccount();
        var entitlement = context.CreateNew<AccountVipEntitlement>();
        entitlement.VipLevel = 2;
        entitlement.StartsAtUtc = UtcNow.AddDays(-1);
        entitlement.ExpiresAtUtc = UtcNow.AddDays(5);
        entitlement.Source = VipEntitlementSource.Purchase;
        account.VipEntitlements.Add(entitlement);

        var status = VipEntitlementService.GetStatus(account, UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(status.IsActive, Is.True);
            Assert.That(status.Level, Is.EqualTo(2));
            Assert.That(status.ExpiresAtUtc, Is.EqualTo(entitlement.ExpiresAtUtc));
        });
    }

    [Test]
    public void HigherLegacyVipLevelWinsOverTimedEntitlement()
    {
        var (context, account) = CreateAccount();
        account.Attributes.Add(context.CreateNew<StatAttribute>(Stats.IsVip, 2));
        AddEntitlement(context, account, UtcNow.AddDays(-1), UtcNow.AddDays(1));

        var status = VipEntitlementService.GetStatus(account, UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(status.Level, Is.EqualTo(2));
            Assert.That(status.ExpiresAtUtc, Is.Null);
            Assert.That(status.IsLegacy, Is.True);
        });
    }

    [Test]
    public void ExpiredFutureAndRevokedEntitlementsAreInactive()
    {
        var (context, account) = CreateAccount();
        AddEntitlement(context, account, UtcNow.AddDays(-2), UtcNow.AddDays(-1));
        AddEntitlement(context, account, UtcNow.AddDays(1), UtcNow.AddDays(2));
        var revoked = AddEntitlement(context, account, UtcNow.AddDays(-1), UtcNow.AddDays(1));
        revoked.RevokedAtUtc = UtcNow;

        var status = VipEntitlementService.GetStatus(account, UtcNow);

        Assert.That(status.IsActive, Is.False);
    }

    [Test]
    public void RenewalPreservesRemainingVipTime()
    {
        var (context, account) = CreateAccount();
        var first = VipEntitlementService.Grant(context, account, 1, TimeSpan.FromDays(30), VipEntitlementSource.Purchase, UtcNow, "payment-1");

        var renewal = VipEntitlementService.Grant(context, account, 1, TimeSpan.FromDays(30), VipEntitlementSource.Purchase, UtcNow.AddDays(10), "payment-2");

        Assert.That(first.ExpiresAtUtc, Is.EqualTo(UtcNow.AddDays(30)));
        Assert.That(renewal.ExpiresAtUtc, Is.EqualTo(UtcNow.AddDays(60)));
    }

    [Test]
    public void RepeatedPaymentReferenceIsIdempotent()
    {
        var (context, account) = CreateAccount();
        var first = VipEntitlementService.Grant(context, account, 1, TimeSpan.FromDays(30), VipEntitlementSource.Purchase, UtcNow, "payment-1");

        var retry = VipEntitlementService.Grant(context, account, 1, TimeSpan.FromDays(30), VipEntitlementSource.Purchase, UtcNow, "payment-1");

        Assert.Multiple(() =>
        {
            Assert.That(retry, Is.SameAs(first));
            Assert.That(account.VipEntitlements, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AccountCreationTrialCanOnlyBeGrantedOnce()
    {
        var (context, account) = CreateAccount();
        var trial = VipEntitlementService.GrantAccountCreationTrial(context, account, 1, 15, UtcNow);

        Assert.That(trial?.ExpiresAtUtc, Is.EqualTo(UtcNow.AddDays(15)));
        Assert.Throws<InvalidOperationException>(() => VipEntitlementService.GrantAccountCreationTrial(context, account, 1, 15, UtcNow));
    }

    [Test]
    public void ZeroTrialDaysDisablesAccountCreationTrial()
    {
        var (_, account) = CreateAccount();
        var context = new InMemoryPersistenceContextProvider().CreateNewContext();

        var trial = VipEntitlementService.GrantAccountCreationTrial(context, account, 1, 0, UtcNow);

        Assert.That(trial, Is.Null);
    }

    private static (MUnique.OpenMU.Persistence.IContext Context, Account Account) CreateAccount()
    {
        var context = new InMemoryPersistenceContextProvider().CreateNewContext();
        var account = context.CreateNew<Account>();
        account.LoginName = "vip-test";
        return (context, account);
    }

    private static AccountVipEntitlement AddEntitlement(
        MUnique.OpenMU.Persistence.IContext context,
        Account account,
        DateTime startsAtUtc,
        DateTime expiresAtUtc)
    {
        var entitlement = context.CreateNew<AccountVipEntitlement>();
        entitlement.VipLevel = 1;
        entitlement.StartsAtUtc = startsAtUtc;
        entitlement.ExpiresAtUtc = expiresAtUtc;
        account.VipEntitlements.Add(entitlement);
        return entitlement;
    }
}
