namespace Chickensoft.LogicBlocks.Tests.Serialization;

using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.Serialization;
using Shouldly;
using Xunit;

public partial class LogicBlockSerializationTest {
  [Meta, Id("a")]
  public partial record A {
    [Save("a_value")]
    public string AValue { get; set; } = "";
  }

  [Meta, Id("b")]
  public partial record B {
    [Save("b_value")]
    public string BValue { get; set; } = "";
  }

  [Fact]
  public void SerializesLogicBlock() {
    var logic = new SerializableLogicBlock();
    logic.Start();

    logic.Save(() => new A { AValue = "a" });
    logic.Save(() => new B { BValue = "b" });

    var options = CreateOptions();

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
          "values": {
            "a": {
              "$type": "a",
              "$v": 1,
              "a_value": "a"
            },
            "b": {
              "$type": "b",
              "$v": 1,
              "b_value": "b"
            }
          }
        }
      }
      """
    );
  }

  [Fact]
  public void DeserializesLogicBlock() {
    var options = CreateOptions();

    var logic =
      JsonSerializer.Deserialize<SerializableLogicBlock>(
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
            "values": {
              "a": {
                "$type": "a",
                "$v": 1,
                "a_value": "a"
              },
              "b": {
                "$type": "b",
                "$v": 1,
                "b_value": "b"
              }
            }
          }
        }
        """,
        options
    );

    logic.ShouldNotBeNull();

    var aEquivalent = new A { AValue = "a" };
    var bEquivalent = new B { BValue = "b" };

    logic.Get<A>().ShouldBe(aEquivalent);
    logic.Get<B>().ShouldBe(bEquivalent);
  }

  [Fact]
  public void SerializesLogicBlockWithNestedLogicBlocks() {
    var options = CreateOptions();

    var logic = new SerializableParallelLogicBlock();
    logic.Input(new SerializableParallelLogicBlock.Input.GoToParallelState());

    var json = JsonSerializer.Serialize(logic, options);

    // States are only saved if they are not equivalent to the
    // reference states cached for that logic block type. So, even
    // though this state contains nested logic blocks, it is not serialized
    // since logic blocks also implement equality checking.
    json.ShouldBe(/*lang=json,strict*/
      """
      {
        "$type": "serializable_parallel_logic_block",
        "$v": 1,
        "state": {
          "$type": "serializable_parallel_logic_block_state_parallel",
          "$v": 1
        },
        "blackboard": {
          "$type": "blackboard",
          "$v": 1,
          "values": {}
        }
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var deserializedLogic =
      JsonSerializer.Deserialize<SerializableParallelLogicBlock>(
        json,
        options
      );

    deserializedLogic.ShouldNotBeNull();

    var parallelState = deserializedLogic
      .Value.ShouldBeOfType<SerializableParallelLogicBlock.ParallelState>();

    parallelState.StateA.ShouldNotBeNull();
    parallelState.StateB.ShouldNotBeNull();
  }

  [Fact]
  public void ThrowsWhenCannotGetStateProperty() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        """
        {
          "$type": "serializable_logic_block"
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenCannotGetStateTypeDiscriminator() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        """
        {
          "$type": "serializable_logic_block",
          "state": {}
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenCannotGetBlackboardProperty() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        """
        {
          "$type": "serializable_logic_block",
          "state": "serializable_logic_block_state"
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenLogicBlockHasUnknownIntrospectiveType() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        """
        {
          "$type": "unknown_logic_block",
          "state": "serializable_logic_block_state",
          "blackboard": {}
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenLogicBlockStateHasUnknownIntrospectiveType() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        """
        {
          "$type": "serializable_logic_block",
          "state": "unknown_logic_block_state",
          "blackboard": {}
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenLogicBlockHasUnknownIntrospectiveTypeInBlackboard() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        """
        {
          "$type": "serializable_logic_block",
          "state": "serializable_logic_block_state",
          "blackboard": {
            "unknown": {}
          }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenBlackboardObjIsNull() {
    // Blackboards only contain non-null values.
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "state": "serializable_logic_block_state",
          "blackboard": {
            "a": null
          }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenStateIsNull() {
    // States are always non-null.
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "state": null,
          "blackboard": { }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenStateHasUnknownType() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "$v": 1,
          "state": {
            "$type": "unknown_state_id",
            "$v": 1
          },
          "blackboard": {
            "$type": "blackboard",
            "$v": 1,
            "values": {}
          }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenStateJsonIsNull() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "$v": 1,
          "state": null,
          "blackboard": {
            "$type": "blackboard",
            "$v": 1,
            "values": {}
          }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenStateIdIsNull() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "$v": 1,
          "state": {
            "$type": null,
            "$v": 1
          },
          "blackboard": {
            "$type": "blackboard",
            "$v": 1,
            "values": {}
          }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenStateVersionIsNull() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "$v": 1,
          "state": {
            "$type": "serializable_logic_block_state",
            "$v": null
          },
          "blackboard": {
            "$type": "blackboard",
            "values": {}
          }
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenBlackboardJsonIsNull() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "$v": 1,
          "state": {
            "$type": "serializable_logic_block_state",
            "$v": 1
          },
          "blackboard": null
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void ThrowsWhenBlackboardDeserializationFails() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        """
        {
          "$type": "serializable_logic_block",
          "$v": 1,
          "state": {
            "$type": "serializable_logic_block_state",
            "$v": 1
          },
          "blackboard": {}
        }
        """,
        options
      )
    );
  }

  [Fact]
  public void UpgradesOutdatedStates() {
    // Pass it some upgrade dependencies.
    var deps = new Blackboard();
    deps.Set("string value");

    var options = CreateOptions(deps);

    // This logic block's start state is v1
    var logic = new OutdatedLogicBlock();
    logic.Start();
    var json = JsonSerializer.Serialize(logic, options);

    var deserializedLogic =
      JsonSerializer.Deserialize<OutdatedLogicBlock>(json, options);

    deserializedLogic.ShouldNotBeNull();

    // Make sure we upgraded from v1 -> v2 -> v3
    deserializedLogic.Value.ShouldBeOfType<OutdatedLogicBlock.V3>();
  }

  private static JsonSerializerOptions CreateOptions(
    IReadOnlyBlackboard? deps = null
  ) {
    deps ??= new Blackboard();
    return new JsonSerializerOptions {
      Converters = {
        new SerializableTypeConverter(deps),
      },
      TypeInfoResolver = new SerializableTypeResolver(),
      WriteIndented = true
    };
  }
}
