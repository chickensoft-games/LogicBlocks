namespace Chickensoft.LogicBlocks;

using System.Runtime.CompilerServices;
using Collections;
using Sync;
using Sync.Primitives;

/// <summary>
/// <para>
/// A logic block is a hierarchical state machine that can process inputs,
/// produce outputs, handle errors, and maintain a history of states (like a
/// pushdown automaton).
/// </para>
/// <para>
/// Logic blocks can be started, stopped, saved, loaded, and easily observed
/// using bindings. Logic blocks are decoupled from their dependencies and
/// optimized to prevent memory allocations during input handler execution in
/// the vast majority of scenarios.
/// </para>
/// </summary>
public interface ILogicBlock :
  IDisposable, IBlackboard, IAutoObject<LogicBlock.Binding>
{
  /// <summary>
  /// Blackboard containing dependencies needed by the states in the state
  /// machine.
  /// </summary>
  Blackboard Blackboard { get; }

  /// <summary>
  /// Logic block state type history stack. States can push and pop state types from
  /// this stack to return to previous states as desired. Logic block history can be
  /// configured with a maximum size to prevent unbound memory growth. See
  /// <see cref="LogicBlock(Type?, Blackboard?, int?)"/> for more details.
  /// </summary>
  History History { get; }

  /// <summary>
  /// Provide an input to the current state. If the current state handles the
  /// input, it will return the next state. If the state machine is already
  /// executing an input handler, this will queue the input to be processed.
  /// </summary>
  /// <typeparam name="TInput">Input value type.</typeparam>
  /// <param name="input">Input value.</param>
  /// <returns>The resulting state. If the state machine is already executing
  /// an input handler, the input will be queued and the current state will be
  /// returned.</returns>
  LogicBlockState Input<TInput>(in TInput input) where TInput : struct;

  /// <summary>
  /// Current state of the logic block, or null if the logic block has not been
  /// started or has been stopped.
  /// </summary>
  LogicBlockState? State { get; }

  /// <summary>
  /// Starts the logic block by entering the specified state. If the logic block
  /// is already started, nothing happens.
  /// </summary>
  /// <param name="initialStateType">Initial state to enter.</param>
  /// <param name="shouldCallOnEnter">Whether or not to call the OnEnter
  /// callback for the initial state. Default is true.</param>
  /// <returns>The current state of the logic block after starting (after any
  /// added inputs settle).</returns>
  LogicBlockState Start(Type initialStateType, bool shouldCallOnEnter = true);

  /// <summary>
  /// Starts the logic block by entering the specified state. If the logic block
  /// is already started, nothing happens.
  /// </summary>
  /// <param name="shouldCallOnEnter">Whether or not to call the OnEnter
  /// callback for the initial state. Default is true.</param>
  /// <typeparam name="TState">Type of the initial state to enter.</typeparam>
  /// <returns>The current state of the logic block after starting (after any
  /// added inputs settle).</returns>
  LogicBlockState Start<TState>(bool shouldCallOnEnter = true)
    where TState : LogicBlockState;

  /// <summary>
  /// Loads and starts the logic block, resuming execution immediately. A logic block
  /// will attempt to restore any missing blackboard data objects, states, and history
  /// from the data provided in <see cref="LogicBlockData"/>.
  /// </summary>
  /// <param name="data">Data to load.</param>
  /// <param name="shouldCallOnEnter">Whether or not to call the state entrance
  /// callbacks for the loaded state. Default is true.</param>
  /// <returns>The current state of the logic block after loading (after any
  /// added inputs settle).</returns>
  LogicBlockState Start(
    LogicBlockData data, bool shouldCallOnEnter = true
  );

  /// <summary>
  /// Stops the logic block. This calls any exit callbacks for the current state
  /// before detaching it. If any inputs are created while the
  /// state is exiting and detaching, they are cleared instead of being
  /// processed.
  /// </summary>
  void Stop();

  /// <summary>
  /// Callback invoked when the logic block is started. This is a great place to
  /// setup listeners for any reactive dependencies provided on the blackboard.
  /// </summary>
  void OnStart();

  IEnumerable<IDisposable> OnStartSubscriptions();
  void OnStopSubscriptions();

  /// <summary>
  /// Callback invoked when the logic block is stopped. This is a great place to
  /// clean up listeners.
  /// </summary>
  void OnStop();

  /// <summary>
  /// A task that completes when all in-flight async operations tracked by this
  /// logic block have completed. If no async operations are in progress, this
  /// returns a completed task.
  /// </summary>
  Task Task { get; }

  LogicBlockData GetData();
}

