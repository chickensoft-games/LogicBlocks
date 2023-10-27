namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

/// <summary>
/// A logic block. Logic blocks are machines that receive input, maintain a
/// single state, and produce outputs. They can be used as simple
/// input-to-state reducers, or built upon to create hierarchical state
/// machines.
/// </summary>
/// <typeparam name="TState">Base state type.</typeparam>
/// <typeparam name="THandler">Input handler type.</typeparam>
/// <typeparam name="TInputReturn">Input method return type.</typeparam>
/// <typeparam name="TUpdate">Update callback type.</typeparam>
public partial interface ILogic<TState, THandler, TInputReturn, TUpdate>
where TState : Logic<TState, THandler, TInputReturn, TUpdate>.ILogicState {
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
  TData Get<TData>() where TData : notnull;
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
  TInputReturn Input<TInputType>(TInputType input) where TInputType : notnull;

  /// <summary>
  /// Creates a binding to a logic block.
  /// </summary>
  /// <returns>Logic block binding.</returns>
  Logic<TState, THandler, TInputReturn, TUpdate>.IBinding Bind();
}

/// <summary>
/// A logic block. Logic blocks are machines that receive input, maintain a
/// single state, and produce outputs. They can be used as simple
/// input-to-state reducers, or built upon to create hierarchical state
/// machines.
/// </summary>
/// <typeparam name="TState">Base state type.</typeparam>
/// <typeparam name="THandler">Input handler type.</typeparam>
/// <typeparam name="TInputReturn">Input method return type.</typeparam>
/// <typeparam name="TUpdate">Update callback type.</typeparam>
public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> :
  ILogic<TState, THandler, TInputReturn, TUpdate> where TState : Logic<
    TState, THandler, TInputReturn, TUpdate
  >.ILogicState {
  internal readonly struct PendingInput {
    public object Input { get; }
    /// <summary>
    /// A function which returns a function that processes the input.
    /// </summary>
    public Func<THandler> GetHandler { get; }

    public PendingInput(object input, Func<THandler> getHandler) {
      Input = input;
      GetHandler = getHandler;
    }
  }

  /// <summary>
  /// Creates a fake logic binding that can be used to more easily test objects
  /// that bind to logic blocks.
  /// </summary>
  /// <returns>Fake binding.</returns>
  public static IFakeBinding CreateFakeBinding() => new FakeBinding();

  /// <summary>
  /// Creates a fake context that can be used to more easily test logic block
  /// states.
  /// </summary>
  /// <returns>Fake context.</returns>
  public static IFakeContext CreateFakeContext() => new FakeContext();

  internal readonly struct UpdateCallback {
    public TUpdate Callback { get; }
    public Func<dynamic?, bool> IsType { get; }

    public UpdateCallback(TUpdate callback, Func<dynamic?, bool> isType) {
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

  /// <inheritdoc />
  public event Action<object>? OnInput;
  /// <inheritdoc />
  public event Action<TState>? OnState;
  /// <inheritdoc />
  public event Action<object>? OnOutput;
  /// <inheritdoc />
  public event Action<Exception>? OnError;

  /// <inheritdoc />
  public TState Value => _value ??= GetInitialState();

  /// <inheritdoc />
  public abstract bool IsProcessing { get; }

  internal TState? _value;

  private readonly Queue<PendingInput> _inputs = new();
  private readonly Dictionary<Type, dynamic> _blackboard = new();

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

  /// <inheritdoc />
  public virtual IBinding Bind() => new Binding(this);

  /// <inheritdoc />
  public abstract TState GetInitialState();

  /// <inheritdoc />
  public virtual TInputReturn Input<TInputType>(TInputType input)
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
  internal abstract THandler GetInputHandler<TInputType>();

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
    OnState?.Invoke(state);

  internal void AnnounceInput(object input) =>
    OnInput?.Invoke(input);

  /// <inheritdoc />
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
