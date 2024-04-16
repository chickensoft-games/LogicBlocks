namespace Chickensoft.LogicBlocks;

public partial class LogicBlock<TState> {
  /// <summary>
  /// Represents a logic block state that is outdated. Outdated states can
  /// upgrade themselves to a new state once deserialized. Because the
  /// blackboard is deserialized before states, outdated states can access
  /// the blackboard to upgrade themselves.
  /// </summary>
  public interface IOutdated {
    /// <summary>
    /// Called on an outdated state to upgrade itself to a new state immediately
    /// after deserialization.
    /// </summary>
    /// <param name="blackboard">Blackboard data.</param>
    /// <returns>The preferred replacement state.</returns>
    TState Upgrade(IBlackboard blackboard);
  }
}