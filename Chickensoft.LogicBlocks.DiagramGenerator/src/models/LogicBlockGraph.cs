namespace Chickensoft.LogicBlocks.DiagramGenerator.Models;

using System.Collections.Generic;
using System.Linq;
using SourceGeneratorUtils;

/// <summary>
/// Logic block state.
/// </summary>
/// <param name="Id">Fully qualified name of the type.</param>
/// <param name="Name">Declared name of the type.</param>
/// <param name="BaseId">Fully qualified name of the base type.</param>
/// <param name="Children">State graph child nodes.</param>
public sealed record LogicBlockGraph(
  string Id,
  string Name,
  string BaseId,
  List<LogicBlockGraph> Children
)
{
  /// <summary>
  /// Logic block graph data (inputs, input to state mappings, and outputs).
  /// </summary>
  public LogicBlockGraphData Data { get; set; } = default!;

  public LogicBlockGraph(
    string id,
    string name,
    string baseId
  ) : this(id, name, baseId, []) { }

  /// <summary>
  /// UML-friendly identifier for the logic block graph.
  /// </summary>
  public string UmlId => Id
    .Replace("global::", "")
    .Replace(':', '_')
    .Replace('.', '_')
    .Replace('<', '_')
    .Replace('>', '_')
    .Replace(',', '_');

  public override string ToString() => Describe(0);

  public string Describe(int level)
  {
    var indent = ChickensoftGenerator.Tab(level);

    return ($"{indent}LogicBlockGraph {{\n" +
            $"{indent}  Id: {Id},\n" +
            $"{indent}  Name: {Name},\n" +
            $"{indent}  BaseId: {BaseId},\n" +
            $"{indent}  Children: [\n" +
            string.Join(
              ",\n", Children.Select(
                child => child.Describe(level + 2)
              )
            ) +
            $"\n{indent}  ]\n" +
            $"{indent}}}").Replace("global::Chickensoft.LogicBlocks.Example.", "");
  }

  public bool Equals(LogicBlockGraph? other) =>
    other is not null &&
    Id == other.Id &&
    Name == other.Name &&
    BaseId == other.BaseId &&
    Children.SequenceEqual(other.Children);

  public override int GetHashCode() => Id.GetHashCode();
}
