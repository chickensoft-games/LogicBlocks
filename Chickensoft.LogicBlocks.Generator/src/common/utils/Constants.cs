namespace Chickensoft.SourceGeneratorUtils;

using System.Collections.Generic;
using System.Collections.Immutable;

public class Constants {
  /// <summary>Spaces per tab. Adjust to your generator's liking.</summary>
  public static int SPACES_PER_INDENT = 2;

  public const string LOGIC_BLOCK_GET_INITIAL_STATE = "GetInitialState";
  public const string LOGIC_BLOCK_CONTEXT_ID = "global::Chickensoft.LogicBlocks.Logic.Context";
  public const string LOGIC_BLOCK_CONTEXT_OUTPUT = "Output";
  public const string LOGIC_BLOCK_STATE_LOGIC_ON_ENTER = "OnEnter";
  public const string LOGIC_BLOCK_STATE_LOGIC_ON_EXIT = "OnExit";
  public const string LOGIC_BLOCK_CLASS_ID = "global::Chickensoft.LogicBlocks.Logic";
  public const string LOGIC_BLOCK_INPUT_INTERFACE_ID = "global::Chickensoft.LogicBlocks.LogicBlock.IGet";
  public const string LOGIC_BLOCK_INPUT_ASYNC_INTERFACE_ID = "global::Chickensoft.LogicBlocks.LogicBlockAsync.IGet";
  public const string STATE_MACHINE_ATTRIBUTE_NAME = "StateMachine";
  public const string STATE_MACHINE_ATTRIBUTE_SOURCE = """
  namespace Chickensoft.LogicBlocks.Generator;

  using System;

  /// <summary>
  /// State machine attribute. Placing this on a LogicBlock subclass will cause
  /// the generator to create a high-level state diagram for the state machine.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  internal sealed class StateMachineAttribute : Attribute { }
  """;

  public const string STATE_MACHINE_ATTRIBUTE_NAME_SUFFIX =
    "StateMachineAttribute";

  /// <summary>
  /// A dictionary of source code that must be injected into the compilation
  /// regardless of whether or not the user has taken advantage of any of the
  /// other features of this source generator.
  /// </summary>
  public static readonly ImmutableDictionary<string, string>
    PostInitializationSources = new Dictionary<string, string>() {
      [STATE_MACHINE_ATTRIBUTE_NAME] = STATE_MACHINE_ATTRIBUTE_SOURCE
    }.ToImmutableDictionary();
}
