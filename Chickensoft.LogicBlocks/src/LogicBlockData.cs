namespace Chickensoft.LogicBlocks;

using Collections;

public sealed class LogicBlockData : IEquatable<LogicBlockData>
{
  public Type StateType { get; }
  public IReadOnlyBlackboard Blackboard { get; }
  public History History { get; }

  private int? _hashCode;

  public LogicBlockData(
    Type stateType,
    IReadOnlyBlackboard blackboard,
    History history
  )
  {
    StateType = stateType;
    Blackboard = blackboard;
    History = history;
  }

  /// <inheritdoc />
  public bool Equals(LogicBlockData? other) => Equals((object?)other);

  /// <summary>
  /// Determines if two logic block data instances are equivalent. Data
  /// instances are equivalent if they are the same reference, or if each of
  /// their states and the contents of their blackboards are equivalent.
  /// </summary>
  /// <param name="obj">Other logic block.</param>
  /// <returns>True if</returns>
  public override bool Equals(object? obj)
  {
    if (ReferenceEquals(this, obj))
    { return true; }

    if (obj is not LogicBlockData data)
    { return false; }

    if (
      !LogicBlock.IsEquivalent(StateType, data.StateType)
    )
    {
      return false;
    }

    var types = Blackboard.Types;
    var otherTypes = data.Blackboard.Types;

    if (types.Count != otherTypes.Count)
    { return false; }

    foreach (var type in types)
    {
      if (!otherTypes.Contains(type))
      { return false; }

      var obj1 = Blackboard.GetObject(type);
      var obj2 = data.Blackboard.GetObject(type);

      if (LogicBlock.IsEquivalent(obj1, obj2))
      {
        continue;
      }

      return false;
    }

    if (History.Count != data.History.Count)
    { return false; }

    var e1 = History.GetEnumerator();
    var e2 = data.History.GetEnumerator();

    while (e1.MoveNext() && e2.MoveNext())
    {
      if (e1.Current != e2.Current)
      {
        return false;
      }
    }

    return true;
  }

  /// <inheritdoc />
  public override int GetHashCode()
  {
    if (_hashCode.HasValue)
    { return _hashCode.Value; }

    var hash = new HashCode();
    hash.Add(StateType);

    foreach (var type in Blackboard.Types)
    {
      var obj = Blackboard.GetObject(type);
      hash.Add(obj);
    }

    foreach (var type in History)
    {
      hash.Add(type);
    }

    _hashCode = hash.ToHashCode();

    return _hashCode.Value;
  }
}
