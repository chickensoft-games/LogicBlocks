namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class LogicBlock<TState> {
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
    IBinding Watch<TInputType>(Action<TInputType> handler);

    /// Registers a binding for a specific type of state.
    /// <summary>
    /// Create a bindings group that allows you to register bindings for a
    /// specific type of state. Bindings are callbacks that only run when the
    /// specific type of state you specify with
    /// <typeparamref name="TStateType" /> is encountered.
    /// </summary>
    /// <typeparam name="TStateType">Type of state to bind to.</typeparam>
    /// <returns>The new binding group.</returns>
    public IBinding When<TStateType>(Action<TStateType> handler)
      where TStateType : TState;

    /// <summary>
    /// Register a callback to be invoked whenever an output type of
    /// <typeparamref name="TOutputType" /> is encountered.
    /// </summary>
    /// <param name="handler">Output callback handler.</param>
    /// <typeparam name="TOutputType">Type of output to register a handler
    /// for.</typeparam>
    /// <returns>The current binding.</returns>
    IBinding Handle<TOutputType>(Action<TOutputType> handler);

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

  internal class BindingBase : IBinding {
    // List of functions that receive a TInput and return whether the binding
    // with the same index in the _inputRunners should be run.
    internal readonly List<Func<object, bool>> _inputCheckers;
    // List of functions that receive an input and invoke the relevant binding
    // when a particular type of input is encountered.
    internal readonly List<Action<object>> _inputRunners;

    // List of functions that receive a TState and return whether the binding
    // with the same index in the _whenBindingRunners should be run.
    internal readonly List<Func<TState, bool>> _stateCheckers;
    // List of functions that receive a TState and invoke the relevant binding
    // when a particular type of state is encountered.
    internal readonly List<Action<TState>> _stateRunners;

    // List of functions that receive a TState and return whether the binding
    // with the same index in the _handledOutputRunners should be run.
    internal readonly List<Func<object, bool>> _handledOutputCheckers;
    // List of functions that receive an output and invoke the relevant binding
    // when a particular type of output is encountered.
    internal readonly List<Action<object>> _handledOutputRunners;

    // List of functions that receive an Exception and return whether the
    // binding with the same index in the _errorRunners should be run.
    internal readonly List<Func<Exception, bool>> _errorCheckers;
    // List of functions that receive an Exception and invoke the relevant
    // binding when a particular type of error is encountered.
    internal readonly List<Action<Exception>> _errorRunners;

    internal BindingBase() {
      _inputCheckers = new();
      _inputRunners = new();
      _stateCheckers = new();
      _stateRunners = new();
      _handledOutputCheckers = new();
      _handledOutputRunners = new();
      _errorCheckers = new();
      _errorRunners = new();
    }

    /// <inheritdoc />
    public IBinding Watch<TInputType>(Action<TInputType> handler) {
      _inputCheckers.Add((input) => input is TInputType);
      _inputRunners.Add((input) => handler((TInputType)input));

      return this;
    }

    /// <inheritdoc />
    public IBinding When<TStateType>(Action<TStateType> handler)
      where TStateType : TState {
      _stateCheckers.Add((state) => state is TStateType);
      _stateRunners.Add((state) => handler((TStateType)state));

      return this;
    }

    /// <inheritdoc />
    public IBinding Handle<TOutputType>(Action<TOutputType> handler) {
      _handledOutputCheckers.Add((output) => output is TOutputType);
      _handledOutputRunners.Add((output) => handler((TOutputType)output));

      return this;
    }

    /// <inheritdoc />
    public IBinding Catch<TException>(
      Action<TException> handler
    ) where TException : Exception {
      _errorCheckers.Add((error) => error is TException);
      _errorRunners.Add((error) => handler((TException)error));

      return this;
    }

    internal void InternalOnInput(object input) {
      // Run each input binding that should be run.
      for (var i = 0; i < _inputCheckers.Count; i++) {
        var checker = _inputCheckers[i];
        var runner = _inputRunners[i];
        if (checker(input)) {
          // If the binding handles this type of input, run it!
          runner(input);
        }
      }
    }

    internal void InternalOnState(TState state) {
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

    internal void InternalOnOutput(object output) {
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

    internal void InternalOnError(Exception error) {
      // Run each error binding that should be run.
      for (var i = 0; i < _errorCheckers.Count; i++) {
        var checker = _errorCheckers[i];
        var runner = _errorRunners[i];
        if (checker(error)) {
          // If the binding handles this type of error, run it!
          runner(error);
        }
      }
    }

    /// <inheritdoc />
    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing) {
      if (disposing) {
        Cleanup();
      }
    }

    /// <summary>Glue finalizer.</summary>
    ~BindingBase() {
      Dispose(false);
    }

    internal virtual void Cleanup() {
      _stateCheckers.Clear();
      _stateRunners.Clear();
      _handledOutputCheckers.Clear();
      _handledOutputRunners.Clear();
      _errorCheckers.Clear();
      _errorRunners.Clear();
      _inputCheckers.Clear();
      _inputRunners.Clear();
    }
  }

  /// <summary>
  /// <para>State bindings for a logic block.</para>
  /// <para>
  /// A binding allows you to select data from a logic block's state, invoke
  /// methods when certain states occur, and handle outputs. Using bindings
  /// enable you to write more declarative code and prevent unnecessary
  /// updates when a state has changed but the relevant data within it has not.
  /// </para>
  /// </summary>
  internal sealed class Binding : BindingBase {
    /// <summary>Logic block that is being bound to.</summary>
    public LogicBlock<TState> LogicBlock { get; }

    internal Binding(
      LogicBlock<TState> logicBlock
    ) {
      LogicBlock = logicBlock;

      LogicBlock.OnInput += InternalOnInput;
      LogicBlock.OnState += InternalOnState;
      LogicBlock.OnOutput += InternalOnOutput;
      LogicBlock.OnError += InternalOnError;
    }

    internal override void Cleanup() {
      base.Cleanup();
      LogicBlock.OnInput -= InternalOnInput;
      LogicBlock.OnState -= InternalOnState;
      LogicBlock.OnOutput -= InternalOnOutput;
      LogicBlock.OnError -= InternalOnError;
    }
  }
}
