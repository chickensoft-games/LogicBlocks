namespace Chickensoft.LogicBlocks;

[AttributeUsage(
  AttributeTargets.Class, Inherited = false, AllowMultiple = false
)]
public class StateDiagramAttribute : Attribute
{
  /// <summary>
  /// Path to place the PlantUML diagram file in (relative to the root of the
  /// project containing the `.csproj` file). If omitted, the diagram file will be
  /// placed alongside the source code file.
  /// </summary>
  public string? Path { get; }

  /// <summary>
  /// Changes the paths so that they're generated as full paths and uses the
  /// vscode:// url protocol. This allows them to be used with the VSCode plugin.
  /// </summary>
  public bool UseVSCodePaths { get; }

  public StateDiagramAttribute(string? path = null, bool useVSCodePaths = false)
  {
    Path = path;
    UseVSCodePaths = useVSCodePaths;
  }
}
