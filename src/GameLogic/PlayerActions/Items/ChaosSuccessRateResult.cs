// <copyright file="ChaosSuccessRateResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>Contains an authoritative item crafting success rate calculation.</summary>
/// <param name="BaseRate">The base success percentage.</param>
/// <param name="EffectiveRate">The final success percentage.</param>
/// <param name="Modifiers">The applied modifiers.</param>
public sealed record ChaosSuccessRateResult(double BaseRate, double EffectiveRate, IReadOnlyCollection<SuccessRateModifier> Modifiers);
