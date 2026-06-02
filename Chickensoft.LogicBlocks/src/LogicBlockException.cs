namespace Chickensoft.LogicBlocks;

public class LogicBlockException : Exception
{
  public LogicBlockException(string message) : base(message) { }
  public LogicBlockException(string message, Exception innerException)
    : base(message, innerException) { }
}
