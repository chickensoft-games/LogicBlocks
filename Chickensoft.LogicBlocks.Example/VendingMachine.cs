namespace Chickensoft.LogicBlocks.Example;

using System.Collections.Generic;
using Chickensoft.Introspection;
using Chickensoft.Serialization;

[Introspective("vending_machine")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class VendingMachine : LogicBlock<VendingMachine.State> {
  // Data shared between states
  [Introspective("vending_machine_data")]
  public partial record Data {
    [Save("type")]
    public ItemType Type { get; set; }

    [Save("price")]
    public int Price { get; set; }

    [Save("amount_received")]
    public int AmountReceived { get; set; }

    public override string ToString() =>
      $"Data(Type: {Type}, Price: {Price}, AmountReceived: {AmountReceived})";
  }

  // Inputs

  public static class Input {
    public readonly record struct SelectionEntered(ItemType Type);
    public readonly record struct PaymentReceived(int Amount);
    public readonly record struct TransactionTimedOut;
    public readonly record struct VendingCompleted;
  }

  // Side effects

  public static class Output {
    public readonly record struct Dispensed(ItemType Type);
    public readonly record struct TransactionStarted;
    public readonly record struct TransactionCompleted(
      ItemType Type, int Price, TransactionStatus Status, int AmountPaid
    );
    public readonly record struct RestartTransactionTimeOutTimer;
    public readonly record struct ClearTransactionTimeOutTimer;
    public readonly record struct MakeChange(int Amount);
    public readonly record struct BeginVending { }
  }

  // Feature-specific stuff

  public static readonly Dictionary<ItemType, int> Prices = new() {
    [ItemType.Juice] = 4,
    [ItemType.Water] = 2,
    [ItemType.Candy] = 6
  };

  public override Transition GetInitialState() => To<Idle>();

  public VendingMachine() {
    Save(() => new Data());
  }
}

// Just a domain layer repository that manages the stock for a vending machine.
public class VendingMachineStock {
  public Dictionary<ItemType, int> Stock { get; }

  public VendingMachineStock(Dictionary<ItemType, int> stock) {
    Stock = stock;
  }

  public int Qty(ItemType type) => Stock[type];
  public bool HasItem(ItemType type) => Stock[type] > 0;
  public void Vend(ItemType type) => Stock[type]--;
}
