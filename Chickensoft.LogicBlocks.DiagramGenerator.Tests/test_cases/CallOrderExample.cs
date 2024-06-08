namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using System;

[LogicBlock(typeof(State), Diagram = true)]
public class CallOrderExample : LogicBlock<CallOrderExample.State> {
  public abstract record State : StateLogic<State>;

  public abstract record Active : State {
    public Active() {
      this.OnEnter(() => Console.WriteLine("Active"));
    }
  }

  public record Walking : Active {
    public Walking() {
      this.OnEnter(() => Console.WriteLine("Walking"));
    }
  }

  public record Running : Active {
    public Running() {
      this.OnEnter(() => Console.WriteLine("Running"));
    }
  }

  public abstract record Inactive : State {
    public Inactive() {
      this.OnEnter(() => Console.WriteLine("Inactive"));
    }
  }

  public record Standing : Inactive {
    public Standing() {
      this.OnEnter(() => Console.WriteLine("Inactive"));
    }
  }

  public override Transition GetInitialState() => To<Standing>();
}
