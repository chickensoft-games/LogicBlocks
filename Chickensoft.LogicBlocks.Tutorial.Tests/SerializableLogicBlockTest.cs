namespace Chickensoft.LogicBlocks.Tutorial.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
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
    var logic = new SerializableLogicBlock();

    var jsonText = JsonSerializer.Serialize(logic, _options);
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
    logic.Value.ShouldBeOfType<SerializableLogicBlock.State.PoweredOff>();
  }
}
