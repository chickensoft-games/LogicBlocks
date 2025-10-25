namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.Introspection;

/// <summary>
/// Fake logic block context — provided for your testing convenience.
/// </summary>
public interface IFakeContext : IContext
{
  /// <summary>Inputs added to the logic block.</summary>
  IEnumerable<object> Inputs { get; }

  /// <summary>Outputs added to the logic block.</summary>
  IEnumerable<object> Outputs { get; }

  /// <summary>Errors added to the logic block.</summary>
  IEnumerable<Exception> Errors { get; }

  /// <summary>
  /// Sets a fake value in the logic block's blackboard.
  /// </summary>
  /// <param name="value">Value to set.</param>
  /// <typeparam name="TDataType">Type of value.</typeparam>
  void Set<TDataType>(TDataType value) where TDataType : class;

  /// <summary>
  /// Clears the blackboard, the inputs, the outputs, and the errors.
  /// </summary>
  void Reset();
}

/// <summary>Fake logic block context used when testing LogicBlocks.</summary>
internal readonly struct FakeContext : IFakeContext
{
  public IEnumerable<object> Inputs => _inputs.AsEnumerable();
  private readonly List<object> _inputs = [];
  public IEnumerable<object> Outputs => _outputs.AsEnumerable();
  private readonly List<object> _outputs = [];
  private readonly Dictionary<Type, object> _blackboard = [];
  public IEnumerable<Exception> Errors => _errors.AsEnumerable();
  private readonly List<Exception> _errors = [];

  public FakeContext() { }

  public TDataType Get<TDataType>() where TDataType : class
  {
    var type = typeof(TDataType);
    if (_blackboard.TryGetValue(type, out var value))
    {
      return (TDataType)value;
    }

    // If we've been asked for a state and it happens to be a concrete type
    // that we can construct, we'll go ahead and make one since states aren't
    // supposed to be mocked anyways (they should be treated as models when
    // testing). This is done as a convenience to the developer and can be
    // easily bypassed by adding a state to the blackboard manually.
    if (
      Types.Graph.GetMetadata(type) is IConcreteTypeMetadata concreteMetadata &&
      Types.Graph
        .GetDescendantSubtypes(typeof(StateBase))
        .Contains(type)
    )
    {
      var state = concreteMetadata.Factory();
      _blackboard[type] = state;
      return (TDataType)state;
    }

    throw new InvalidOperationException(
      $"No value of type {typeof(TDataType)} exists in the blackboard."
    );
  }

  public void Set<TDataType>(TDataType value) where TDataType : class =>
    _blackboard[typeof(TDataType)] = value;

  public void Input<TInputType>(in TInputType input)
    where TInputType : struct => _inputs.Add(input);

  public void Output<TOutputType>(in TOutputType output)
    where TOutputType : struct => _outputs.Add(output);

  public void AddError(Exception e) => _errors.Add(e);

  public void Reset()
  {
    _inputs.Clear();
    _outputs.Clear();
    _errors.Clear();
    _blackboard.Clear();
  }

  /// <inheritdoc />
  public override readonly bool Equals(object? obj) => true;

  /// <inheritdoc />
  public override readonly int GetHashCode() =>
    HashCode.Combine(_inputs, _outputs, _errors);
}
