namespace Chickensoft.LogicBlocks;

/// <summary>
/// Represents the current status of a logic block.
/// </summary>
public enum LogicBlockStatus
{
  /// <summary>
  /// The logic block is stopped and not running. This is the default status
  /// when a logic block is created.
  /// </summary>
  Stopped,

  /// <summary>
  /// The logic block is running and can receive inputs. This status is set when
  /// the logic block is started and remains until it is stopped or disposed.
  /// </summary>
  Started,

  /// <summary>
  /// The logic block has been disposed and can no longer be used. This status
  /// is set when the logic block is disposed and remains until the logic block
  /// is garbage collected and no longer exists in memory.
  /// </summary>
  Disposed
}
