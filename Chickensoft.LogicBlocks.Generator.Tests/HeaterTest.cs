namespace Chickensoft.LogicBlocks.Generator.Tests;

using Shouldly;
using Xunit;

public class HeaterTest {
  [Fact]
  public void Runs() {
    var tempSensor = new TemperatureSensor();
    var heater = new Heater(tempSensor);

    using var binding = heater.Bind();

    binding.Handle<Heater.Output.AirTempChanged>(
      (output) => Console.WriteLine($"Air temp changed to {output.AirTemp}")
    );

    binding.When<Heater.State.Off>().Call(
      (state) => Console.WriteLine("Heater is off")
    );

    binding.When<Heater.State.Idle>().Call(
      (state) => Console.WriteLine("Heater is idle")
    );

    heater.Input(new Heater.Input.TurnOn());

    tempSensor.UpdateReading(64);

    var heating = heater.Value.ShouldBeAssignableTo<Heater.State.Heating>();
  }
}
