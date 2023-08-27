namespace Chickensoft.LogicBlocks.Tests.Fixtures;
public class NonEquatable :
  LogicBlock<NonEquatable.Input, NonEquatable.State, NonEquatable.Output> {
  public abstract record Input {
    public record GoToA : Input;
    public record GoToB : Input;
  }

  public abstract class State : IStateLogic,
    IGet<Input.GoToA>, IGet<Input.GoToB> {
    public IContext Context { get; }
    public State(IContext context) {
      Context = context;
    }

    public class A : State {
      public A(IContext context) : base(context) { }
    }

    public class B : State {
      public B(IContext context) : base(context) { }
    }

    public State On(Input.GoToA input) => new A(Context);
    public State On(Input.GoToB input) => new B(Context);
  }

  public class Output { }

  public override State GetInitialState(IContext context) =>
    new State.A(context);
}
