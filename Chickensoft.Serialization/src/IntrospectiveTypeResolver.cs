namespace Chickensoft.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Introspective type resolver for use when serializing and deserializing
/// type hierarchies that only contain types marked with the
/// <see cref="Introspection.MetaAttribute" />
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

  private readonly Dictionary<Type, JsonTypeInfo> _jsonTypeInfosByType = new();
  private readonly DefaultJsonTypeInfoResolver _defaultResolver = new();

  /// <inheritdoc />
  public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
    if (_jsonTypeInfosByType.TryGetValue(type, out var existingTypeInfo)) {
      return existingTypeInfo;
    }

    if (!Introspection.Types.Graph.IsIntrospectiveType(type)) {
      // Not an introspective type — let the default resolver handle it.
      //
      // For best results, only use primitive types or other types that can
      // be serialized in AOT environments for non-introspective types.
      return _defaultResolver.GetTypeInfo(type, options);
    }

    var metatype = Introspection.Types.Graph.GetMetatype(type);

    var typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, options);
    var derivedTypes = Introspection.Types.Graph.GetSubtypes(type);

    if (derivedTypes.Count > 0) {
      // Automatically register the derived types for polymorphic serialization.
      var polymorphismOptions = typeInfo.PolymorphismOptions ??= new();

      polymorphismOptions.UnknownDerivedTypeHandling =
        JsonUnknownDerivedTypeHandling.FailSerialization;
      polymorphismOptions.IgnoreUnrecognizedTypeDiscriminators = true;
      polymorphismOptions.TypeDiscriminatorPropertyName = TYPE_DISCRIMINATOR;

      foreach (var derivedType in derivedTypes) {
        var derivedTypeMetatype =
          Introspection.Types.Graph.GetMetatype(derivedType);
        polymorphismOptions.DerivedTypes.Add(
          new JsonDerivedType(derivedType, derivedTypeMetatype.Id)
        );
      }
    }

    // Converters handle object creation, so we will only register a factory
    // if there are no converters that can handle the type.
    var hasConverter = options.Converters.Any(
      c => c.CanConvert(type)
    );

    // If the type is concrete and doesn't have a converter, we need to
    // register a factory so that the type can be created during
    // deserialization.
    if (!hasConverter && Introspection.Types.Graph.IsConcrete(type)) {
      typeInfo.CreateObject =
        Introspection.Types.Graph.ConcreteVisibleTypes[type];
    }

    // Add properties with the [Save] attribute.
    AddProperties(typeInfo);

    // Cache type info for future use.
    _jsonTypeInfosByType[type] = typeInfo;

    return typeInfo;
  }

  private void AddProperties(JsonTypeInfo typeInfo) {
    foreach (
      var property in Introspection.Types.Graph.GetProperties(typeInfo.Type)
    ) {
      if (
        !property.AttributesByType.TryGetValue(
          typeof(SaveAttribute), out var saveAttributes
        ) ||
        saveAttributes.FirstOrDefault() is not SaveAttribute saveAttribute
      ) {
        continue;
      }

      var name = saveAttribute.Id ?? property.Name;

      var jsonProp = typeInfo.CreateJsonPropertyInfo(
        property.Type, name
      );
      jsonProp.IsRequired = false;
      jsonProp.Get = property.Getter;
      jsonProp.Set = property.Setter;

      typeInfo.Properties.Add(jsonProp);
    }
  }
}
