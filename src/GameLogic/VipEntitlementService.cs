// <copyright file="VipEntitlementService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Provides the shared VIP entitlement rules for game servers, administrative tools and future account frontends.
/// </summary>
public static class VipEntitlementService
{
    /// <summary>
    /// Gets the effective VIP status of an account at the supplied UTC timestamp.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <returns>The effective VIP status.</returns>
    public static VipStatus GetStatus(Account account, DateTime utcNow)
    {
        var legacyVipLevel = (int)(account.Attributes?
            .FirstOrDefault(attribute => attribute.Definition?.Id == Stats.IsVip.Id)?.Value ?? 0);
        var activeEntitlements = account.VipEntitlements?
            .Where(entitlement => entitlement.VipLevel > 0
                                  && entitlement.StartsAtUtc <= utcNow
                                  && (entitlement.ExpiresAtUtc is null || entitlement.ExpiresAtUtc > utcNow)
                                  && entitlement.RevokedAtUtc is null)
            .ToList() ?? [];

        if (activeEntitlements.Count == 0)
        {
            return legacyVipLevel > 0
                ? new VipStatus(legacyVipLevel, null, VipEntitlementSource.Manual, true)
                : VipStatus.Inactive;
        }

        var effectiveLevel = activeEntitlements.Max(entitlement => entitlement.VipLevel);
        if (legacyVipLevel >= effectiveLevel)
        {
            return new VipStatus(legacyVipLevel, null, VipEntitlementSource.Manual, true);
        }

        var effectiveEntitlements = activeEntitlements
            .Where(entitlement => entitlement.VipLevel == effectiveLevel)
            .ToList();
        var permanentEntitlement = effectiveEntitlements.FirstOrDefault(entitlement => entitlement.ExpiresAtUtc is null);
        if (permanentEntitlement is not null)
        {
            return new VipStatus(effectiveLevel, null, permanentEntitlement.Source, false);
        }

        var latestEntitlement = effectiveEntitlements.MaxBy(entitlement => entitlement.ExpiresAtUtc);
        return latestEntitlement is null
            ? VipStatus.Inactive
            : new VipStatus(effectiveLevel, latestEntitlement.ExpiresAtUtc, latestEntitlement.Source, false);
    }

    /// <summary>
    /// Grants a VIP entitlement and preserves the remaining duration of an active entitlement.
    /// </summary>
    /// <param name="context">The persistence context which owns the account.</param>
    /// <param name="account">The account.</param>
    /// <param name="vipLevel">The VIP level.</param>
    /// <param name="duration">The granted duration.</param>
    /// <param name="source">The source of the grant.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <param name="sourceReference">An optional external source identifier.</param>
    /// <returns>The newly created entitlement.</returns>
    public static AccountVipEntitlement Grant(
        IContext context,
        Account account,
        int vipLevel,
        TimeSpan duration,
        VipEntitlementSource source,
        DateTime utcNow,
        string? sourceReference = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vipLevel, 1);
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "The VIP duration must be greater than zero.");
        }

        sourceReference = string.IsNullOrWhiteSpace(sourceReference) ? null : sourceReference.Trim();
        if (sourceReference is not null
            && account.VipEntitlements.FirstOrDefault(entitlement => entitlement.SourceReference == sourceReference) is { } existingEntitlement)
        {
            return existingEntitlement;
        }

        var latestExpiration = account.VipEntitlements
            .Where(entitlement => entitlement.RevokedAtUtc is null && entitlement.ExpiresAtUtc > utcNow)
            .Select(entitlement => entitlement.ExpiresAtUtc!.Value)
            .DefaultIfEmpty(utcNow)
            .Max();
        var entitlement = context.CreateNew<AccountVipEntitlement>();
        entitlement.VipLevel = vipLevel;
        entitlement.StartsAtUtc = utcNow;
        entitlement.ExpiresAtUtc = latestExpiration.Add(duration);
        entitlement.Source = source;
        entitlement.SourceReference = sourceReference;
        entitlement.CreatedAtUtc = utcNow;
        account.VipEntitlements.Add(entitlement);
        return entitlement;
    }

    /// <summary>
    /// Grants an account creation trial if this account has never received one before.
    /// </summary>
    /// <returns>The created trial, or <see langword="null"/> when trial days are disabled.</returns>
    public static AccountVipEntitlement? GrantAccountCreationTrial(IContext context, Account account, int vipLevel, int trialDays, DateTime utcNow)
    {
        if (trialDays <= 0)
        {
            return null;
        }

        if (account.VipEntitlements?.Any(entitlement => entitlement.Source == VipEntitlementSource.Trial) ?? false)
        {
            throw new InvalidOperationException("This account has already received its VIP trial.");
        }

        return Grant(context, account, vipLevel, TimeSpan.FromDays(trialDays), VipEntitlementSource.Trial, utcNow);
    }
}

/// <summary>
/// Represents the effective VIP status at a point in time.
/// </summary>
/// <param name="Level">The effective VIP level.</param>
/// <param name="ExpiresAtUtc">The expiration timestamp, or <see langword="null"/> for permanent access.</param>
/// <param name="Source">The source of the effective entitlement.</param>
/// <param name="IsLegacy">Whether the status comes from the legacy Is VIP account attribute.</param>
public readonly record struct VipStatus(int Level, DateTime? ExpiresAtUtc, VipEntitlementSource Source, bool IsLegacy)
{
    /// <summary>
    /// Gets an inactive VIP status.
    /// </summary>
    public static VipStatus Inactive => new(0, null, VipEntitlementSource.Manual, false);

    /// <summary>
    /// Gets a value indicating whether this status grants VIP access.
    /// </summary>
    public bool IsActive => this.Level > 0;
}
