namespace Chickensoft.LogicBlocks.Auto.Tests;

using Shouldly;

public class LogicBlockSerializationTest
{
  [Fact]
  public void SetupDoesNotThrow()
  {
    Should.NotThrow(LogicBlockSerialization.Setup);
  }
}
