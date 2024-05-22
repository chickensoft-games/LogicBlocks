namespace Chickensoft.LogicBlocks.Serialization;

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chickensoft.Collections;
using Chickensoft.Serialization;
using Chickensoft.Introspection;

/// <summary>
/// Introspective type converter that upgrades outdated introspective types
/// as soon as they are deserialized.
/// </summary>
public interface IIdentifiableTypeConverter {
  /// <summary>
  /// Dependencies that outdated introspective types might need after being
  /// deserialized to upgrade themselves.
  /// </summary>
  public IReadOnlyBlackboard DependenciesBlackboard { get; }
}

/// <inheritdoc />
public class IdentifiableTypeConverter :
JsonConverter<object>, IIdentifiableTypeConverter {
  /// <inheritdoc />
  public IReadOnlyBlackboard DependenciesBlackboard { get; }

  internal static ITypeGraph DefaultGraph => Types.Graph;
  // Graph to use for introspection. Allows it to be shimmed for testing.
  internal static ITypeGraph Graph { get; set; } = DefaultGraph;

  private string TypeDiscriminator =>
    Serializer.TYPE_DISCRIMINATOR;

  /// <summary>
  /// Create a new logic block converter with the given type info resolver.
  /// </summary>
  /// <param name="dependenciesBlackboard">Dependencies that might be needed
  /// by outdated states to upgrade themselves.</param>
  public IdentifiableTypeConverter(
    IReadOnlyBlackboard dependenciesBlackboard
  ) {
    DependenciesBlackboard = dependenciesBlackboard;
  }

  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert) =>
    Graph.GetMetadata(typeToConvert) is IIdentifiableTypeMetadata;

  /// <inheritdoc />
  public override void Write(
    Utf8JsonWriter writer,
    object value,
    JsonSerializerOptions options
  ) {
    var type = value.GetType();

    if (Graph.GetMetadata(type) is not IIdentifiableTypeMetadata metadata) {
      throw new JsonException(
        $"The type `{type}` is not an identifiable introspective type."
      );
    }

    writer.WriteStartObject();
    writer.WriteString(TypeDiscriminator, metadata.Id);

    // Get all serializable properties, including those from base types.
    var properties = Graph.GetProperties(type);

    foreach (var property in properties) {
      if (GetPropertyId(property) is not { } propertyId) {
        // Only write properties marked with the [Save] attribute.
        continue;
      }

      // If the property is a collection type, we need to make sure we've
      // cached the closed type of the collection type (recursively) before
      // trying to serialize it.
      Serializer.IdentifyCollectionTypes(
        property.GenericType,
        options.TypeInfoResolver,
        options
      );

      writer.WritePropertyName(propertyId);

      var propertyValue = property.Getter(value);
      var propertyType = property.GenericType.ClosedType;

      JsonSerializer.Serialize(
        writer: writer,
        value: propertyValue,
        inputType: propertyType,
        options: options
      );
    }

    writer.WriteEndObject();
  }

  /// <inheritdoc />
  public override object? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  ) {
    var json = JsonNode.Parse(ref reader) ?? throw new JsonException(
      $"Failed to parse JSON for introspective type {typeToConvert}."
    );

    var typeId =
      json[TypeDiscriminator]?.ToString() ?? throw new JsonException(
        $"Type {typeToConvert} is missing the `{TypeDiscriminator}` type " +
        "discriminator."
      );

    if (
      Graph.GetIdentifiableType(typeId) is not { } type ||
      Graph.GetMetadata(type) is not IIdentifiableTypeMetadata metadata
    ) {
      throw new JsonException(
        $"The type `{typeToConvert}` has an unknown identifiable type id " +
        $"`{typeId}`."
      );
    }

    if (metadata is not IConcreteTypeMetadata concreteTypeMetadata) {
      throw new JsonException(
        $"The type `{typeToConvert}` with id `{typeId}` is not a concrete " +
        "(non-abstract) identifiable type."
      );
    }

    // Get all serializable properties, including those from base types.
    var properties = Graph.GetProperties(type);

    // Create an instance of the type using the generated factory.
    var value = concreteTypeMetadata.Factory();

    foreach (var property in properties) {
      if (GetPropertyId(property) is not { } propertyId) {
        // Only read properties marked with the [Save] attribute.
        continue;
      }

      // If the property is a collection type, we need to make sure we've
      // cached the closed type of the collection type (recursively) before
      // trying to serialize it.
      Serializer.IdentifyCollectionTypes(
        property.GenericType,
        options.TypeInfoResolver,
        options
      );

      var propertyJson = json[propertyId];

      // We are a forgiving lord: we don't require properties to be present.
      if (propertyJson is null) { continue; }

      var propertyValue = JsonSerializer.Deserialize(
        propertyJson,
        property.GenericType.ClosedType,
        options
      );

      // This can happen if a property on a model has been removed since
      // the last time the model was serialized. We don't mind that, either.
      if (property.Setter is not { } propertySetter) { continue; }

      propertySetter(value, propertyValue);
    }

    return value;
  }

  internal static string? GetPropertyId(PropertyMetadata property) =>
    property
      .Attributes
      .TryGetValue(typeof(SaveAttribute), out var saveAttributes) &&
    saveAttributes is { Length: > 0 } &&
    saveAttributes[0] is SaveAttribute saveAttribute
      ? saveAttribute.Id
      : null;
}
