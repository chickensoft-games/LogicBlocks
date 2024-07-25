namespace Chickensoft.LogicBlocks;

using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Serialization;
using Chickensoft.Introspection;

public partial class LogicBlock<TState> : ICustomSerializable {
  /// <summary>Json property name for the state.</summary>
  public const string STATE_PROPERTY = "state";
  /// <summary>Json property name for the blackboard.</summary>
  public const string BLACKBOARD_PROPERTY = "blackboard";

  /// <inheritdoc />
  public object OnDeserialized(
    IdentifiableTypeMetadata metadata,
    JsonObject json,
    JsonSerializerOptions options
  ) {
    var graph = Introspection.Types.Graph;

    var type = GetType();

    var stateJson = json[STATE_PROPERTY]?.AsObject() ??
      throw new JsonException(
        $"Logic block `{metadata.Id}` is missing the `{STATE_PROPERTY}` " +
        "property."
      );

    var stateId =
      stateJson[Serializer.TYPE_PROPERTY]?.ToString() ??
      throw new JsonException(
        $"Logic block `{metadata.Id}` is missing the state type."
      );

    var stateVersion =
      stateJson[Serializer.VERSION_PROPERTY]?.GetValue<int>() ??
      throw new JsonException(
        $"Logic block `{metadata.Id}` is missing the state version."
      );

    var blackboardJson =
      json[BLACKBOARD_PROPERTY] ?? throw new JsonException(
        $"Logic block `{metadata.Id}` is missing the `{BLACKBOARD_PROPERTY}` " +
        "property."
      );

    var blackboard = JsonSerializer.Deserialize<SerializableBlackboard>(
      blackboardJson,
      options
    )!; // Blackboard deserialization will throw on its own if invalid.

    // Rather than replace our blackboard, we simply overwrite any values that
    // we found in the deserialized blackboard. Preserving our blackboard
    // enables us to respect new persisted values or non-persisted values that
    // have already been added to the blackboard during construction.
    foreach (var objType in blackboard.Types) {
      Blackboard.OverwriteObject(objType, blackboard.GetObject(objType));
    }

    if (
      graph.GetIdentifiableType(stateId) is not { } stateType ||
      graph.GetMetadata(stateType) is not IIdentifiableTypeMetadata
    ) {
      throw new JsonException(
        $"Logic block {metadata.Id} has an unknown identifiable state " +
        $"type id `{stateId}`."
      );
    }

    // Load the state from the logic block's blackboard, since we have
    // preallocated states during construction, and deserialization has
    // overwritten any preallocated states with the deserialized state.
    var state = Blackboard.GetObject(stateType);

    // Set the state to be used (instead of the logic block's initial state)
    // whenever the logic block is started.
    RestoreState(state);

    return this;
  }

  /// <inheritdoc />
  public void OnSerialized(
    IdentifiableTypeMetadata metadata,
    JsonObject json,
    JsonSerializerOptions options
  ) {
    var graph = Introspection.Types.Graph;

    var stateJson = new JsonObject();

    var stateType = Value.GetType();

    // Serializable logic blocks validate that concrete states are identifiable
    // during preallocation.
    var stateMetadata = (IdentifiableTypeMetadata)graph.GetMetadata(stateType)!;

    stateJson[Serializer.TYPE_PROPERTY] = stateMetadata.Id;
    stateJson[Serializer.VERSION_PROPERTY] = stateMetadata.Version;

    json[STATE_PROPERTY] = stateJson;

    json[BLACKBOARD_PROPERTY] = JsonSerializer.SerializeToNode(
      Blackboard,
      options
    );
  }
}
