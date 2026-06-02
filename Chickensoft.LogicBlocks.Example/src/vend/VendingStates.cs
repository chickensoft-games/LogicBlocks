namespace Chickensoft.LogicBlocks.Example.Vend;

using System;

public partial record VendingState
{
  public record Idle : VendingState, IGet<Input.SelectionEntered>
  {
    public Idle()
    {
      this.OnEnter(() => Output(new Output.ShowWelcomeMessage()));
    }

    public Type On(in Input.SelectionEntered input)
    {
      if (Stock.HasItem(input.ItemType))
      {
        Data.ItemType = input.ItemType;
        Data.StartTime = input.TickCount;
        return To<WaitingForPayment>();
      }

      Output(new Output.OutOfStockNotification(input.ItemType));

      return ToSelf();
    }
  }

  public abstract record TimeOutEnabled : VendingState, IGet<Input.Tick>
  {
    public abstract int GetTimeoutDurationSeconds();

    public TimeOutEnabled()
    {
      this.OnEnter(() =>
        Output(new Output.Countdown(GetTimeoutDurationSeconds()))
      );

      this.OnExit(() => Output(new Output.CountdownFinished()));
    }

    public Type On(in Input.Tick input)
    {
      // Compute total elapsed since start
      var totalMs = input.TickCount - Data.StartTime;
      var elapsedSec = totalMs / 1000;

      // Only emit when the whole‐second tick changes
      if (elapsedSec != Data.ElapsedSeconds)
      {
        Data.ElapsedSeconds = elapsedSec;

        var timeoutSec = GetTimeoutDurationSeconds();
        var rem = Math.Max(0, timeoutSec - elapsedSec);
        Output(new Output.Countdown(rem));
      }

      // Fire timeout as soon as we hit (or exceed) the limit
      if (elapsedSec >= GetTimeoutDurationSeconds())
      {
        Input(new Input.TimedOut());
      }

      return ToSelf();
    }
  }

  public record WaitingForPayment :
    TimeOutEnabled,
    IGet<Input.CashReceived>,
    IGet<Input.SelectionEntered>,
    IGet<Input.TimedOut>
  {
    public WaitingForPayment()
    {
      this.OnEnter(() =>
      {
        Data.Price = Stock.GetPrice(Data.ItemType);
        Data.AmountReceived = 0;

        Output(new Output.TransactionStarted());
      });
    }

    public override int GetTimeoutDurationSeconds() => 10;

    public Type On(in Input.CashReceived input)
    {
      // restart transaction timer when we receive cash
      Data.StartTime = input.TickCount;
      Data.AmountReceived += input.Amount;

      var changeAmount = Data.AmountReceived - Data.Price;

      if (changeAmount > 0)
      {
        Output(new Output.DispenseCash(changeAmount));
      }

      if (changeAmount >= 0)
      {
        return To<Vending>();
      }

      return ToSelf();
    }

    public Type On(in Input.TimedOut input)
    {
      if (Data.AmountReceived > 0)
      {
        Output(new Output.DispenseCash(Data.AmountReceived));
      }

      return To<Idle>();
    }

    public Type On(in Input.SelectionEntered input)
    {
      // restart transaction timer when selection is updated
      Data.StartTime = input.TickCount;

      if (Stock.HasItem(input.ItemType))
      {
        Data.ItemType = input.ItemType;
        Data.StartTime = input.TickCount;
        return ToSelf();
      }

      Output(new Output.OutOfStockNotification(input.ItemType));

      return ToSelf();
    }
  }

  public record Vending : TimeOutEnabled, IGet<Input.TimedOut>
  {
    public Vending()
    {
      this.OnEnter(() => Stock.Dispense(Data.ItemType));
      this.OnExit(() => Output(new Output.Dispensed(Data.ItemType)));
    }

    public override int GetTimeoutDurationSeconds() => 3;

    public Type On(in Input.TimedOut input)
    {
      if (Stock.HasAnyItems())
      {
        return To<Idle>();
      }

      return To<OutOfStock>();
    }
  }

  public record OutOfStock : VendingState
  {
    public OutOfStock()
    {
      this.OnEnter(() => Output(
        new Output.OutOfStockNotification(Data.ItemType))
      );
    }
  }
}
