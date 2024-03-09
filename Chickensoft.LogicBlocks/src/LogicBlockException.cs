namespace Chickensoft.LogicBlocks;

using System;

/// <summary>Creates a new logic block exception.</summary>
public class LogicBlockException : Exception {
  /// <summary>
  /// Creates a new logic block exception with the specified message.
  /// </summary>
  /// <param name="message">Exception message.</param>
  public LogicBlockException(string message) : base(message) { }
}
