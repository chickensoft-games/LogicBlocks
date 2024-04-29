namespace Chickensoft.LogicBlocks.Example;

using Chickensoft.Introspection;

public partial class VendingMachine {
  [Introspective("vending_machine_state")]
  public abstract partial record State : StateLogic<State>;
}
