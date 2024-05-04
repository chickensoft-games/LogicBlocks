namespace Chickensoft.Introspection.Generator.Tests;

using System.IO;
using Chickensoft.GeneratorTester;
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
    foreach (var file in Directory.GetFiles(Tester.CurrentDir("../test_cases"), "*.cs")) {
      var contents = File.ReadAllText(file);

      new TypeGenerator().Generate(contents);
    }
  }
}
