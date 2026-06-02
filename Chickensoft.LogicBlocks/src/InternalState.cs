using Chickensoft.Collections;
using Chickensoft.LogicBlocks;

public interface IStateful : ITaskTracker
{
  bool IsAttached { get; }
  LogicBlockStatus Status { get; }

  // state housekeeping
  void Attach(ILogicBlock logic);

  void Detach();

  // logic block operations
  void Input<TInput>(in TInput input) where TInput : struct;

  void Output<TOutput>(in TOutput output) where TOutput : struct;

  // blackboard
  TData Get<TData>() where TData : class;

  bool Has<T>() where T : class;

  // history
  bool HasHistory { get; }
  void Push(Type type);
  Type? Peek();
  Type? Pop();
  void ClearHistory();
}

// Fake logic block context used when testing LogicBlocks.
public sealed class StateTester : IStateful
{
  public bool IsAttached => true;
  public LogicBlockStatus Status { get; private set; } = LogicBlockStatus.Started;

  public void Start() => Status = LogicBlockStatus.Started;
  public void Stop() => Status = LogicBlockStatus.Stopped;
  public void Dispose() => Status = LogicBlockStatus.Disposed;

  private Blackboard _blackboard = new();
  private readonly List<object> _inputs = [];
  private readonly List<object> _outputs = [];
  private readonly History _history = new();
  internal ITaskTracker TaskTracker { get; set; } = new TaskTracker();

  public IReadOnlyList<object> Inputs => _inputs;
  public IReadOnlyList<object> Outputs => _outputs;
  public History History => _history;

  public void Attach(ILogicBlock logic) { }
  public void Detach() { }

  public TData Get<TData>() where TData : class
  {
    if (_blackboard.Has<TData>())
    {
      return _blackboard.Get<TData>();
    }

    try
    {
      // object isn't in the blackboard. if it has a parameterless constructor,
      // we can probably just whip one up. this prevents developers from needing
      // to register states with parameterless constructors on the blackboard
      // during testing

      var instance = Activator.CreateInstance<TData>();
      _blackboard.Set(instance);
      return instance;
      // safe in AOT since it is the generic form. irrelevant though, since
      // code in this StateTester class is only going to execute in tests :)
    }
    catch (Exception e)
    {
      throw new LogicBlockException(
        $"Failed to get (or create) a value of type {typeof(TData)} from the " +
        "blackboard.",
        e
      );
    }
  }

  public bool Has<T>() where T : class => _blackboard.Has<T>();

  public void Set<TData>(TData value) where TData : class =>
    _blackboard.Set(value);

  public void Input<TInput>(in TInput input)
    where TInput : struct => _inputs.Add(input);

  public void Output<TOutput>(in TOutput output)
    where TOutput : struct => _outputs.Add(output);

  public bool HasHistory => _history.Count > 0;

  public void Push(Type type) => _history.Push(type);

  public Type? Peek() => _history.Peek();

  public Type? Pop() => _history.Count > 0 ? _history.Pop() : null;

  public void ClearHistory() => _history.Clear();

  public Task Task => TaskTracker.Task;
  public void TrackTask(Task task) => TaskTracker.TrackTask(task);

  public void Reset()
  {
    _inputs.Clear();
    _outputs.Clear();
    _history.Clear();
  }

  public void Clear()
  {
    Reset();
    _blackboard = new Blackboard();
  }
}

// Forwards to a real logic block
internal sealed class LogicContext : IStateful
{
  // Active logic block reference — throws if detached. Used for operations
  // that require the state to be the current active state.
  private LogicBlock ActiveLogic =>
    _isAttached
      ? _logic!
      : throw new LogicBlockException(
          "Cannot operate on a logic block from a state that is not the " +
          "active state of a logic block. Do you have a subscription, " +
          "callback, or long-running task that was connected to a previous " +
          "state?"
        );

  // Kept for backwards compatibility with tests that access .Logic directly.
  public LogicBlock Logic => ActiveLogic;

  private LogicBlock? _logic;
  private bool _isAttached;

  public bool IsAttached => _isAttached;

  // Status reads from the logic block even after detach so that async
  // continuations can check whether the logic block is still running.
  public LogicBlockStatus Status =>
    _logic?.Status ?? LogicBlockStatus.Stopped;

  public void Attach(ILogicBlock logic)
  {
    if (logic is not LogicBlock logicBlock)
    {
      throw new LogicBlockException(
        "LogicContext can only be attached to a LogicBlock instance."
      );
    }

    if (_isAttached)
    {
      throw new LogicBlockException(
        "LogicContext is already attached to a logic block."
      );
    }

    _logic = logicBlock;
    _isAttached = true;
  }

  public void Detach() => _isAttached = false;

