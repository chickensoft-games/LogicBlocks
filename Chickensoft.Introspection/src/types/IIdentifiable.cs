namespace Chickensoft.Introspection;

/// <summary>
/// An introspective type whose metatype has been given a user-defined id by
/// passing a string in as the first argument of the
/// <see cref="MetaAttribute"/> attribute.
/// </summary>
public interface IIdentifiable : IIntrospective;
