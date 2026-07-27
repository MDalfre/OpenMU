// <copyright file="ImperialStatusCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>Shows the current imperial reign to any player.</summary>
[PlugIn]
[Guid("4CB04BBE-BFD2-4DFB-9137-9120DC935E7B")]
public sealed class ImperialStatusCommandPlugIn : IChatCommandPlugIn
{
    private readonly IValoriaThroneEventController _controller;

    /// <summary>Initializes a new instance of the <see cref="ImperialStatusCommandPlugIn"/> class.</summary>
    public ImperialStatusCommandPlugIn()
        : this(ValoriaThronePlugIn.DefaultController)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ImperialStatusCommandPlugIn"/> class.</summary>
    public ImperialStatusCommandPlugIn(IValoriaThroneEventController controller) => this._controller = controller;

    /// <inheritdoc />
    public string Key => "/imperador";

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public ValueTask HandleCommandAsync(Player player, string command)
    {
        var message = this._controller.ActiveReign is not { } reign
            ? "Valoria não possui um Imperador neste momento."
            : $"Imperador: {reign.EmperorCharacterName}; Guild Imperial: {reign.ImperialGuildName}; Era: {ImperialEraPresentation.GetName(reign.SelectedEra)}; Benefício: {this._controller.GetEraDescription(reign.SelectedEra)}; Fim: {reign.ExpiresAt.ToLocalTime():g}.";
        return player.ShowBlueMessageAsync(message);
    }
}
