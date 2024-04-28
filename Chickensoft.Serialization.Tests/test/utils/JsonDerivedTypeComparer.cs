namespace Chickensoft.Serialization.Tests;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

public class JsonDerivedTypeComparer : IEqualityComparer<JsonDerivedType> {
  public static JsonDerivedTypeComparer Instance { get; } =
    new JsonDerivedTypeComparer();

  public bool Equals(JsonDerivedType x, JsonDerivedType y) =>
    x.DerivedType == y.DerivedType &&
    x.TypeDiscriminator == y.TypeDiscriminator;

  public int GetHashCode(JsonDerivedType obj) =>
    HashCode.Combine(obj.DerivedType, obj.TypeDiscriminator);
}