/// <inheritdoc cref="ILogicBlock" path="/summary" />
public abstract partial class LogicBlock : ILogicBlock,
  IPerformAnyOperation,
  IPerform<LogicBlock.StartOp>,
  IPerform<LogicBlock.StartFromDataOp>,
  IPerform<LogicBlock.StopOp>,
  IPerform<LogicBlock.DisposeOp>
{
  // Atomic operations
  private readonly record struct StartOp(bool IsLoading = false);

  private readonly record struct StartFromDataOp(
    LogicBlockData Data,
    bool ShouldCallOnEnter = true
  );

  private readonly record struct StopOp();

  private readonly record struct DisposeOp();

  private readonly TaskTracker _taskTracker = new();

  internal Blackboard? _blackboard;
  internal LogicBlockState? _state;
  internal History? _history;
  private List<IDisposable>? _disposables = [];

  public History History => _history ??=
    new History(maxCapacity: _maxHistoryCapacity);

  private readonly int? _maxHistoryCapacity;
  private readonly SyncSubject _subject;

  public Blackboard Blackboard => _blackboard ?? throw Disposed;

  public LogicBlockState? State => _state;

  public Task Task => _taskTracker.Task;
  internal void TrackTask(Task task) => _taskTracker.TrackTask(task);

  public bool IsBusy => _subject.IsBusy;
  public LogicBlockStatus Status { get; internal set; } = LogicBlockStatus.Stopped;
  public bool IsStarted => Status == LogicBlockStatus.Started;
  public bool IsStopped => Status == LogicBlockStatus.Stopped;
  public bool IsDisposed => Status == LogicBlockStatus.Disposed;

  internal static LogicBlockException NotStarted =>
    new(
      "Logic block has not been started. Call Start() first."
    );

  internal static LogicBlockException AlreadyStarted =>
    new("Logic block is already started.");

  internal static LogicBlockException Disposed =>
    new("Logic block has been disposed.");


  public const int MAX_HISTORY_DEFAULT = 8;

  protected LogicBlock() : this(null, MAX_HISTORY_DEFAULT) { }

  protected LogicBlock(
    Blackboard? blackboard = null,
    int? maxHistory = MAX_HISTORY_DEFAULT
  )
  {
    _blackboard = blackboard ?? new Blackboard();
    _maxHistoryCapacity = maxHistory;
    _subject = new SyncSubject(this);
  }

  ~LogicBlock()
  {
    Dispose(false);
  }

  #region StartAndStop

  public LogicBlockState Start(
    Type initialStateType, bool shouldCallOnEnter = true
  )
  {
    // start must be called before other operations

    if (IsStarted)
    { throw AlreadyStarted; }

    if (IsDisposed)
    { throw Disposed; }

    Status = LogicBlockStatus.Started;

    _state = LookupInitialState(initialStateType);

    _subject.Perform(new StartOp());

    // state may have changed after broadcasting *if* the initial state sequence
    // resulted in inputs that were handled synchronously and resulted in state
    // change(s)
    return _state;
  }

  public LogicBlockState Start<TState>(bool shouldCallOnEnter = true)
    where TState : LogicBlockState =>
    Start(typeof(TState), shouldCallOnEnter);

  // defer stop until after current operations, otherwise stop right away
  public void Stop() => _subject.Perform(new StopOp());

  public virtual void OnStart() { }

  public virtual IEnumerable<IDisposable> OnStartSubscriptions()
  {
    yield break;
  }

  public virtual void OnStop() { }

  public virtual void OnStopSubscriptions() { }

  #endregion StartAndStop

  #region Persistence

  public LogicBlockState Start(
    LogicBlockData data, bool shouldCallOnEnter = true
  )
  {
    // can only be called when the logic block is stopped and not disposed
    ValidateNotStartedAndNotDisposed();

    _subject.Perform(new StartFromDataOp(data, shouldCallOnEnter));

    return _state!;
  }

  public LogicBlockData GetData()
  {
    ValidateStartedAndNotDisposed();

    return new LogicBlockData(State!.GetType(), Blackboard, History);
  }

  // Called immediately after a logic block has loaded from LogicBlockData
  internal virtual void Loaded() { }

  #endregion Persistence

  public LogicBlockState Input<TInput>(in TInput input) where TInput : struct
  {
    ValidateStartedAndNotDisposed();

    _subject.Perform(input);

    return _state!;
  }

  internal void Output<TOutput>(in TOutput output)
    where TOutput : struct
  {
    ValidateStartedAndNotDisposed();


    // immediately invokes output bindings when called, rather than scheduling them.
    // this technically allows re-entry on output bindings...but because this method
    // can only be called from a state, developers are more or less prevented
    // from accidentally re-entering via outputs (you'd have to write some awful code
    // to get around that).
    _subject.Broadcast(new OutputBroadcast<TOutput>(output)); // runs user code
  }

  #region AtomicOperations

  void IPerformAnyOperation.Perform<TOp>(in TOp op) where TOp : struct
  {
    if (op is not StartOp and not StartFromDataOp and not StopOp
        and not DisposeOp)
    {
      HandleInput(op);
    }
  }

  void IPerform<StartOp>.Perform(in StartOp op)
  {
    // run the second half of a state change sequence since there's no state to exit
    var state = _state!;

    state.Attach(this);
    state.Enter(null); // runs user code

    OnStart(); // runs user code

    var disposables = OnStartSubscriptions(); // runs user code

    foreach (var disposable in disposables)
    {
      _disposables!.Add(disposable);
    }

    // announce that we've started
    _subject.Broadcast(new StartedBroadcast()); // runs user code

    // also announce the state itself for any bindings that may be listening
    _subject.Broadcast(new StateBroadcast(state)); // runs user code

    if (op.IsLoading)
    {
      Loaded(); // AutoBlock extends this to run user code, OnLoad()

      _subject.Broadcast(new LoadedBroadcast()); // runs user code
    }
  }

  void IPerform<StopOp>.Perform(in StopOp op)
  {
    if (!IsStarted)
    { return; }

    _state!.Exit(null); // runs user code
    _state.Detach();

    _state = null;

    OnStop();
    OnStopSubscriptions();

    foreach (var disposable in _disposables!)
    {
      disposable.Dispose();
    }

    _disposables.Clear();

    _subject.Broadcast(new StoppedBroadcast());

    // history might be null since we create it lazily (most logic blocks don't
    // make use of it)
    _history?.Clear();
    _history = null;

    // any pending operations previously scheduled are cleared
    // (no restart from reentry allowed)
    _subject.Clear();

    Status = LogicBlockStatus.Stopped;
  }

  void IPerform<StartFromDataOp>.Perform(in StartFromDataOp op)
  {
    var data = op.Data;
    var blackboard = data.Blackboard;

    foreach (var type in blackboard.Types)
    {
      // deserialized objects take precedence
      Blackboard.OverwriteObject(type, blackboard.GetObject(type));
    }

    // Restore history from saved data
    foreach (var type in data.History)
    {
      History.Push(type);
    }

    Status = LogicBlockStatus.Started;

    _state = LookupInitialState(data.StateType);

    (this as IPerform<StartOp>).Perform(new StartOp(true));
  }

  void IPerform<DisposeOp>.Perform(in DisposeOp op)
  {
    _blackboard = null;
    _state = null;
    _history = null;
    _disposables = null;

    Status = LogicBlockStatus.Disposed;

    _subject.ClearBindings();
  }

  private void HandleInput<TInput>(in TInput input) where TInput : struct
  {
    var state = _state;
    if (_state is IGetEveryInput handlerForEverything)
    {
      var stateType = handlerForEverything.On(in input);
      state = (LogicBlockState)GetObject(stateType);
    }
    // only check for specific handler if the state doesn't have an any-input handler
    else if (_state is IGet<TInput> handler)
    {
      var stateType = handler.On(in input);
      state = (LogicBlockState)GetObject(stateType);
    }

    _subject.Broadcast(new InputBroadcast<TInput>(input));

    if (IsEquivalent(state, _state))
    {
      return;
    }

    var previous = _state;
    previous!.Exit(state); // runs user code
    previous.Detach();
    _state = state;
    state?.Attach(this);
    state?.Enter(previous); // runs user code

    if (state is not null)
    {
      _subject.Broadcast(new StateBroadcast(state)); // runs user code
    }
  }

  #endregion AtomicOperations

  #region Utilities

  /// <summary>
  /// Determines if two logic block states are equivalent. Logic block states
  /// are equivalent if they are the same reference or are equal according to
  /// the default equality comparer.
  /// </summary>
  /// <param name="a">First state.</param>
  /// <param name="b">Second state.</param>
  /// <returns>True if the states are equivalent.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsEquivalent(object? a, object? b) =>
    ReferenceEquals(a, b) || (
      a is null &&
      b is null
    ) || (
      a is not null &&
      b is not null &&
      EqualityComparer<object>.Default.Equals(a, b)
    );

  public Binding Bind() => new(_subject);

  public void ClearBindings() => _subject.ClearBindings();

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    if (IsDisposed) { return; }

    // stops the logic block if needed
    _subject.Perform(new StopOp());
    // a logic block can't be restarted after being disposed
    _subject.Perform(new DisposeOp());
    _subject.Dispose();
  }

  private LogicBlockState LookupInitialState(Type initialStateType)
  {
    if (
      !_blackboard!.HasObject(initialStateType) ||
      _blackboard.GetObject(initialStateType) is not LogicBlockState state
    )
    {
      // courtesy error — if you haven't set the initial state, you likely haven't
      // set any of them yet
      throw new LogicBlockException(
        $"Initial state of type {initialStateType} has not been set on the " +
        "logic block. Please set an instance of each state type used on the " +
        "logic block."
      );
    }

    return state;
  }

  private void ValidateNotStartedAndNotDisposed()
  {
    if (IsDisposed)
    { throw Disposed; }

    if (IsStarted)
    { throw AlreadyStarted; }
  }

  private void ValidateStartedAndNotDisposed()
  {
    if (IsDisposed)
    { throw Disposed; }

    if (!IsStarted)
    { throw NotStarted; }
  }

  #endregion Utilities
}
