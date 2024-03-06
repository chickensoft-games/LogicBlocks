namespace Chickensoft.LogicBlocks;

using System;
using System.Collections.Generic;

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
  void Set<TDataType>(TDataType value) where TDataType : class;

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
