// <copyright file="ImperialEraCommandPlugIn.cs" company="MUnique">
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

/// <summary>Allows the emperor to proclaim the era of the current reign.</summary>
[PlugIn]
[Guid("2F3D255D-F25B-47E6-928D-52A84C3123A8")]
public sealed class ImperialEraCommandPlugIn : IChatCommandPlugIn
{
    private readonly IValoriaThroneEventController _controller;

    /// <summary>Initializes a new instance of the <see cref="ImperialEraCommandPlugIn"/> class.</summary>
    public ImperialEraCommandPlugIn()
        : this(ValoriaThronePlugIn.DefaultController)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ImperialEraCommandPlugIn"/> class.</summary>
    public ImperialEraCommandPlugIn(IValoriaThroneEventController controller) => this._controller = controller;

    /// <inheritdoc />
    public string Key => "/era";

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var value = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
        var era = Enum.TryParse<ImperialEra>(value, true, out var parsed) ? parsed : ImperialEra.None;
        var selected = await this._controller.SelectEraAsync(player, era, CancellationToken.None).ConfigureAwait(false);
        await player.ShowBlueMessageAsync(selected ? "A Era Imperial foi proclamada." : "Uso: /era <Ascension|Fortune|Freedom|Luck>. Somente o Imperador pode escolher uma vez por reinado.").ConfigureAwait(false);
    }
}
