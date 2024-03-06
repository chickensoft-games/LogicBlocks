namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public record Idle : State,
  IGet<Input.SelectionEntered>, IGet<Input.PaymentReceived> {
    public Idle() {
      this.OnEnter(() => Context.Output(
        new Output.ClearTransactionTimeOutTimer()
      ));
    }

    public State On(Input.SelectionEntered input) {
      if (Get<VendingMachineStock>().HasItem(input.Type)) {
        return new TransactionStarted(
          input.Type, Prices[input.Type], 0
        );
      }
      return this;
    }

    public State On(Input.PaymentReceived input) {
      // Money was deposited with no selection — eject it right back.
      //
      // We could be evil and keep it, but we'd ruin our reputation as a
      // reliable vending machine in the office and then we'd never get ANY
      // money!
      Context.Output(new Output.MakeChange(input.Amount));
      return this;
    }
  }
}
