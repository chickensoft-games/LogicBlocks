namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public abstract partial record TransactionActive : SelectionEditable,
  IGet<Input.PaymentReceived>, IGet<Input.TransactionTimedOut> {
    public TransactionActive() {
      this.OnEnter(() => Output(new Output.RestartTransactionTimeOutTimer()));
      this.OnExit(() => Get<Data>().AmountReceived = 0);
    }

    public Transition On(in Input.PaymentReceived input) {
      var data = Get<Data>();

      Output(new Output.RestartTransactionTimeOutTimer());

      data.AmountReceived += input.Amount;

      if (data.AmountReceived < data.Price) {
        // Waiting on the user to insert enough cash to finish the transaction.
        return ToSelf();
      }

      if (data.AmountReceived > data.Price) {
        // If we end up receiving more money than the item costs, we make
        // change and dispense it back to the user.
        Output(new Output.MakeChange(data.AmountReceived - data.Price));
      }

      Output(
        new Output.TransactionCompleted(
          Type: data.Type,
          Price: data.Price,
          Status: TransactionStatus.Success,
          AmountPaid: data.AmountReceived
        )
      );

      Get<VendingMachineStock>().Vend(data.Type);

      return To<Vending>();
    }

    public Transition On(in Input.TransactionTimedOut input) {
      var data = Get<Data>();

      if (data.AmountReceived > 0) {
        // Give any money received back before timing out.
        Output(new Output.MakeChange(data.AmountReceived));
      }

      return To<Idle>();
    }
  }
}
