namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using WeakEvent;

/// <summary>
/// A logic block. Logic blocks are machines that receive input, maintain a
/// single state, and produce outputs. They can be used as simple
/// input-to-state reducers, or built upon to create hierarchical state
/// machines.
/// </summary>
/// <typeparam name="TInput">Base input type.</typeparam>
/// <typeparam name="TState">Base state type.</typeparam>
/// <typeparam name="TOutput">Base output type.</typeparam>
/// <typeparam name="THandler">Input handler type.</typeparam>
/// <typeparam name="TInputReturn">Input method return type.</typeparam>
/// <typeparam name="TUpdate">Update callback type.</typeparam>
public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
>
  where TInput : notnull
  where TState : Logic<
    TInput, TState, TOutput, THandler, TInputReturn, TUpdate
  >.IStateLogic
  where TOutput : notnull {
  internal readonly struct PendingInput {
    public TInput Input { get; }
    /// <summary>
    /// A function which returns a function that processes the input.
    /// </summary>
    public Func<THandler> GetHandler { get; }

    public PendingInput(TInput input, Func<THandler> getHandler) {
      Input = input;
      GetHandler = getHandler;
    }
  }

  internal readonly struct UpdateCallback {
    public TUpdate Callback { get; }
    public Func<dynamic, bool> IsType { get; }

    public UpdateCallback(TUpdate callback, Func<dynamic, bool> isType) {
      Callback = callback;
      IsType = isType;
    }
  }

  /// <summary>
  /// Signature of a callback action which can be invoked when a transition
  /// occurs from one state to another.
  /// </summary>
  /// <param name="stateA">Starting (previous) state.</param>
  /// <param name="stateB">Ending (current) state.</param>
  /// <typeparam name="TStateTypeA">Type of the starting (previous) state.
  /// </typeparam>
  /// <typeparam name="TStateTypeB">Type of the ending (current) state.
  /// </typeparam>
  public delegate void Transition<TStateTypeA, TStateTypeB>(
    TStateTypeA stateA, TStateTypeB stateB
  ) where TStateTypeA : TState where TStateTypeB : TState;

  /// <summary>Event invoked whenever an input is processed.</summary>
  public event EventHandler<TInput> OnInput {
    add => _inputEventSource.Subscribe(value);
    remove => _inputEventSource.Unsubscribe(value);
  }

  /// <summary>Event invoked whenever the state is updated.</summary>
  public event EventHandler<TState> OnState {
    add => _stateEventSource.Subscribe(value);
    remove => _stateEventSource.Unsubscribe(value);
  }

  /// <summary>
  /// Event invoked whenever an error occurs in an input handler.
  /// </summary>
  public event EventHandler<Exception> OnError {
    add => _errorEventSource.Subscribe(value);
    remove => _errorEventSource.Unsubscribe(value);
  }

  /// <summary>
  /// Event invoked whenever an output is produced by an input handler.
  /// </summary>
  public event EventHandler<TOutput> OnOutput {
    add => _outputEventSource.Subscribe(value);
    remove => _outputEventSource.Unsubscribe(value);
  }

  /// <summary>Current state of the logic block.</summary>
  public TState Value => _value ??= GetInitialState();

  private TState? _value;

  /// <summary>
  /// Whether or not the logic block is currently processing inputs.
  /// </summary>
  public abstract bool IsProcessing { get; }

  private readonly Queue<PendingInput> _inputs = new();
  private readonly Dictionary<Type, dynamic> _blackboard = new();

  private readonly WeakEventSource<TInput> _inputEventSource = new();
  private readonly WeakEventSource<TState> _stateEventSource = new();
  private readonly WeakEventSource<Exception> _errorEventSource = new();
  private readonly WeakEventSource<TOutput> _outputEventSource = new();

  /// <summary>
  /// <para>Creates a new LogicBlock.</para>
  /// <para>
  /// A logic block is a machine that receives input, maintains a
  /// single state, and produces outputs. It can be used as a simple
  /// input-to-state reducer, or built upon to create a hierarchical state
  /// machine.
  /// </para>
  /// </summary>
  internal Logic() { }

  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this to provide a valid initial state.
  /// </summary>
  /// <returns>Initial logic block state.</returns>
  public abstract TState GetInitialState();

  /// <summary>
  /// Adds an input value to the logic block's internal input queue.
  /// </summary>
  /// <param name="input">Input to process.</param>
  /// <typeparam name="TInputType">Type of the input.</typeparam>
  /// <returns>Logic block input return value.</returns>
  public virtual TInputReturn Input<TInputType>(TInputType input)
    where TInputType : TInput {
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
      (
        state is IEquatable<TState> &&
        EqualityComparer<TState>.Default.Equals(state, Value)
      )
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
    _errorEventSource.Raise(this, e);
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
  internal virtual void OutputValue(TOutput output) =>
    _outputEventSource.Raise(this, output);

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
  internal abstract THandler GetInputHandler<TInputType>()
    where TInputType : TInput;

  /// <summary>
  /// Called whenever an input needs to be processed. This method should remove
  /// the input from the input queue and invoke the appropriate input handler.
  /// It may use <see cref="AnnounceInput(TInput)" /> to announce when it
  /// processes an individual input. This method should call
  /// <see cref="SetState(TState)" /> whenever the state should be changed, and
  /// <see cref="FinalizeStateChange(TState)" /> after all state changes have
  /// been made.
  /// </summary>
  /// <returns>Returns a value that is returned to the caller by the input
  /// handler.</returns>
  internal abstract TInputReturn Process();

  internal bool GetNextInput(out PendingInput input) {
    if (_inputs.TryDequeue(out var pending)) {
      input = pending;
      return true;
    }
    input = default;
    return false;
  }

  internal void SetState(TState state) => _value = state;

  // Announce state change.
  internal void FinalizeStateChange(TState state) =>
    _stateEventSource.Raise(this, state);

  internal void AnnounceInput(TInput input) =>
    _inputEventSource.Raise(this, input);

  /// <summary>
  /// Gets data from the blackboard.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="KeyNotFoundException" />
  public TData Get<TData>() where TData : notnull {
    var type = typeof(TData);
    return !_blackboard.TryGetValue(type, out var data)
      ? throw new KeyNotFoundException(
        $"Data of type {type} not found in the blackboard."
      )
      : (TData)data;
  }

  /// <summary>
  /// Adds data to the blackboard. Data is retrieved by its type, so do not add
  /// more than one piece of data with the same type.
  /// </summary>
  /// <param name="data">Closure which returns the data.</param>
  /// <typeparam name="TData">Type of the data to add.</typeparam>
  /// <exception cref="ArgumentException">Thrown if data of the provided type
  /// has already been added.</exception>
  protected void Set<TData>(TData data) where TData : notnull {
    var type = typeof(TData);
    if (_blackboard.ContainsKey(type)) {
      throw new ArgumentException(
        $"Data of type {type} already exists in the blackboard."
      );
    }
    _blackboard.Add(type, data);
  }
}
