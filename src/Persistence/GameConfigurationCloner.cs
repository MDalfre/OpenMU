// <copyright file="GameConfigurationCloner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using System.Collections;
using System.Reflection;
using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// Creates an independent deep copy of a <see cref="GameConfiguration"/>.
/// </summary>
public static class GameConfigurationCloner
{
    /// <summary>
    /// Clones the complete object graph into the specified persistence context.
    /// </summary>
    /// <param name="source">The configuration to clone.</param>
    /// <param name="context">The context which creates and tracks the cloned objects.</param>
    /// <returns>The cloned game configuration.</returns>
    public static GameConfiguration Clone(GameConfiguration source, IContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);

        var clones = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        DiscoverEntities(source, context, clones);

        foreach (var (sourceEntity, targetEntity) in clones)
        {
            CopyProperties(sourceEntity, targetEntity, clones);
        }

        return (GameConfiguration)clones[source];
    }

    private static void DiscoverEntities(object source, IContext context, IDictionary<object, object> clones)
    {
        if (clones.ContainsKey(source))
        {
            return;
        }

        var contractType = GetContractType(source);
        clones.Add(source, context.CreateNew(contractType));

        foreach (var property in GetModelProperties(contractType))
        {
            var value = property.GetValue(source);
            if (value is IIdentifiable)
            {
                DiscoverEntities(value, context, clones);
            }
            else if (value is IEnumerable enumerable and not string and not byte[])
            {
                foreach (var item in enumerable.OfType<IIdentifiable>())
                {
                    DiscoverEntities(item, context, clones);
                }
            }
        }
    }

    private static void CopyProperties(object source, object target, IReadOnlyDictionary<object, object> clones)
    {
        var contractType = GetContractType(source);
        foreach (var property in GetModelProperties(contractType))
        {
            var sourceValue = property.GetValue(source);
            if (sourceValue is IEnumerable enumerable and not string and not byte[])
            {
                CopyCollection(property, enumerable, target, clones);
                continue;
            }

            if (!property.CanWrite)
            {
                continue;
            }

            var targetValue = sourceValue switch
            {
                null => null,
                IIdentifiable => clones[sourceValue],
                byte[] bytes => bytes.ToArray(),
                _ => sourceValue,
            };
            property.SetValue(target, targetValue);
        }
    }

    private static void CopyCollection(PropertyInfo property, IEnumerable source, object target, IReadOnlyDictionary<object, object> clones)
    {
        var targetCollection = property.GetValue(target)
            ?? throw new InvalidOperationException($"Collection '{property.DeclaringType?.FullName}.{property.Name}' is not initialized.");
        var collectionInterface = targetCollection.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collectionInterface is null)
        {
            throw new InvalidOperationException($"Collection '{property.DeclaringType?.FullName}.{property.Name}' is not mutable.");
        }

        collectionInterface.GetMethod(nameof(ICollection<object>.Clear))!.Invoke(targetCollection, null);
        var addMethod = collectionInterface.GetMethod(nameof(ICollection<object>.Add))!;
        foreach (var item in source)
        {
            var targetItem = item is IIdentifiable ? clones[item] : item;
            addMethod.Invoke(targetCollection, new[] { targetItem });
        }
    }

    private static Type GetContractType(object entity)
    {
        var type = entity.GetType();
        while (type.Assembly == typeof(IContext).Assembly)
        {
            type = type.BaseType
                ?? throw new InvalidOperationException($"No data model type found for '{entity.GetType().FullName}'.");
        }

        return type;
    }

    private static IEnumerable<PropertyInfo> GetModelProperties(Type contractType)
    {
        return contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead
                && property.GetIndexParameters().Length == 0
                && property.Name != nameof(IIdentifiable.Id));
    }
}
