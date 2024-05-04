namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using Chickensoft.Introspection;

public enum SecondaryState {
  Blooped,
  Bopped
}

[Meta("test_machine")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class TestMachine : LogicBlock<TestMachine.State> {
  public static class Input {
    public readonly record struct Activate(SecondaryState Secondary);
    public readonly record struct Deactivate();
  }

  [Meta("test_machine_state")]
  public abstract partial record State : StateLogic<State>, IGet<Input.Activate> {
    public Transition On(in Input.Activate input) =>
      input.Secondary switch {
        SecondaryState.Blooped => To<Activated.Blooped>(),
        SecondaryState.Bopped => To<Activated.Bopped>(),
        _ => throw new ArgumentException("Unrecognized secondary state.")
      };

    [Meta("test_machine_state_activated")]
    public abstract partial record Activated : State, IGet<Input.Deactivate> {
      public Activated() {
        this.OnEnter(() => Output(new Output.Activated()));
        this.OnExit(() => Output(new Output.ActivatedCleanUp()));
      }

      public Transition On(in Input.Deactivate input) => To<Deactivated>();

      [Meta("test_machine_state_activated_blooped")]
      public partial record Blooped : Activated {
        public Blooped() {
          this.OnEnter(() => Output(new Output.Blooped()));
          this.OnExit(() => Output(new Output.BloopedCleanUp()));
        }
      }

      [Meta("test_machine_state_activated_bopped")]
      public partial record Bopped : Activated {
        public Bopped() {
          this.OnEnter(() => Output(new Output.Bopped()));
          this.OnExit(() => Output(new Output.BoppedCleanUp()));
        }
      }
    }

    [Meta("test_machine_state_deactivated")]
    public partial record Deactivated : State {
      public Deactivated() {
        this.OnEnter(() => Output(new Output.Deactivated()));
        this.OnExit(() => Output(new Output.DeactivatedCleanUp()));
      }
    }
  }

  public static class Output {
    public readonly record struct Activated;
    public readonly record struct ActivatedCleanUp;
    public readonly record struct Deactivated;
    public readonly record struct DeactivatedCleanUp;
    public readonly record struct Blooped;
    public readonly record struct BloopedCleanUp;
    public readonly record struct Bopped;
    public readonly record struct BoppedCleanUp;
  }

  public override Transition GetInitialState() => To<State.Deactivated>();
}
