namespace Chickensoft.LogicBlocks.Tutorial.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Chickensoft.Serialization;
using Shouldly;
using Xunit;

public class SerializableLogicBlockTest {
  [Fact]
  public void Serializes() {
    var options = new JsonSerializerOptions {
      WriteIndented = true,
      // Use the type resolver and converter from the
      // Chickensoft.Serialization package. You can combine these with other
      // type resolvers and converters.
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter() }
    };

    var logic = new SerializableLogicBlock();

    var jsonText = JsonSerializer.Serialize(logic, options);
    var jsonNode = JsonNode.Parse(jsonText);

    var jsonExpectedText = /*language=json*/
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
    var jsonExpectedNode = JsonNode.Parse(jsonExpectedText);

    JsonNode.DeepEquals(jsonNode, jsonExpectedNode).ShouldBeTrue();
  }

  [Fact]
  public void Deserializes() {
    var options = new JsonSerializerOptions {
      // Use the type resolver and converter from the
      // Chickensoft.Serialization package. You can combine these with other
      // type resolvers and converters.
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new SerializableTypeConverter() }
    };

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
      json, options
    );

    logic.ShouldNotBeNull();
    logic.Value.ShouldBeOfType<SerializableLogicBlock.State.PoweredOff>();
  }
}
