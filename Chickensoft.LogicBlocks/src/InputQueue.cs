namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

internal interface IInputHandler {
  internal void HandleInput<TInputType>(in TInputType input)
    where TInputType : struct;
}

/// <summary>
/// <para>
/// Input queue that can store multiple types of structs without boxing. It
/// does this by quietly creating a new queue whenever it sees a new input type.
/// This should drastically reduce memory churn when processing inputs.
/// </para>
/// <para>
/// This is built around standard queues, so it takes advantage of the internal
/// capacity of the queue and all of its resizing functionality at the expense
/// of a little additional memory usage if many input types are seen by the
/// logic block. This trade-off allows the logic block to drastically reduce
/// the amount of memory churn caused by boxing and unboxing inputs and/or
/// allocating lambdas to capture generic contexts.
/// </para>
/// <para>
/// Adapted from https://stackoverflow.com/a/6164880.
/// </para>
/// </summary>
internal class InputQueue {

  private abstract class TypedInputQueue {
    public abstract void HandleInput(IInputHandler inputProcessor);
    public abstract void Clear();
  }

  private class TypedMessageQueue<T> : TypedInputQueue where T : struct {
    private readonly Queue<T> _queue = new();

    public void Enqueue(T message) => _queue.Enqueue(message);

    public override void HandleInput(IInputHandler inputProcessor) =>
      inputProcessor.HandleInput(_queue.Dequeue());

    public override void Clear() => _queue.Clear();
  }

  public IInputHandler Handler { get; }
  private readonly Queue<Type> _queueSelectorQueue = new();
  private readonly Dictionary<Type, TypedInputQueue> _queues = new();

  public InputQueue(IInputHandler handler) {
    Handler = handler;
  }

  public void Enqueue<T>(T message) where T : struct {
    TypedMessageQueue<T> queue;

    if (!_queues.ContainsKey(typeof(T))) {
      queue = new TypedMessageQueue<T>();
      _queues[typeof(T)] = queue;
    }
    else {
      queue = (TypedMessageQueue<T>)_queues[typeof(T)];
    }

    queue.Enqueue(message);
    _queueSelectorQueue.Enqueue(typeof(T));
  }

  public bool HasInputs => _queueSelectorQueue.Count > 0;

  public void HandleInput() {
    var type = _queueSelectorQueue.Dequeue();
    _queues[type].HandleInput(Handler);
  }

  public void Clear() {
    _queueSelectorQueue.Clear();

    foreach (var queue in _queues.Values) {
      queue.Clear();
    }
  }
}
