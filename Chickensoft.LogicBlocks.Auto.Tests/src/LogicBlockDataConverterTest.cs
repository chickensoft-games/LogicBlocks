namespace Chickensoft.LogicBlocks.Auto.Tests;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Collections;
using Fixtures;
using Serialization;
using Shouldly;

// Minimal concrete subclass for tests that need a non-identifiable state.
public record UnidentifiableState : LogicBlockState;

public class LogicBlockDataConverterTest
{
  // Private so the introspection generator won't register it, causing
  // GetMetadata to return null.
  private sealed record UnknownState : LogicBlockState;

  private static JsonSerializerOptions Options => new()
  {
    WriteIndented = true,
    TypeInfoResolver = new SerializableTypeResolver(),
    Converters =
    {
      new SerializableTypeConverter(new Blackboard()),
      new LogicBlockDataConverter()
    }
  };

  [Fact]
  public void ConvertsLogicBlockData()
  {
    var converter = new LogicBlockDataConverter();

    converter.CanConvert(typeof(LogicBlockData)).ShouldBeTrue();
    converter.CanConvert(typeof(string)).ShouldBeFalse();
  }

  [Fact]
  public void SerializesAndDeserializes()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();
    var data = logic.GetData();

    var json = JsonSerializer.Serialize(data, options);
    var deserialized = JsonSerializer.Deserialize<LogicBlockData>(json, options);

