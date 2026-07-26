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
    private readonly List<Monster> _supportMonsters = new();
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
        await this.CleanupEventMonstersAsync(cancellationToken).ConfigureAwait(false);
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
        var monstersToSpawn = new List<(short MonsterId, byte SpawnX, byte SpawnY, byte Direction, string Role)>
        {
            (this._options.GuardianMonsterId, this._options.GuardianSpawnX, this._options.GuardianSpawnY, this._options.GuardianDirection, "guardian"),
        };
        monstersToSpawn.AddRange((this._options.SupportMonsters ?? []).Select(monster => (monster.MonsterId, monster.SpawnX, monster.SpawnY, monster.Direction, "support monster")));

        var resolvedMonsters = new List<(MonsterDefinition Definition, byte SpawnX, byte SpawnY, byte Direction, string Role)>();
        foreach (var monster in monstersToSpawn)
        {
            var monsterDefinition = context.Configuration.Monsters.FirstOrDefault(definition => definition.Number == (ushort)monster.MonsterId)
                                    ?? throw new InvalidOperationException($"The configured {monster.Role} definition {monster.MonsterId} was not found.");
            if (!map.Terrain.WalkMap[monster.SpawnX, monster.SpawnY])
            {
                throw new InvalidOperationException($"The configured {monster.Role} spawn position is not walkable.");
            }

            resolvedMonsters.Add((monsterDefinition, monster.SpawnX, monster.SpawnY, monster.Direction, monster.Role));
        }

        try
        {
            var guardian = await this.SpawnMonsterAsync(context, map, resolvedMonsters[0], cancellationToken).ConfigureAwait(false);
            this._guardian = guardian;
            this._guardianEventInstanceId = eventInstanceId;
            foreach (var supportMonster in resolvedMonsters.Skip(1))
            {
                this._supportMonsters.Add(await this.SpawnMonsterAsync(context, map, supportMonster, cancellationToken).ConfigureAwait(false));
            }

            this._logger.LogInformation("Spawned Valoria guardian {GuardianId} and {SupportMonsterCount} support monsters for {EventInstanceId} on server {ServerId}.", guardian.Id, this._supportMonsters.Count, eventInstanceId, context.Id);
            return guardian.Id;
        }
        catch
        {
            await this.CleanupEventMonstersAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask CleanupAsync(Guid? eventInstanceId, CancellationToken cancellationToken)
    {
        await this.CleanupEventMonstersAsync(cancellationToken).ConfigureAwait(false);
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

    private async ValueTask<Monster> SpawnMonsterAsync(
        IGameServerContext context,
        GameMap map,
        (MonsterDefinition Definition, byte SpawnX, byte SpawnY, byte Direction, string Role) configuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var area = new MonsterSpawnArea
        {
            GameMap = map.Definition,
            MonsterDefinition = configuration.Definition,
            SpawnTrigger = SpawnTrigger.ManuallyForEvent,
            Quantity = 1,
            X1 = configuration.SpawnX,
            X2 = configuration.SpawnX,
            Y1 = configuration.SpawnY,
            Y2 = configuration.SpawnY,
            Direction = (Direction)configuration.Direction,
        };
        var monster = new Monster(
            area,
            configuration.Definition,
            map,
            NullDropGenerator.Instance,
            new BasicMonsterIntelligence(),
            context.PlugInManager,
            context.PathFinderPool);
        monster.Initialize();
        await map.AddAsync(monster).ConfigureAwait(false);
        monster.OnSpawn();
        return monster;
    }

    private async ValueTask CleanupEventMonstersAsync(CancellationToken cancellationToken)
    {
        var monsters = this._supportMonsters.Append(this._guardian).OfType<Monster>().ToArray();
        this._supportMonsters.Clear();
        this._guardian = null;
        this._guardianEventInstanceId = null;
        foreach (var monster in monsters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (monster.Id != 0)
            {
                await monster.CurrentMap.RemoveAsync(monster).ConfigureAwait(false);
            }
        }
    }
}
