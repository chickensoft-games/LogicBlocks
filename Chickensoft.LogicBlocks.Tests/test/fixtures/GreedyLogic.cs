namespace Chickensoft.LogicBlocks.Tests.Fixtures;

public class GreedyLogic : LogicBlock<GreedyLogic.State> {
  public override State GetInitialState() => new State.A();

  public static class Input {
    public readonly record struct GoToB;
    public readonly record struct GoToC;
  }

  public abstract partial record State : StateLogic<State> {
    public record A : State, IGet<Input.GoToB>, IGet<Input.GoToC> {
      public A() {
        OnAttach(() => {
          Input(new Input.GoToB());
          Input(new Input.GoToC());
        });
      }

      public State On(in Input.GoToB input) => new B();
      public State On(in Input.GoToC input) => new C();
    }

    public record B : State { }
    public record C : State { }
  }
}
