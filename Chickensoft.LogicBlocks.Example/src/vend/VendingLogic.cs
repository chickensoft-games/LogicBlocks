namespace Chickensoft.LogicBlocks.Example.Vend;

public partial class VendingLogic : LogicBlock
{
  public VendingLogic()
  {
    // Initialize data shared between states
    Set(new VendingData());

    // Preallocate states
    Set(new VendingState.Idle());
    Set(new VendingState.WaitingForPayment());
    Set(new VendingState.Vending());
    Set(new VendingState.OutOfStock());
  }
}
