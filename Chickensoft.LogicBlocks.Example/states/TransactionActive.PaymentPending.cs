namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public record PaymentPending(
    ItemType Type, int Price, int AmountReceived
  ) : TransactionActive(Type, Price, AmountReceived);
}
