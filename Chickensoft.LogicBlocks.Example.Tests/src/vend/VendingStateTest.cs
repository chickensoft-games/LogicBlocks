namespace Chickensoft.LogicBlocks.Example.Tests;

using Moq;
using Vend;

public class IdleTest
{
  [Fact]
  public void OutputsShowWelcomeMessageOnEnter()
  {
    var state = new VendingState.Idle();
    var tester = state.Test();

    state.Enter();

    tester.Outputs.ShouldBe([
      new VendingState.Output.ShowWelcomeMessage()
    ]);
  }

  [Fact]
  public void
    OnSelectionEnteredOutputsOutOfStockNotificationWhenItemNotInStock()
  {
    var stock = new Mock<IVendingMachineStock>();
    var type = ItemType.Juice;

    stock.Setup(s => s.HasItem(type)).Returns(false);

    var state = new VendingState.Idle();
    var tester = state.Test();
    tester.Set(stock.Object);

    state.On(new VendingState.Input.SelectionEntered(type, 0))
      .ShouldBeSameAs(state.GetType());

    tester.Outputs.ShouldBe([
      new VendingState.Output.OutOfStockNotification(type)
    ]);
  }

  [Fact]
  public void OnSelectionEnteredTransitionsToWaitingForPaymentWhenItemInStock()
  {
    var stock = new Mock<IVendingMachineStock>();
    var type = ItemType.Candy;

    stock.Setup(s => s.HasItem(type)).Returns(true);

    var state = new VendingState.Idle();
    var tester = state.Test();
    tester.Set(new VendingData());
    tester.Set(stock.Object);

    state.On(new VendingState.Input.SelectionEntered(type, 0))
      .ShouldBeSameAs(typeof(VendingState.WaitingForPayment));
  }
}

public class TimeOutEnabledTest
{
  private sealed record TestState : VendingState.TimeOutEnabled
  {
    public override int GetTimeoutDurationSeconds() => 10;
  }

  [Fact]
  public void OnEnterOutputsCountdownWithDurationRemaining()
  {
    var state = new TestState();
    var tester = state.Test();

    state.Enter();

    tester.Outputs.ShouldBe([
      new VendingState.Output.Countdown(10)
    ]);
  }

  [Fact]
  public void OnTickOutputsCountdownOnlyWhenWholeSecondChanges()
  {
    var state = new TestState();
    var tester = state.Test();

    var data = new VendingData { StartTime = 0, ElapsedSeconds = 0 };
    tester.Set(data);

    state.On(new VendingState.Input.Tick(1000));
    state.On(new VendingState.Input.Tick(1800));
    state.On(new VendingState.Input.Tick(2000));

    tester.Outputs.ShouldBe([
      new VendingState.Output.Countdown(9),
      new VendingState.Output.Countdown(8)
    ]);
  }

  [Fact]
  public void OnTickInputsTimedOutWhenElapsed()
  {
    var state = new TestState();
    var tester = state.Test();

    var data = new VendingData { StartTime = 0, ElapsedSeconds = 0 };
    tester.Set(data);

    state.On(new VendingState.Input.Tick(10000)); // 10 seconds

    tester.Outputs.ShouldBe([
      new VendingState.Output.Countdown(0)
    ]);

    tester.Inputs.ShouldBe([
      new VendingState.Input.TimedOut()
    ]);
  }
}

public class WaitingForPaymentTest
{
  [Fact]
  public void ResetsDataOnEnter()
  {
    var state = new VendingState.WaitingForPayment();
    var tester = state.Test();

    var stock = new Mock<IVendingMachineStock>();
    var price = 50;
    stock.Setup(s => s.GetPrice(It.IsAny<ItemType>())).Returns(price);

    var data = new VendingData { Price = -1, AmountReceived = 50 };

    tester.Set(data);
    tester.Set(stock.Object);

    state.Enter();

    data.Price.ShouldBe(price);
    data.AmountReceived.ShouldBe(0);

    tester.Outputs.ShouldBe([
      new VendingState.Output.Countdown(10),
      new VendingState.Output.TransactionStarted()
    ]);
  }

  [Fact]
  public void OnCashReceivedUpdatesAmountReceivedAndTransitionsToVending()
  {
    var state = new VendingState.WaitingForPayment();
    var tester = state.Test();

    _ = new Mock<IVendingMachineStock>();
    var type = ItemType.Candy;
    var price = 50;

    var data =
      new VendingData { ItemType = type, Price = price, AmountReceived = 0 };

    tester.Set(data);

    state.On(new VendingState.Input.CashReceived(20, 0)).ShouldBeSameAs(state.GetType());
    data.AmountReceived.ShouldBe(20);

    state.On(new VendingState.Input.CashReceived(35, 0))
      .ShouldBeSameAs(typeof(VendingState.Vending));

    data.AmountReceived.ShouldBe(55);

    tester.Outputs.ShouldBe([
      new VendingState.Output.DispenseCash(5)
    ]);
  }

  [Fact]
  public void OnTimedOutDispensesCashAndTransitionsToIdle()
  {
    var state = new VendingState.WaitingForPayment();
    var tester = state.Test();

    var type = ItemType.Candy;
    var price = 50;

    var data = new VendingData
    {
      ItemType = type,
      Price = price,
      AmountReceived = 30
    };

    tester.Set(data);

    state.On(new VendingState.Input.TimedOut())
      .ShouldBeSameAs(typeof(VendingState.Idle));

    tester.Outputs.ShouldBe([
      new VendingState.Output.DispenseCash(30)
    ]);
  }
}

public class VendingTest
{
  [Fact]
  public void OnEnterDispensesItemAndOutputsDispensed()
  {
    var state = new VendingState.Vending();
    var tester = state.Test();

    var stock = new Mock<IVendingMachineStock>();
    var type = ItemType.Candy;

    var data = new VendingData { ItemType = type };

    tester.Set(data);
    tester.Set(stock.Object);

    state.Enter();

    stock.Verify(s => s.Dispense(type));
  }

  [Fact]
  public void OnExitOutputsDispensed()
  {
    var state = new VendingState.Vending();
    var tester = state.Test();

    var type = ItemType.Candy;

    var data = new VendingData { ItemType = type };

    tester.Set(data);

    state.Exit();

    tester.Outputs.ShouldBe([
      new VendingState.Output.Dispensed(type),
      new VendingState.Output.CountdownFinished()
    ]);
  }

  [Fact]
  public void OnTimedOutGoesToIdleIfStockHasItems()
  {
    var state = new VendingState.Vending();
    var tester = state.Test();

    var stock = new Mock<IVendingMachineStock>();
    stock.Setup(s => s.HasAnyItems()).Returns(true);

    var data = new VendingData();

    tester.Set(data);
    tester.Set(stock.Object);

    state.On(new VendingState.Input.TimedOut())
      .ShouldBeSameAs(typeof(VendingState.Idle));

    tester.Outputs.ShouldBeEmpty();
  }

  [Fact]
  public void OnTimedOutGoesToOutOfStockIfNoItems()
  {
    var state = new VendingState.Vending();
    var tester = state.Test();

    var stock = new Mock<IVendingMachineStock>();
    stock.Setup(s => s.HasAnyItems()).Returns(false);

    var data = new VendingData();

    tester.Set(data);
    tester.Set(stock.Object);

    state.On(new VendingState.Input.TimedOut())
      .ShouldBeSameAs(typeof(VendingState.OutOfStock));

    tester.Outputs.ShouldBeEmpty();
  }
}
