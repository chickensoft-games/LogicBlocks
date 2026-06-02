namespace Chickensoft.LogicBlocks.DiagramGenerator.Models;

using System.Collections.Immutable;

public sealed record LogicBlockImplementation(
  string FilePath,
  string Id,
  string Name,
  ImmutableArray<string> InitialStateIds,
  ImmutableArray<string> DependencyIds
);

public sealed record StateDiagramImplementation(
  string FilePath,
  string Id,
  string Name,
  LogicBlockGraph Graph,
  ImmutableDictionary<string, LogicBlockGraph> StatesById
)
{
  public bool Equals(StateDiagramImplementation? other) =>
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

public interface IValidLogicBlockResult
{
  string FilePath { get; }
  string Name { get; }
  string Content { get; }
}

public record InvalidLogicBlockResult : ILogicBlockResult;
public record LogicBlockOutputResult(
  string FilePath, string Name, string Content
) : ILogicBlockResult, IValidLogicBlockResult;

public record LogicBlockGraphData(
  ImmutableDictionary<string, LogicBlockInput> Inputs,
  ImmutableDictionary<string, ImmutableHashSet<string>> InputToStates,
  ImmutableDictionary<IOutputContext, ImmutableHashSet<LogicBlockOutput>>
    Outputs
);

public interface IOutputContext
{
  /// <summary>
  /// Display name of the context in which the output is being produced. This
  /// is usually OnEnter, OnExit, or an input handler name.
  /// </summary>
  string DisplayName { get; }
}
public static class OutputContexts
{
  private record OutputOnEnterContext : IOutputContext
  {
    public string DisplayName => "OnEnter";
  }
  private record OutputOnExitContext : IOutputContext
  {
    public string DisplayName => "OnExit";
  }
  private record OutputOnHandlerContext(string InputName) : IOutputContext
  {
    public string DisplayName => $"On{InputName}";
  }

  private record NoOutputContext : IOutputContext
  {
    public string DisplayName => "None";
  }

  private record OutputMethodContext(string MethodName) : IOutputContext
  {
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
