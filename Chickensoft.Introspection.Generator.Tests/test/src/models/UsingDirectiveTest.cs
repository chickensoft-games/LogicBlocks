namespace Chickensoft.Introspection.Generator.Tests.Models;

using Chickensoft.Introspection.Generator.Models;
using Shouldly;
using Xunit;

public class UsingDirectiveTest {
  [Fact]
  public void CodeString() {
    var @using = new UsingDirective(
      Alias: null,
      Name: "System",
      IsGlobal: true,
      IsStatic: false,
      IsAlias: false
    );

    @using.CodeString.ShouldBe("global using System;");
  }

  [Fact]
  public void Equality() {
    var a = new UsingDirective(
      Alias: null,
      Name: "System",
      IsGlobal: true,
      IsStatic: false,
      IsAlias: false
    );

    a.Equals(null).ShouldBeFalse();
  }
}
