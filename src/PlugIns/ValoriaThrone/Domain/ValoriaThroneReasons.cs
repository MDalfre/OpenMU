// <copyright file="ValoriaThroneReasons.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Identifies the cause which started an event execution.
/// </summary>
public enum ValoriaThroneStartReason
{
    Schedule,
    Administrator,
}

/// <summary>
/// Identifies the cause which stopped an event execution.
/// </summary>
public enum ValoriaThroneStopReason
{
    Administrator,
    Completed,
    Timeout,
    Recovery,
    Failure,
}
