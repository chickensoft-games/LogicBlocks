namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// <para>
/// A logic block. Logic blocks are machines that receive input, maintain a
/// single state, and produce outputs. They can be used as simple
/// input-to-state reducers, or built upon to create hierarchical state
/// machines.
/// </para>
/// <para>
/// Logic blocks are similar to statecharts, and enable the state pattern to
/// be easily leveraged using traditional object oriented programming built
/// into C#. Each state is a self-contained record.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public interface ILogicBlock<TState> where TState : class, IStateLogic<TState> {
  /// <summary>
  /// Logic block execution context.
  /// </summary>
  IContext Context { get; }
  /// <summary>Current state of the logic block.</summary>
  TState Value { get; }
  /// <summary>
  /// Whether or not the logic block is currently processing inputs.
  /// </summary>
  bool IsProcessing { get; }
  /// <summary>Event invoked whenever an input is processed.</summary>
  event Action<object>? OnInput;
  /// <summary>Event invoked whenever the state is updated.</summary>
  event Action<TState>? OnState;
  /// <summary>
  /// Event invoked whenever an output is produced by an input handler.
  /// </summary>
  event Action<object>? OnOutput;
  /// <summary>
  /// Event invoked whenever an error occurs in a state's input handler.
  /// </summary>
  event Action<Exception>? OnError;
  /// <summary>
  /// Gets data from the blackboard.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="KeyNotFoundException" />
  TData Get<TData>() where TData : class;
  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this to provide a valid initial state.
  /// </summary>
  /// <returns>Initial logic block state.</returns>
  TState GetInitialState();

  /// <summary>
  /// Adds an input value to the logic block's internal input queue.
  /// </summary>
  /// <param name="input">Input to process.</param>
  /// <typeparam name="TInputType">Type of the input.</typeparam>
  /// <returns>Logic block input return value.</returns>
  TState Input<TInputType>(TInputType input) where TInputType : notnull;

  /// <summary>
  /// Creates a binding to a logic block.
  /// </summary>
  /// <returns>Logic block binding.</returns>
  LogicBlock<TState>.IBinding Bind();

  /// <summary>
  /// Restores the logic block from a state. This method can only be called
  /// before the logic block has been started. The state provided to this method
  /// takes precedence over <see cref="GetInitialState"/>, ensuring that the
  /// logic block's first state will be the one provided here.
  /// </summary>
  /// <param name="state">State to use as the logic block's initial state.
  /// </param>
  void Restore(TState state);

  /// <summary>
  /// Starts the logic block by entering the current state. If the logic block
  /// hasn't initialized yet, this will create the initial state before entering
  /// it.
  /// </summary>
  void Start();

  /// <summary>
  /// Stops the logic block by exiting the current state. For best results,
  /// don't continue to give inputs to the logic block after stopping it.
  /// Otherwise, you might end up firing the exit handler of a state more than
  /// once.
  /// </summary>
  void Stop();
}

