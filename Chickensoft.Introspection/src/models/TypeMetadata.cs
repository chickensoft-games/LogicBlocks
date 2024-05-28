namespace Chickensoft.Introspection;

using System;

/// <summary>
/// Metadata about a type that is visible from the top-level namespace.
/// </summary>
public interface ITypeMetadata {
  /// <summary>
  /// Simple name of the type. If the type is generic, this will also include
  /// the open generics.
  /// </summary>
  string Name { get; }
}

/// <summary>
/// Metadata about a type that is not a non-generic or closed generic type.
/// </summary>
public interface IClosedTypeMetadata : ITypeMetadata {
  /// <summary>
  /// Function which receives a type receiver and calls the
  /// <see cref="ITypeReceiver.Receive{T}" /> method with the generic type.
  /// </summary>
  Action<ITypeReceiver> GenericTypeGetter { get; }
}

/// <summary>
/// Metadata about a concrete (instantiable) type.
/// </summary>
public interface IConcreteTypeMetadata : IClosedTypeMetadata {
  /// <summary>Function which returns a new instance of the type.</summary>
  Func<object> Factory { get; }
}

/// <summary>
/// Metadata about an introspective type.
/// </summary>
public interface IIntrospectiveTypeMetadata : IClosedTypeMetadata {
  /// <summary>
  /// Metatype description of the introspective type which includes
  /// information about the type, its properties, attributes applied to it,
  /// etc.
  /// </summary>
  IMetatype Metatype { get; }
}

/// <summary>
/// Metadata about a concrete introspective type.
/// </summary>
public interface IConcreteIntrospectiveTypeMetadata :
IIntrospectiveTypeMetadata, IConcreteTypeMetadata {
  /// <summary>
  /// Introspective type version as specified by the
  /// <see cref="VersionAttribute" />, if any.
  /// </summary>
  int Version { get; }
}

/// <summary>
/// Metadata about an identifiable introspective type.
/// </summary>
public interface IIdentifiableTypeMetadata : IIntrospectiveTypeMetadata {
  /// <summary>
  /// Introspective type identifier as specified by the
  /// <see cref="IdAttribute" />, if any.
  /// </summary>
  string Id { get; }
}

/// <summary>
/// Metadata about a type that is visible from the top-level namespace and is
/// either abstract, generic, or both.
/// </summary>
/// <param name="Name">
/// <inheritdoc cref="ITypeMetadata.Name" path="/summary" />
/// </param>
public record TypeMetadata(string Name) : ITypeMetadata;

/// <summary>
/// Metadata about a type that is visible from the top-level namespace and is
/// concrete (can be instantiated). These are always non-generic or closed
/// generic types.
/// </summary>
/// <param name="Name">
/// <inheritdoc cref="ITypeMetadata.Name" path="/summary" />
/// </param>
/// <param name="GenericTypeGetter">
/// <inheritdoc cref="IClosedTypeMetadata.GenericTypeGetter" path="/summary" />
/// </param>
/// <param name="Factory">
/// <inheritdoc cref="IConcreteTypeMetadata.Factory" path="/summary" />
/// </param>
public record ConcreteTypeMetadata(
  string Name,
  Action<ITypeReceiver> GenericTypeGetter,
  Func<object> Factory
) : TypeMetadata(Name), IConcreteTypeMetadata;

/// <summary>
/// Metadata about an abstract introspective type.
/// </summary>
/// <param name="Name">
/// <inheritdoc cref="ITypeMetadata.Name" path="/summary" />
/// </param>
/// <param name="GenericTypeGetter">
/// <inheritdoc cref="IClosedTypeMetadata.GenericTypeGetter" path="/summary" />
/// </param>
/// <param name="Metatype">
/// <inheritdoc cref="IIntrospectiveTypeMetadata.Metatype" path="/summary" />
/// </param>
public record AbstractIntrospectiveTypeMetadata(
  string Name,
  Action<ITypeReceiver> GenericTypeGetter,
  IMetatype Metatype
) : TypeMetadata(Name), IIntrospectiveTypeMetadata;

/// <summary>
/// Metadata about a concrete (instantiable) introspective type.
/// </summary>
/// <param name="Name">
/// <inheritdoc cref="ITypeMetadata.Name" path="/summary" />
/// </param>
/// <param name="GenericTypeGetter">
/// <inheritdoc cref="IClosedTypeMetadata.GenericTypeGetter" path="/summary" />
/// </param>
/// <param name="Factory">
/// <inheritdoc cref="IConcreteTypeMetadata.Factory" path="/summary" />
/// </param>
/// <param name="Metatype">
/// <inheritdoc cref="IIntrospectiveTypeMetadata.Metatype" path="/summary" />
/// </param>
/// <param name="Version">
/// <inheritdoc cref="IConcreteIntrospectiveTypeMetadata.Version"
/// path="/summary" />
/// </param>
public record IntrospectiveTypeMetadata(
  string Name,
  Action<ITypeReceiver> GenericTypeGetter,
  Func<object> Factory,
  IMetatype Metatype,
  int Version
) : ConcreteTypeMetadata(Name, GenericTypeGetter, Factory),
IConcreteIntrospectiveTypeMetadata;

/// <summary>
/// Metadata about an abstract and identifiable introspective type.
/// </summary>
/// <param name="Name">
/// <inheritdoc cref="ITypeMetadata.Name" path="/summary" />
/// </param>
/// <param name="GenericTypeGetter">
/// <inheritdoc cref="IClosedTypeMetadata.GenericTypeGetter" path="/summary" />
/// </param>
/// <param name="Metatype">
/// <inheritdoc cref="IIntrospectiveTypeMetadata.Metatype" path="/summary" />
/// </param>
/// <param name="Id">
/// <inheritdoc cref="IIdentifiableTypeMetadata.Id" path="/summary" />
/// </param>
public record AbstractIdentifiableTypeMetadata(
  string Name,
  Action<ITypeReceiver> GenericTypeGetter,
  IMetatype Metatype,
  string Id
) : AbstractIntrospectiveTypeMetadata(
  Name,
  GenericTypeGetter,
  Metatype
), IIdentifiableTypeMetadata;


/// <summary>
/// Metadata about a concrete and identifiable introspective type.
/// </summary>
/// <param name="Name">
/// <inheritdoc cref="ITypeMetadata.Name" path="/summary" />
/// </param>
/// <param name="GenericTypeGetter">
/// <inheritdoc cref="IClosedTypeMetadata.GenericTypeGetter" path="/summary" />
/// </param>
/// <param name="Factory">
/// <inheritdoc cref="IConcreteTypeMetadata.Factory" path="/summary" />
/// </param>
/// <param name="Metatype">
/// <inheritdoc cref="IIntrospectiveTypeMetadata.Metatype" path="/summary" />
/// </param>
/// <param name="Id">
/// <inheritdoc cref="IIdentifiableTypeMetadata.Id" path="/summary" />
/// </param>
/// <param name="Version">
/// <inheritdoc cref="IConcreteIntrospectiveTypeMetadata.Version"
/// path="/summary" />
/// </param>
public record IdentifiableTypeMetadata(
  string Name,
  Action<ITypeReceiver> GenericTypeGetter,
  Func<object> Factory,
  IMetatype Metatype,
  string Id,
  int Version
) : IntrospectiveTypeMetadata(
  Name, GenericTypeGetter, Factory, Metatype, Version
), IIdentifiableTypeMetadata, IConcreteIntrospectiveTypeMetadata;
