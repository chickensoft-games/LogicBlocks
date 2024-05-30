#pragma warning disable SYSLIB1045
namespace Chickensoft.Introspection.Generator.Tests.Utils;

using System.Text.RegularExpressions;
using Chickensoft.Introspection.Generator.Utils;
using Shouldly;
using Xunit;

public class CodeTest {
  [Theory]
  [InlineData("Parent<string>.Child<string>", "Child")]
  [InlineData("nameof(Parent<string>.Child<string>)", "Child")]
  [InlineData("Parent<string>.Child<string>.Value", "Value")]
  [InlineData("nameof(Parent<string>.Child<string>.Value)", "Value")]
  [InlineData("System.Collections.Generic", "Generic")]
  [InlineData("nameof(System.Collections.Generic)", "Generic")]
  [InlineData("point", "point")]
  [InlineData("nameof(point)", "point")]
  [InlineData("point.x", "x")]
  [InlineData("nameof(point.x)", "x")]
  [InlineData("Program", "Program")]
  [InlineData("nameof(Program)", "Program")]
  [InlineData("System.Int32", "Int32")]
  [InlineData("nameof(System.Int32)", "Int32")]
  [InlineData("TestAlias", "TestAlias")]
  [InlineData("nameof(TestAlias)", "TestAlias")]
  [InlineData("List<int>", "List")]
  [InlineData("nameof(List<int>)", "List")]
  public void NameOf(string value, string expected) =>
    Code.NameOf(value).ShouldBe(expected);

  [Fact]
  public void NameOfNoSuccess() {
    // Make a regex that doesn't match anything
    Code.NameOfRegex = new Regex("nope");
    Code.NameOf("input").ShouldBe("input");
  }
}
#pragma warning restore SYSLIB1045
