namespace Chickensoft.LogicBlocks.Example;

using Chickensoft.Introspection;

public partial class VendingMachine {
  [Introspective("vending_machine_selection_editable")]
  public abstract partial record SelectionEditable : State,
  IGet<Input.SelectionEntered> {
    public Transition On(Input.SelectionEntered input) {
      var data = Get<Data>();

      if (Get<VendingMachineStock>().HasItem(input.Type)) {
        data.Type = input.Type;
        data.Price = Prices[input.Type];

        return To<TransactionStarted>();
      }

      return ToSelf();
    }
  }
}
