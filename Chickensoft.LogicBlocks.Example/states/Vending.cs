namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public partial record Vending : State, IGet<Input.VendingCompleted> {
    public Vending() {
      this.OnEnter(() => Output(new Output.BeginVending()));
    }

    public Transition On(in Input.VendingCompleted input) => To<Idle>();
  }
}
