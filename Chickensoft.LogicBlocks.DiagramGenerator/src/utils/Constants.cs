namespace Chickensoft.SourceGeneratorUtils;

public class Constants
{
  /// <summary>Spaces per tab. Adjust to your generator's liking.</summary>
  public const int SPACES_PER_INDENT = 2;

  public const string OVERRIDE = "override";
  public const string SYSTEMTYPE = "System.Type";
  public const string DISABLE_CSPROJ_PROP = "LogicBlocksDiagramGeneratorDisabled";
  public const string LOGIC_BLOCK_STATE = "LogicBlockState";
  public const string LOGIC_BLOCK_STATE_OUTPUT = "Output";
  public const string LOGIC_BLOCK_STATE_LOGIC_START = "Start";
  public const string LOGIC_BLOCK_STATE_LOGIC_SET = "Set";
  public const string LOGIC_BLOCK_STATE_LOGIC_SET_OBJECT = "SetObject";
  public const string LOGIC_BLOCK_STATE_LOGIC_OVERWRITE = "Overwrite";
  public const string LOGIC_BLOCK_STATE_LOGIC_OVERWRITE_OBJECT = "OverwriteObject";
  public const string LOGIC_BLOCK_STATE_LOGIC_ON_ENTER = "OnEnter";
  public const string LOGIC_BLOCK_STATE_LOGIC_ON_EXIT = "OnExit";
  public const string LOGIC_BLOCK_INPUT_INTERFACE_ID = "global::Chickensoft.LogicBlocks.IGet";
  public const string LOGIC_BLOCK_TYPE_NAME = "LogicBlock";
  public const string LOGIC_BLOCK_ATTRIBUTE_NAME = "StateDiagram";
  public const string LOGIC_BLOCK_ATTRIBUTE_NAME_FULL = "StateDiagramAttribute";
  public const string AUTO_BLOCK_TYPE_NAME = "AutoBlock";
}
