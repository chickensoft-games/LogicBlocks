namespace Chickensoft.Introspection;

/// <summary>
/// Interface applied to a type to indicate that it has generated metatype
/// information available.
/// </summary>
public interface IIntrospective {
  /// <summary>
  /// Arbitrary data that is shared between mixins. Mixins are free to store
  /// additional instance state in this blackboard.
  /// </summary>
  MixinBlackboard MixinState { get; }

  /// <summary>
  /// Generated metatype information.
  /// </summary>
  public IMetatype Metatype { get; }
}
