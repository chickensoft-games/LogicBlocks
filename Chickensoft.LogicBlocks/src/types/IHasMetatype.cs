namespace Chickensoft.LogicBlocks;

/// <summary>
/// Interface applied to a type to indicate that it has metatype information
/// available.
/// </summary>
public interface IHasMetatype {
  /// <summary>
  /// Arbitrary data that is shared between mixins. Mixins are free to store
  /// additional instance state in this blackboard.
  /// </summary>
  IBlackboard MixinState { get; }

  /// <summary>
  /// Generated metatype information.
  /// </summary>
  public IMetatype Metatype { get; }
}
