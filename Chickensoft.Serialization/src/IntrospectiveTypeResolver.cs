namespace Chickensoft.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Introspection;

/// <summary>
/// Introspective type resolver for use when serializing and deserializing
/// type hierarchies that only contain types marked with the
/// <see cref="MetaAttribute" />
/// attribute.
/// </summary>
public interface IIntrospectiveTypeResolver : IJsonTypeInfoResolver;

/// <inheritdoc cref="IIntrospectiveTypeResolver" />
public class IntrospectiveTypeResolver : IIntrospectiveTypeResolver {
  /// <summary>
  /// Type discriminator used when serializing and deserializing polymorphic
  /// introspective types.
  /// </summary>
  public const string TYPE_DISCRIMINATOR = "$type";

  // Stores type info factories for introspective types as they are requested.
  private static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > _introspectiveTypes = new();

  // Stores collection type info factories as they are requested.
  private static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > _collections = new();

  private static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>> _builtInTypes = new() {
      [typeof(bool)] = (options) =>
        JsonMetadataServices.CreateValueInfo<bool>(
          options, JsonMetadataServices.BooleanConverter
        ),
      [typeof(byte[])] = (options) =>
        JsonMetadataServices.CreateValueInfo<byte[]>(
          options, JsonMetadataServices.ByteArrayConverter
        ),
      [typeof(byte)] = (options) =>
        JsonMetadataServices.CreateValueInfo<byte>(
          options, JsonMetadataServices.ByteConverter
        ),
      [typeof(char)] = (options) =>
        JsonMetadataServices.CreateValueInfo<char>(
          options, JsonMetadataServices.CharConverter
        ),
      [typeof(DateTime)] = (options) =>
        JsonMetadataServices.CreateValueInfo<DateTime>(
          options, JsonMetadataServices.DateTimeConverter
        ),
      [typeof(DateTimeOffset)] = (options) =>
        JsonMetadataServices.CreateValueInfo<DateTimeOffset>(
          options, JsonMetadataServices.DateTimeOffsetConverter
        ),
      [typeof(decimal)] = (options) =>
        JsonMetadataServices.CreateValueInfo<decimal>(
          options, JsonMetadataServices.DecimalConverter
        ),
      [typeof(double)] = (options) =>
        JsonMetadataServices.CreateValueInfo<double>(
          options, JsonMetadataServices.DoubleConverter
        ),
      [typeof(Guid)] = (options) =>
        JsonMetadataServices.CreateValueInfo<Guid>(
          options, JsonMetadataServices.GuidConverter
        ),
      [typeof(short)] = (options) =>
        JsonMetadataServices.CreateValueInfo<short>(
          options, JsonMetadataServices.Int16Converter
        ),
      [typeof(int)] = (options) =>
        JsonMetadataServices.CreateValueInfo<int>(
          options, JsonMetadataServices.Int32Converter
        ),
      [typeof(long)] = (options) =>
        JsonMetadataServices.CreateValueInfo<long>(
          options, JsonMetadataServices.Int64Converter
        ),
      [typeof(JsonArray)] = (options) =>
        JsonMetadataServices.CreateValueInfo<JsonArray>(
          options, JsonMetadataServices.JsonArrayConverter
        ),
      [typeof(JsonDocument)] = (options) =>
        JsonMetadataServices.CreateValueInfo<JsonDocument>(
          options, JsonMetadataServices.JsonDocumentConverter
        ),
      [typeof(JsonElement)] = (options) =>
        JsonMetadataServices.CreateValueInfo<JsonElement>(
          options, JsonMetadataServices.JsonElementConverter
        ),
      [typeof(JsonNode)] = (options) =>
        JsonMetadataServices.CreateValueInfo<JsonNode>(
          options, JsonMetadataServices.JsonNodeConverter
        ),
      [typeof(JsonObject)] = (options) =>
        JsonMetadataServices.CreateValueInfo<JsonObject>(
          options, JsonMetadataServices.JsonObjectConverter
        ),
      [typeof(JsonValue)] = (options) =>
        JsonMetadataServices.CreateValueInfo<JsonValue>(
          options, JsonMetadataServices.JsonValueConverter
        ),
      [typeof(Memory<byte>)] = (options) =>
        JsonMetadataServices.CreateValueInfo<Memory<byte>>(
          options, JsonMetadataServices.MemoryByteConverter
        ),
      [typeof(object)] = (options) =>
        JsonMetadataServices.CreateValueInfo<object>(
          options, JsonMetadataServices.ObjectConverter
        ),
      [typeof(ReadOnlyMemory<byte>)] = (options) =>
        JsonMetadataServices.CreateValueInfo<ReadOnlyMemory<byte>>(
          options, JsonMetadataServices.ReadOnlyMemoryByteConverter
        ),
      [typeof(sbyte)] = (options) =>
        JsonMetadataServices.CreateValueInfo<sbyte>(
          options, JsonMetadataServices.SByteConverter
        ),
      [typeof(float)] = (options) =>
        JsonMetadataServices.CreateValueInfo<float>(
          options, JsonMetadataServices.SingleConverter
        ),
      [typeof(string)] = (options) =>
        JsonMetadataServices.CreateValueInfo<string>(
          options, JsonMetadataServices.StringConverter
        ),
      [typeof(TimeSpan)] = (options) =>
        JsonMetadataServices.CreateValueInfo<TimeSpan>(
          options, JsonMetadataServices.TimeSpanConverter
        ),
      [typeof(ushort)] = (options) =>
        JsonMetadataServices.CreateValueInfo<ushort>(
          options, JsonMetadataServices.UInt16Converter
        ),
      [typeof(uint)] = (options) =>
        JsonMetadataServices.CreateValueInfo<uint>(
          options, JsonMetadataServices.UInt32Converter
        ),
      [typeof(ulong)] = (options) =>
        JsonMetadataServices.CreateValueInfo<ulong>(
          options, JsonMetadataServices.UInt64Converter
        ),
      [typeof(Uri)] = (options) =>
        JsonMetadataServices.CreateValueInfo<Uri>(
          options, JsonMetadataServices.UriConverter
        ),
      [typeof(Version)] = (options) =>
        JsonMetadataServices.CreateValueInfo<Version>(
          options, JsonMetadataServices.VersionConverter
        )
    };

