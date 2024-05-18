namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests;

using System.IO;
using Chickensoft.GeneratorTester;
using Shouldly;
using Xunit;

public class LogicBlocksDiagramGeneratorTest {
  [Fact]
  public void GeneratesUml() {
    // This is a type of golden testing for generator outputs.
    // First point of testing: we let the generator run on our actual test
    // cases in this project. If it fails, we can fix the generator and rebuild.
    //
    // Second point of testing: we use the test cases as generator tests to
    // examine test coverage. This points out paths we might not be testing and
    // helps eliminate unused code. Fully unit testing generators makes them
    // brittle to maintain, so we prefer this heuristic based approach.
    //
    // Because we are comparing to the generated PUML files, we're basically
    // saying "the generator should do what the generator should do." However,
    // since the PUML files are committed to source control, any changes to them
    // has to be approved by a developer, making this a form of golden testing.
    foreach (var file in Directory.GetFiles(Tester.CurrentDir("../test_cases"), "*.cs")) {
      var pumlFile = file.Replace(".cs", ".g.puml");
      var hasPumlFile = File.Exists(pumlFile);

      if (!hasPumlFile) {
        continue;
      }

      var contents = File.ReadAllText(file);
      var expected = File.ReadAllText(pumlFile);
      var result = new Diagrammer().Generate(contents);
      var csPumlFile = Path.GetFileName(file).Replace(".cs", ".puml.g.cs");
      result.Outputs[csPumlFile].ShouldBe(expected.Clean());
    }
  }
}
