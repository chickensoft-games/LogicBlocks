namespace Chickensoft.Introspection;

using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

public class PropertyMetadataTest {
  [Fact]
  public void Initializes() {
    var property = new PropertyMetadata(
      Name: "Name",
      Type: typeof(string),
      Getter: _ => "Value",
      Setter: (_, _) => { },
      GenericTypeGetter: _ => { },
      AttributesByType: new Dictionary<Type, Attribute[]>()
    );

    property.ShouldBeOfType<PropertyMetadata>();
  }
}
