// <copyright file="ValoriaThroneAdmissionPolicy.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Applies event-specific admission rules through the generic map entry hook.
/// </summary>
public sealed class ValoriaThroneAdmissionPolicy
{
    private readonly IValoriaThroneEventController _controller;
    private ValoriaThroneOptions _options = ValoriaThroneOptions.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValoriaThroneAdmissionPolicy"/> class.
    /// </summary>
    public ValoriaThroneAdmissionPolicy(IValoriaThroneEventController controller)
    {
        this._controller = controller;
    }

    /// <summary>Configures the policy.</summary>
    public void Configure(ValoriaThroneOptions options) => this._options = options;

    /// <summary>Validates a map entry attempt.</summary>
    public async ValueTask ValidateAsync(Player player, GameMapDefinition targetMap, MapEntrySource source, MapEntryValidationEventArgs eventArgs)
    {
        if (this._controller.IsCrownHolder(player))
        {
            if (targetMap.Number != this._options.EventMapId)
            {
                eventArgs.Denied = true;
                eventArgs.Message = "O Portador da Coroa nÃ£o pode deixar Valley of Loren.";
                return;
            }

            if (source == MapEntrySource.Gate)
            {
                return;
            }
        }

        if (this._options.LandsOfTrials.Enabled && targetMap.Number == this._options.LandsOfTrials.MapId)
        {
            if (await this._controller.CanEnterLandsOfTrialsAsync(player, CancellationToken.None).ConfigureAwait(false))
            {
                return;
            }

            eventArgs.Denied = true;
            eventArgs.Message = "Somente a Guild Imperial e suas alianças podem entrar em Lands of Trials.";
            if (source == MapEntrySource.CharacterSelection)
            {
                var fallbackMap = player.GameContext.Configuration.Maps.FirstOrDefault(map => map.Number == this._options.FallbackMapId);
                if (fallbackMap is not null)
                {
                    eventArgs.RedirectGate = new ExitGate { Map = fallbackMap, X1 = this._options.FallbackPositionX, X2 = this._options.FallbackPositionX, Y1 = this._options.FallbackPositionY, Y2 = this._options.FallbackPositionY };
                }
            }

            return;
        }

        if (!this._options.Enabled || targetMap.Number != this._options.EventMapId)
        {
            return;
        }

        var state = this._controller.State;
        if (state is ValoriaThroneEventState.Idle or ValoriaThroneEventState.Cooldown)
        {
            return;
        }

        if (this._options.AllowGameMasterBypass && player.SelectedCharacter?.CharacterStatus >= CharacterStatus.GameMaster)
        {
            return;
        }

        var eventServerId = this._options.EventServerId;
        var isAuthorizedServer = player.GameContext is IGameServerContext context && context.Id == eventServerId;
        var allowed = state == ValoriaThroneEventState.RegistrationOpen && isAuthorizedServer
                      || state == ValoriaThroneEventState.GuardianBattle && isAuthorizedServer && this._options.AllowLateEntry;
        if (allowed)
        {
            return;
        }

        eventArgs.Denied = true;
        eventArgs.Message = isAuthorizedServer
            ? "Você não pode entrar em Valley of Loren neste momento."
            : "O evento está disponível apenas no servidor PvP.";
        if (!isAuthorizedServer)
        {
            eventArgs.Message = $"O evento está disponível apenas no server {(eventServerId ?? 0) + 1}.";
        }

        if (source == MapEntrySource.CharacterSelection)
        {
            var fallbackMap = player.GameContext.Configuration.Maps.FirstOrDefault(map => map.Number == this._options.FallbackMapId);
            if (fallbackMap is not null)
            {
                eventArgs.RedirectGate = new ExitGate
                {
                    Map = fallbackMap,
                    X1 = this._options.FallbackPositionX,
                    X2 = this._options.FallbackPositionX,
                    Y1 = this._options.FallbackPositionY,
                    Y2 = this._options.FallbackPositionY,
                };
            }
        }

        return;
    }
}
