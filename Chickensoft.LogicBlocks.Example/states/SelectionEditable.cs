namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public abstract partial record SelectionEditable : State,
  IGet<Input.SelectionEntered> {
    public Transition On(in Input.SelectionEntered input) {
      var data = Get<Data>();

      Output(new Output.RestartTransactionTimeOutTimer());

      if (Get<VendingMachineStock>().HasItem(input.Type)) {
        data.Type = input.Type;
        data.Price = Prices[input.Type];

        return To<TransactionStarted>();
      }

      return ToSelf();
    }
  }
}
