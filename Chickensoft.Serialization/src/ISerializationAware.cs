namespace Chickensoft.Serialization;

using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Introspection;

/// <summary>
/// Interface for identifiable types that wish to customize serialization and
/// deserialization.
/// </summary>
public interface ISerializationAware {
  /// <summary>
  /// Invoked immediately after the object has been deserialized. This provides
  /// the object with a chance to read the actual json data and perform any
  /// internal initialization that might be needed, as well as a chance to
  /// return an entirely different object (if desired).
  /// </summary>
  /// <param name="metadata">Generated metadata for the type.</param>
  /// <param name="json">Json object node.</param>
  /// <param name="options">Json serialization options.</param>
  /// <returns></returns>
  object OnDeserialized(
    IdentifiableTypeMetadata metadata,
    JsonObject json,
    JsonSerializerOptions options
  );

  /// <summary>
  /// Invoked immediately before the object is serialized. This provides the
  /// object with a chance to write or mutate additional data on the json object
  /// node.
  /// </summary>
  /// <param name="metadata">Generated metadata for the type.</param>
  /// <param name="json">Json object node.</param>
  /// <param name="options">Json serialization options.</param>
  void OnSerialized(
    IdentifiableTypeMetadata metadata,
    JsonObject json,
    JsonSerializerOptions options
  );
}
