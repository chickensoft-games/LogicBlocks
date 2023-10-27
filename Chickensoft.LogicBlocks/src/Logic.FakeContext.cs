namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;
using System.Linq;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>
  /// Fake logic block context — provided for your testing convenience.
  /// </summary>
  public interface IFakeContext : IContext {
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
    void Set<TDataType>(TDataType value) where TDataType : notnull;

    /// <summary>
    /// Sets multiple fake values in the logic block's blackboard.
    /// </summary>
    /// <param name="values">Values to set, keyed by type.</param>
    void Set(Dictionary<Type, object> values);

    /// <summary>
    /// Clears the blackboard, the inputs, the outputs, and the errors.
    /// </summary>
    void Reset();
  }

  /// <summary>Logic block context provided to each logic block state.</summary>
  internal record FakeContext : IFakeContext {
    public IEnumerable<object> Inputs => _inputs.AsEnumerable();
    private readonly List<object> _inputs = new();
    public IEnumerable<object> Outputs => _outputs.AsEnumerable();
    private readonly List<object> _outputs = new();
    private readonly Dictionary<Type, object> _blackboard = new();
    public IEnumerable<Exception> Errors => _errors.AsEnumerable();
    private readonly List<Exception> _errors = new();

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

    public void Output(in object output) => _outputs.Add(output);

    public void AddError(Exception e) => _errors.Add(e);

    public void Reset() {
      _inputs.Clear();
      _outputs.Clear();
      _errors.Clear();
      _blackboard.Clear();
    }
  }
}
