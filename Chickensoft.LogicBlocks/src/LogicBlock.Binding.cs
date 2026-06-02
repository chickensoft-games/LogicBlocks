namespace Chickensoft.LogicBlocks;

using Sync;

public delegate void InputCallback<TInput>(in TInput input)
  where TInput : struct;

public delegate void OutputCallback<TOutput>(in TOutput input)
  where TOutput : struct;

public partial class LogicBlock
{
  // Broadcasts
  private readonly record struct
    InputBroadcast<TInput>(in TInput Input) where TInput : struct;

  private readonly record struct StateBroadcast(object State);

  private readonly record struct
    OutputBroadcast<TOutput>(in TOutput Output) where TOutput : struct;

  // -- lifecycle-specific broadcasts
  private readonly record struct StartedBroadcast();

  private readonly record struct StoppedBroadcast();

  private readonly record struct LoadedBroadcast();

  public class Binding : SyncBinding
  {
    internal Binding(ISyncSubject subject) : base(subject) { }

    public Binding OnInput<TInput>(InputCallback<TInput> callback)
      where TInput : struct
    {
      AddCallback((in InputBroadcast<TInput> broadcast) =>
        callback(broadcast.Input)
      );

      return this;
    }

    public Binding OnState<TState>(Action<TState> callback)
      where TState : LogicBlockState
    {
      AddCallback(
        (in StateBroadcast broadcast) => callback((TState)broadcast.State),
        (in StateBroadcast broadcast) => broadcast.State is TState
      );

      return this;
    }

    public Binding OnOutput<TOutput>(OutputCallback<TOutput> callback)
      where TOutput : struct
    {
      AddCallback((in OutputBroadcast<TOutput> broadcast) =>
        callback(broadcast.Output)
      );

      return this;
    }

    public Binding OnStart(Action callback)
    {
      AddCallback((in StartedBroadcast _) => callback());
      return this;
    }

    public Binding OnStop(Action callback)
    {
      AddCallback((in StoppedBroadcast _) => callback());
      return this;
    }

    public Binding OnLoad(Action callback)
    {
      AddCallback((in LoadedBroadcast _) => callback());
      return this;
    }
  }

  /// <summary>
  /// A fake binding that can be used to test objects which consume logic blocks
  /// without needing a real logic block. Use <see cref="CreateFakeBinding"/>
  /// to create one, then register callbacks with the standard
  /// <see cref="Binding"/> methods. Various methods are provided to simulate
  /// inputs, state changes, outputs, and lifecycle events.
  /// </summary>
  public sealed class FakeBinding : Binding
  {
    private readonly SyncSubject _fakeSubject;

    internal FakeBinding() : this(new SyncSubject(new object())) { }

    private FakeBinding(SyncSubject subject) : base(subject)
    {
      _fakeSubject = subject;
    }

    /// <summary>Simulates an input being received.</summary>
    public void Input<TInput>(in TInput input)
      where TInput : struct =>
      _fakeSubject.Broadcast(new InputBroadcast<TInput>(input));

    /// <summary>Simulates a state change.</summary>
    public void SetState<TState>(TState state)
      where TState : LogicBlockState =>
      _fakeSubject.Broadcast(new StateBroadcast(state));

    /// <summary>Simulates an output being produced.</summary>
    public void Output<TOutput>(in TOutput output)
      where TOutput : struct =>
      _fakeSubject.Broadcast(new OutputBroadcast<TOutput>(output));

    /// <summary>Simulates the logic block starting.</summary>
    public void Start() =>
      _fakeSubject.Broadcast(new StartedBroadcast());

    /// <summary>Simulates the logic block stopping.</summary>
    public void Stop() =>
      _fakeSubject.Broadcast(new StoppedBroadcast());

    /// <summary>Simulates the logic block loading from saved data.</summary>
    public void Load() =>
      _fakeSubject.Broadcast(new LoadedBroadcast());
  }

  /// <summary>
  /// Creates a fake binding that can be used to test objects which consume
  /// logic block bindings without needing a real logic block.
  /// </summary>
  public static FakeBinding CreateFakeBinding() => new();
}
