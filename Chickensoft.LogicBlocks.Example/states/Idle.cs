namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public partial record Idle : SelectionEditable, IGet<Input.PaymentReceived> {
    public Idle() {
      this.OnEnter(() => Output(new Output.ClearTransactionTimeOutTimer()));
    }

    public Transition On(in Input.PaymentReceived input) {
      // Money was deposited with no selection — eject it right back.
      //
      // We could be evil and keep it, but we'd ruin our reputation as a
      // reliable vending machine in the office and then we'd never get ANY
      // money!
      Output(new Output.MakeChange(input.Amount));
      return ToSelf();
    }
  }
}
