namespace Chickensoft.LogicBlocks.Example;

using Chickensoft.Introspection;

public partial class VendingMachine {
  [Introspective("vending_machine_transaction_started")]
  public partial record TransactionStarted : TransactionActive,
  IGet<Input.SelectionEntered> {
    public TransactionStarted() {
      this.OnEnter(() => Output(new Output.TransactionStarted()));
    }
  }
}