  /// <inheritdoc />
  public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) {
    if (!Types.Graph.IsIntrospectiveType(type)) {
      if (_collections.TryGetValue(type, out var collectionInfo)) {
        return collectionInfo(options);
      }

      if (_builtInTypes.TryGetValue(type, out var builtInFactory)) {
        return builtInFactory(options);
      }

      // Not an introspective type, collection we have seen, or built-in type.
      return null;
    }

    if (_introspectiveTypes.TryGetValue(type, out var typeInfoFactory)) {
      return typeInfoFactory(options);
    }

    typeInfoFactory = (JsonSerializerOptions options) => {
      var introspectiveTypeInfoCreator = new IntrospectiveTypeInfoCreator(
        options, this, type
      );

      var metatype = Types.Graph.GetMetatype(type);

      metatype.GetGenericType(introspectiveTypeInfoCreator);

      return introspectiveTypeInfoCreator.TypeInfo;
    };

    // Cache type info for future use.
    _introspectiveTypes[type] = typeInfoFactory;

    return typeInfoFactory(options);
  }

  // Recursively identify collection types described by the introspection data
  // for a generic member type.
  private static void IdentifyCollectionTypes(
    IJsonTypeInfoResolver resolver,
    GenericType genericType,
    JsonSerializerOptions options
  ) {
    if (_collections.ContainsKey(genericType.ClosedType)) {
      return;
    }

    if (genericType.OpenType == typeof(List<>)) {
      _collections[genericType.ClosedType] = (options) => {
        var listInfoCreator = new ListInfoCreator(options);
        genericType.GenericTypeGetter(listInfoCreator);
        var typeInfo = listInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(resolver, genericType.Arguments[0], options);
    }
    else if (genericType.OpenType == typeof(HashSet<>)) {
      _collections[genericType.ClosedType] = (options) => {
        var hashSetInfoCreator = new HashSetInfoCreator(options);
        genericType.GenericTypeGetter(hashSetInfoCreator);
        var typeInfo = hashSetInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(resolver, genericType.Arguments[0], options);
    }
    else if (genericType.OpenType == typeof(Dictionary<,>)) {
      _collections[genericType.ClosedType] = (options) => {
        var dictionaryInfoCreator = new DictionaryInfoCreator(options);
        genericType.GenericTypeGetter2!(dictionaryInfoCreator);
        var typeInfo = dictionaryInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(resolver, genericType.Arguments[0], options);
      IdentifyCollectionTypes(resolver, genericType.Arguments[1], options);
    }
  }

  private class IntrospectiveTypeInfoCreator : ITypeReceiver {
    public JsonSerializerOptions Options { get; }
    public IJsonTypeInfoResolver Resolver { get; }
    public Type Type { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public IntrospectiveTypeInfoCreator(
      JsonSerializerOptions options,
      IJsonTypeInfoResolver resolver,
      Type type
    ) {
      Options = options;
      Resolver = resolver;
      Type = type;
    }

    public void Receive<T>() {
      // Converters handle object creation, so we will only register a factory
      // if there are no converters that can handle the type.
      var possibleConverter = Options.Converters.FirstOrDefault(
        c => c.CanConvert(Type)
      );

      if (possibleConverter is { } converter) {
        TypeInfo = JsonMetadataServices.CreateValueInfo<T>(Options, converter);
        return;
      }

      var objectInfo = new JsonObjectInfoValues<T>() {
        ObjectCreator = null,
        ObjectWithParameterizedConstructorCreator = null,
        ConstructorParameterMetadataInitializer = null,
        SerializeHandler = null,
        PropertyMetadataInitializer = _ => Types.Graph
          .GetProperties(Type)
          .Select(property => {
            var propertyInfoCreator = new PropertyInfoCreator(
              Options, Resolver, Type, property
            );

            property.GenericType.GenericTypeGetter(propertyInfoCreator);

            return propertyInfoCreator.PropertyInfo;
          })
          .ToArray()
      };

#pragma warning disable CS8714 // non-applicable nullability warnings
      TypeInfo = JsonMetadataServices.CreateObjectInfo(
        Options, objectInfo
      );
#pragma warning restore CS8714

      TypeInfo.NumberHandling = null;
      TypeInfo.OriginatingResolver = Resolver;

      var derivedTypes = Types.Graph.GetSubtypes(Type);

      if (derivedTypes.Count > 0) {
        // Automatically register the derived types for polymorphic serialization.
        var polymorphismOptions = TypeInfo.PolymorphismOptions ??= new();

        polymorphismOptions.UnknownDerivedTypeHandling =
          JsonUnknownDerivedTypeHandling.FailSerialization;
        polymorphismOptions.IgnoreUnrecognizedTypeDiscriminators = true;
        polymorphismOptions.TypeDiscriminatorPropertyName = TYPE_DISCRIMINATOR;

        foreach (var derivedType in derivedTypes) {
          var derivedTypeMetatype =
            Types.Graph.GetMetatype(derivedType);
          polymorphismOptions.DerivedTypes.Add(
            new JsonDerivedType(derivedType, derivedTypeMetatype.Id)
          );
        }
      }

      if (Types.Graph.IsConcrete(Type)) {
        // If the type is concrete and doesn't have a converter, we need to
        // register a factory so that the type can be created during
        // deserialization.
        TypeInfo.CreateObject =
          () => (T)Types.Graph.ConcreteVisibleTypes[Type]();
      }
    }
  }

  private class PropertyInfoCreator : ITypeReceiver {
    public JsonSerializerOptions Options { get; }
    public IJsonTypeInfoResolver Resolver { get; }
    public Type DeclaringType { get; }
    public PropertyMetadata Property { get; }
    public JsonPropertyInfo PropertyInfo { get; private set; } = default!;

    public PropertyInfoCreator(
      JsonSerializerOptions options,
      IJsonTypeInfoResolver resolver,
      Type declaringType,
      PropertyMetadata property
    ) {
      Options = options;
      Resolver = resolver;
      DeclaringType = declaringType;
      Property = property;
    }

    public void Receive<T>() {
      if (
        !Property.Attributes.TryGetValue(
          typeof(SaveAttribute), out var saveAttributes
        ) ||
        saveAttributes.FirstOrDefault() is not SaveAttribute saveAttribute
      ) {
        return;
      }

      IdentifyCollectionTypes(Resolver, Property.GenericType, Options);

      var info = new JsonPropertyInfoValues<T>() {
        IsProperty = true,
        IsPublic = true,
        IsVirtual = false,
        DeclaringType = DeclaringType,
        Converter = null,
#pragma warning disable CS8600, CS8602 // non-applicable nullability warnings
        Getter = obj => (T)Property.Getter(obj),
        Setter = (obj, value) => Property.Setter(obj, value),
#pragma warning restore CS8600, CS8602
        IgnoreCondition = null,
        HasJsonInclude = false,
        IsExtensionData = false,
        NumberHandling = null,
        PropertyName = Property.Name,
        JsonPropertyName = saveAttribute.Id
      };

      PropertyInfo = JsonMetadataServices.CreatePropertyInfo(Options, info);
      PropertyInfo.IsRequired = false;
    }
  }

  // Call with list element type
  private class ListInfoCreator : ITypeReceiver {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public ListInfoCreator(JsonSerializerOptions options) {
      Options = options;
    }

    public void Receive<T>() {
      var info = new JsonCollectionInfoValues<List<T>>() {
        ObjectCreator = () => new List<T>(),
        SerializeHandler = null
      };
      TypeInfo = JsonMetadataServices.CreateListInfo<List<T>, T>(Options, info);
      TypeInfo.NumberHandling = null;
    }
  }

  // Call with hash set element type
  private class HashSetInfoCreator : ITypeReceiver {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public HashSetInfoCreator(JsonSerializerOptions options) {
      Options = options;
    }

    public void Receive<T>() {
      var info = new JsonCollectionInfoValues<HashSet<T>>() {
        ObjectCreator = () => new HashSet<T>(),
        SerializeHandler = null
      };
      TypeInfo = JsonMetadataServices.CreateISetInfo<HashSet<T>, T>(
        Options, info
      );
      TypeInfo.NumberHandling = null;
    }
  }

  private class DictionaryInfoCreator : ITypeReceiver2 {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo TypeInfo { get; private set; } = default!;

    public DictionaryInfoCreator(JsonSerializerOptions options) {
      Options = options;
    }

    public void Receive<TA, TB>() {
      var info = new JsonCollectionInfoValues<Dictionary<TA, TB>>() {
        ObjectCreator = () => new Dictionary<TA, TB>(),
        SerializeHandler = null
      };
#pragma warning disable CS8714
      TypeInfo = JsonMetadataServices.CreateDictionaryInfo<
        Dictionary<TA, TB>, TA, TB
      >(Options, info);
#pragma warning restore CS8714
      TypeInfo.NumberHandling = null;
    }
  }
}
