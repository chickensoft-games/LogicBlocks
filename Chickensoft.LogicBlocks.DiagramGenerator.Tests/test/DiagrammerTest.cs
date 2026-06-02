namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests;

using System.IO;
using System.Runtime.CompilerServices;
using EmptyFiles;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;


public class TestDirectory
{
  public required string DirPath { get; init; }
  public override string ToString() => Path.GetFileName(DirPath);
}

public class LogicBlocksDiagramGeneratorTest
{
  static LogicBlocksDiagramGeneratorTest()
  {
    FileExtensions.AddTextExtension("puml");
  }

  public static IEnumerable<object[]> TestFolderPaths =
    Directory.GetDirectories(CurrentDir("../test_cases")).Select(x => new []{new TestDirectory { DirPath = x }});

  [Theory, MemberData(nameof(TestFolderPaths))]
  public async Task GeneratesUml(TestDirectory testDir)
  {
    var testFolderPath = testDir.DirPath;
		var csharpFiles = new List<SyntaxTree>();

		var exitingPumls = Directory.GetFiles(testFolderPath, "*.g.puml", SearchOption.AllDirectories);
		foreach (var puml in exitingPumls)
		{
			File.Delete(puml);
		}

		foreach (var filePath in Directory.GetFiles(testFolderPath, "*.cs", SearchOption.AllDirectories))
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(
				await File.ReadAllTextAsync(filePath),
				path: Path.GetFullPath(filePath));
			csharpFiles.Add(syntaxTree);
		}

    csharpFiles.Add(await GetIGetSyntaxTree());

		var generator = new DiagramGenerator();

		var driver = CSharpGeneratorDriver
			.Create(generator)
			.WithUpdatedAnalyzerConfigOptions(
				new ConfigOptionsProvider(
					new ConfigOptions(
						new Dictionary<string, string>
						{
							{
								"build_property.projectdir", testFolderPath
							}
						}
					)
				)
			);

		var compilation = CSharpCompilation.Create(
			nameof(LogicBlocksDiagramGeneratorTest),
			syntaxTrees: csharpFiles);

		driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

		var generatedPumls = Directory.GetFiles(testFolderPath, "*.g.puml", SearchOption.AllDirectories);

		if(diagnostics.Any())
    {
      Assert.Fail(string.Join("\n", diagnostics.Select(x => x.ToString())));
    }

    if(generatedPumls.Length == 0)
    {
      Assert.Fail("No generated files were found.");
    }

    foreach (var pumlPath in generatedPumls)
		{
			var testName = $"{Path.GetFileName(testFolderPath)}_{Path.GetFileName(pumlPath).Replace(".g.puml", "")}";
			var settings = new VerifySettings();
			settings.UseDirectory("../snapshots");
			settings.UseFileName(testName);

			var text = await File.ReadAllTextAsync(pumlPath);
			await Verify(text, extension: "puml", settings: settings);
		}
  }

  public static async Task<SyntaxTree> GetIGetSyntaxTree()
  {
    var filePath = CurrentDir("../../Chickensoft.LogicBlocks/src/IGet.cs");
    return CSharpSyntaxTree.ParseText(
      await File.ReadAllTextAsync(filePath),
      path: Path.GetFullPath(filePath));
  }

  public static string CurrentDir(
    string relativePathInProject,
    [CallerFilePath] string? callerFilePath = null
  ) => Path.GetFullPath(Path.Join(
    Path.GetDirectoryName(callerFilePath),
    relativePathInProject
  ));
}
