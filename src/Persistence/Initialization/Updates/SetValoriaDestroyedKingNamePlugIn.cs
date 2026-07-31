// <copyright file="SetValoriaDestroyedKingNamePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets the display name of the monster used as Valoria's destroyed king.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("0E2D98A1-CE66-4C5A-A05E-42E7E928CC8B")]
public sealed class SetValoriaDestroyedKingNamePlugIn : UpdatePlugInBase
{
    private const short MonsterNumber = 580;
    private const string MonsterName = "Ruined King of Valoria";
    private const string PlugInName = "Set Valoria Destroyed King Name";
    private const string PlugInDescription = "Sets the display name of monster 580, used by the Valoria Throne event.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.SetValoriaDestroyedKingName;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Monsters.FirstOrDefault(monster => monster.Number == MonsterNumber) is { } monster)
        {
            monster.Designation = MonsterName;
        }

        return ValueTask.CompletedTask;
    }
}
