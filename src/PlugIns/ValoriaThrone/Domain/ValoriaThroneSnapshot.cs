// <copyright file="ValoriaThroneSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Represents the observable state of the current event execution.
/// </summary>
public sealed record ValoriaThroneSnapshot(
    Guid? EventInstanceId,
    ValoriaThroneEventState State,
    DateTimeOffset? NextTransitionAt,
    ushort? GuardianId,
    ushort? CrownGroundItemId,
    Guid? CrownHolderCharacterId,
    uint? CrownHolderGuildId,
    DateTimeOffset? CoronationEndsAt);
