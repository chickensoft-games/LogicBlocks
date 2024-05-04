namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Chickensoft.Serialization;

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
public interface ILogicBlock<TState> : ISerializableBlackboard
where TState : StateLogic<TState> {
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

  /// <summary>
  /// Whether or not the logic block has been started. A logic block is started
  /// if its underlying state has been initialized.
  /// </summary>
  bool IsStarted { get; }

  /// <summary>
  /// Returns the initial state of the logic block. Implementations must
  /// override this to provide a valid initial state.
  /// </summary>
  /// <returns>Initial logic block state.</returns>
  LogicBlock<TState>.Transition GetInitialState();

  /// <summary>
  /// Adds an input value to the logic block's internal input queue.
  /// </summary>
  /// <param name="input">Input to process.</param>
  /// <typeparam name="TInputType">Type of the input.</typeparam>
  /// <returns>Logic block input return value.</returns>
  TState Input<TInputType>(in TInputType input) where TInputType : struct;

  /// <summary>
  /// Creates a binding to a logic block.
  /// </summary>
  /// <returns>Logic block binding.</returns>
  LogicBlock<TState>.IBinding Bind();

  /// <summary>
  /// Starts the logic block by entering the current state. If the logic block
  /// is already started, nothing happens. If the logic block
  /// has not initialized its underlying state, it will initialize it by calling
  /// <see cref="GetInitialState" /> and attaching it to the logic block first.
  /// </summary>
  void Start();

  /// <summary>
  /// Stops the logic block. This calls any OnExit callbacks the current state
  /// registered before detaching it. If any inputs are created while the
  /// state is exiting and detaching, they are cleared instead of being
  /// processed.
  /// </summary>
  void Stop();

  /// <summary>
  /// <para>
  /// Forcibly resets the logic block to the specified state, even if the
  /// LogicBlock is on another state that would never transition to the
  /// given state. This can be leveraged by systems outside the logic block's
  /// own states to force the logic block to a specific state, such as when
  /// deserializing a logic block state.
  /// </para>
  /// <para>
  /// If the logic block has no underlying state (because it hasn't been started
  /// or was stopped), this will make the specified state the initial state.
  /// </para>
  /// <para>
  /// When resetting, the logic block will exit and detach any current state,
  /// if it has one, before attaching and entering the given state.
  /// </para>
  /// </summary>
  /// <param name="state">State to forcibly reset to.</param>
  TState ForceReset(TState state);

  /// <summary>
  /// Adds a binding to the logic block. This is used internally by the standard
  /// bindings implementation. Prefer using <see cref="Bind" /> to create an
  /// instance of the standard bindings which allow you to easily observe a
  /// logic block's inputs, states, outputs, and exceptions.
  /// </summary>
  /// <param name="binding">Logic block binding.</param>
  void AddBinding(ILogicBlockBinding<TState> binding);

  /// <summary>
  /// Removes a binding from the logic block. This is used internally by the
  /// standard bindings implementation. Prefer using <see cref="Bind" /> to
  /// create an instance of the standard bindings which allow you to easily
  /// observe a logic block's inputs, states, outputs, and exceptions.
  /// </summary>
  /// <param name="binding">Logic block binding.</param>
  void RemoveBinding(ILogicBlockBinding<TState> binding);
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
public abstract partial class LogicBlock<TState> : LogicBlockBase,
ILogicBlock<TState>, IInputHandler where TState : StateLogic<TState> {
  // We do want static members on generic types here since it makes for a
  // really ergonomic API.
  /// <summary>
  /// Creates a fake logic binding that can be used to more easily test objects
  /// that bind to logic blocks.
  /// </summary>
  /// <returns>Fake binding.</returns>
  public static IFakeBinding CreateFakeBinding() => new FakeBinding();

  /// <inheritdoc />
  public IContext Context { get; }

  /// <inheritdoc />
  public bool IsProcessing => _isProcessing > 0;

  /// <inheritdoc />
  public bool IsStarted => _value is not null;

  /// <inheritdoc />
  public TState Value => _value ?? Flush();

  #region LogicBlockBase
  /// <inheritdoc />
  internal override object? ValueAsObject => _value;

  /// <inheritdoc />
  public override void RestoreState(object state) {
    if (_value is not null) {
      throw new LogicBlockException(
        "Cannot restore state once a state has been initialized."
      );
    }

    _restoredState = (TState)state;
  }
  #endregion LogicBlockBase

  private TState? _restoredState;
  private TState? _value;
  private int _isProcessing;
  private readonly InputQueue _inputs;
  private readonly HashSet<ILogicBlockBinding<TState>> _bindings = new();

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
    _inputs = new(this);
    Context = new DefaultContext(this);
    PreallocateStates(this);
  }

  /// <inheritdoc />
  public abstract Transition GetInitialState();

  /// <inheritdoc />
  public virtual IBinding Bind() => new Binding(this);

  /// <inheritdoc />
  public virtual TState Input<TInputType>(
    in TInputType input
  ) where TInputType : struct {
    if (IsProcessing) {
      _inputs.Enqueue(input);
      return Value;
    }
    return ProcessInputs<TInputType>(input);
  }

  /// <inheritdoc />
  public void Start() {
    if (IsProcessing || _value is not null) { return; }

    Flush();
  }

  /// <inheritdoc />
  public void Stop() {
    if (IsProcessing || _value is null) { return; }

    // Repeatedly exit and detach the current state until there is none.
    ChangeState(null);

    _inputs.Clear();

    // A state finally exited and detached without queuing additional inputs.
    _value = null;
  }

  /// <inheritdoc />
  public TState ForceReset(TState state) {
    if (IsProcessing) {
      throw new LogicBlockException(
        "Cannot force reset a logic block while it is processing inputs. " +
        "Do not call ForceReset() from inside a logic block's own state."
      );
    }

    ChangeState(state);

    return Flush();
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void AddBinding(ILogicBlockBinding<TState> binding) =>
    _bindings.Add(binding);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void RemoveBinding(ILogicBlockBinding<TState> binding) =>
    _bindings.Remove(binding);

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
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected virtual bool CanChangeState(TState state) =>
    !IsEquivalent(state, _value);

  /// <summary>
  /// Adds an error to the logic block. Call this from your states to
  /// register errors that occur. Logic blocks are designed to be resilient
  /// to errors, so registering errors instead of stopping execution is
  /// preferred in most cases.
  /// </summary>
  /// <param name="e">Exception to add.</param>
  internal virtual void AddError(Exception e) {
    AnnounceException(e);
    HandleError(e);
  }

  /// <summary>
  /// Produces an output. Outputs are one-shot side effects that allow you
  /// to communicate with the world outside the logic block. Outputs are
  /// equivalent to the idea of actions in statecharts.
  /// </summary>
  /// <typeparam name="TOutput">Output type.</typeparam>
  /// <param name="output">Output value.</param>
  internal virtual void OutputValue<TOutput>(in TOutput output)
    where TOutput : struct => AnnounceOutput(output);

  /// <summary>
  /// Called when the logic block encounters an error. Overriding this method
  /// allows you to customize how errors are handled. If you throw the error
  /// again from this method, you can make errors stop execution.
  /// </summary>
  /// <param name="e">Exception that occurred.</param>
  protected virtual void HandleError(Exception e) { }

  /// <summary>
  /// Defines a transition to a state stored on the logic block's blackboard.
  /// </summary>
  /// <typeparam name="TStateType">Type of state to transition to.</typeparam>
  protected Transition To<TStateType>()
    where TStateType : TState => new(Context.Get<TStateType>());

  #region IReadOnlyBlackboard
  /// <inheritdoc />
  public IReadOnlySet<Type> Types => _blackboard.Types;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public TData Get<TData>() where TData : class => _blackboard.Get<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object GetObject(Type type) => _blackboard.GetObject(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => _blackboard.Has<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => _blackboard.HasObject(type);
  #endregion IReadOnlyBlackboard

  #region IBlackboard
  /// <inheritdoc cref="IBlackboard.Set{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class =>
    _blackboard.Set(data);

  /// <inheritdoc cref="IBlackboard.SetObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(Type type, object data) =>
    _blackboard.SetObject(type, data);

  /// <inheritdoc cref="IBlackboard.Overwrite{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Overwrite<TData>(TData data) where TData : class =>
    _blackboard.Overwrite(data);

  /// <inheritdoc cref="IBlackboard.OverwriteObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(Type type, object data) =>
    _blackboard.OverwriteObject(type, data);
  #endregion IBlackboard

  #region ISerializableBlackboard
  /// <inheritdoc cref="ISerializableBlackboard.SavedTypes" />
  public IEnumerable<Type> SavedTypes => _blackboard.SavedTypes;

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIdentifiable => _blackboard.Save(factory);

  /// <inheritdoc
  ///   cref="ISerializableBlackboard.SaveObject(Type, Func{object})" />
  public void SaveObject(Type type, Func<object> factory) =>
    _blackboard.SaveObject(type, factory);
  #endregion ISerializableBlackboard

  internal TState ProcessInputs<TInputType>(
    TInputType? input = null
  ) where TInputType : struct {
    _isProcessing++;

    if (_value is null) {
      // No state yet. Let's get the first state going!
      _value = _restoredState ?? GetInitialState().State;
      _restoredState = null;
      _value.Attach(Context);
      _value.Enter(null);
    }

    // We can always process the first input directly.
    // This keeps single inputs off the heap.
    if (input.HasValue) {
      (this as IInputHandler).HandleInput(input.Value);
    }

    while (_inputs.HasInputs) {
      _inputs.HandleInput();
    }

    _isProcessing--;

    return _value;
  }

  void IInputHandler.HandleInput<TInputType>(in TInputType input)
  where TInputType : struct {
    if (_value is not IGet<TInputType> stateWithInputHandler) {
      return;
    }

    // Run the input handler on the state to get the next state.
    var state = RunInputHandler(stateWithInputHandler, in input, _value);

    AnnounceInput(in input);

    if (!CanChangeState(state)) {
      // The only time we can't change states is if the new state is
      // equivalent to the old state (determined by the default equality
      // comparer)
      return;
    }

    ChangeState(state);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ChangeState(TState? state) {
    _isProcessing++;
    var previous = _value;

    previous?.Exit(state);
    previous?.Detach();

    _value = state;

    if (state is not null) {
      state.Attach(Context);
      state.Enter(previous);
      AnnounceState(state);
    }
    _isProcessing--;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceInput<TInputType>(in TInputType input)
  where TInputType : struct {
    foreach (var binding in _bindings) {
      binding.MonitorInput(in input);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceState(TState state) {
    foreach (var binding in _bindings) {
      binding.MonitorState(state);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceOutput<TOutputType>(TOutputType output)
  where TOutputType : struct {
    foreach (var binding in _bindings) {
      binding.MonitorOutput(in output);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceException(Exception exception) {
    foreach (var binding in _bindings) {
      binding.MonitorException(exception);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState RunInputHandler<TInputType>(
    IGet<TInputType> inputHandler,
    in TInputType input,
    TState fallback
  ) where TInputType : struct {
    try { return inputHandler.On(input).State; }
    catch (Exception e) { AddError(e); }
    return fallback;
  }

  /// <summary>
  /// Processes inputs and changes state until there are no more inputs.
  /// </summary>
  /// <returns>The resting state.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState Flush() {
    if (_value is null) {
      _blackboard.InstantiateAnyMissingSavedData();
    }

    return ProcessInputs<int>();
  }

  /// <summary>
  /// Determines if two logic blocks are equivalent. Logic blocks are equivalent
  /// if they are the same reference, or if each of their states and blackboards
  /// are equivalent.
  /// </summary>
  /// <param name="obj">Other logic block.</param>
  /// <returns>True if</returns>
  public override bool Equals(object? obj) {
    if (obj is not LogicBlockBase logic) { return false; }

    if (GetType() != logic.GetType()) {
      // Two different types of logic blocks are never equal.
      return false;
    }

    // Ensure current states are equal.
    if (!IsEquivalent(ValueAsObject, logic.ValueAsObject)) {
      return false;
    }

    // Ensure blackboard entries are equal.
    var types = _blackboard.Types;
    var otherTypes = logic._blackboard.Types;

    if (types.Count != otherTypes.Count) { return false; }

    foreach (var type in types) {
      if (!otherTypes.Contains(type)) { return false; }

      var obj1 = _blackboard.GetObject(type);
      var obj2 = logic._blackboard.GetObject(type);

      if (IsEquivalent(obj1, obj2)) {
        continue;
      }

      return false;
    }

    return true;
  }

  /// <inheritdoc />
  public override int GetHashCode() => base.GetHashCode();
}
