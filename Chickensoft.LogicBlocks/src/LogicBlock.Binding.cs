namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class LogicBlock<TState> {
  /// <summary>
  /// Action that responds to a particular type of input.
  /// </summary>
  /// <typeparam name="TInputType">Input type.</typeparam>
  /// <param name="input">Input object.</param>
  public delegate void InputAction<TInputType>(in TInputType input)
    where TInputType : struct;

  /// <summary>
  /// Action that responds to a particular type of output.
  /// </summary>
  /// <typeparam name="TOutputType">Output type.</typeparam>
  /// <param name="output">Output object.</param>
  public delegate void OutputAction<TOutputType>(in TOutputType output)
    where TOutputType : struct;

  /// <summary>
  /// <para>State bindings for a logic block.</para>
  /// <para>
  /// A binding allows you to select data from a logic block's state, invoke
  /// methods when certain states occur, and handle outputs. Using bindings
  /// enable you to write more declarative code and prevent unnecessary
  /// updates when a state has changed but the relevant data within it has not.
  /// </para>
  /// </summary>
  public interface IBinding : IDisposable {
    /// <summary>
    /// Register a callback to be invoked whenever an input type of
    /// <typeparamref name="TInputType" /> is encountered.
    /// </summary>
    /// <param name="handler">Input callback handler.</param>
    /// <typeparam name="TInputType">Type of input to register a handler
    /// for.</typeparam>
    /// <returns>The current binding.</returns>
    IBinding Watch<TInputType>(InputAction<TInputType> handler)
      where TInputType : struct;

    /// Registers a binding for a specific type of state.
    /// <summary>
    /// Create a bindings group that allows you to register bindings for a
    /// specific type of state. Bindings are callbacks that only run when the
    /// specific type of state you specify with
    /// <typeparamref name="TStateType" /> is encountered.
    /// </summary>
    /// <typeparam name="TStateType">Type of state to bind to.</typeparam>
    /// <param name="handler">Handler invoked when the state changes.</param>
    /// <returns>The new binding group.</returns>
    IBinding When<TStateType>(Action<TStateType> handler)
      where TStateType : TState;

    /// <summary>
    /// Register a callback to be invoked whenever an output type of
    /// <typeparamref name="TOutputType" /> is encountered.
    /// </summary>
    /// <param name="handler">Output callback handler.</param>
    /// <typeparam name="TOutputType">Type of output to register a handler
    /// for.</typeparam>
    /// <returns>The current binding.</returns>
    IBinding Handle<TOutputType>(OutputAction<TOutputType> handler)
      where TOutputType : struct;

    /// <summary>
    /// Register a callback to be invoked whenever an error type of
    /// <typeparamref name="TException" /> is encountered.
    /// </summary>
    /// <param name="handler">Error callback handler.</param>
    /// <typeparam name="TException">Type of exception to handle.</typeparam>
    /// <returns>The current binding.</returns>
    IBinding Catch<TException>(Action<TException> handler)
      where TException : Exception;
  }

  /// <summary>
  /// <para>Fluent bindings for a logic block.</para>
  /// <para>
  /// A binding allows you to select data from a logic block's state, invoke
  /// methods when certain states occur, and handle outputs. Using bindings
  /// enable you to write more declarative code and prevent unnecessary
  /// updates when a state has changed but the relevant data within it has not.
  /// </para>
  /// <para>
  /// Always dispose your binding when you're finished with it!
  /// </para>
  /// </summary>
  internal abstract class BindingBase :
  LogicBlockListenerBase<TState>, IBinding {
    // Map of an input type to a list of functions that receive that input.
    // We store the functions as "object" and cast to the specific function
    // type later when we have a generic argument to avoid boxing inputs.
    internal readonly Dictionary<Type, List<object>> _inputRunners;
    internal readonly Dictionary<Type, List<object>> _outputRunners;

    // List of functions that receive a TState and return whether the binding
    // with the same index in the _whenBindingRunners should be run.
    internal readonly List<Func<TState, bool>> _stateCheckers;
    // List of functions that receive a TState and invoke the relevant binding
    // when a particular type of state is encountered.
    internal readonly List<Action<TState>> _stateRunners;

    // List of functions that receive an Exception and return whether the
    // binding with the same index in the _errorRunners should be run.
    internal readonly List<Func<Exception, bool>> _exceptionCheckers;
    // List of functions that receive an Exception and invoke the relevant
    // binding when a particular type of error is encountered.
    internal readonly List<Action<Exception>> _exceptionRunners;

    internal BindingBase() {
      _inputRunners = [];
      _outputRunners = [];
      _stateCheckers = [];
      _stateRunners = [];
      _exceptionCheckers = [];
      _exceptionRunners = [];
    }

    /// <inheritdoc />
    public IBinding Watch<TInputType>(InputAction<TInputType> handler)
    where TInputType : struct {
      if (_inputRunners.TryGetValue(typeof(TInputType), out var runners)) {
        runners.Add(handler);
      }
      else {
        _inputRunners[typeof(TInputType)] = [handler];
      }

      return this;
    }

    /// <inheritdoc />
    public IBinding When<TStateType>(Action<TStateType> handler)
      where TStateType : TState {
      // Only run the callback if the incoming state is the expected type of
      // state. All incoming states are guaranteed to be non-equivalent to the
      // previous state.
      _stateCheckers.Add((state) => state is TStateType);
      _stateRunners.Add((state) => handler((TStateType)state));

      return this;
    }

    /// <inheritdoc />
    public IBinding Handle<TOutputType>(OutputAction<TOutputType> handler)
    where TOutputType : struct {
      if (_outputRunners.TryGetValue(typeof(TOutputType), out var runners)) {
        runners.Add(handler);
      }
      else {
        _outputRunners[typeof(TOutputType)] = [handler];
      }

      return this;
    }

    /// <inheritdoc />
    public IBinding Catch<TException>(
      Action<TException> handler
    ) where TException : Exception {
      _exceptionCheckers.Add((error) => error is TException);
      _exceptionRunners.Add((error) => handler((TException)error));

      return this;
    }

    protected override void ReceiveInput<TInputType>(in TInputType input)
    where TInputType : struct {
      if (!_inputRunners.TryGetValue(typeof(TInputType), out var runners)) {
        return;
      }

      // Run each input binding that should be run.
      foreach (var runner in runners) {
        // If the binding handles this type of input, run it!
        (runner as InputAction<TInputType>)!(in input);
      }
    }

    protected override void ReceiveState(TState state) {
      // Run each when binding that should be run.
      for (var i = 0; i < _stateCheckers.Count; i++) {
        var checker = _stateCheckers[i];
        var runner = _stateRunners[i];
        if (checker(state)) {
          // If the binding handles this type of state, run it!
          runner(state);
        }
      }
    }

    protected override void ReceiveOutput<TOutputType>(in TOutputType output)
    where TOutputType : struct {
      if (!_outputRunners.TryGetValue(typeof(TOutputType), out var runners)) {
        return;
      }

      // Run each output binding that should be run.
      foreach (var runner in runners) {
        // If the binding handles this type of output, run it!
        (runner as OutputAction<TOutputType>)!(in output);
      }
    }

    protected override void ReceiveException(Exception e) {
      // Run each error binding that should be run.
      for (var i = 0; i < _exceptionCheckers.Count; i++) {
        var checker = _exceptionCheckers[i];
        var runner = _exceptionRunners[i];
        if (checker(e)) {
          // If the binding handles this type of error, run it!
          runner(e);
        }
      }
    }

    protected override void Cleanup() {
      _inputRunners.Clear();
      _outputRunners.Clear();
      _stateCheckers.Clear();
      _stateRunners.Clear();
      _exceptionCheckers.Clear();
      _exceptionRunners.Clear();
    }
  }

  internal class Binding : BindingBase {
    /// <summary>Logic block being listened to.</summary>
    public LogicBlock<TState> LogicBlock { get; }

    internal Binding(LogicBlock<TState> logicBlock) {
      LogicBlock = logicBlock;
      logicBlock.AddBinding(this);
    }

    protected override void Cleanup() {
      LogicBlock.RemoveBinding(this);
      base.Cleanup();
    }
  }
}
