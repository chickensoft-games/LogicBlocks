namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

public interface IMyLogicBlock : ILogicBlock<MyLogicBlock.State>;

[LogicBlock(typeof(State), Diagram = true), Meta]
public partial class MyLogicBlock : LogicBlock<MyLogicBlock.State>, IMyLogicBlock
{
  public override Transition GetInitialState() => To<State.SomeState>();

  public static class Input
  {
    public readonly record struct SomeInput;
    public readonly record struct SomeOtherInput;
  }

  public abstract partial record State : StateLogic<State>
  {
    public partial record SomeState : State, IGet<Input.SomeInput>
    {
      public SomeState()
      {
        this.OnEnter(() => Output(new Output.SomeOutput()));
        this.OnExit(() => Output(new Output.SomeOutput()));
      }

      public Transition On(in Input.SomeInput input)
      {
        Output(new Output.SomeOutput());
        return To<SomeOtherState>();
      }
    }

    public partial record SomeOtherState : State, IGet<Input.SomeOtherInput>
    {
      public Transition On(in Input.SomeOtherInput input)
      {
        Output(new Output.SomeOtherOutput());
        return To<SomeState>();
      }
    }
  }

  public static class Output
  {
    public readonly record struct SomeOutput;
    public readonly record struct SomeOtherOutput;
  }
}
