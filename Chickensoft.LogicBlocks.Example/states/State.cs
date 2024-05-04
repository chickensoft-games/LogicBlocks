namespace Chickensoft.LogicBlocks.Example;

using Chickensoft.Introspection;

public partial class VendingMachine {
  [Meta("vending_machine_state")]
  public abstract partial record State : StateLogic<State>;
}
