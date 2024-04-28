namespace Chickensoft.Serialization;

using Chickensoft.Collections;

/// <summary>
/// Represents an outdated object. Outdated objects are given a chance to
/// upgrade themselves as soon as they are deserialized. A blackboard is
/// provided which may contain data that the outdated object needs to be able
/// to upgrade itself.
/// </summary>
public interface IOutdated {
  /// <summary>
  /// Called on an outdated object to upgrade itself to a new object immediately
  /// after deserialization.
  /// </summary>
  /// <param name="blackboard">Blackboard data.</param>
  /// <returns>The preferred replacement object. Can also be another outdated
  /// object, which will be upgraded in turn.</returns>
  object Upgrade(IReadOnlyBlackboard blackboard);
}
