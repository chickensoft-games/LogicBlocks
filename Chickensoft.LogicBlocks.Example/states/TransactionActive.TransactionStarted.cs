namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public partial record TransactionStarted : TransactionActive,
  IGet<Input.SelectionEntered> {
    public TransactionStarted() {
      this.OnEnter(() => Output(new Output.TransactionStarted()));
    }
  }
}
