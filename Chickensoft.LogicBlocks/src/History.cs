namespace Chickensoft.LogicBlocks;

using System.Collections;
using Nito.Collections;

public class History : IReadOnlyCollection<Type>
{
  private readonly Deque<Type> _deque;

  public int? MaxCapacity { get; }

  internal History(
    IEnumerable<Type>? collection = null,
    int? maxCapacity = null
  )
  {
    MaxCapacity = maxCapacity;

    _deque = new Deque<Type>(collection ?? []);

    if (MaxCapacity.HasValue)
    {
      // drop oldest entries
      while (_deque.Count > MaxCapacity)
      {
        _deque.RemoveFromFront();
      }
    }
  }

  public int Count => _deque.Count;
  public bool IsEmpty => _deque.Count == 0;

  IEnumerator<Type> IEnumerable<Type>.GetEnumerator() =>
    _deque.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => _deque.GetEnumerator();

  public IEnumerator<Type> GetEnumerator() =>
    _deque.GetEnumerator();

#pragma warning disable IDE0305
  // use deque's ToArray implementation, not collection semantics
  public Type[] ToArray() => _deque.ToArray();
#pragma warning restore IDE0305

  public Type? Peek() => _deque.Count > 0
    ? _deque[_deque.Count - 1]
    : null;

  internal void Push(Type type)
  {
    if (MaxCapacity.HasValue && _deque.Count == MaxCapacity)
    {
      // max capacity enforced: drop oldest entry to make room
      _deque.RemoveFromFront();
    }

    _deque.AddToBack(type);
  }

  internal Type? Pop() => _deque.Count > 0
    ? _deque.RemoveFromBack()
    : null;

  internal void Clear() => _deque.Clear();
}
