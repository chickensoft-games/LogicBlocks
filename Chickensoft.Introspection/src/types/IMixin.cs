namespace Chickensoft.Introspection;

/// <summary>
/// Base mixin interface. To create a mixin, extend this interface and mark your
/// mixin with the <see cref="MixinAttribute"/>.
/// </summary>
/// <typeparam name="TMixin">Type of the mixin.</typeparam>
public interface IMixin<TMixin> : IIntrospective {
  /// <summary>
  /// Mixin handler method. Types marked with the
  /// <see cref="MetaAttribute"/> have information about their mixins
  /// generated at build time, allowing this handler to be invoked at runtime
  /// by client code without knowing what all mixins are applied to a type.
  /// </summary>
  void Handler();
}
