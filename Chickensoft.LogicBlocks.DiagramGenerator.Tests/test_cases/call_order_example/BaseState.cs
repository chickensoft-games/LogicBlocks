namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class CallOrderExample
{
  [StateDiagram]
  public abstract record BaseState : LogicBlockState;

  public abstract record Active : BaseState
  {
    public Active()
    {
      this.OnEnter(() => Console.WriteLine("Active"));
    }
  }

  public record Walking : Active
  {
    public Walking()
    {
      this.OnEnter(() => Console.WriteLine("Walking"));
    }
  }

  public record Running : Active
  {
    public Running()
    {
      this.OnEnter(() => Console.WriteLine("Running"));
    }
  }

  public abstract record Inactive : BaseState
  {
    public Inactive()
    {
      this.OnEnter(() => Console.WriteLine("Inactive"));
    }
  }

  public record Standing : Inactive
  {
    public Standing()
    {
      this.OnEnter(() => Console.WriteLine("Inactive"));
    }
  }
}
