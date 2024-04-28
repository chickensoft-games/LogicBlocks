namespace Chickensoft.LogicBlocks.DiagramGenerator.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.SourceGeneratorUtils;

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
) {
  /// <summary>
  /// Logic block graph data (inputs, input to state mappings, and outputs).
  /// </summary>
  public LogicBlockGraphData Data { get; set; } = default!;

  public LogicBlockGraph(
    string id,
    string name,
    string baseId
  ) : this(id, name, baseId, new()) { }

  public string UmlId => Id
    .Replace("global::", "")
    .Replace(':', '_')
    .Replace('.', '_');

  public override string ToString() => Describe(0);

  public string Describe(int level) {
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

public sealed record LogicBlockImplementation(
  string FilePath,
  string Id,
  string Name,
  ImmutableHashSet<string> InitialStateIds,
  LogicBlockGraph Graph,
  ImmutableDictionary<string, LogicBlockGraph> StatesById
) {
  public bool Equals(LogicBlockImplementation? other) =>
    other is not null &&
    Id == other.Id &&
    Name == other.Name &&
    Graph.Equals(other.Graph);

  public override int GetHashCode() => Id.GetHashCode();
}

public record LogicBlockSubclass(
  string Id,
  string Name,
  string BaseId
);

public record LogicBlockInput(string Id, string Name);
public record LogicBlockOutput(string Id, string Name);

public interface ILogicBlockResult;
public record InvalidLogicBlockResult : ILogicBlockResult;
public record LogicBlockOutputResult(
  string FilePath, string Name, string Content
) : ILogicBlockResult;

public record LogicBlockGraphData(
  ImmutableDictionary<string, LogicBlockInput> Inputs,
  ImmutableDictionary<string, ImmutableHashSet<string>> InputToStates,
  ImmutableDictionary<IOutputContext, ImmutableHashSet<LogicBlockOutput>>
    Outputs
);

public interface IOutputContext {
  /// <summary>
  /// Display name of the context in which the output is being produced. This
  /// is usually OnEnter, OnExit, or an input handler name.
  /// </summary>
  string DisplayName { get; }
}
public static class OutputContexts {
  private record OutputOnEnterContext : IOutputContext {
    public string DisplayName => "OnEnter";
  }
  private record OutputOnExitContext : IOutputContext {
    public string DisplayName => "OnExit";
  }
  private record OutputOnHandlerContext(string InputName) : IOutputContext {
    public string DisplayName => $"On{InputName}";
  }

  private record NoOutputContext : IOutputContext {
    public string DisplayName => "None";
  }

  private record OutputMethodContext(string MethodName) : IOutputContext {
    public string DisplayName => $"{MethodName}()";
  }

  public static readonly IOutputContext None = new NoOutputContext();
  public static readonly IOutputContext OnEnter = new OutputOnEnterContext();
  public static readonly IOutputContext OnExit = new OutputOnExitContext();
  public static IOutputContext OnInput(string inputName) =>
    new OutputOnHandlerContext(inputName);
  public static IOutputContext Method(string displayName) =>
    new OutputMethodContext(displayName);
}

public record OutputEntry(IOutputContext OutputContext, string OutputId);
