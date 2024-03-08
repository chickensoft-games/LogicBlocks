namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public abstract record TransactionActive : State,
  IGet<Input.PaymentReceived>, IGet<Input.TransactionTimedOut> {

    public ItemType Type { get; }
    public int Price { get; }
    public int AmountReceived { get; }

    public TransactionActive(
      ItemType type, int price, int amountReceived
    ) {
      Type = type;
      Price = price;
      AmountReceived = amountReceived;

      this.OnEnter(
       () => Context.Output(
         new Output.RestartTransactionTimeOutTimer()
       )
     );
    }

    public State On(in Input.PaymentReceived input) {
      var total = AmountReceived + input.Amount;

      if (total < Price) {
        // Waiting on the user to insert enough cash to finish the transaction.
        return new PaymentPending(Type, Price, total);
      }

      if (total > Price) {
        // If we end up receiving more money than the item costs, we make
        // change and dispense it back to the user.
        Context.Output(new Output.MakeChange(total - Price));
      }

      Context.Output(
        new Output.TransactionCompleted(
          Type: Type,
          Price: Price,
          Status: TransactionStatus.Success,
          AmountPaid: total
        )
      );

      Get<VendingMachineStock>().Vend(Type);

      return new Vending(Type, Price);
    }

    public State On(in Input.TransactionTimedOut input) {
      if (AmountReceived > 0) {
        // Give any money received back before timing out.
        Context.Output(new Output.MakeChange(AmountReceived));
      }

      return new Idle();
    }
  }
}
