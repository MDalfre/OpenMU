// <copyright file="GameConfigurationCloner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        var clones = new Dictionary<object, object>(EntityIdentityComparer.Instance);
        DiscoverEntities(source, context, clones);

        foreach (var (sourceEntity, targetEntity) in clones)
        {
            CopyProperties(sourceEntity, targetEntity, clones);
        }

        foreach (var targetEntity in clones.Values.Distinct(ReferenceEqualityComparer.Instance))
        {
            context.MarkNew(targetEntity);
        }

        return (GameConfiguration)clones[source];
    }

    /// <summary>
    /// Marks all entities which are reachable from the specified configuration as new.
    /// </summary>
    /// <param name="gameConfiguration">The root of the object graph.</param>
    /// <param name="context">The persistence context which tracks the graph.</param>
    public static void MarkGraphAsNew(GameConfiguration gameConfiguration, IContext context)
    {
        var entities = new HashSet<object>(EntityIdentityComparer.Instance);
        DiscoverGraphEntities(gameConfiguration, entities);
        foreach (var entity in entities)
        {
            context.MarkNew(entity);
        }
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

    private static void DiscoverGraphEntities(object source, ISet<object> entities)
    {
        if (!entities.Add(source))
        {
            return;
        }

        foreach (var property in GetModelProperties(GetContractType(source)))
        {
            var value = property.GetValue(source);
            if (value is IIdentifiable)
            {
                DiscoverGraphEntities(value, entities);
            }
            else if (value is IEnumerable enumerable and not string and not byte[])
            {
                foreach (var item in enumerable.OfType<IIdentifiable>())
                {
                    DiscoverGraphEntities(item, entities);
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
            SetRawReference(property, target, targetValue);
        }
    }

    private static void CopyCollection(PropertyInfo property, IEnumerable source, object target, IReadOnlyDictionary<object, object> clones)
    {
        var rawProperty = target.GetType().GetProperty($"Raw{property.Name}", BindingFlags.Public | BindingFlags.Instance);
        var rawCollection = rawProperty?.GetValue(target);
        var targetCollection = GetCollectionInterface(rawCollection) is not null ? rawCollection : property.GetValue(target)
            ?? throw new InvalidOperationException($"Collection '{property.DeclaringType?.FullName}.{property.Name}' is not initialized.");
        var collectionInterface = GetCollectionInterface(targetCollection);
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

    private static Type? GetCollectionInterface(object? collection)
    {
        return collection?.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
    }

    private static void SetRawReference(PropertyInfo property, object target, object? targetValue)
    {
        var rawProperty = target.GetType().GetProperty($"Raw{property.Name}", BindingFlags.Public | BindingFlags.Instance);
        if (rawProperty?.CanWrite is true
            && (targetValue is null || rawProperty.PropertyType.IsInstanceOfType(targetValue)))
        {
            rawProperty.SetValue(target, targetValue);
        }
    }

    private static Type GetContractType(object entity)
    {
        var type = entity.GetType();
        while (IsPersistenceType(type) || type.BaseType is { } baseType && IsPersistenceType(baseType))
        {
            type = type.BaseType
                ?? throw new InvalidOperationException($"No data model type found for '{entity.GetType().FullName}'.");
        }

        return type;
    }

    private static bool IsPersistenceType(Type type)
    {
        return type.Assembly.GetName().Name?.StartsWith("MUnique.OpenMU.Persistence", StringComparison.Ordinal) is true;
    }

    private static IEnumerable<PropertyInfo> GetModelProperties(Type contractType)
    {
        return contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead
                && property.GetIndexParameters().Length == 0
                && property.Name != nameof(IIdentifiable.Id));
    }

    private sealed class EntityIdentityComparer : IEqualityComparer<object>
    {
        public static EntityIdentityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return x is IIdentifiable left
                && y is IIdentifiable right
                && left.Id != Guid.Empty
                && left.Id == right.Id
                && GetContractType(x) == GetContractType(y);
        }

        public int GetHashCode(object obj)
        {
            return obj is IIdentifiable identifiable && identifiable.Id != Guid.Empty
                ? HashCode.Combine(GetContractType(obj), identifiable.Id)
                : RuntimeHelpers.GetHashCode(obj);
        }
    }
}
