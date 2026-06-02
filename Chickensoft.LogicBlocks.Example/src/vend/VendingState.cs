namespace Chickensoft.LogicBlocks.Example.Vend;

public abstract partial record VendingState : LogicBlockState
{
  // Convenience getters
  public VendingData Data => Get<VendingData>();
  public IVendingMachineStock Stock => Get<IVendingMachineStock>();

  // Inputs
  public static class Input
  {
    public readonly record struct SelectionEntered(
      ItemType ItemType,
      int TickCount
    );

    public readonly record struct CashReceived(int Amount, int TickCount);

    public readonly record struct Tick(int TickCount);

    public readonly record struct TimedOut;
  }

  // Outputs (side effects)
  public static class Output
  {
    public readonly record struct Dispensed(ItemType Type);

    public readonly record struct OutOfStockNotification(ItemType Type);

    public readonly record struct TransactionStarted;

    public readonly record struct DispenseCash(int Amount);

    public readonly record struct ShowWelcomeMessage();

    public readonly record struct Countdown(int SecondsRemaining);

    public readonly record struct CountdownFinished();
  }
}
