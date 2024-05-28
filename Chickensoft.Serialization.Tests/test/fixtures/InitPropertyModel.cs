namespace Chickensoft.Serializer.Tests.Fixtures;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Meta, Id("init_property_model")]
public partial class InitPropertyModel : IEquatable<InitPropertyModel> {
  [Save("name")]
  public required string Name { get; init; } // required — never null

  [Save("age")]
  public int? Age { get; init; } // not required — must be nullable

  [Save("descriptions")]
  public List<string>? Descriptions { get; set; } // not an init prop.

  public override bool Equals(object? obj) {
    if (obj is not InitPropertyModel other) {
      return false;
    }
    return
      Name == other.Name &&
      Age == other.Age &&
      Descriptions?.SequenceEqual(other.Descriptions ?? new()) == true;
  }

  public bool Equals(InitPropertyModel? other) => Equals((object?)other);

  public override int GetHashCode() {
    var hash = new HashCode();
    hash.Add(Name.GetHashCode());
    hash.Add(Age.GetHashCode());
    foreach (var description in Descriptions ?? new()) {
      hash.Add(description.GetHashCode());
    }
    return hash.ToHashCode();
  }
}
