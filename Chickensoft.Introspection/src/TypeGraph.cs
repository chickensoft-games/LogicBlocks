namespace Chickensoft.Introspection;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Chickensoft.Collections;

internal class TypeGraph : ITypeGraph {
  #region Caches
  private readonly ConcurrentDictionary<Type, ITypeMetadata> _types = new();
  private readonly ConcurrentDictionary<Type, IIdentifiableTypeMetadata>
    _identifiableTypes = new();
  private readonly ConcurrentDictionary<string, Type>
    _identifiableTypesById = new();
  private readonly ConcurrentDictionary<Type, Set<Type>> _typesByBaseType =
    new();
  private readonly ConcurrentDictionary<Type, Set<Type>> _typesByAncestor =
    new();
  private readonly ConcurrentDictionary<Type, IEnumerable<PropertyMetadata>>
    _properties = new();
  private readonly ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>>
    _attributes = new();

  private readonly IReadOnlySet<Type> _emptyTypeSet = new Set<Type>();

  private readonly IReadOnlyDictionary<Type, Attribute[]> _emptyAttributes =
    new Dictionary<Type, Attribute[]>();
  #endregion Caches

  internal void Reset() {
    _identifiableTypes.Clear();
    _identifiableTypesById.Clear();
    _typesByBaseType.Clear();
    _typesByAncestor.Clear();
    _properties.Clear();
    _attributes.Clear();
  }

  #region ITypeGraph
  public void Register(ITypeRegistry registry) {
    RegisterTypes(registry);
    ComputeTypesByBaseType(registry);
    // ValidateDerivedTypesOfIdentifiableTypesAreAlsoIdentifiable();
  }

  public Type? GetIdentifiableType(string id) =>
    _identifiableTypesById.TryGetValue(id, out var type) ? type : null;

  public bool HasMetadata(Type type) =>
    _types.ContainsKey(type);

  public ITypeMetadata? GetMetadata(Type type) =>
    _types.TryGetValue(type, out var metadata) ? metadata : null;

  public IReadOnlySet<Type> GetSubtypes(Type type) =>
    _typesByBaseType.TryGetValue(type, out var subtypes)
      ? subtypes
      : _emptyTypeSet;

  public IReadOnlySet<Type> GetDescendantSubtypes(Type type) {
    CacheDescendants(type);
    return _typesByAncestor[type];
  }

  public IEnumerable<PropertyMetadata> GetProperties(Type type) {
    if (
      !_types.ContainsKey(type) ||
      _types[type] is not IIntrospectiveTypeMetadata metadata
    ) {
      return [];
    }

    if (!_properties.TryGetValue(type, out var properties)) {
      // Cache the properties for a type so we never have to do this again.
      _properties[type] =
        GetTypeAndBaseTypes(type)
          .Select(type => GetMetadata(type))
          .OfType<IIntrospectiveTypeMetadata>()
          .SelectMany(metadata => metadata.Metatype.Properties)
          .Distinct();
    }
    return _properties[type];
  }

  public TAttribute? GetAttribute<TAttribute>(Type type)
  where TAttribute : Attribute =>
    GetAttributes(type).TryGetValue(typeof(TAttribute), out var attributes) &&
    attributes is { Length: > 0 } &&
    attributes[0] is TAttribute attribute
      ? attribute
      : null;

  public IReadOnlyDictionary<Type, Attribute[]> GetAttributes(Type type) {
    if (
      !_types.ContainsKey(type) ||
      _types[type] is not IIntrospectiveTypeMetadata metadata
    ) {
      return _emptyAttributes;
    }

    if (!_attributes.TryGetValue(type, out var attributes)) {
      // Cache the attributes for a type so we never have to do this again.
      _attributes[type] = GetTypeAndBaseTypes(type)
        .Select(type => _types[type])
        .OfType<IIntrospectiveTypeMetadata>()
        .SelectMany((metadata) => metadata.Metatype.Attributes)
        .ToDictionary(
          keySelector: (typeToAttr) => typeToAttr.Key,
          elementSelector: (typeToAttr) => typeToAttr.Value
        );
    }
    return _attributes[type];
  }
  #endregion ITypeGraph

  #region Private Utilities
  private void CacheDescendants(Type type) {
    if (_typesByAncestor.ContainsKey(type)) {
      return;
    }
    _typesByAncestor[type] = FindDescendants(type);
  }

