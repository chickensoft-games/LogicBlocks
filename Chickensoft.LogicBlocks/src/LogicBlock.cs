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
public interface ILogicBlock<TState> :
ILogicBlockBase, ISerializableBlackboard where TState : StateLogic<TState> {
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
  /// Produces a logic block output value.
  /// </summary>
  /// <typeparam name="TOutputType">Type of output to produce.</typeparam>
  /// <param name="output">Output value.</param>
  void Output<TOutputType>(in TOutputType output) where TOutputType : struct;

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
  /// Restores the logic block from a deserialized logic block.
  /// </summary>
  /// <param name="logic">Other logic block.</param>
  /// <param name="shouldCallOnEnter">Whether or not to call OnEnter callbacks
  /// when entering the restored state.</param>
  void RestoreFrom(ILogicBlock<TState> logic, bool shouldCallOnEnter = true);

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
  /// <returns>True if the binding was removed, false otherwise.</returns>
  bool RemoveBinding(ILogicBlockBinding<TState> binding);
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
ILogicBlock<TState>, IBoxlessValueHandler where TState : StateLogic<TState> {
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
  public override object? ValueAsObject => _value;

  /// <inheritdoc />
  public override void RestoreState(object state) {
    if (_value is not null) {
      throw new LogicBlockException(
        "Cannot restore state once a state has been initialized."
      );
    }

    RestoredState = (TState)state;
  }
  #endregion LogicBlockBase

  private TState? _value;
  private int _isProcessing;
  private readonly BoxlessQueue _inputs;
  private List<ILogicBlockBinding<TState>> _bindings = [];
  private int _bindingsInvocationCount;
  private bool IsInvokingBindings => _bindingsInvocationCount > 0;
  private readonly HashSet<ILogicBlockBinding<TState>> _bindingsToRemove = [];
  private void StartBindingInvocation() => _bindingsInvocationCount++;
  private void EndBindingInvocation() {
    _bindingsInvocationCount--;

    if (_bindingsInvocationCount == 0) {
      _bindings = [.. _bindings.Where(
        binding => !_bindingsToRemove.Contains(binding)
      )];
      _bindingsToRemove.Clear();
    }
  }

  // Sometimes, it is preferable not to call OnEnter callbacks when starting
  // a logic block, such as when restoring from a saved / serialized logic
  // block.
  private bool _shouldCallOnEnter = true;

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
  public virtual void Output<TOutputType>(
    in TOutputType output
  ) where TOutputType : struct =>
    AnnounceOutput(output);

  /// <inheritdoc />
  public void Start() {
    if (IsProcessing || _value is not null) { return; }

    Flush();
  }

  /// <summary>
  /// Called when the logic block is started. Override this method to
  /// perform any initialization logic.
  /// </summary>
  public virtual void OnStart() { }

  /// <inheritdoc />
  public void Stop() {
    if (IsProcessing || _value is null) { return; }

    OnStop();

    // Repeatedly exit and detach the current state until there is none.
    ChangeState(null);

    _inputs.Clear();

    // A state finally exited and detached without queuing additional inputs.
    _value = null;
  }

  /// <summary>
  /// Called when the logic block is stopped. Override this method to
  /// perform any cleanup logic.
  /// </summary>
  public virtual void OnStop() { }

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
  public void AddBinding(ILogicBlockBinding<TState> binding) {
    if (_bindings.Contains(binding)) {
      return;
    }

    _bindings.Add(binding);
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool RemoveBinding(ILogicBlockBinding<TState> binding) {
    if (IsInvokingBindings) {

    }

    return _bindings.Remove(binding);
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
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected virtual bool CanChangeState(TState? state) =>
    !SerializationUtilities.IsEquivalent(state, _value);

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
      where TStateType : TState {
    try {
      return new(Context.Get<TStateType>());
    }
    catch (Exception e) {
      throw new InvalidOperationException(
        $"Could not retrieve state {typeof(TStateType)}. You may need to add the Meta attribute to your LogicBlock, or add the states to the blackboard manually.",
        e
      );
    }
  }


  #region IReadOnlyBlackboard
  /// <inheritdoc />
  public IReadOnlySet<Type> Types => Blackboard.Types;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public TData Get<TData>() where TData : class => Blackboard.Get<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object GetObject(Type type) => Blackboard.GetObject(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => Blackboard.Has<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => Blackboard.HasObject(type);
  #endregion IReadOnlyBlackboard

  #region IBlackboard
  /// <inheritdoc cref="IBlackboard.Set{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class =>
    Blackboard.Set(data);

  /// <inheritdoc cref="IBlackboard.SetObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(Type type, object data) =>
    Blackboard.SetObject(type, data);

  /// <inheritdoc cref="IBlackboard.Overwrite{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Overwrite<TData>(TData data) where TData : class =>
    Blackboard.Overwrite(data);

  /// <inheritdoc cref="IBlackboard.OverwriteObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(Type type, object data) =>
    Blackboard.OverwriteObject(type, data);
  #endregion IBlackboard

  #region ISerializableBlackboard
  /// <inheritdoc cref="ISerializableBlackboard.SavedTypes" />
  public IEnumerable<Type> SavedTypes => Blackboard.SavedTypes;

  /// <inheritdoc cref="ISerializableBlackboard.TypesToSave" />
  public IEnumerable<Type> TypesToSave => Blackboard.TypesToSave;

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIdentifiable => Blackboard.Save(factory);

  /// <inheritdoc
  /// cref="ISerializableBlackboard.SaveObject(Type, Func{object}, object?)" />
  public void SaveObject(
    Type type, Func<object> factory, object? referenceValue
  ) => Blackboard.SaveObject(type, factory, referenceValue);
  #endregion ISerializableBlackboard

  internal TState ProcessInputs<TInputType>(
    TInputType? input = null
  ) where TInputType : struct {
    _isProcessing++;

    if (_value is null) {
      // No state yet. Let's get the first state going!
      Blackboard.InstantiateAnyMissingSavedData();
      ChangeState(RestoredState as TState ?? GetInitialState().State);
      RestoredState = null;
      OnStart();
    }

    // We can always process the first input directly.
    // This keeps single inputs off the heap.
    if (input.HasValue) {
      (this as IBoxlessValueHandler).HandleValue(input.Value);
    }

    while (_inputs.HasValues) {
      _inputs.Dequeue();
    }

    _isProcessing--;

    _shouldCallOnEnter = true;

    return _value!;
  }

  void IBoxlessValueHandler.HandleValue<TInputType>(in TInputType input)
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

    var stateIsDifferent = CanChangeState(previous);

    if (state is not null) {
      state.Attach(Context);

      if (_shouldCallOnEnter) {
        state.Enter(previous);
      }

      if (stateIsDifferent) {
        AnnounceState(state);
      }
    }
    _isProcessing--;
  }

  internal void InvokeBindings(Action<ILogicBlockBinding<TState>> callback) {
    StartBindingInvocation();
    var i = 0;
    while (i < _bindings.Count) {
      var binding = _bindings[i];
      callback(binding);
      i++;
    }
    EndBindingInvocation();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceInput<TInputType>(in TInputType input)
  where TInputType : struct {
    StartBindingInvocation();
    var i = 0;
    while (i < _bindings.Count) {
      var binding = _bindings[i];
      binding.MonitorInput(in input);
      i++;
    }
    EndBindingInvocation();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceState(TState state) => InvokeBindings(
    binding => binding.MonitorState(state)
  );

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceOutput<TOutputType>(TOutputType output)
  where TOutputType : struct {
    StartBindingInvocation();
    var i = 0;
    while (i < _bindings.Count) {
      var binding = _bindings[i];
      binding.MonitorOutput(in output);
      i++;
    }
    EndBindingInvocation();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceException(Exception exception) => InvokeBindings(
    binding => binding.MonitorException(exception)
  );

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState RunInputHandler<TInputType>(
    IGet<TInputType> inputHandler,
    in TInputType input,
    TState fallback
  ) where TInputType : struct {
    try { return inputHandler.On(in input).State; }
    catch (Exception e) { AddError(e); }
    return fallback;
  }

  /// <summary>
  /// Processes inputs and changes state until there are no more inputs.
  /// </summary>
  /// <returns>The resting state.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState Flush() => ProcessInputs<int>();

  /// <summary>
  /// Determines if two logic blocks are equivalent. Logic blocks are equivalent
  /// if they are the same reference, or if each of their states and blackboards
  /// are equivalent.
  /// </summary>
  /// <param name="obj">Other logic block.</param>
  /// <returns>True if</returns>
  public override bool Equals(object? obj) {
    if (ReferenceEquals(this, obj)) { return true; }

    if (obj is not LogicBlockBase logic) { return false; }

    if (GetType() != logic.GetType()) {
      // Two different types of logic blocks are never equal.
      return false;
    }

    // Ensure current states are equal.
    if (
      !SerializationUtilities.IsEquivalent(ValueAsObject, logic.ValueAsObject)
    ) {
      return false;
    }

    // Ensure blackboard entries are equal.
    var types = Blackboard.Types;
    var otherTypes = logic.Blackboard.Types;

    if (types.Count != otherTypes.Count) { return false; }

    foreach (var type in types) {
      if (!otherTypes.Contains(type)) { return false; }

      var obj1 = Blackboard.GetObject(type);
      var obj2 = logic.Blackboard.GetObject(type);

      if (SerializationUtilities.IsEquivalent(obj1, obj2)) {
        continue;
      }

      return false;
    }

    return true;
  }

  // Equivalent logic blocks have different hash codes because they are
  // different instances.
  /// <inheritdoc />
  public override int GetHashCode() => base.GetHashCode();

  /// <inheritdoc />
  public void RestoreFrom(
    ILogicBlock<TState> logic, bool shouldCallOnEnter = true
  ) {
    _shouldCallOnEnter = shouldCallOnEnter;

    if ((logic.ValueAsObject ?? logic.RestoredState) is not TState state) {
      throw new LogicBlockException(
        $"Cannot restore from an uninitialized logic block ({logic}). Please " +
        "make sure you've called Start() on it first."
      );
    }

    Stop();

    foreach (var type in logic.Blackboard.Types) {
      Blackboard.OverwriteObject(type, logic.Blackboard.GetObject(type));
    }

    var stateType = state.GetType();
    OverwriteObject(stateType, state);
    RestoreState(state);
  }
}
