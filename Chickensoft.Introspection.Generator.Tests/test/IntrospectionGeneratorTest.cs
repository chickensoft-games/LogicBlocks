namespace Chickensoft.Introspection.Generator.Tests;

using System.IO;
using Chickensoft.GeneratorTester;
using Shouldly;
using Xunit;

public class IntrospectionGeneratorTest {
  [Fact]
  public void GeneratesUml() {
    // Run the test cases through the generator. If the project builds and
    // allows this to run, we've already passed a considerable barrier of
    // testing. Fully unit testing generators makes them brittle to maintain,
    // so we prefer this heuristic based approach.
    //
    // Running test cases through the generator allows us to collect coverage
    // which helps us identify missing test cases or unused code.
    foreach (
      var file in Directory.GetFiles(Tester.CurrentDir("../test_cases"), "*.cs")
    ) {
      var contents = File.ReadAllText(file);

      new TypeGenerator().Generate(contents);
    }
  }

  // Have to test error diagnostics in unit tests since it would not build.

  [Fact]
  public void NotFullyPartialError() {
    var contents = """
    namespace Chickensoft.Introspection.Generator.Tests.TestCases;

    public sealed class Parent {
      public sealed partial class Child {
        [Meta]
        public sealed partial class NotFullyPartial { }
      }
    }
    """;

    new TypeGenerator().Generate(contents).Diagnostics.ShouldNotBeEmpty();
  }

  [Fact]
  public void TypeDoesNotHaveUniqueIdError() {
    var contents = """
    namespace Chickensoft.Introspection.Generator.Tests.TestCases;

    [Meta, Id("same_model")]
    public partial class SameModel;

    [Meta, Id("same_model")]
    public partial class OtherModel;
    """;

    new TypeGenerator().Generate(contents).Diagnostics.ShouldNotBeEmpty();
  }
}
