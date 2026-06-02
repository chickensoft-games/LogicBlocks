namespace Chickensoft.LogicBlocks.Auto;

using Serialization;

/// <summary>
/// Logic block serialization.
/// </summary>
public static class LogicBlockSerialization
{
  /// <summary>
  /// Register the JSON converters for logic block types with the Chickensoft
  /// Serialization system. Call this before performing any serialization with
  /// logic blocks.
  /// </summary>
  public static void Setup() =>
    Serializer.AddConverter(new LogicBlockDataConverter());
}
