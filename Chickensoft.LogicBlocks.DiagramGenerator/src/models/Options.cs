namespace Chickensoft.LogicBlocks.DiagramGenerator.Models;

/// <summary>
/// Options for the source generator that are specified in a `.csproj` file by the
/// developer using the generator
/// </summary>
/// <param name="LogicBlocksDiagramGeneratorDisabled">Whether or not the source
/// generator is disabled</param>
public record GenerationOptions(
  bool LogicBlocksDiagramGeneratorDisabled
);
