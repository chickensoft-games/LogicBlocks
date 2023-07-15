namespace Chickensoft.LogicBlocks.Tests.Fixtures;
public class NonEquatable :
  LogicBlock<NonEquatable.Input, NonEquatable.State, NonEquatable.Output> {
  public abstract record Input {
    public record GoToA : Input;
    public record GoToB : Input;
  }

  public abstract class State : IStateLogic,
    IGet<Input.GoToA>, IGet<Input.GoToB> {
    public Context Context { get; }
    public State(Context context) {
      Context = context;
    }

    public class A : State {
      public A(Context context) : base(context) { }
    }

    public class B : State {
      public B(Context context) : base(context) { }
    }

    public State On(Input.GoToA input) => new A(Context);
    public State On(Input.GoToB input) => new B(Context);
  }

  public class Output { }

  public override State GetInitialState(Context context) =>
    new State.A(context);
}
