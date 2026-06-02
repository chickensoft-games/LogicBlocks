namespace Chickensoft.LogicBlocks.Tests;

using Shouldly;

public class LogicBlockExceptionTest
{
  [Fact]
  public void MessageConstructor()
  {
    var ex = new LogicBlockException("oops");

    ex.Message.ShouldBe("oops");
    ex.InnerException.ShouldBeNull();
  }

  [Fact]
  public void InnerExceptionConstructor()
  {
    var inner = new InvalidOperationException("inner");
    var ex = new LogicBlockException("oops", inner);

    ex.Message.ShouldBe("oops");
    ex.InnerException.ShouldBeSameAs(inner);
  }
}
