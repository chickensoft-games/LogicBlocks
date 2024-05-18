namespace Chickensoft.Introspection.Generator.Models;

using System.Collections.Immutable;
using System;
using System.Linq;

/// <summary>
/// Data used to generate source output. Represents the finished work of the
/// generator before it is converted into source code output.
/// </summary>
/// <param name="AllTypes">All identified types.</param>
/// <param name="Metatypes">Valid metatypes that were found in the project's
/// source code.</param>
/// <param name="VisibleTypes">Declared types that are visible at the top-level
/// of the project's source code.</param>
/// <param name="ConcreteVisibleTypes">Declared types that are visible
/// at the top level of the project's source code and are not interfaces, static
/// classes, or abstract types.</param>
/// <param name="Mixins">Map of declared types that are marked with the mixin
/// attribute by their type name.</param>
public class GenerationData {
  public ImmutableDictionary<string, DeclaredType> AllTypes { get; init; }
  public ImmutableDictionary<string, DeclaredType> Metatypes { get; init; }
  public ImmutableDictionary<string, DeclaredType> VisibleTypes { get; init; }
  public ImmutableDictionary<string, DeclaredType> ConcreteVisibleTypes { get; init; }
  public ImmutableDictionary<string, DeclaredType> Mixins { get; init; }

  public GenerationData(
    ImmutableDictionary<string, DeclaredType> allTypes,
    ImmutableDictionary<string, DeclaredType> metatypes,
    ImmutableDictionary<string, DeclaredType> visibleTypes,
    ImmutableDictionary<string, DeclaredType> concreteVisibleTypes,
    ImmutableDictionary<string, DeclaredType> mixins
  ) {
    AllTypes = allTypes;
    Metatypes = metatypes;
    VisibleTypes = visibleTypes;
    ConcreteVisibleTypes = concreteVisibleTypes;
    Mixins = mixins;
  }

  public override int GetHashCode() => HashCode.Combine(
    AllTypes, Metatypes, VisibleTypes, ConcreteVisibleTypes, Mixins
  );

  public override bool Equals(object? obj) =>
    obj is GenerationData data &&
    AllTypes.SequenceEqual(data.AllTypes) &&
    Metatypes.SequenceEqual(data.Metatypes) &&
    VisibleTypes.SequenceEqual(data.VisibleTypes) &&
    ConcreteVisibleTypes.SequenceEqual(data.ConcreteVisibleTypes) &&
    Mixins.SequenceEqual(data.Mixins);
}
