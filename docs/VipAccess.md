# VIP access

This fork stores time-limited VIP access as account entitlements and uses these
entitlements to protect configured game servers. The implementation uses only
existing client packets, so no matching client modification is required.

## Data model

Each `AccountVipEntitlement` belongs to one account and contains:

* `VipLevel`: granted access level;
* `StartsAtUtc` and `ExpiresAtUtc`: validity interval in UTC;
* `ExpiresAtUtc = null`: permanent access;
* `Source`: `Trial`, `Purchase`, `Manual`, or `Compensation`;
* `SourceReference`: optional unique external identifier, such as a payment ID;
* `RevokedAtUtc`: optional revocation timestamp;
* `CreatedAtUtc`: audit timestamp.

An entitlement is active when it has started, has not expired and has not been
revoked. If several entitlements are active, the highest VIP level wins. For
the same level, permanent access wins; otherwise the latest expiration wins.

The old `IsVip` account stat attribute remains a legacy fallback and is treated
as permanent VIP access. New integrations should create entitlements instead.

## Account creation trial

The administrative account creation dialog has `Initial VIP Days` and
`Initial VIP Level` fields. The default is 15 days at level 1. Set the number
of days to `0` to create an account without a trial. An account can receive the
account-creation trial only once.

The Admin Panel is not the public registration frontend. When the public
frontend is implemented, its backend must read the configured trial duration
on the server and call `VipEntitlementService.GrantAccountCreationTrial` in the
same persistence transaction that creates the account. Never accept the trial
duration supplied by an untrusted browser.

## Payment integration

After a payment provider confirms a payment, the website backend should load
the account in an `IContext`, grant the period and save the transaction:

```csharp
VipEntitlementService.Grant(
    context,
    account,
    vipLevel: 1,
    duration: TimeSpan.FromDays(30),
    source: VipEntitlementSource.Purchase,
    utcNow: DateTime.UtcNow,
    sourceReference: paymentTransactionId);
await context.SaveChangesAsync();
```

An active timed entitlement is extended from its current expiration, so the
remaining paid time is preserved. `SourceReference` is unique. Repeating the
same reference in a loaded account is idempotent; concurrent webhook handlers
must also treat a unique-index violation as an already processed payment and
reload the existing entitlement.

## Runtime behavior

`VipServerAccessPlugIn` performs these actions:

1. Rejects login to restricted servers when the effective VIP level is too low.
2. Shows the VIP level and UTC expiration when the character enters the world.
3. Schedules warnings for that player with `Task.Delay`; it does not poll every
   online account or keep a dedicated thread blocked.
4. Reloads the account from the database at expiration time, so a payment or
   renewal made while the player is online is recognized immediately.
5. Returns the player to server selection if access actually expired.

The `/vip` chat command reloads the account from the database and displays the
current VIP level and expiration. This lets a player confirm a newly processed
payment without reconnecting.

Revoking an entitlement does not immediately disconnect an online player,
because there is intentionally no periodic database polling. The revocation is
enforced on the next login or scheduled expiration check.

## Plugin configuration

The plugin is disabled by default and must be enabled in the Admin Panel. Its
custom configuration supports:

```json
{
  "MinimumVipLevel": 1,
  "RestrictedServers": [3, 4],
  "BypassGameMasters": true,
  "ShowExpirationOnEnterWorld": true,
  "ExpirationWarningMinutes": [10, 5, 1],
  "ActiveVipMessage": "VIP level {0} active until {1:yyyy-MM-dd HH:mm} UTC.",
  "PermanentVipMessage": "VIP level {0} is permanently active.",
  "ExpirationWarningMessage": "Your VIP access expires in {0} minute(s).",
  "ExpiredMessage": "Your VIP access has expired. Returning to server selection."
}
```

The `RestrictedServers` field is a numeric list. Server IDs `3` and `4` are
therefore entered as `[3, 4]`, not as a comma-separated string or packet bytes.

OpenMU uses one global plugin manager for all game servers. Global plugin
settings are therefore loaded only from the Default Game Configuration. A
cloned Gold Game Configuration may customize maps, monsters, drops and other
game data, but its copied plugin configuration rows are intentionally ignored.

## Database and deployment

Migration `AddAccountVipEntitlement` creates only the new
`data.AccountVipEntitlement` table, its account foreign key and indexes. Back
up PostgreSQL before deploying a new image. With `AutoUpdateSchema` enabled,
OpenMU applies the pending migration during startup.

The VPS must run an image built from this fork; restarting a compose service
which still references the public `munique/openmu` image will not include these
changes. Build and tag the custom image, push it to a private/public registry,
update the compose `image`, then recreate the `openmu` service.