/// <summary>
/// <para>
/// A synchronous logic block. Logic blocks are machines that process inputs
/// one-at-a-time, maintain a current state graph, and produce outputs.
/// </para>
/// <para>
/// Logic blocks are essentially statecharts that are created using the state
/// pattern. Each state is a self-contained record.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public abstract partial class LogicBlock<TState>
where TState : class, IStateLogic<TState> {
  /// <summary>
  /// Creates a fake logic binding that can be used to more easily test objects
  /// that bind to logic blocks.
  /// </summary>
  /// <returns>Fake binding.</returns>
  public static IFakeBinding CreateFakeBinding() => new FakeBinding();

  /// <summary>
  /// Represents a function which receives an input object and returns a
  /// state.
  /// </summary>
  /// <param name="input">Input object.</param>
  /// <returns>State.</returns>
  internal delegate TState InputHandler(object input);

  /// <summary>
  /// Represents a single input that is waiting to be processed.
  /// </summary>
  internal readonly struct PendingInput {
    /// <summary>Input object.</summary>
    public object Input { get; }
    /// <summary>
    /// A function which returns a function that processes the input.
    /// </summary>
    public Func<InputHandler> GetHandler { get; }

    /// <summary>Create a new pending input.</summary>
    /// <param name="input">Input object.</param>
    /// <param name="getHandler">Function which returns a function that
    /// processes the input.</param>
    public PendingInput(object input, Func<InputHandler> getHandler) {
      Input = input;
      GetHandler = getHandler;
    }
  }

  /// <inheritdoc />
  public abstract TState GetInitialState();

  /// <inheritdoc />
  public event Action<object>? OnInput;
  /// <inheritdoc />
  public event Action<TState>? OnState;
  /// <inheritdoc />
  public event Action<object>? OnOutput;
  /// <inheritdoc />
  public event Action<Exception>? OnError;

  /// <summary>
  /// The context provided to the states of the logic block.
  /// </summary>
  public IContext Context { get; }

  private TState? _value;
  private TState? _restoredState;

  private readonly Queue<PendingInput> _inputs = new();
  private readonly Dictionary<Type, dynamic> _blackboard = new();

  /// <inheritdoc />
  public virtual IBinding Bind() => new Binding(this);

  /// <inheritdoc />
  public void Restore(TState state) {
    if (_restoredState is not null) {
      throw new InvalidOperationException(
        $"Logic block was already restored. Note that a logic block cannot " +
        "be restored more than once."
      );
    }

    if (_value is not null) {
      throw new InvalidOperationException(
        $"Attempted to restore a logic block that was already started. Note " +
        "that a logic block cannot be restored after its first state is " +
        "created."
      );
    }

    _restoredState = state;
  }

  /// <inheritdoc />
  public virtual TState Input<TInputType>(TInputType input)
  where TInputType : notnull {
    _inputs.Enqueue(
      new PendingInput(input, GetInputHandler<TInputType>)
    );

    return Process();
  }

  /// <summary>
  /// <para>
  /// Determines if the logic block can transition to the next state.
  /// </para>
  /// <para>
  /// A logic block can only transition to a state if the state is not
  /// equivalent to the current state. That is, the state must not be the same
  /// reference and must not be equal to the current state (as determined by
  /// the default equality comparer).
  /// </para>
  /// </summary>
  /// <param name="state">Next potential state.</param>
  /// <returns>True if the logic block can change to the given state, false
  /// otherwise.</returns>
  protected virtual bool CanChangeState(TState state) {
    if (
      ReferenceEquals(Value, state) ||
      EqualityComparer<TState>.Default.Equals(state, Value)
    ) {
      // A state may always transition to itself or an equivalent state.
      return false;
    }
    return true;
  }

  /// <summary>
  /// Adds an error to the logic block. Call this from your states to
  /// register errors that occur. Logic blocks are designed to be resilient
  /// to errors, so registering errors instead of stopping execution is
  /// preferred in most cases. You can subscribe to the <see cref="OnError"/>
  /// event and re-throw the error if you want to stop execution.
  /// </summary>
  /// <param name="e">Exception to add.</param>
  internal virtual void AddError(Exception e) {
    OnError?.Invoke(e);
    HandleError(e);
  }

  /// <summary>
  /// Called when the logic block encounters an error. Overriding this method
  /// allows you to customize how errors are handled. If you throw the error
  /// again from this method, you can make errors stop execution.
  /// </summary>
  /// <param name="e">Exception that occurred.</param>
  protected virtual void HandleError(Exception e) { }

  /// <summary>
  /// Produces an output. Outputs are one-shot side effects that allow you
  /// to communicate with the world outside the logic block. Outputs are
  /// equivalent to the idea of actions in statecharts.
  /// </summary>
  /// <param name="output">Output value.</param>
  internal virtual void OutputValue(in object output) =>
    OnOutput?.Invoke(output);

  internal bool GetNextInput(out PendingInput input) {
    if (_inputs.TryDequeue(out var pending)) {
      input = pending;
      return true;
    }
    input = default;
    return false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetState(TState state) => _value = state;

  // Announce state change.
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void FinalizeStateChange(TState state) =>
    OnState?.Invoke(state);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceInput(object input) =>
    OnInput?.Invoke(input);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState GetStartState() {
    if (_restoredState is not { } state) {
      // No state to restore from, so let the developer's override of
      // GetInitialState determine the start state.
      return GetInitialState();
    }

    // Clear restored state and return it as the first state.
    _restoredState = default;
    return state;
  }

  /// <inheritdoc />
  public TData Get<TData>() where TData : class {
    var type = typeof(TData);
    return !_blackboard.TryGetValue(type, out var data)
      ? throw new KeyNotFoundException(
        $"Data of type {type} not found in the blackboard."
      )
      : (data as TData)!;
  }

  /// <summary>
  /// Adds data to the blackboard. Data is retrieved by its type, so do not add
  /// more than one piece of data with the same type.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add.</typeparam>
  /// <exception cref="ArgumentException">Thrown if data of the provided type
  /// has already been added.</exception>
  protected void Set<TData>(TData data) where TData : class {
    var type = typeof(TData);
    if (!_blackboard.TryAdd(type, data)) {
      throw new ArgumentException(
        $"Data of type {type} already exists in the blackboard."
      );
    }
  }

  /// <summary>
  /// Adds new data or overwrites existing data in the blackboard. Data is
  /// retrieved by its type, so this will overwrite any existing data of the
  /// given type, unlike <see cref="Set{TData}(TData)" />.
  /// </summary>
  /// <param name="data">Data to write to the blackboard.</param>
  /// <typeparam name="TData">Type of the data to add or overwrite.</typeparam>
  protected void Overwrite<TData>(TData data) where TData : class =>
    _blackboard[typeof(TData)] = data;

  /// <summary>
  /// Whether or not the logic block is processing inputs.
  /// </summary>
  public bool IsProcessing { get; private set; }

  /// <summary>
  /// <para>Creates a new LogicBlock.</para>
  /// <para>
  /// A logic block is a machine that receives input, maintains a
  /// single state, and produces outputs. It can be used as a simple
  /// input-to-state reducer, or built upon to create a hierarchical state
  /// machine.
  /// </para>
  /// </summary>
  protected LogicBlock() {
    Context = new DefaultContext(this);
  }

  /// <inheritdoc />
  public TState Value => _value ?? AttachState();

  private TState AttachState() {
    IsProcessing = true;
    _value = GetStartState();
    _value.Attach(Context);
    IsProcessing = false;
    return Process();
  }

  /// <summary>
  /// Called whenever an input needs to be processed. This method should remove
  /// the input from the input queue and invoke the appropriate input handler.
  /// It may use <see cref="AnnounceInput(object)" /> to announce when it
  /// processes an individual input. This method should call
  /// <see cref="SetState(TState)" /> whenever the state should be changed, and
  /// <see cref="FinalizeStateChange(TState)" /> after all state changes have
  /// been made.
  /// </summary>
  /// <returns>Returns a value that is returned to the caller by the input
  /// handler.</returns>
  internal TState Process() {
    if (_value is null) {
      // No state yet.
      return AttachState(); // Calls Process() again.
    }

    if (IsProcessing) {
      return Value;
    }

    IsProcessing = true;

    while (GetNextInput(out var pendingInput)) {
      var handler = pendingInput.GetHandler();
      var input = pendingInput.Input;

      // Get next state.
      var state = handler(input);

      AnnounceInput(input);

      if (!CanChangeState(state)) {
        // The only time we can't change states is if the new state is
        // equivalent to the old state (determined by the default equality
        // comparer)
        continue;
      }

      var previous = Value;

      // Exit the previous state.
      previous.Exit(state, AddError);

      previous.Detach();

      SetState(state);

      state.Attach(Context);

      // Enter the next state.
      state.Enter(previous, AddError);

      FinalizeStateChange(state);
    }

    IsProcessing = false;

    return Value;
  }

  /// <inheritdoc />
  public void Start() =>
    Value.Enter(previous: null, onError: AddError);

  /// <inheritdoc />
  public void Stop() {
    Value.Exit(next: null, onError: AddError);
    Value.Detach();
    SetState(null!);
  }

  /// <summary>
  /// <para>
  /// Gets the input handler for a given input type. This can be overridden by
  /// subclasses that want to customize how input handlers are retrieved.
  /// </para>
  /// <para>
  /// Customizing how input handlers are resolved can be useful in scenarios
  /// where you want to redirect input to the state itself or some other
  /// mechanism.
  /// </para>
  /// </summary>
  /// <typeparam name="TInputType">Input type.</typeparam>
  /// <exception cref="InvalidOperationException" />
  private InputHandler GetInputHandler<TInputType>()
    => (input) => {
      if (Value is IGet<TInputType> stateWithHandler) {
        return RunSafe(() => stateWithHandler.On((TInputType)input), Value);
      }

      return Value;
    };

  private TState RunSafe(Func<TState> handler, TState fallback) {
    try { return handler(); }
    catch (Exception e) { AddError(e); }
    return fallback;
  }
}
