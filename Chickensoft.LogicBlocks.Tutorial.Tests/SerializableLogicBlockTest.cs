namespace Chickensoft.LogicBlocks.Tutorial.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Auto;
using Chickensoft.Serialization;
using Shouldly;
using Xunit;

public class SerializableLogicBlockTest
{
  private readonly JsonSerializerOptions _options = new()
  {
    WriteIndented = true,
    // Use the type resolver and converter from the
    // Chickensoft.Serialization package. You can combine these with other
    // type resolvers and converters.
    TypeInfoResolver = new SerializableTypeResolver(),
    Converters = { new SerializableTypeConverter() }
  };

  [Fact]
  public void Serializes()
  {
    LogicBlockSerialization.Setup();

    var logic = new SerializableLogicBlock();
    logic.Start<TimerState.PoweredOff>();

    var saveData = logic.GetSaveData().ShouldBeOfType<SerializableLogicBlockSaveData>();
    var jsonText = JsonSerializer.Serialize(saveData, _options);
    var jsonNode = JsonNode.Parse(jsonText);

    var jsonExpectedText = /*language=json*/
      """
      {
        "$type": "serializable_logic_block_save_data",
        "$v": 1,
        "data": {
          "$v": 6,
          "$type": "$lb",
          "state": "serializable_logic_state_off",
          "blackboard": {
            "$type": "blackboard",
            "$v": 1,
            "values": {}
          },
          "history": []
        }
      }
      """;
    var jsonExpectedNode = JsonNode.Parse(jsonExpectedText);

    JsonNode.DeepEquals(jsonNode, jsonExpectedNode).ShouldBeTrue();
  }

  [Fact]
  public void Deserializes()
  {
    var json =
      /*language=json*/
      """
      {
        "$type": "serializable_logic",
        "$v": 1,
        "state": {
          "$type": "serializable_logic_state_off",
          "$v": 1
        },
        "blackboard": {
          "$type": "blackboard",
          "$v": 1,
          "values": {}
        }
      }
      """;

    var logic = JsonSerializer.Deserialize<SerializableLogicBlock>(
      json, _options
    );

    logic.ShouldNotBeNull();

    logic.Start<TimerState.PoweredOff>();

    logic.State.ShouldBeOfType<TimerState.PoweredOff>();
  }
}
