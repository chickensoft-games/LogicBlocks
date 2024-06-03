namespace Chickensoft.Introspection;

using System;

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

  /// <summary>
  /// Determines if the type has a mixin applied to it.
  /// </summary>
  /// <param name="type">Type of mixin to look for.</param>
  /// <returns>True if the type has the specified mixin, false otherwise.
  /// </returns>
  public bool HasMixin(Type type) => Metatype.MixinHandlers.ContainsKey(type);

  /// <summary>
  /// Invokes the handler of each mixin that is applied to the type.
  /// </summary>
  public void InvokeMixins() {
    foreach (var mixin in Metatype.Mixins) {
      Metatype.MixinHandlers[mixin](this);
    }
  }

  /// <summary>
  /// Invokes the handler of a specific mixin that is applied to the type.
  /// </summary>
  /// <param name="type">Mixin type.</param>
  /// <exception cref="InvalidOperationException" />
  public void InvokeMixin(Type type) {
    if (!HasMixin(type)) {
      throw new InvalidOperationException(
        $"Type {GetType()} does not have mixin {type}"
      );
    }

    Metatype.MixinHandlers[type](this);
  }
}
