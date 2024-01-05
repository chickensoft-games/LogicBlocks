namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Linq;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>Logic block context provided to each logic block state.</summary>
  internal readonly struct FakeContext : IFakeContext {
    public IEnumerable<object> Inputs => _inputs.AsEnumerable();
    private readonly List<object> _inputs = new();
    public IEnumerable<object> Outputs => _outputs.AsEnumerable();
    private readonly List<object> _outputs = new();
    private readonly Dictionary<Type, object> _blackboard = new();
    public IEnumerable<Exception> Errors => _errors.AsEnumerable();
    private readonly List<Exception> _errors = new();

    public FakeContext() { }

    public TDataType Get<TDataType>() where TDataType : notnull =>
      _blackboard.ContainsKey(typeof(TDataType))
        ? (TDataType)_blackboard[typeof(TDataType)]
        : throw new InvalidOperationException(
          $"No value of type {typeof(TDataType)} exists in the blackboard."
        );

    public void Set<TDataType>(TDataType value) where TDataType : notnull =>
      _blackboard[typeof(TDataType)] = value;

    public void Set(Dictionary<Type, object> values) {
      foreach (var (type, value) in values) {
        _blackboard[type] = value;
      }
    }

    public void Input<TInputType>(TInputType input)
      where TInputType : notnull => _inputs.Add(input);

    public void Output<T>(in T output) where T : struct =>
      _outputs.Add(output);

    public void AddError(Exception e) => _errors.Add(e);

    public void Reset() {
      _inputs.Clear();
      _outputs.Clear();
      _errors.Clear();
      _blackboard.Clear();
    }

    /// <inheritdoc />
    public override readonly bool Equals(object obj) => true;

    /// <inheritdoc />
    public override readonly int GetHashCode() =>
      HashCode.Combine(_inputs, _outputs, _errors);
  }
}