  private Set<Type> FindDescendants(Type type) {
    var descendants = new Set<Type>();
    var queue = new Queue<Type>();
    queue.Enqueue(type);

    while (queue.Count > 0) {
      var currentType = queue.Dequeue();
      descendants.Add(currentType);

      if (_typesByBaseType.TryGetValue(currentType, out var children)) {
        foreach (var child in children) {
          queue.Enqueue(child);
        }
      }
    }

    descendants.Remove(type);

    return descendants;
  }

  private void RegisterTypes(ITypeRegistry registry) {
    // Iterate through all visible types in O(n) time and add them to our
    // internal caches.
    // Why do this? We want to allow multiple assemblies to use this system to
    // find types by their base type or ancestor.
    foreach (var type in registry.VisibleTypes.Keys) {
      var typeMetadata = registry.VisibleTypes[type];

      // Cache types by system type.
      _types[type] = typeMetadata;

      if (
        typeMetadata is IIdentifiableTypeMetadata identifiableTypeMetadata
      ) {
        var id = identifiableTypeMetadata.Id;

        if (_identifiableTypesById.ContainsKey(id)) {
          throw new DuplicateNameException(
            $"Cannot register introspective type `{type}` with id `{id}`. " +
            "A different type with the same id has already been " +
            $"registered: `{_identifiableTypesById[id]}`."
          );
        }

        // Cache identifiable introspective type system types by their id for
        // fast and convenient lookups.
        _identifiableTypesById[id] = type;
        // Cache by system type, too.
        _identifiableTypes[type] = identifiableTypeMetadata;
      }
    }
  }

  private void ComputeTypesByBaseType(ITypeRegistry registry) {
    // Iterate through each type in the registry and its base types,
    // constructing a flat map of base types to their immediately derived types.
    // The beauty of this approach is that it discovers base types which may be
    // in other modules, and works in reflection-free mode since BaseType is
    // always supported by every C# environment, even AOT environments.
    foreach (var type in registry.VisibleTypes.Keys) {
      var lastType = type;
      var baseType = type.BaseType;

      // As far as we know, any type could be a base type.
      if (!_typesByBaseType.ContainsKey(type)) {
        _typesByBaseType[type] = new Set<Type>();
      }

      while (baseType != null) {
        if (!_typesByBaseType.TryGetValue(baseType, out var existingSet)) {
          existingSet = new Set<Type>();
          _typesByBaseType[baseType] = existingSet;
        }
        existingSet.Add(lastType);

        lastType = baseType;
        baseType = lastType.BaseType;
      }
    }
  }

  /// <summary>
  /// Enumerates through a type and its base type hierarchy to discover all
  /// metatypes that describe the type and its base types.
  /// </summary>
  /// <param name="type">Type whose type hierarchy should be examined.</param>
  /// <returns>The type's metatype (if it has one), and any metatypes that
  /// describe its base types, in the order of the most derived type to the
  /// least derived type.</returns>
  private IEnumerable<Type> GetTypeAndBaseTypes(Type type) {
    var currentType = type;

    do {
      if (
        _types.ContainsKey(currentType) &&
        _types[currentType] is IIntrospectiveTypeMetadata
      ) {
        yield return currentType;
      }

      currentType = currentType.BaseType;
    } while (currentType != null);
  }

  private void ValidateDerivedTypesOfIdentifiableTypesAreAlsoIdentifiable() {
    var nonIdTypes = new HashSet<string>();

    foreach (var type in _identifiableTypes.Keys) {
      nonIdTypes.Clear();

      var typeMetadata = _identifiableTypes[type];

      // Validate types derived from an identifiable type are also
      // identifiable.
      foreach (var descendant in GetDescendantSubtypes(type)) {
        if (_types[descendant] is not IIdentifiableTypeMetadata) {
          // Found a derived type of an identifiable type that is not
          // also identifiable.
          //
          // All derived types of of an identifiable type must also be
          // identifiable to ensure serialization systems are sound.
          nonIdTypes.Add(_types[descendant].Name);
        }
      }

      if (nonIdTypes.Count == 0) { continue; }

      throw new InvalidOperationException(
        $"The identifiable type `{typeMetadata}` has derived types which are " +
        "not identifiable. Please ensure they are identifiable, " +
        $"introspective types by adding the `[{nameof(MetaAttribute)}]` and " +
        $"`[{nameof(IdAttribute)}]` attributes to them: " +
        $"{string.Join(", ", nonIdTypes)}."
      );
    }
  }
  #endregion Private Utilities
}
