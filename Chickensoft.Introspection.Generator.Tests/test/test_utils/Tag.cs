namespace Chickensoft.Introspection.Generator.Tests.TestUtils;

using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class TagAttribute : Attribute {
  public string Tag { get; }

  public int Number { get; set; }

  public TagAttribute(string tag) {
    Tag = tag;
  }
}
