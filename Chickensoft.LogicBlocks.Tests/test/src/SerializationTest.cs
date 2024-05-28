namespace Chickensoft.LogicBlocks.Tests;

using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.Serialization;
using Shouldly;
using Xunit;

public class SerializationTest {
  [Fact]
  public void SerializesAndDeserializes() {
    var logic = new SerializableLogicBlock();

    var options = new JsonSerializerOptions() {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new IdentifiableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(logic, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "serializable_logic_block",
        "$v": 1,
        "state": {
          "$type": "serializable_logic_block_state",
          "$v": 1
        },
        "blackboard": {
          "$type": "blackboard",
          "$v": 1,
          "values": {}
        }
      }
      """,
      StringCompareShould.IgnoreLineEndings
    );

    var deserialized =
      JsonSerializer.Deserialize<SerializableLogicBlock>(json, options);

    deserialized.ShouldNotBeNull();

    deserialized.Value.ShouldBe(logic.Value);
  }
}
