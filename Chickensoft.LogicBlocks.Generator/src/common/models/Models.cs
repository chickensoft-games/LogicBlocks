namespace Chickensoft.LogicBlocks.Generator.Common.Models;

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
/// <param name="InputToStates">Map of input id's to the states transitioned
/// to by that input's handler.</param>
/// <param name="Outputs">Output id's keyed by where the output can be produced.
/// </param>
/// <param name="Children">State graph child nodes.</param>
public sealed record LogicBlockGraph(
  string Id,
  string Name,
  string BaseId,
  Dictionary<string, ImmutableHashSet<string>> InputToStates,
  Dictionary<IOutputContext, ImmutableHashSet<string>> Outputs,
  List<LogicBlockGraph> Children
) {
  public LogicBlockGraph(
    string id,
    string name,
    string baseId
  ) : this(id, name, baseId, new(), new(), new()) { }

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

  public bool Equals(LogicBlockGraph? other) {
    if (other is null) {
      return false;
    }

    return Id == other.Id && Name == other.Name && BaseId == other.BaseId &&
      Children.SequenceEqual(other.Children);
  }

  public override int GetHashCode() => Id.GetHashCode();
}

public sealed record LogicBlockImplementation(
  string FilePath,
  string Id,
  string Name,
  string? InitialStateId,
  LogicBlockGraph Graph,
  ImmutableDictionary<string, ILogicBlockSubclass> Inputs,
  ImmutableDictionary<string, ILogicBlockSubclass> Outputs,
  ImmutableDictionary<string, LogicBlockGraph> StatesById
) {
  public bool Equals(LogicBlockImplementation? other) {
    if (other is null) {
      return false;
    }

    return Id == other.Id && Name == other.Name && Graph.Equals(other.Graph) &&
      Inputs.SequenceEqual(other.Inputs) &&
      Outputs.SequenceEqual(other.Outputs);
  }

  public override int GetHashCode() => Id.GetHashCode();
}

public interface ILogicBlockSubclass {
  string Id { get; }
  string Name { get; }
  string BaseId { get; }
}

public record LogicBlockSubclass(
  string Id,
  string Name,
  string BaseId
) : ILogicBlockSubclass;

public interface ILogicBlockResult { }
public record InvalidLogicBlockResult : ILogicBlockResult;
public record LogicBlockOutputResult(
  string FilePath, string Name, string Content
) : ILogicBlockResult;

public record StatesAndOutputs(
  ImmutableDictionary<string, ImmutableHashSet<string>> InputToStates,
  ImmutableDictionary<IOutputContext, ImmutableHashSet<string>> Outputs
);

public interface IOutputContext {
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
