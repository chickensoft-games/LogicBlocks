namespace Chickensoft.Serialization;

using Chickensoft.Collections;

/// <summary>
/// Introspective type converter that upgrades outdated introspective types
/// as soon as they are deserialized.
/// </summary>
public interface IIntrospectiveTypeConverter {
  /// <summary>
  /// Dependencies that outdated introspective types might need after being
  /// deserialized to upgrade themselves.
  /// </summary>
  public IReadOnlyBlackboard DependenciesBlackboard { get; }
}
