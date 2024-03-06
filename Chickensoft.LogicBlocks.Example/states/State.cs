namespace Chickensoft.LogicBlocks.Example;

public partial class VendingMachine {
  public abstract record State : StateLogic<State>;
}
