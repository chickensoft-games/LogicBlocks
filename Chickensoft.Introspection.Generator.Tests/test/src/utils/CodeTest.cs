namespace Chickensoft.Introspection.Generator.Tests.Utils;

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

  [Theory]
  [InlineData("TheURLRef", "the_url_ref")]
  [InlineData("camelCase", "camel_case")]
  [InlineData("snake_case", "snake_case")]
  [InlineData("ALLCapsGOHereEndOnCAPS", "all_caps_go_here_end_on_caps")]
  public void SnakeCase(string value, string expected) =>
    Code.PascalCaseToSnakeCase(value).ShouldBe(expected);
}
