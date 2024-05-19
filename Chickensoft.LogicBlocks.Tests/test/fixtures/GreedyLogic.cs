namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[LogicBlock(typeof(State)), Meta]
public partial class GreedyLogic : LogicBlock<GreedyLogic.State> {
  public override Transition GetInitialState() => To<State.A>();

  public static class Input {
    public readonly record struct GoToB;
    public readonly record struct GoToC;
  }

  public abstract partial record State : StateLogic<State> {
    public partial record A : State, IGet<Input.GoToB>, IGet<Input.GoToC> {
      public A() {
        OnAttach(() => {
          Input(new Input.GoToB());
          Input(new Input.GoToC());
        });
      }

      public Transition On(in Input.GoToB input) => To<B>();
      public Transition On(in Input.GoToC input) => To<C>();
    }
    public partial record B : State { }
    public partial record C : State { }
  }
}
