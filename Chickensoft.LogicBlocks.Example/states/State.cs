namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine
{
  public abstract partial record State : StateLogic<State>;
}
