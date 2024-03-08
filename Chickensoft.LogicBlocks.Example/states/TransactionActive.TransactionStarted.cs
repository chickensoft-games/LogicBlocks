namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public record TransactionStarted : TransactionActive,
  IGet<Input.SelectionEntered> {

    public TransactionStarted(
      ItemType type, int price, int amountReceived
    ) : base(type, price, amountReceived) {
      this.OnEnter(() => Context.Output(new Output.TransactionStarted()));
    }

    // While in this state, user can change selection as much as they want.
    public State On(in Input.SelectionEntered input) {
      if (Get<VendingMachineStock>().HasItem(input.Type)) {
        return new TransactionStarted(
          input.Type, Prices[input.Type], AmountReceived
        );
      }

      // Item not in stock — clear selection.
      return new Idle();
    }
  }
}
