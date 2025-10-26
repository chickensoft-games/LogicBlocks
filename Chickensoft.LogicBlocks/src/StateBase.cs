namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Logic block state base class. Used internally by LogicBlocks.
/// Prefer <see cref="StateLogic{TState}"/> over this for user-defined states.
/// </summary>
public abstract record StateBase
{
  /// <inheritdoc />
  internal IContext Context => InternalState.ContextAdapter;

  internal InternalState InternalState { get; }

  internal StateBase(IContextAdapter contextAdapter)
  {
    InternalState = new(contextAdapter);
  }

  internal bool IsAttached => InternalState.ContextAdapter.Context is not null;

  /// <summary>
  /// Creates a fake context and assigns it internally to be the state's
  /// underlying context object. Fake contexts facilitate testing of logic
  /// block states in isolation, allowing interactions with the context to
  /// be captured and verified more easily.
  /// </summary>
  /// <returns>Fake logic block context.</returns>
  public IFakeContext CreateFakeContext()
  {
    if (InternalState.ContextAdapter.Context is FakeContext fakeContext)
    {
      fakeContext.Reset();
      return fakeContext;
    }

    var context = new FakeContext();
    InternalState.ContextAdapter.Adapt(context);
    return context;
  }

  /// <summary>
  /// Adds a callback that will be invoked when the state is attached to a
  /// logic block. A state instance is attached to a logic block when it is
  /// the active state of the logic block. Only one state instance can be
  /// active at a time. Unlike entrance callbacks, all attach callbacks
  /// will be invoked when the state is attached.
  /// </summary>
  /// <param name="handler">Callback invoked when the state is attached.
  /// </param>
  public void OnAttach(Action handler) =>
    InternalState.AttachCallbacks.Enqueue(handler);

  /// <summary>
  /// Adds a callback that will be invoked when the state is detached from
  /// a logic block. A state instance is detached from a logic block when it
  /// is no longer the active state of the logic block. Only one state
  /// instance can be active at a time. Unlike exit callbacks, all detach
  /// callbacks will be invoked when the state is detached.
  /// </summary>
  /// <param name="handler">Callback invoked when the state is detached.
  /// </param>
  public void OnDetach(Action handler) =>
    InternalState.DetachCallbacks.Push(handler);

  /// <summary>
  /// Runs all of the registered attach callbacks for the state.
  /// </summary>
  /// <param name="context">Logic block context.</param>
  public void Attach(IContext context)
  {
    InternalState.ContextAdapter.Adapt(context);
    CallAttachCallbacks();
  }

  /// <summary>
  /// Runs all of the registered detach callbacks for the state.
  /// </summary>
  public void Detach()
  {
    if (!IsAttached)
    {
      return;
    }

    CallDetachCallbacks();
    InternalState.ContextAdapter.Clear();
  }

  private void CallAttachCallbacks()
  {
    foreach (var onAttach in InternalState.AttachCallbacks)
    {
      RunSafe(onAttach);
    }
  }

  private void CallDetachCallbacks()
  {
    foreach (var onDetach in InternalState.DetachCallbacks)
    {
      RunSafe(onDetach);
    }
  }

  private void RunSafe(Action callback)
  {
    try
    { callback(); }
    catch (Exception e)
    {
      if (InternalState.ContextAdapter.OnError is { } onError)
      {
        onError(e);
        return;
      }
      throw;
    }
  }

  /// <summary>
  /// Adds a callback that will be invoked when the state is entered. The
  /// callback will receive the previous state as an argument.
  /// <br />
  /// Each class in an inheritance hierarchy can register callbacks and they
  /// will be invoked in the order they were registered, base class to most
  /// derived class. This ordering matches the order in which entrance
  /// callbacks should be invoked in a statechart.
  /// </summary>
  /// <typeparam name="TDerivedState">Derived state type that would be entered.
  /// </typeparam>
  /// <param name="handler">Callback to be invoked when the state is entered.
  /// </param>
  internal abstract void OnEnter<TDerivedState>(Action<object?> handler);

  /// <summary>
  /// Adds a callback that will be invoked when the state is exited. The
  /// callback will receive the next state as an argument.
  /// <br />
  /// Each class in an inheritance hierarchy can register callbacks and they
  /// will be invoked in the opposite order they were registered, most
  /// derived class to base class. This ordering matches the order in which
  /// exit callbacks should be invoked in a statechart.
  /// </summary>
  /// <typeparam name="TDerivedState">Derived state type that would be exited.
  /// </typeparam>
  /// <param name="handler">Callback to be invoked when the state is exited.
  /// </param>
  internal abstract void OnExit<TDerivedState>(Action<object?> handler);
}
