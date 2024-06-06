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
    ) ?? throw new JsonException(
      $"Failed to deserialize blackboard for logic block {metadata.Id}."
    );

    // Rather than replace our blackboard, we simply overwrite any values that
    // we found in the deserialized blackboard. Preserving our blackboard
    // enables us to respect new persisted values or non-persisted values that
    // have already been added to the blackboard during construction.
    foreach (var objType in blackboard.Types) {
      _blackboard.OverwriteObject(objType, blackboard.GetObject(objType));
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
    var state = _blackboard.GetObject(stateType);

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
    var stateMetadata = graph.GetMetadata(stateType);

    if (stateMetadata is not IdentifiableTypeMetadata stateIdMetadata) {
      throw new JsonException(
        $"Logic block `{GetType()}` with id `{metadata.Id}` has an unknown " +
        $"identifiable state `{stateType}`."
      );
    }

    stateJson[Serializer.TYPE_PROPERTY] = stateIdMetadata.Id;
    stateJson[Serializer.VERSION_PROPERTY] = stateIdMetadata.Version;

    json[STATE_PROPERTY] = stateJson;

    json[BLACKBOARD_PROPERTY] = JsonSerializer.SerializeToNode(
      _blackboard,
      options
    );
  }
}
