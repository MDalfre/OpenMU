// <copyright file="ValoriaThroneMapOperations.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Applies evacuation, spawn, and cleanup effects in local game-server contexts.
/// </summary>
public sealed class ValoriaThroneMapOperations : IValoriaThroneMapOperations
{
    private readonly ValoriaThroneRuntimeRegistry _runtimeRegistry;
    private readonly ILogger<ValoriaThroneMapOperations> _logger;
    private ValoriaThroneOptions _options = ValoriaThroneOptions.Default;
    private Monster? _guardian;
    private Guid? _guardianEventInstanceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValoriaThroneMapOperations"/> class.
    /// </summary>
    public ValoriaThroneMapOperations(ValoriaThroneRuntimeRegistry runtimeRegistry, ILogger<ValoriaThroneMapOperations> logger)
    {
        this._runtimeRegistry = runtimeRegistry;
        this._logger = logger;
    }

    /// <inheritdoc />
    public void Configure(ValoriaThroneOptions options)
    {
        this._options = options;
    }

    /// <inheritdoc />
    public async ValueTask PrepareAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        await this.CleanupGuardianAsync(cancellationToken).ConfigureAwait(false);
        foreach (var context in this._runtimeRegistry.Contexts)
        {
            await this.EvacuateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask EvacuateAsync(CancellationToken cancellationToken)
    {
        foreach (var context in this._runtimeRegistry.Contexts)
        {
            await this.EvacuateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<ushort?> SpawnGuardianAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        if (this._guardian is not null || this._options.EventServerId is not { } eventServerId)
        {
            return this._guardian?.Id;
        }

        var context = this._runtimeRegistry.Contexts.FirstOrDefault(candidate => candidate.Id == eventServerId);
        if (context is null)
        {
            throw new InvalidOperationException($"The configured event server {eventServerId} is not hosted by this process.");
        }

        if (!context.PvpEnabled)
        {
            throw new InvalidOperationException($"The configured event server {eventServerId} does not have PvP enabled.");
        }

        var map = await context.GetMapAsync(this._options.EventMapId).ConfigureAwait(false)
                  ?? throw new InvalidOperationException($"Event map {this._options.EventMapId} was not found.");
        var monsterDefinition = context.Configuration.Monsters.FirstOrDefault(monster => monster.Number == this._options.GuardianMonsterId)
                                ?? throw new InvalidOperationException($"Guardian definition {this._options.GuardianMonsterId} was not found.");
        if (!map.Terrain.WalkMap[this._options.GuardianSpawnX, this._options.GuardianSpawnY])
        {
            throw new InvalidOperationException("The configured guardian spawn position is not walkable.");
        }

        var area = new MonsterSpawnArea
        {
            GameMap = map.Definition,
            MonsterDefinition = monsterDefinition,
            SpawnTrigger = SpawnTrigger.ManuallyForEvent,
            Quantity = 1,
            X1 = this._options.GuardianSpawnX,
            X2 = this._options.GuardianSpawnX,
            Y1 = this._options.GuardianSpawnY,
            Y2 = this._options.GuardianSpawnY,
            Direction = (Direction)this._options.GuardianDirection,
        };

        var guardian = new Monster(
            area,
            monsterDefinition,
            map,
            NullDropGenerator.Instance,
            new BasicMonsterIntelligence(),
            context.PlugInManager,
            context.PathFinderPool);
        guardian.Initialize();
        await map.AddAsync(guardian).ConfigureAwait(false);
        guardian.OnSpawn();

        this._guardian = guardian;
        this._guardianEventInstanceId = eventInstanceId;
        this._logger.LogInformation("Spawned Valoria guardian {GuardianId} for {EventInstanceId} on server {ServerId}.", guardian.Id, eventInstanceId, context.Id);
        return guardian.Id;
    }

    /// <inheritdoc />
    public async ValueTask CleanupAsync(Guid? eventInstanceId, CancellationToken cancellationToken)
    {
        await this.CleanupGuardianAsync(cancellationToken).ConfigureAwait(false);
        foreach (var context in this._runtimeRegistry.Contexts)
        {
            await this.EvacuateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public bool IsCurrentGuardian(IAttackable attackable, Guid eventInstanceId)
    {
        return ReferenceEquals(this._guardian, attackable) && this._guardianEventInstanceId == eventInstanceId;
    }

    private async ValueTask EvacuateAsync(IGameServerContext context, CancellationToken cancellationToken)
    {
        var players = (await context.GetPlayersAsync().ConfigureAwait(false))
            .Where(player => player.CurrentMap?.Definition.Number == this._options.EventMapId)
            .ToArray();
        if (players.Length == 0)
        {
            return;
        }

        var fallbackMap = context.Configuration.Maps.FirstOrDefault(map => map.Number == this._options.FallbackMapId)
                          ?? throw new InvalidOperationException($"Fallback map {this._options.FallbackMapId} was not found.");
        var fallbackGate = new ExitGate
        {
            Map = fallbackMap,
            X1 = this._options.FallbackPositionX,
            X2 = this._options.FallbackPositionX,
            Y1 = this._options.FallbackPositionY,
            Y2 = this._options.FallbackPositionY,
        };

        foreach (var player in players)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await player.WarpToAsync(fallbackGate).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Could not evacuate character {CharacterName} from server {ServerId}.", player.Name, context.Id);
            }
        }
    }

    private async ValueTask CleanupGuardianAsync(CancellationToken cancellationToken)
    {
        if (this._guardian is not { } guardian)
        {
            return;
        }

        this._guardian = null;
        this._guardianEventInstanceId = null;
        if (guardian.Id != 0)
        {
            await guardian.CurrentMap.RemoveAsync(guardian).ConfigureAwait(false);
        }
    }
}
