namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public record Vending : State, IGet<Input.VendingCompleted> {
    public ItemType Type { get; }
    public int Price { get; }

    public Vending(ItemType type, int price) {
      Type = type;
      Price = price;

      this.OnEnter(() => Output(new Output.BeginVending()));
    }

    public State On(in Input.VendingCompleted input) => new Idle();
  }
}
