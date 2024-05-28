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
  private readonly ConcurrentDictionary<string, Dictionary<int, Type>>
    _identifiableTypesByIdAndVersion = new();
  private readonly ConcurrentDictionary<string, int>
    _identifiableLatestVersionsById = new();
  private readonly ConcurrentDictionary<Type, Set<Type>> _typesByBaseType =
    new();
  private readonly ConcurrentDictionary<Type, Set<Type>> _typesByAncestor =
    new();
  private readonly ConcurrentDictionary<Type, IEnumerable<PropertyMetadata>>
    _properties = new();
  private readonly ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>>
    _attributes = new();

  private readonly IReadOnlySet<Type> _emptyTypeSet = new Set<Type>();
  // Custom serializable types that do not need generated metadata.

  private readonly IReadOnlyDictionary<Type, Attribute[]> _emptyAttributes =
    new Dictionary<Type, Attribute[]>();
  #endregion Caches

  internal void Reset() {
    _types.Clear();
    _identifiableTypes.Clear();
    _identifiableTypesByIdAndVersion.Clear();
    _identifiableLatestVersionsById.Clear();
    _typesByBaseType.Clear();
    _typesByAncestor.Clear();
    _properties.Clear();
    _attributes.Clear();
  }

  #region ITypeGraph
  public void Register(ITypeRegistry registry) {
    RegisterTypes(registry);
    PromoteInheritedIdentifiableTypes(registry);
    ComputeTypesByBaseType(registry);
    ValidateTypeGraph();
  }

  public int? GetLatestVersion(string id) =>
    _identifiableLatestVersionsById.TryGetValue(id, out var version)
      ? version
      : null;

  public Type? GetIdentifiableType(string id, int? version = null) => (
    (version ?? GetLatestVersion(id)) is int actualVersion &&
    _identifiableTypesByIdAndVersion.TryGetValue(id, out var versions) &&
    versions.TryGetValue(actualVersion, out var type)
  ) ? type : null;

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
          .SelectMany(metadata => metadata.Metatype.Properties);

      _properties[type] = _properties[type].Distinct();
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

  public void AddCustomType(
    Type type,
    string name,
    Action<ITypeReceiver> genericTypeGetter,
    Func<object> factory,
    string id,
    int version = 1
  ) => RegisterType(
    type,
    new IdentifiableTypeMetadata(
      name,
      genericTypeGetter,
      factory,
      new EmptyMetatype(type),
      id,
      version
    )
  );

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
      RegisterType(type, registry.VisibleTypes[type]);
    }
  }

  private void RegisterType(
    Type @type, ITypeMetadata metadata, bool overwrite = false
  ) {
    // Cache types by system type.
    _types[type] = metadata;

    if (
      metadata is IIdentifiableTypeMetadata identifiableTypeMetadata
    ) {

      // Track types by both id and version.
      if (
        metadata is IConcreteIntrospectiveTypeMetadata introspectiveMetadata
      ) {
        var id = identifiableTypeMetadata.Id;
        var version = introspectiveMetadata.Version;
        // Only concrete types are allowed to be versioned.

        if (_identifiableTypesByIdAndVersion.TryGetValue(id, out var versions)) {
          // Validate that we're not overwriting an existing type if we're not
          // allowed to.
          if (!overwrite && versions.TryGetValue(version, out var existingType)) {
            throw new DuplicateNameException(
              $"Cannot register introspective type `{type}` with id `{id}` " +
              $"and version `{version}`. A different type with the same id " +
              $"and version has already been registered: {existingType}."
            );
          }
        }
        else {
          versions = new();
          _identifiableTypesByIdAndVersion[id] = versions;
        }

        versions[version] = type;

        // Track the latest version of an identifiable type since that's
        // usually the one we want to use.
        if (
          _identifiableLatestVersionsById.TryGetValue(
            id, out var existingVersion
          )
        ) {
          if (version > existingVersion) {
            _identifiableLatestVersionsById[id] = version;
          }
        }
        else {
          _identifiableLatestVersionsById[id] = version;
        }
      }
    }
  }

  private void PromoteInheritedIdentifiableTypes(ITypeRegistry registry) {
    // Some introspective types may not be known to be identifiable at
    // compile-time since base types cannot be examined without looking at
    // analyzer symbol data, which is slow. So, we look at them at runtime and
    // promote them to identifiable types right after registration.

    foreach (var visibleType in registry.VisibleTypes.Keys) {
      var metadata = registry.VisibleTypes[visibleType];

      if (metadata is not IntrospectiveTypeMetadata introspectiveMetadata) {
        // only promote concrete introspective types
        continue;
      }

      var version = introspectiveMetadata.Version;
      var isIdentifiable = false;

      // Only iterate through base types.
      foreach (var type in GetTypeAndBaseTypes(visibleType).Skip(1)) {
        metadata = _types[type];

        if (metadata is not IIdentifiableTypeMetadata idMetadata) {
          continue;
        }

        if (isIdentifiable) {
          throw new InvalidOperationException(
            $"The type `{visibleType}` is marked as identifiable, but " +
            $"extends another identifiable type, `{type}`. Please ensure " +
            "only the base introspective type is marked as identifiable."
          );
        }

        if (
          metadata is IConcreteIntrospectiveTypeMetadata concreteMetadata &&
          version == concreteMetadata.Version
        ) {
          throw new InvalidOperationException(
            $"The type `{visibleType}` is marked as identifiable, but " +
            $"extends another identifiable type `{type}` with the same " +
            $"version `{version}`. Please ensure the version is unique."
          );
        }

        isIdentifiable = true;

        // Promote metadata to an identifiable type. Basically, we just take
        // the id from the base type and use it for the derived type, while
        // keeping its other metadata intact.
        metadata = new IdentifiableTypeMetadata(
          Name: introspectiveMetadata.Name,
          GenericTypeGetter: introspectiveMetadata.GenericTypeGetter,
          Factory: introspectiveMetadata.Factory,
          Metatype: introspectiveMetadata.Metatype,
          Id: idMetadata.Id,
          Version: version
        );

        // Replace existing type metadata with the updated metadata.
        RegisterType(visibleType, metadata, overwrite: true);
      }
    }
  }

  private void DiscoverInheritedIdentifiableTypes() { }

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

  private void ValidateTypeGraph() {
    // Ensures that all types derived from an identifiable type are also
    // identifiable.
    var nonIdTypes = new HashSet<string>();

    foreach (var type in _identifiableTypes.Keys) {
      nonIdTypes.Clear();

      var typeMetadata = _identifiableTypes[type];
      var id = typeMetadata.Id;

      // Validate types derived from an identifiable type are also
      // identifiable.
      foreach (var descendant in GetDescendantSubtypes(type)) {
        if (_types[descendant] is not IIntrospectiveTypeMetadata) {
          // Found a derived type of an identifiable type that is not
          // also identifiable.
          //
          // All derived types of of an identifiable type must also be
          // identifiable to ensure serialization systems are sound.
          nonIdTypes.Add(_types[descendant].Name);
        }
      }

      if (nonIdTypes.Count == 0) { continue; }

      var warning =
        $"WARNING: The identifiable type `{typeMetadata.Id}` has derived " +
        "types which are not introspective. Please ensure they are " +
        "identifiable, introspective types by adding the " +
        $"`[{nameof(MetaAttribute)}]` attribute to them: " +
        $"{string.Join(", ", nonIdTypes)}.";

      PrintWarning(warning);
    }
  }

  private void PrintWarning(string warning) {
    var bgColor = Console.BackgroundColor;
    var fgColor = Console.ForegroundColor;
    Console.BackgroundColor = ConsoleColor.DarkRed;
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write(warning);
    Console.BackgroundColor = bgColor;
    Console.ForegroundColor = fgColor;
    Console.WriteLine();
  }

  private class EmptyMetatype(Type type) : IMetatype {
    private static readonly List<PropertyMetadata> _properties = new();
    private static readonly Dictionary<Type, Attribute[]> _attributes = new();
    private static readonly List<Type> _mixins = new();
    private static readonly Dictionary<Type, Action<object>> _mixinHandlers =
      new();

    public Type Type => type;

    public bool HasInitProperties => false;

    public IReadOnlyList<PropertyMetadata> Properties => _properties;

    public IReadOnlyDictionary<Type, Attribute[]> Attributes => _attributes;

    public IReadOnlyList<Type> Mixins => _mixins;

    public IReadOnlyDictionary<Type, Action<object>> MixinHandlers =>
      _mixinHandlers;

    public object Construct(
      IReadOnlyDictionary<string, object?>? args = null
    ) => throw new NotImplementedException();

    // Always be equal to avoid messing up record comparisons when a member
    // of a type.
    public override bool Equals(object obj) => true;
    public override int GetHashCode() => base.GetHashCode();
  }

  #endregion Private Utilities
}
