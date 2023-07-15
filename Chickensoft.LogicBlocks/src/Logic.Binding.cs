namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using Chickensoft.LogicBlocks.Extensions;

public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
> {
  /// <summary>
  /// Creates a binding to a logic block.
  /// </summary>
  /// <returns>A new <see cref="Binding" />
  /// </returns>
  public Binding Bind() => new(this);

  /// <summary>
  /// <para>State bindings for a logic block.</para>
  /// <para>
  /// A binding allows you to select data from a logic block's state, invoke
  /// methods when certain states occur, and handle outputs. Using glue
  /// enables you to write more declarative code and prevent unnecessary
  /// updates when a state has changed but the relevant data within it has not.
  /// </para>
  /// </summary>
  public sealed class Binding : IDisposable {
    /// <summary>Logic block that is being bound to.</summary>
    public Logic<
      TInput, TState, TOutput, THandler, TInputReturn, TUpdate
    > LogicBlock { get; }

    private TState _previousState;

    private readonly Dictionary<Type, List<IGlue>> _stateGlues =
      new();
    private readonly Dictionary<Type, List<Action<TState, dynamic>>> _invokers =
      new();
    private readonly Dictionary<Type, Action<TOutput>> _outputHandlers = new();

    internal Binding(
      Logic<TInput, TState, TOutput, THandler, TInputReturn, TUpdate> logicBlock
    ) {
      LogicBlock = logicBlock;
      _previousState = logicBlock.Value;
      LogicBlock.OnNextState += OnNextState;
      LogicBlock.OnOutput += OnOutput;
    }

    /// <summary>
    /// Register a handler for a specific type of state.
    /// </summary>
    /// <typeparam name="TSubstate">The type of state to glue to.</typeparam>
    /// <returns>A <see cref="GlueInvoker{TSubstate}" /> that
    /// allows bindings to be registered for selected data within the state.
    /// </returns>
    public Glue<TSubstate> When<TSubstate>()
      where TSubstate : TState {
      var type = typeof(TSubstate);
      var stateGlue = new GlueInvoker<TSubstate>();

      _stateGlues.AddIfNotPresent(type, new());
      _invokers.AddIfNotPresent(type, new());

      _stateGlues[type].Add(stateGlue);
      _invokers[type].Add(
        (state, prev) => stateGlue.Invoke((TSubstate)state, (TState)prev)
      );

      return new Glue<TSubstate>(stateGlue);
    }

    /// <summary>
    /// Registers an output handler for the logic block.
    /// </summary>
    /// <typeparam name="TOutputType">Output subtype to handle.</typeparam>
    /// <param name="handler">Action which handles an instance of the output.
    /// </param>
    public Binding Handle<TOutputType>(
      Action<TOutputType> handler
    ) where TOutputType : TOutput {
      var type = typeof(TOutputType);
      _outputHandlers[type] = (TOutput output) => handler((TOutputType)output!);
      return this;
    }

    private void Cleanup() {
      foreach (var stateGlueList in _stateGlues.Values) {
        foreach (var stateGlue in stateGlueList) {
          stateGlue.Cleanup();
        }
      }
      _stateGlues.Clear();
      _invokers.Clear();
      _outputHandlers.Clear();
    }

    private void OnNextState(object? _, TState state) {
      var type = state.GetType();
      if (_invokers.TryGetValue(type, out var glues)) {
        for (var i = 0; i < glues.Count; i++) {
          var glue = glues[i];
          _invokers[state.GetType()][i](state, _previousState);
        }
      }

      _previousState = state;
    }

    private void OnOutput(object? _, TOutput output) {
      var type = output.GetType();
      if (_outputHandlers.TryGetValue(type, out var handler)) {
        handler(output);
      }
    }

    /// <summary>
    /// Clean up registered glue bindings for all states and stop listening
    /// for state changes.
    /// </summary>
    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing) {
      if (disposing) {
        LogicBlock.OnOutput -= OnOutput;
        LogicBlock.OnNextState -= OnNextState;
        Cleanup();
      }
    }

    /// <summary>Glue finalizer.</summary>
    ~Binding() {
      Dispose(false);
    }

    /// <summary>
    /// Glue for a specific type of state.
    /// </summary>
    internal interface IGlue {
      /// <summary>
      /// Invoke all registered glue bindings for a specific type of state. Used
      /// by <see cref="Binding" />.
      /// </summary>
      /// <param name="state">Current state of the logic block.</param>
      /// <param name="previous">Previous state of the logic block.</param>
      /// <typeparam name="TOtherSubstate">Specific type of the logic block's
      /// current state.</typeparam>
      void Invoke<TOtherSubstate>(TOtherSubstate state, TState previous)
        where TOtherSubstate : TState;

      /// <summary>
      /// Clean up registered glue bindings for a specific type of state.
      /// </summary>
      void Cleanup();
    }

    internal class GlueInvoker<TSubstate>
      : IGlue where TSubstate : TState {
      private readonly List<Action<dynamic, TState>> _bindings = new();

      /// <inheritdoc />
      public void Invoke<TOtherSubstate>(TOtherSubstate state, TState previous)
        where TOtherSubstate : TState {
        foreach (var action in _bindings) {
          action(state, previous);
        }
      }

      public GlueInvoker<TSubstate> Call(Action<TSubstate> action) {
        var handler = (dynamic state, TState previous) => {
          if (previous is TSubstate) {
            // Overall state hasn't changed. No need to update.
            return;
          }
          action((TSubstate)state);
        };

        _bindings.Add(handler);

        return this;
      }

      public GlueInvoker<TSubstate> Use<TSelected>(
        Func<TSubstate, TSelected> data, Action<TSelected> to
      ) {
        var handler = (dynamic state, TState previous) => {
          var selectedData = data((TSubstate)state);
          if (previous is TSubstate previousSubstate) {
            var previousData = data(previousSubstate);
            if (
              EqualityComparer<TSelected>.Default.Equals(
                selectedData, previousData
              )
            ) {
              // Selected data hasn't changed. No need to update!
              return;
            }
          }

          to(selectedData);
        };

        _bindings.Add(handler);

        return this;
      }

      /// <inheritdoc />
      public void Cleanup() => _bindings.Clear();
    }

    /// <summary>
    /// Glue for a specific type of state.
    /// </summary>
    /// <typeparam name="TSubstate">The type of state that is glued.</typeparam>
    public class Glue<TSubstate> where TSubstate : TState {
      internal GlueInvoker<TSubstate> StateGlue { get; }

      internal Glue(
        GlueInvoker<TSubstate> stateGlue
      ) {
        StateGlue = stateGlue;
      }

      /// <summary>
      /// Selects data from the state and performs an action whenever the
      /// selected data changes.
      /// </summary>
      /// <param name="data">Data selected from the logic block's state.</param>
      /// <param name="to">Action to perform when selected data changes.</param>
      /// <typeparam name="TSelected">Type of the selected data.</typeparam>
      public Glue<TSubstate> Use<TSelected>(
        Func<TSubstate, TSelected> data, Action<TSelected> to
      ) {
        StateGlue.Use(data, to);
        return this;
      }

      /// <summary>
      /// Calls an action whenever the state changes into an instance of
      /// <typeparamref name="TSubstate" />. If the state is already an instance
      /// of <typeparamref name="TSubstate" />, nothing happens, guaranteeing
      /// that the action is only called when changing into a
      /// <typeparamref name="TSubstate" />.
      /// </summary>
      /// <param name="action">Action to invoke when the state changes into
      /// a <typeparamref name="TSubstate" />.</param>
      public Glue<TSubstate> Call(Action<TSubstate> action) {
        StateGlue.Call(action);
        return this;
      }
    }
  }
}
