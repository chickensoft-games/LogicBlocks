namespace Chickensoft.LogicBlocks.Auto;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Introspection;
using Serialization;

/// <summary>
/// JSON converter for <see cref="LogicBlockData"/> that handles serialization
/// and deserialization of logic block state, blackboard, and history.
/// </summary>
public class LogicBlockDataConverter : JsonConverter<LogicBlockData>
{
  /// <summary>Json property name for the state.</summary>
  public const string STATE_PROPERTY = "state";

  /// <summary>Json property name for the blackboard.</summary>
  public const string BLACKBOARD_PROPERTY = "blackboard";

  /// <summary>Json property name for the logic block's state history.</summary>
  public const string HISTORY_PROPERTY = "history";

  /// <summary>Current serialization format version for logic block data.</summary>
  public const int LOGIC_BLOCK_VERSION = 6;

  /// <inheritdoc/>
  public override bool CanConvert(Type typeToConvert) =>
    typeToConvert == typeof(LogicBlockData);

  /// <inheritdoc/>
  [UnconditionalSuppressMessage(
    "AOT",
    "IL3050:RequiresDynamicCode",
    Justification = "Chickensoft introspection & serialization system " +
                    "ensures compatible types are serializable."
  )]
  [UnconditionalSuppressMessage(
    "AOT",
    "IL2026:RequiresUnreferencedCodeAttribute",
    Justification = "Chickensoft introspection & serialization system " +
                    "ensures compatible types are preserved against trimming."
  )]
  public override void Write(
    Utf8JsonWriter writer,
    LogicBlockData value,
    JsonSerializerOptions options
  )
  {
    // Serializable logic blocks validate that concrete states are identifiable
    // during preallocation.
    var stateMetadata =
      Types.Graph.GetMetadata(value.StateType) as IdentifiableTypeMetadata ??
      throw new JsonException(
        $"State type `{value.StateType}` is not an identifiable " +
        "introspective type."
      );

    var json = new JsonObject
    {
      [Serializer.VERSION_PROPERTY] = LOGIC_BLOCK_VERSION,
      [Serializer.TYPE_PROPERTY] = "$lb",
      [STATE_PROPERTY] = stateMetadata.Id,
      [BLACKBOARD_PROPERTY] = JsonSerializer.SerializeToNode(
        value.Blackboard,
        value.Blackboard.GetType(),
        options
      ),
      [HISTORY_PROPERTY] = new JsonArray(
        [
          ..value.History
            .Select(type => Types.Graph.GetMetadata(type) ??
              throw new JsonException(
                $"Logic block {stateMetadata.Id} has an unknown " +
                $"history state type `{type}` in its history."
              ))
            .Cast<IdentifiableTypeMetadata>()
            .Select(metadata => JsonSerializer.SerializeToNode(metadata.Id))
        ]
      ),
    };

    json.WriteTo(writer, options);
  }


  /// <inheritdoc/>
  [UnconditionalSuppressMessage(
    "AOT",
    "IL3050:RequiresDynamicCode",
    Justification = "Chickensoft introspection & serialization system " +
                    "ensures compatible types are serializable."
  )]
  [UnconditionalSuppressMessage(
    "AOT",
    "IL2026:RequiresUnreferencedCodeAttribute",
    Justification = "Chickensoft introspection & serialization system " +
                    "ensures compatible types are preserved against trimming."
  )]
  public override LogicBlockData? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    var json = JsonNode.Parse(ref reader)?.AsObject() ??
      throw new JsonException(
        "Logic block data must be a JSON object."
      );

    var version = json[Serializer.VERSION_PROPERTY]?.GetValue<int>() ??
                  throw new JsonException(
                    "Logic block data must have a version property.");

    if (LOGIC_BLOCK_VERSION < version)
    {
      throw new JsonException(
        $"Cannot deserialize a logic block version `{version}`  that is " +
        $"newer than the current version `{LOGIC_BLOCK_VERSION}`. Check that " +
        "your save data was generated correctly and that you are using the " +
        "appropriate Chickensoft.LogicBlocks versions."
      );
    }

    var stateId = json[STATE_PROPERTY]?.GetValue<string>() ??
                  throw new JsonException(
                    "Logic block data must have a state property."
                  );

    var stateType = Types.Graph.GetIdentifiableType(stateId) ??
                    throw new JsonException(
                      $"Logic block data state type `{stateId}` is not a known " +
                      "identifiable type."
                    );

    var blackboardJson = json[BLACKBOARD_PROPERTY]?.AsObject() ??
                         throw new JsonException(
                           "Logic block data must have a blackboard property."
                         );

    var blackboard = JsonSerializer.Deserialize<SerializableBlackboard>(
      blackboardJson,
      options
    )!;

    var historyJson = json[HISTORY_PROPERTY]?.AsArray();
    var history = new History();

    if (historyJson is { } historyArray)
    {
      foreach (var historyItem in historyArray)
      {
        if (historyItem is not JsonValue historyValue)
        {
          throw new JsonException(
            "Logic block history must be an array of strings."
          );
        }

        var typeId = historyValue.GetValue<string>();

        var type = Types.Graph.GetIdentifiableType(typeId) ??
          throw new JsonException(
            $"Logic block history item `{typeId}` is not a known type."
          );

        // Ensure the type exists in the blackboard
        if (!blackboard.HasObject(type))
        {
          var metadata = (IConcreteTypeMetadata)Types.Graph.GetMetadata(type)!;
          var state = (LogicBlockState)metadata.Factory();
          blackboard.OverwriteObject(type, state);
        }

        history.Push(type);
      }
    }

    return new LogicBlockData(stateType, blackboard, history);
  }
}
