namespace Chickensoft.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Introspection;

/// <summary>
/// Chickensoft serialization utilities.
/// </summary>
public static class Serializer {

  /// <summary>
  /// Type discriminator used when serializing and deserializing identifiable
  /// types. Helps with polymorphism.
  /// </summary>
  public const string TYPE_PROPERTY = "$type";

  /// <summary>
  /// Version discriminator used when serializing and deserializing polymorphic
  /// types. Helps with making upgradeable models.
  /// </summary>
  public const string VERSION_PROPERTY = "$v";

  // Stores collection type info factories as they are requested.
  internal static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > _collections = new();

  internal static readonly Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > _customConverters = new();

  /// <summary>
  /// Type converter factories for built-in types that System.Text.Json supports
  /// out of the box on every platform.
  /// </summary>
  public static Dictionary<
    Type, Func<JsonSerializerOptions, JsonTypeInfo>
  > BuiltInConverterFactories { get; } = new() {
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

  [ModuleInitializer]
  internal static void Initialize() {
    Types.Graph.AddCustomType(
      type: typeof(SerializableBlackboard),
      name: "SerializableBlackboard",
      genericTypeGetter: (r) => r.Receive<SerializableBlackboard>(),
      factory: () => new SerializableBlackboard(),
      id: "blackboard",
      version: 1
    );

    return;
  }

  /// <summary>
  /// Adds a custom converter for a type that is outside the current assembly.
  /// </summary>
  /// <param name="converter">Custom converter.</param>
  /// <typeparam name="T">Type of value to convert.</typeparam>
  public static void AddConverter<T>(JsonConverter<T> converter) =>
    _customConverters[typeof(T)] = (options) => {
      var expandedConverter = ExpandConverter(typeof(T), converter, options);

      return JsonMetadataServices.CreateValueInfo<T>(
        options, expandedConverter
      );
    };

  #region Private Helper Types
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

  // Call with dictionary key and value types
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

  internal class CustomConverterTypeInfoCreator : ITypeReceiver {
    public JsonSerializerOptions Options { get; }
    public JsonTypeInfo? TypeInfo { get; private set; }

    public CustomConverterTypeInfoCreator(
      JsonSerializerOptions options
    ) {
      Options = options;
    }

    public void Receive<T>() {
      TypeInfo = null;

      var converter = GetRuntimeConverterForType(typeof(T), Options);

      if (converter is null) {
        return;
      }

      TypeInfo = JsonMetadataServices.CreateValueInfo<T>(Options, converter);
    }
  }
  #endregion Private Helper Types

  #region Private Methods
  // Recursively identify collection types described by the introspection data
  // for a generic member type.

  /// <summary>
  /// Recursively identifies and caches collection types described by the given
  /// generated generic type information.
  /// </summary>
  /// <param name="genericType">Generic type description.</param>
  /// <param name="resolver">Originating type resolver, if any.</param>
  /// <param name="options">Serialization options.</param>
  public static void IdentifyCollectionTypes(
    GenericType genericType,
    IJsonTypeInfoResolver? resolver,
    JsonSerializerOptions options
  ) {
    if (_collections.ContainsKey(genericType.ClosedType)) {
      // We've already cached this collection type.
      return;
    }

    if (genericType.OpenType == typeof(List<>)) {
      _collections[genericType.ClosedType] = (options) => {
        var listInfoCreator = new ListInfoCreator(options);
        genericType.Arguments[0].GenericTypeGetter(listInfoCreator);
        var typeInfo = listInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(genericType.Arguments[0], resolver, options);
    }
    else if (genericType.OpenType == typeof(HashSet<>)) {
      _collections[genericType.ClosedType] = (options) => {
        var hashSetInfoCreator = new HashSetInfoCreator(options);
        genericType.Arguments[0].GenericTypeGetter(hashSetInfoCreator);
        var typeInfo = hashSetInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(genericType.Arguments[0], resolver, options);
    }
    else if (genericType.OpenType == typeof(Dictionary<,>)) {
      _collections[genericType.ClosedType] = (options) => {
        var dictionaryInfoCreator = new DictionaryInfoCreator(options);
        genericType.GenericTypeGetter2!(dictionaryInfoCreator);
        var typeInfo = dictionaryInfoCreator.TypeInfo;
        typeInfo.OriginatingResolver = resolver;
        return typeInfo;
      };

      IdentifyCollectionTypes(genericType.Arguments[0], resolver, options);
      IdentifyCollectionTypes(genericType.Arguments[1], resolver, options);
    }
  }

  internal static JsonConverter? GetRuntimeConverterForType(
    Type type, JsonSerializerOptions options
  ) {
    for (var i = 0; i < options.Converters.Count; i++) {
      var converter = options.Converters[i];
      if (converter?.CanConvert(type) == true) {
        return ExpandConverter(type, converter, options);
      }
    }

    return null;
  }

  [return: NotNullIfNotNull(nameof(converter))]
  private static JsonConverter? ExpandConverter(
    Type type,
    JsonConverter? converter,
    JsonSerializerOptions options
  ) {
    if (converter is JsonConverterFactory factory) {
      converter = factory.CreateConverter(type, options);
      if (converter is null or JsonConverterFactory) {
        throw new InvalidOperationException(string.Format(
          "The converter '{0}' cannot return null or a " +
          "JsonConverterFactory instance.",
          factory.GetType()
        ));
      }
    }

    return converter;
  }
  #endregion Private Methods
}
