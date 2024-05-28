namespace Chickensoft.Serialization;

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Chickensoft.Introspection;

/// <summary>
/// Chickensoft serialization type resolver.
/// </summary>
public class SerializableTypeResolver : IJsonTypeInfoResolver {
  /// <inheritdoc />
  public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) {
    // Check for converters we have been explicitly told about.
    //
    // This covers cases where we have converters for types that we don't have
    // generated introspection metadata for, typically because the originate
    // outside our assembly.
    //
    // Converters for these external types have to be registered with
    // Serializer.AddConverter<T>(JsonConverter<T>). We store a converter
    // factory there that captures the generic argument so we can reuse it
    // to invoke the JsonMetadataServices.CreateValueInfo<T> needed.
    if (
      Serializer._customConverters.TryGetValue(
        type, out var customConverterFactory
      )
    ) {
      return customConverterFactory(options);
    }

    // Check for identifiable types that have generated metadata and are
    // converted by the IdentifiableTypeConverter.

    if (
      Serializer.GetRuntimeConverterForType(type, options) is { } converter &&
      Types.Graph.GetMetadata(type) is IClosedTypeMetadata closedTypeMetadata
    ) {
      // Type has a runtime converter (specified in serializer options) and
      // is also a closed type that we possess generated introspection metadata
      // for.
      //
      // Use the generated type info to resolve the generic argument for the
      // JsonMetadataServices.CreateValueInfo<T> method.
      var customConverterTypeInfoCreator =
        new Serializer.CustomConverterTypeInfoCreator(options);

      closedTypeMetadata.GenericTypeGetter(customConverterTypeInfoCreator);

      if (customConverterTypeInfoCreator.TypeInfo is { } typeInfo) {
        // Defer to any custom converter for this type.
        return typeInfo;
      }
    }

    // Check built-in types.

    if (
      Serializer.BuiltInConverterFactories.TryGetValue(
        type, out var builtInConverterFactory
      )
    ) {
      // Type has a built-in converter.
      return builtInConverterFactory(options);
    }

    // Check collection types that we know about.

    if (Serializer._collections.TryGetValue(type, out var collectionInfo)) {
      // Supported collection type we discovered previously
      // (List, HashSet, Dictionary)
      return collectionInfo(options);
    }

    // Not an introspective type, identifiable type, collection, or
    // built-in type. Maybe another type resolver down the chain can handle it.
    return null;
  }
}
