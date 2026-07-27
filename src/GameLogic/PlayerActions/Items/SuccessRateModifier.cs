// <copyright file="SuccessRateModifier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>Describes one applied success rate modifier.</summary>
/// <param name="Source">The modifier source.</param>
/// <param name="Multiplier">The relative multiplier.</param>
public sealed record SuccessRateModifier(string Source, double Multiplier);
