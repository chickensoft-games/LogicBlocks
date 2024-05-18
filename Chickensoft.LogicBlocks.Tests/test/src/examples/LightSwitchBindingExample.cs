namespace Chickensoft.LogicBlocks.Tests.Examples;

using System;

public class LightSwitchBindingExample {
  public void BindingExample() {
    var logic = new LightSwitch();

    // Start the logic block to force the initial state to be active.
    //
    // This is optional: you can also start a logic block by just adding an
    // input to it or reading its state.
    logic.Start();

    // Add an input to turn our light switch on.
    logic.Input(new LightSwitch.Input.Toggle());

    // The logic block's value represents the current state.
    var state = logic.Value; // PoweredOn

    // Bindings allow you to observe the logic block easily.
    using var binding = logic.Bind();

    // Monitor an output:
    binding.Handle((in LightSwitch.Output.StatusChanged output) =>
      Console.WriteLine(
        $"Status changed to {(output.IsOn ? "on" : "off")}"
      )
    );

    // Can also use bindings to monitor inputs, state changes, and exceptions.
    //
    // In general, prefer monitoring outputs over state changes for more
    // flexible code.

    // Monitor an input:
    binding.Watch((in LightSwitch.Input.Toggle input) =>
      Console.WriteLine("Toggled!")
    );

    // Monitor a specific type of state:
    binding.When((LightSwitch.State.PoweredOn _) =>
      Console.WriteLine("Powered on!")
    );

    // Monitor all exceptions:
    binding.Catch((Exception e) => Console.WriteLine(e.Message));

    // Monitor specific types of exceptions:
    binding.Catch((InvalidOperationException e) =>
      Console.WriteLine(e.Message)
    );
  }
}
