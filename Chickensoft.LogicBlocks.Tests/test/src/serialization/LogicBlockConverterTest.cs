namespace Chickensoft.LogicBlocks.Tests.Serialization;

using System;
using System.Text;
using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks.Serialization;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.Serialization;
using Moq;
using Shouldly;
using Xunit;

public partial class LogicBlockConverterTest {
  private const string LOGIC_BLOCK_JSON = /*lang=json,strict*/
    """
    {
      "$type": "serializable_logic_block",
      "state": "serializable_logic_block_state",
      "blackboard": {
        "a": {
          "a_value": "a"
        },
        "b": {
          "b_value": "b"
        },
        "serializable_logic_block_state": {}
      }
    }
    """;

  [Meta("a")]
  public partial record A {
    [Save("a_value")]
    public string AValue { get; set; } = "";
  }

  [Meta("b")]
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

    json.ShouldBe(LOGIC_BLOCK_JSON);
  }

  [Fact]
  public void DeserializesLogicBlock() {
    var options = CreateOptions();

    var logic =
      JsonSerializer.Deserialize<SerializableLogicBlock>(
        LOGIC_BLOCK_JSON,
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
        "state": "serializable_parallel_logic_block_state_parallel",
        "blackboard": {}
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
  public void ThrowsWhenTryingToDeserializeNull() {
    var json = "null";
    var options = CreateOptions();
    var converter = (LogicBlockConverter)options.Converters[0];

    Should.Throw<JsonException>(
      () => {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        converter.Read(ref reader, typeof(SerializableLogicBlock), options);
      }
    );
  }

  [Fact]
  public void ThrowsWhenCannotGetTypeDiscriminator() {
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Deserialize<SerializableLogicBlock>(
        /*lang=json,strict*/
        "{}",
        options
      )
    );
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
  public void ThrowsIfResolverFailsToGetBlackboardObjTypeInfo() {
    var json = /*lang=json,strict*/
    """
    {
      "$type": "serializable_logic_block",
      "state": "serializable_logic_block_state",
      "blackboard": {
        "a": {
          "a_value": "a"
        }
      }
    }
    """;

    var resolver = new Mock<IIntrospectiveTypeResolver>();
    resolver
      .Setup(resolver => resolver.GetTypeInfo(
        It.IsAny<Type>(), It.IsAny<JsonSerializerOptions>())
      )
      .Returns(() => null);

    var converter = new LogicBlockConverter(resolver.Object, new Blackboard());
    var options = new JsonSerializerOptions();

    Should.Throw<JsonException>(
      () => {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        converter.Read(ref reader, typeof(SerializableLogicBlock), options);
      }
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

  [Fact]
  public void ThrowsIfLogicBlockIsNotStarted() {
    var logic = new SerializableLogicBlock();
    var options = CreateOptions();

    Should.Throw<JsonException>(
      () => JsonSerializer.Serialize(logic, options)
    );
  }

  private static JsonSerializerOptions CreateOptions(
    IReadOnlyBlackboard? deps = null
  ) {
    deps ??= new Blackboard();
    var resolver = new IntrospectiveTypeResolver();
    return new JsonSerializerOptions {
      Converters = {
        new LogicBlockConverter(resolver, deps)
      },
      TypeInfoResolver = resolver,
      WriteIndented = true
    };
  }
}