  // Input survives detach so async continuations (which already guard on
  // Status) can deliver results back to the logic block.
  public void Input<TInput>(in TInput input) where TInput : struct =>
    _logic!.Input(input);

  public void Output<TOutput>(in TOutput output)
    where TOutput : struct => ActiveLogic.Output(output);

  public T Get<T>() where T : class => ActiveLogic.Get<T>();
  public bool Has<T>() where T : class => ActiveLogic.Has<T>();

  public bool HasHistory => ActiveLogic.History.Count > 0;
  public void Push(Type type) => ActiveLogic.History.Push(type);
  public Type? Peek() => ActiveLogic.History.Peek();
  public Type? Pop() => ActiveLogic.History.Pop();
  public void ClearHistory() => ActiveLogic.History.Clear();

  // Task operations survive detach — async continuations need these
  public Task Task => _logic?.Task ?? Task.CompletedTask;
  public void TrackTask(Task task) => _logic?.TrackTask(task);
}

/// <summary>
/// Internal state stored in each logic block state. This is used to store
/// entrance and exit callbacks without tripping up equality checking. Forwards to
/// either a <see cref="LogicContext"/> or a <see cref="StateTester"/> to facilitate
/// real logic block operations or unit testing, respectively.
/// </summary>
internal class InternalState : IStateful
{
  /// <summary>
  /// Callbacks to be invoked when the state is entered.
  /// </summary>
  internal List<Action> EnterCallbacks { get; } = [];

  internal List<Func<LogicBlockState?, bool>> EnterPredicates { get; } = [];

  /// <summary>
  /// Callbacks to be invoked when the state is exited.
  /// </summary>
  internal List<Action> ExitCallbacks { get; } = [];

  internal List<Func<LogicBlockState?, bool>> ExitPredicates { get; } = [];

  internal IStateful Context { get; private set; }

  public bool IsAttached => Context.IsAttached;
  public LogicBlockStatus Status => Context.Status;

  internal InternalState()
  {
    Context = new LogicContext();
  }

  internal StateTester Test()
  {
    if (Context is LogicContext context && context.IsAttached)
    {
      // State is currently active in a logic block instance. Not in a position
      // to test it.
      throw new LogicBlockException(
        "Cannot test a state that is currently attached to a real logic " +
        "block. Are you sure you've setup your test correctly?"
      );
    }

    if (Context is StateTester tester)
    {
      // State is already being tested — we can reset and reuse the existing tester.
      tester.Reset();
      return tester;
    }

    // State is not being used and does not have an existing state tester context.
    tester = new StateTester();

    Context = tester;
    return tester;
  }

  internal void AddOnEnterCallback<TState>(Action callback)
  {
    EnterCallbacks.Add(callback);
    EnterPredicates.Add(static (state) => state is TState);
  }

  internal void AddOnExitCallback<TState>(Action callback)
  {
    ExitCallbacks.Add(callback);
    ExitPredicates.Add(static (state) => state is TState);
  }

  internal void CallOnEnterCallbacks(LogicBlockState? previous,
    LogicBlockState? next)
  {
    if (next is null)
    { return; }

    for (var i = 0; i < EnterCallbacks.Count; i++)
    {
      if (EnterPredicates[i](previous))
      {
        // already entered this state type
        continue;
      }

      EnterCallbacks[i]();
    }
  }

  internal void CallOnExitCallbacks(LogicBlockState? previous,
    LogicBlockState? next)
  {
    if (previous is null)
    { return; }

    for (var i = ExitCallbacks.Count - 1; i >= 0; i--)
    {
      if (ExitPredicates[i](next))
      {
        // not actually exiting from this state type
        continue;
      }

      ExitCallbacks[i]();
    }
  }

  // We don't want state logic states to be compared, so we make them
  // always equal to whatever other state logic state they are compared to.
  // This prevents issues where two seemingly equivalent states are not
  // deemed equivalent because their callbacks are different.

  /// <inheritdoc />
  public override bool Equals(object? obj) => true;

  /// <inheritdoc />
  public override int GetHashCode() => base.GetHashCode();

  public void Attach(ILogicBlock logic) => Context.Attach(logic);
  public void Detach() => Context.Detach();

  public void Input<TInput>(in TInput input)
    where TInput : struct => Context.Input(input);

  public void Output<TOutput>(in TOutput output)
    where TOutput : struct => Context.Output(output);

  public TData Get<TData>()
    where TData : class => Context.Get<TData>();

  public bool Has<T>()
    where T : class => Context.Has<T>();

  public bool HasHistory => Context.HasHistory;
  public void Push(Type type) => Context.Push(type);
  public Type? Peek() => Context.Peek();
  public Type? Pop() => Context.Pop();
  public void ClearHistory() => Context.ClearHistory();

  public Task Task => Context.Task;
  public void TrackTask(Task task) => Context.TrackTask(task);
}
