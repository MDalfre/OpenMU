// <copyright file="ValoriaThroneEventState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// The lifecycle states of a Valoria Throne event execution.
/// </summary>
public enum ValoriaThroneEventState
{
    Disabled,
    Idle,
    Announcing,
    Preparing,
    RegistrationOpen,
    GuardianBattle,
    CrownOnGround,
    CrownCarried,
    CoronationInProgress,
    Finishing,
    Cooldown,
    Faulted,
}
