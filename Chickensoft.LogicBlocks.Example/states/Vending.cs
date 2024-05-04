namespace Chickensoft.LogicBlocks.Example;

using Chickensoft.Introspection;

public partial class VendingMachine {
  [Meta("vending_machine_vending")]
  public partial record Vending : State, IGet<Input.VendingCompleted> {
    public Vending() {
      this.OnEnter(() => Output(new Output.BeginVending()));
    }

    public Transition On(Input.VendingCompleted input) => To<Idle>();
  }
}