    deserialized.ShouldNotBeNull();
    deserialized.StateType.ShouldBe(data.StateType);
  }

  [Fact]
  public void SerializationAndDeserializationRespectsHistoryOrder()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();

    // Push oldest first, then newest — deque is [State, OtherState]
    logic.History.Push(typeof(SerializableBlockState));
    logic.History.Push(typeof(SerializableBlockState.OtherState));
    var data = logic.GetData();

    var json = JsonSerializer.Serialize(data, options);
    var deserialized = JsonSerializer.Deserialize<LogicBlockData>(json, options);

    deserialized.ShouldNotBeNull();
    deserialized.History.Count.ShouldBe(2);

    var historyArray = deserialized.History.ToArray();
    historyArray[0].ShouldBe(typeof(SerializableBlockState));
    historyArray[1].ShouldBe(typeof(SerializableBlockState.OtherState));
  }

  [Fact]
  public void FullRoundtripRestoresHistoryInCorrectOrder()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    // Start a logic block and push history entries
    using var original = new SerializableBlock();
    original.Start<SerializableBlockState>();
    original.History.Push(typeof(SerializableBlockState));
    original.History.Push(typeof(SerializableBlockState.OtherState));

    // Save → serialize → deserialize → load into a new logic block
    var data = original.GetData();
    var json = JsonSerializer.Serialize(data, options);
    var deserialized = JsonSerializer.Deserialize<LogicBlockData>(json, options)!;

    using var restored = new SerializableBlock();
    restored.Start(deserialized);

    // Verify history was restored in the correct order
    restored.History.Count.ShouldBe(2);
    var restoredHistory = restored.History.ToArray();
    restoredHistory[0].ShouldBe(typeof(SerializableBlockState));
    restoredHistory[1].ShouldBe(typeof(SerializableBlockState.OtherState));

    // Pop should return newest first (LIFO)
    restored.History.Pop().ShouldBe(typeof(SerializableBlockState.OtherState));
    restored.History.Pop().ShouldBe(typeof(SerializableBlockState));
  }

  [Fact]
  public void WriteThrowsForUnidentifiableState()
  {
    var options = Options;
    var bb = new Blackboard();
    var data = new LogicBlockData(typeof(UnidentifiableState), bb, new History());

    Should.Throw<JsonException>(() => JsonSerializer.Serialize(data, options));
  }

  [Fact]
  public void ReadThrowsForNullJson()
  {
    var options = Options;
    var converter = new LogicBlockDataConverter();

    Should.Throw<JsonException>(
      () =>
      {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));
        converter.Read(ref reader, typeof(LogicBlockData), options);
      }
    );
  }

  [Fact]
  public void ReadThrowsForMissingVersion()
  {
    var options = Options;
    var json = """{"$type":"$lb","state":"foo","blackboard":{},"history":[]}""";

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(json, options)
    );
  }

  [Fact]
  public void ReadThrowsForNewerVersion()
  {
    var options = Options;
    var json = """{"$v":999,"$type":"$lb","state":"foo","blackboard":{},"history":[]}""";

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(json, options)
    );
  }

  [Fact]
  public void ReadThrowsForMissingState()
  {
    var options = Options;
    var json = """{"$v":6,"$type":"$lb","blackboard":{},"history":[]}""";

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(json, options)
    );
  }

  [Fact]
  public void ReadThrowsForUnknownStateId()
  {
    var options = Options;
    var json = """{"$v":6,"$type":"$lb","state":"bogus_id","blackboard":{},"history":[]}""";

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(json, options)
    );
  }

  [Fact]
  public void ReadThrowsForMissingBlackboard()
  {
    var options = Options;
    var json = """{"$v":6,"$type":"$lb","state":"serializable_block_state","history":[]}""";

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(json, options)
    );
  }

  [Fact]
  public void ReadThrowsForInvalidHistoryItem()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    // Serialize a valid block first to get a proper blackboard
    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();
    var data = logic.GetData();
    var json = JsonSerializer.Serialize(data, options);

    // Corrupt the history to contain a non-string
    var parsed = JsonNode.Parse(json)!.AsObject();
    parsed["history"] = new JsonArray(new JsonObject());
    var corrupted = parsed.ToJsonString(options);

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(corrupted, options)
    );
  }

  [Fact]
  public void ReadUsesBlackboardStateForHistory()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    // Craft JSON where the blackboard values include the history state.
    var json = """
    {
      "$v": 6,
      "$type": "$lb",
      "state": "serializable_block_state",
      "blackboard": {
        "$type": "blackboard",
        "$v": 1,
        "values": {
          "serializable_block_other_state": {
            "$type": "serializable_block_other_state",
            "$v": 1
          }
        }
      },
      "history": ["serializable_block_other_state"]
    }
    """;

    var deserialized = JsonSerializer.Deserialize<LogicBlockData>(json, options);

    deserialized.ShouldNotBeNull();
    deserialized.History.Count.ShouldBe(1);
  }

  [Fact]
  public void ReadUsesReferenceStateForHistoryWhenNotSerialized()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    // History references a state that is NOT in the blackboard values.
    // The deserializer should resolve it from the preallocated reference state.
    var json = """
    {
      "$v": 6,
      "$type": "$lb",
      "state": "serializable_block_state",
      "blackboard": {
        "$type": "blackboard",
        "$v": 1,
        "values": {}
      },
      "history": ["serializable_block_state"]
    }
    """;

    var deserialized = JsonSerializer.Deserialize<LogicBlockData>(json, options);

    deserialized.ShouldNotBeNull();
    deserialized.History.Count.ShouldBe(1);
    deserialized.History.ToArray()[0].ShouldBe(typeof(SerializableBlockState));
  }

  [Fact]
  public void WriteThrowsForUnknownHistoryStateType()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();
    // Push a state type not in the type graph.
    logic.History.Push(typeof(UnknownState));
    var data = logic.GetData();

    Should.Throw<JsonException>(
      () => JsonSerializer.Serialize(data, options)
    );
  }

  [Fact]
  public void ReadSucceedsWithMissingHistory()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    var json = """
    {
      "$v": 6,
      "$type": "$lb",
      "state": "serializable_block_state",
      "blackboard": {
        "$type": "blackboard",
        "$v": 1,
        "values": {}
      }
    }
    """;

    var deserialized = JsonSerializer.Deserialize<LogicBlockData>(json, options);

    deserialized.ShouldNotBeNull();
    deserialized.History.Count.ShouldBe(0);
  }

  [Fact]
  public void ReadThrowsForNullHistoryItem()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();
    var data = logic.GetData();
    var json = JsonSerializer.Serialize(data, options);

    var parsed = JsonNode.Parse(json)!.AsObject();
    parsed["history"] = new JsonArray(JsonValue.Create<string?>(null));
    var corrupted = parsed.ToJsonString(options);

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(corrupted, options)
    );
  }

  [Fact]
  public void ReadThrowsForAbstractHistoryStateType()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();
    var data = logic.GetData();
    var json = JsonSerializer.Serialize(data, options);

    var parsed = JsonNode.Parse(json)!.AsObject();
    parsed["history"] = new JsonArray("abstract_state");
    var corrupted = parsed.ToJsonString(options);

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(corrupted, options)
    );
  }

  [Fact]
  public void ReadThrowsForUnknownHistoryId()
  {
    LogicBlockSerialization.Setup();
    var options = Options;

    using var logic = new SerializableBlock();
    logic.Start<SerializableBlockState>();
    var data = logic.GetData();
    var json = JsonSerializer.Serialize(data, options);

    var parsed = JsonNode.Parse(json)!.AsObject();
    parsed["history"] = new JsonArray("bogus_history_id");
    var corrupted = parsed.ToJsonString(options);

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<LogicBlockData>(corrupted, options)
    );
  }
}
