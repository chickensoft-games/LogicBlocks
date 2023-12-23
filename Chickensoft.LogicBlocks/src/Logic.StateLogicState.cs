namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>
  /// State common to both synchronous and asynchronous logic block states.
  /// </summary>
  public interface ILogicState {
    /// <summary>Logic block context.</summary>
    IContext Context { get; }

    /// <summary>
    /// Creates a fake context and assigns it internally to be the state's
    /// underlying context object. Fake contexts facilitate testing of logic
    /// block states in isolation, allowing interactions with the context to
    /// be captured and verified more easily.
    /// </summary>
    /// <returns>Fake logic block context.</returns>
    public IFakeContext CreateFakeContext();

    /// <summary>
    /// Adds a callback that will be invoked when the state is attached to a
    /// logic block. A state instance is attached to a logic block when it is
    /// the active state of the logic block. Only one state instance can be
    /// active at a time. Unlike entrance callbacks, all attach callbacks
    /// will be invoked when the state is attached.
    /// </summary>
    /// <param name="handler">Callback invoked when the state is attached.
    /// </param>
    void OnAttach(Action handler);

    /// <summary>
    /// Adds a callback that will be invoked when the state is detached from
    /// a logic block. A state instance is detached from a logic block when it
    /// is no longer the active state of the logic block. Only one state
    /// instance can be active at a time. Unlike exit callbacks, all detach
    /// callbacks will be invoked when the state is detached.
    /// </summary>
    /// <param name="handler">Callback invoked when the state is detached.
    /// </param>
    void OnDetach(Action handler);

    /// <summary>
    /// Runs all of the registered attach callbacks for the state.
    /// </summary>
    /// <param name="context">Logic block context.</param>
    /// <param name="onError">Error callback, if any.</param>
    void Attach(IContext context, Action<Exception>? onError = null);

    /// <summary>
    /// Runs all of the registered detach callbacks for the state.
    /// </summary>
    /// <param name="onError">Error callback, if any.</param>
    void Detach(Action<Exception>? onError = null);
  }

  /// <summary>
  /// Common state superclass shared between synchronous and asynchronous
  /// logic block states. Don't use this yourself, use
  /// <see cref="LogicBlock{TState}.StateLogic"/> or
  /// <see cref="LogicBlockAsync{TState}.StateLogic"/> instead.
  /// </summary>
  public abstract record InternalSharedState : ILogicState {
    /// <inheritdoc />
    public IContext Context => InternalState.ContextAdapter;

    /// <inheritdoc />
    internal InternalState InternalState { get; } = new();

    /// <inheritdoc />
    public void OnAttach(Action handler) =>
      InternalState.AttachCallbacks.Enqueue(handler);

    /// <inheritdoc />
    public void OnDetach(Action handler) =>
      InternalState.DetachCallbacks.Push(handler);

    /// <inheritdoc />
    public void Attach(IContext context, Action<Exception>? onError = null) {
      InternalState.ContextAdapter.Adapt(context);
      CallAttachCallbacks(onError);
    }

    /// <inheritdoc />
    public void Detach(Action<Exception>? onError = null) {
      if (InternalState.ContextAdapter.Context is null) {
        return;
      }

      CallDetachCallbacks(onError);
      InternalState.ContextAdapter.Clear();
    }

    /// <inheritdoc />
    public IFakeContext CreateFakeContext() {
      if (InternalState.ContextAdapter.Context is FakeContext fakeContext) {
        return fakeContext;
      }
      var context = new FakeContext();
      Attach(context);
      return context;
    }

    internal void RunSafe(Action callback, Action<Exception>? onError) {
      try { callback(); }
      catch (Exception e) {
        if (onError is Action<Exception> onErrorHandler) {
          onErrorHandler.Invoke(e);
          return;
        }
        throw;
      }
    }

    private void CallAttachCallbacks(Action<Exception>? onError = null) {
      foreach (var onAttach in InternalState.AttachCallbacks) {
        RunSafe(onAttach, onError);
      }
    }

    private void CallDetachCallbacks(Action<Exception>? onError = null) {
      foreach (var onDetach in InternalState.DetachCallbacks) {
        RunSafe(onDetach, onError);
      }
    }
  }

  /// <summary>
  /// Internal state stored in each logic block state. This is used to store
  /// entrance and exit callbacks without tripping up equality checking.
  /// </summary>
  public readonly struct InternalState {
    /// <summary>
    /// Callbacks to be invoked when the state is entered.
    /// </summary>
    internal Queue<UpdateCallback> EnterCallbacks { get; }

    /// <summary>
    /// Callbacks to be invoked when the state is exited.
    /// </summary>
    internal Stack<UpdateCallback> ExitCallbacks { get; }

    /// <summary>
    /// Callbacks to be invoked when the state is attached to the logic block.
    /// </summary>
    internal Queue<Action> AttachCallbacks { get; } = new();

    /// <summary>
    /// Callbacks to be invoked when the state is detached from the logic block.
    /// </summary>
    internal Stack<Action> DetachCallbacks { get; } = new();

    /// <summary>
    /// <para>
    /// Internal context adapter. If there's no underlying context in the
    /// adapter, the context has not been initialized yet. An uninitialized
    /// context implies the state has never been active in a logic block.
    /// </para>
    /// <para>
    /// If an underlying object exists, it is either the real logic block
    /// context or a fake one supplied to facilitate unit-testing.
    /// </para>
    /// </summary>
    internal ContextAdapter ContextAdapter { get; }

    /// <summary>Creates a new state logic internal state.</summary>
    public InternalState() {
      EnterCallbacks = new();
      ExitCallbacks = new();
      ContextAdapter = new();
    }

    // We don't want state logic states to be compared, so we make them
    // always equal to whatever other state logic state they are compared to.
    // This prevents issues where two seemingly equivalent states are not
    // deemed equivalent because their callbacks are different.

    /// <inheritdoc />
    public override bool Equals(object obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
      EnterCallbacks,
      ExitCallbacks
    );
  }
}
