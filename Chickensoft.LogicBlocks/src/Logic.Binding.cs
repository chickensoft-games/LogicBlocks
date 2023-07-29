namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
> {
  /// <summary>
  /// Creates a binding to a logic block.
  /// </summary>
  /// <returns>Logic block binding.</returns>
  public Binding Bind() => new(this);

  /// <summary>
  /// <para>State bindings for a logic block.</para>
  /// <para>
  /// A binding allows you to select data from a logic block's state, invoke
  /// methods when certain states occur, and handle outputs. Using bindings
  /// enable you to write more declarative code and prevent unnecessary
  /// updates when a state has changed but the relevant data within it has not.
  /// </para>
  /// </summary>
  public sealed class Binding : IDisposable {
    /// <summary>Logic block that is being bound to.</summary>
    public Logic<
      TInput, TState, TOutput, THandler, TInputReturn, TUpdate
    > LogicBlock { get; }

    private TState _previousState;

    // List of functions that receive a TState and return whether the binding
    // with the same index in the _whenBindingRunners should be run.
    private readonly List<Func<TState, bool>> _whenBindingCheckers;
    // List of functions that receive a TState and invoke the relevant binding
    // when a particular type of state is encountered.
    private readonly List<Action<TState, TState>> _whenBindingRunners;

    // List of functions that receive a TState and return whether the binding
    // with the same index in the _handledOutputRunners should be run.
    private readonly List<Func<TOutput, bool>> _handledOutputCheckers;
    // List of functions that receive a TOutput and invoke the relevant binding
    // when a particular type of output is encountered.
    private readonly List<Action<TOutput>> _handledOutputRunners;

    internal Binding(
      Logic<TInput, TState, TOutput, THandler, TInputReturn, TUpdate> logicBlock
    ) {
      LogicBlock = logicBlock;
      _previousState = logicBlock.Value;
      _whenBindingRunners = new();
      _whenBindingCheckers = new();
      _handledOutputRunners = new();
      _handledOutputCheckers = new();

      LogicBlock.OnState += OnState;
      LogicBlock.OnOutput += OnOutput;
    }

    // Registers a binding for a specific type of state.
    /// <summary>
    /// Create a bindings group that allows you to register bindings for a
    /// specific type of state. Bindings are callbacks that only run when the
    /// specific type of state you specify with
    /// <typeparamref name="TStateType" /> is encountered.
    /// </summary>
    /// <typeparam name="TStateType">Type of state to bind to.</typeparam>
    /// <returns>The new binding group.</returns>
    public WhenBinding<TStateType> When<TStateType>()
      where TStateType : TState {
      var whenBinding = new WhenBinding<TStateType>();
      // Add a closure to the list of when binding runners that invokes
      // the captured generic binding with the captured generic type.
      _whenBindingRunners.Add(
        (state, previous) => whenBinding.Run((TStateType)state, previous)
      );
      // Let the when binding itself decide if it should handle the state.
      _whenBindingCheckers.Add(whenBinding.ShouldRun);

      return whenBinding;
    }

    /// <summary>
    /// Register a callback to be invoked whenever an output type of
    /// <typeparamref name="TOutputType" /> is encountered.
    /// </summary>
    /// <param name="handler">Output callback handler.</param>
    /// <typeparam name="TOutputType">Type of output to register a handler
    /// for.</typeparam>
    /// <returns>The current binding.</returns>
    public Binding Handle<TOutputType>(
      Action<TOutputType> handler
    ) where TOutputType : TOutput {
      _handledOutputCheckers.Add((output) => output is TOutputType);
      _handledOutputRunners.Add((output) => handler((TOutputType)output));

      return this;
    }

    /// <summary>
    /// Clean up registered bindings for all states and stop listening
    /// for state changes.
    /// </summary>
    public void Dispose() => Dispose(true);

    private void OnState(object? _, TState state) {
      // Run each when binding that should be run.
      for (var i = 0; i < _whenBindingCheckers.Count; i++) {
        var checker = _whenBindingCheckers[i];
        var runner = _whenBindingRunners[i];
        if (checker(state)) {
          // If the binding handles this type of state, run it!
          runner(state, _previousState);
        }
      }

      _previousState = state;
    }
    private void OnOutput(object? _, TOutput output) {
      // Run each handled output binding that should be run.
      for (var i = 0; i < _handledOutputCheckers.Count; i++) {
        var checker = _handledOutputCheckers[i];
        var runner = _handledOutputRunners[i];
        if (checker(output)) {
          // If the binding handles this type of output, run it!
          runner(output);
        }
      }
    }

    private void Dispose(bool disposing) {
      if (disposing) {
        LogicBlock.OnOutput -= OnOutput;
        LogicBlock.OnState -= OnState;
        Cleanup();
      }
    }

    private void Cleanup() {
      _whenBindingCheckers.Clear();
      _whenBindingRunners.Clear();
      _handledOutputCheckers.Clear();
      _handledOutputRunners.Clear();
    }

    /// <summary>Glue finalizer.</summary>
    ~Binding() {
      Dispose(false);
    }
  }

  /// <summary>
  /// A bindings group that allows you to register bindings for a specific type
  /// of state. Bindings are callbacks that only run when the specific type of
  /// state you specify with <typeparamref name="TStateType" /> is encountered.
  /// </summary>
  /// <typeparam name="TStateType">Type of state to bind to.</typeparam>
  public sealed class WhenBinding<TStateType> {
    // Selected data bindings checkers registered with .Use()
    // These callbacks receive the current state, the previous state, the
    // selected data from the current state and the
    // last selected data from the previous state and return true if the
    // binding should be run.
    private readonly List<Func<dynamic, TState, dynamic, dynamic, bool>>
      _bindingCheckers = new();
    private readonly List<Func<dynamic, dynamic>> _bindingSelectors = new();
    // Callbacks that run a binding. The index of this list and the
    // _bindingCheckers refers to the same binding.
    private readonly List<Action<dynamic>> _bindingRunners = new();
    // Cached values of the selected data bindings.
    // Caching the selected data prevents the bindings from breaking when
    // logic block states are reused to avoid unnecessary allocations.
    // The index of this list and the _bindingCheckers refers to the same
    // binding.
    private readonly List<dynamic?> _bindingValues = new();
    // Callbacks for this state type registered with .Call()
    private readonly List<Action<dynamic, TState>> _callbacks = new();

    internal WhenBinding() { }

    /// <summary>
    /// Determines if this binding should run for a given state.
    /// </summary>
    /// <param name="state">The current state in question.</param>
    /// <returns>True if this binding handles the given type of state, false
    /// otherwise.</returns>
    internal bool ShouldRun(TState state) => state is TStateType;

    /// <summary>
    /// Runs all the registered bindings inside this binding that handle the
    /// given type of state.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="previous">Previous state.</param>
    internal void Run(TState state, TState previous) {
      for (var i = 0; i < _bindingCheckers.Count; i++) {
        var checker = _bindingCheckers[i];
        var selector = _bindingSelectors[i];
        // get current selected data
        var selectedData = selector(state);
        // get previously selected data (if any)
        var previousData = _bindingValues[i];
        if (checker(state, previous, selectedData, previousData)) {
          // Binding should be run.
          var runner = _bindingRunners[i];
          runner(selectedData);
          _bindingValues[i] = selectedData;
        }
      }

      if (previous is TStateType) {
        // Overall state hasn't changed types. No need to run callbacks.
        return;
      }

      foreach (var callback in _callbacks) {
        callback(state, previous);
      }
    }

    /// <summary>
    /// Use data from the state to invoke a method that receives the data. This
    /// allows you to "select" data from a given type of state and invoke a
    /// callback only when the selected data actually changes (as determined by
    /// reference equality and the default equality comparer).
    /// </summary>
    /// <param name="data">Data to select from the state type
    /// <typeparamref name="TStateType" />.</param>
    /// <param name="to">Callback that receives the selected data and runs
    /// only when the selected data has changed after a state update.</param>
    /// <typeparam name="TSelectedData">Type of data to select from the state.
    /// </typeparam>
    /// <returns>The current when-binding clause so that you can continue to use
    /// data from the state or register callbacks.</returns>
    public WhenBinding<TStateType> Use<TSelectedData>(
      Func<TStateType, TSelectedData> data, Action<TSelectedData> to
    ) where TSelectedData : notnull {
      var checker = (
        dynamic state,
        TState previous,
        dynamic selectedData,
        dynamic previousData
      ) => {
        // If previous data is null, it indicates that we don't have any
        // previous data because the binding hadn't been run before. So we don't
        // try to compare the previous data.
        if (previous is TStateType previousState && previousData is not null) {
          // If the previous state is the same type as the current state,
          // check if the selected data has changed.
          if (
            ReferenceEquals(selectedData, previousData) ||
            EqualityComparer<TSelectedData>.Default.Equals(
              selectedData, previousData
            )
          ) {
            // Selected data hasn't changed. No need to update!
            return false;
          }
        }
        return true;
      };

      var selector = (dynamic state) => data((TStateType)state) as dynamic;
      var runner = (dynamic value) => to((TSelectedData)value);

      _bindingCheckers.Add(checker);
      _bindingSelectors.Add(selector);
      _bindingRunners.Add(runner);
      _bindingValues.Add(default);

      return this;
    }

    /// <summary>
    /// Register a callback to be invoked whenever the state changes to the
    /// state type <typeparamref name="TStateType" />.
    /// </summary>
    /// <param name="callback">Callback invoked whenever the state changes to
    /// the state type <typeparamref name="TStateType" />.</param>
    /// <returns>The current when-binding clause so that you can continue to use
    /// data from the state or register callbacks.</returns>
    public WhenBinding<TStateType> Call(Action<TStateType> callback) {
      var handler =
        (dynamic state, TState previous) => callback((TStateType)state);

      _callbacks.Add(handler);

      return this;
    }
  }
}
