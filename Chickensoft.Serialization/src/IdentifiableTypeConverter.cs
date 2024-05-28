namespace Chickensoft.Serialization;

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using System.Collections.Generic;

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

  private string TypeDiscriminator => Serializer.TYPE_PROPERTY;
  private string VersionDiscriminator => Serializer.VERSION_PROPERTY;

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
    Graph.GetMetadata(typeToConvert) is IIntrospectiveTypeMetadata;

  /// <inheritdoc />
  public override object? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  ) {
    var json = JsonNode.Parse(ref reader)?.AsObject() ?? throw new JsonException(
      $"Failed to parse JSON for introspective type {typeToConvert}."
    );

    var typeId =
      json[TypeDiscriminator]?.ToString() ?? throw new JsonException(
        $"Type {typeToConvert} is missing the `{TypeDiscriminator}` type " +
        "discriminator."
      );

    var version =
      json[VersionDiscriminator]?.GetValue<int>() ?? throw new JsonException(
        $"Type {typeToConvert} is missing the `{VersionDiscriminator}` " +
        "version discriminator."
      );

    if (
      Graph.GetIdentifiableType(typeId, version) is not { } type ||
      Graph.GetMetadata(type) is not IIdentifiableTypeMetadata metadata
    ) {
      throw new JsonException(
        $"The type `{typeToConvert}` has an unknown identifiable type: " +
        $"id = {typeId}, version = {version}."
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

    var hasInitProps = metadata.Metatype.HasInitProperties;

    // Create an instance of the type using the generated factory if
    // it does not have init props.
    var value = hasInitProps ? null : concreteTypeMetadata.Factory();

    var initProps = hasInitProps ? new Dictionary<string, object?>() : null;

    foreach (var property in properties) {
      if (GetPropertyId(property) is not { } propertyId) {
        // Only read properties marked with the [Save] attribute.
        continue;
      }

      // This can happen if a property has been made read-only since
      // the last time the model was serialized. We don't mind that.
      if (property.Setter is not { } propertySetter) { continue; }

      // If the property is a collection type, we need to make sure we've
      // cached the closed type of the collection type (recursively) before
      // trying to deserialize it.
      Serializer.IdentifyCollectionTypes(
        property.GenericType,
        options.TypeInfoResolver,
        options
      );

      var isPresentInJson = json.ContainsKey(propertyId);
      var propertyValueJsonNode = isPresentInJson ? json[propertyId] : null;

      object? propertyValue = null;

      if (isPresentInJson) {
        propertyValue = JsonSerializer.Deserialize(
          propertyValueJsonNode,
          property.GenericType.ClosedType,
          options
        );
      }

      if (
        !isPresentInJson &&
        propertyValue is null &&
        IsCollection(property.GenericType.OpenType)
      ) {
        // The value of this collection property is missing from the json.
        // In this scenario, we actually prefer an empty collection. We only
        // deserialize a collection to null if it doesn't have a setter or
        // if it's present in the json *and* explicitly set to null.
        //
        // We know we've discovered the collection type already, so it will
        // have type info. Also, we expect the type resolver to exist and be
        // a SerializableTypeResolver that provides our cached collection type
        // info.
        var typeInfo =
          options
            .TypeInfoResolver!
            .GetTypeInfo(property.GenericType.ClosedType, options)!;

        // Our type resolver companion will have cached the closed type of
        // the collection type by using the callbacks provided in the generated
        // introspection data, which is AOT-friendly :D
        propertyValue = typeInfo.CreateObject!();
      }

      if (hasInitProps) {
        // We'll construct the object later.
        initProps!.Add(property.Name, propertyValue);
      }
      else {
        // We can set the property right away.
        propertySetter(value!, propertyValue);
      }
    }

    // We have to use the generated metatype method to construct objects with
    // init properties.
    if (hasInitProps) {
      value = metadata.Metatype.Construct(initProps);
    }

    // Upgrade the deserialized object as needed.
    while (value is IOutdated outdated) {
      value = outdated.Upgrade(DependenciesBlackboard);
    }

    // At this point, we've successfully deserialized a type and its properties.
    // If the type implements ISerializationAware, we'll call the OnDeserialized
    // method to allow it to modify itself (or replace itself altogether) based
    // on the json object data.
    if (value is ISerializationAware aware) {
      // We know the type must be concrete and identifiable at this point.
      var identifiableMetadata = (IdentifiableTypeMetadata)metadata;
      value = aware.OnDeserialized(identifiableMetadata, json, options);
    }

    return value;
  }

  /// <inheritdoc />
  public override void Write(
    Utf8JsonWriter writer,
    object value,
    JsonSerializerOptions options
  ) {
    var type = value.GetType();

    var json = new JsonObject();

    var metadata = Graph.GetMetadata(type);

    if (
      metadata is not IIdentifiableTypeMetadata idMetadata ||
      metadata is not IConcreteIntrospectiveTypeMetadata concreteMetadata
    ) {
      throw new JsonException(
        $"The type `{type}` is not an identifiable introspective type."
      );
    }

    var typeId = idMetadata.Id;
    var version = concreteMetadata.Version;

    json[TypeDiscriminator] = typeId;
    json[VersionDiscriminator] = version;

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

      var propertyValue = property.Getter(value);
      var propertyType = property.GenericType.ClosedType;

      json[propertyId] = JsonSerializer.SerializeToNode(
        value: propertyValue,
        inputType: propertyType,
        options: options
      );
    }

    // We've constructed the json data and we're about to write it to the
    // Utf8JsonWriter. If the type implements ISerializationAware, we'll call
    // the OnSerialized method to allow it to modify the json object data
    // before we actually output it.
    if (
      value is ISerializationAware aware &&
      metadata is IdentifiableTypeMetadata identifiableTypeMetadata
    ) {
      aware.OnSerialized(identifiableTypeMetadata, json, options);
    }

    json.WriteTo(writer);
  }

  internal static string? GetPropertyId(PropertyMetadata property) =>
    property
      .Attributes
      .TryGetValue(typeof(SaveAttribute), out var saveAttributes) &&
    saveAttributes is { Length: > 0 } &&
    saveAttributes[0] is SaveAttribute saveAttribute
      ? saveAttribute.Id
      : null;

  internal static bool IsCollection(Type openType) =>
    openType == typeof(List<>) ||
    openType == typeof(HashSet<>) ||
    openType == typeof(Dictionary<,>);
}
