namespace Chickensoft.LogicBlocks.Example;

using Chickensoft.LogicBlocks.Generator;

[StateMachine]
public partial class VendingMachine {
  // Inputs

  public static class Input {
    public readonly record struct SelectionEntered(ItemType Type);
    public readonly record struct PaymentReceived(int Amount);
    public readonly record struct TransactionTimedOut;
    public readonly record struct VendingCompleted;
  }

  public abstract record State : StateLogic {
    public record Idle : State,
      IGet<Input.SelectionEntered>, IGet<Input.PaymentReceived> {
      public Idle() {
        OnEnter<Idle>((previous) => Context.Output(
          new Output.ClearTransactionTimeOutTimer()
        ));
      }

      public State On(Input.SelectionEntered input) {
        if (Context.Get<VendingMachineStock>().HasItem(input.Type)) {
          return new TransactionActive.Started(
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

        OnEnter<TransactionActive>(
         (previous) => Context.Output(
           new Output.RestartTransactionTimeOutTimer()
         )
       );
      }

      public State On(Input.PaymentReceived input) {
        var total = AmountReceived + input.Amount;

        if (total >= Price) {
          if (total > Price) {
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
          Context.Get<VendingMachineStock>().Vend(Type);
          return new Vending(Type, Price);
        }

        return new PaymentPending(Type, Price, total);
      }

      public State On(Input.TransactionTimedOut input) {
        if (AmountReceived > 0) {
          // Give any money received back before timing out.
          Context.Output(new Output.MakeChange(AmountReceived));
        }
        return new Idle();
      }

      public record Started : TransactionActive,
        IGet<Input.SelectionEntered> {
        public Started(
          ItemType type, int price, int amountReceived
        ) : base(type, price, amountReceived) {
          OnEnter<Started>(
            (previous) => Context.Output(new Output.TransactionStarted())
          );
        }

        // While in this state, user can change selection as much as they want.
        public State On(Input.SelectionEntered input) {
          if (Context.Get<VendingMachineStock>().HasItem(input.Type)) {
            return new Started(
              input.Type, Prices[input.Type], AmountReceived
            );
          }
          // Item not in stock — clear selection.
          return new Idle();
        }
      }

      public record PaymentPending(
        ItemType Type, int Price, int AmountReceived
      ) : TransactionActive(Type, Price, AmountReceived);
    }

    public record Vending : State, IGet<Input.VendingCompleted> {
      public ItemType Type { get; }
      public int Price { get; }

      public Vending(ItemType type, int price) {
        Type = type;
        Price = price;

        OnEnter<Vending>(
          (previous) => Context.Output(new Output.BeginVending())
        );
      }

      public State On(Input.VendingCompleted input) =>
        new Idle();
    }
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
}

// Logic Block / Hierarchical State Machine

public partial class VendingMachine : LogicBlock<VendingMachine.State> {
  public VendingMachine(VendingMachineStock stock) {
    Set(stock);
  }

  public override State GetInitialState() => new State.Idle();
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
