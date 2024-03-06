namespace Chickensoft.LogicBlocks.Generator.Tests;

using Shouldly;
using Xunit;

public class HeaterTest {
  [Fact]
  public void Runs() {
    var tempSensor = new TemperatureSensor();
    var heater = new Heater(tempSensor);

    heater.Value.ShouldBeOfType<Heater.State.Off>();

    var finishedHeating = false;
    using var binding = heater.Bind();

    binding.Handle<Heater.Output.FinishedHeating>((output) =>
      finishedHeating = true
    );

    heater.Input(new Heater.Input.TargetTempChanged(73.0d));
    heater.Value.ShouldBeOfType<Heater.State.Off>();
    heater.Value.TargetTemp.ShouldBe(73.0d);

    heater.Input(new Heater.Input.TurnOn());
    // Turning it on will go straight to heating since the air temp (72) is
    // below the target temp (73)
    heater.Value.ShouldBeOfType<Heater.State.Heating>();

    // Updating air temp to above target temp should move it to idle
    tempSensor.UpdateReading(74.0d);
    heater.Value.ShouldBeOfType<Heater.State.Idle>();

    finishedHeating.ShouldBeTrue();
  }

  [Fact]
  public static void BindingsRespondToHeater() {
    var tempSensor = new TemperatureSensor();
    var heater = new Heater(tempSensor);

    using var binding = heater.Bind();

    var messages = new List<string>();

    // Handle an output produced by the heater.
    binding.Handle<Heater.Output.FinishedHeating>(
      (output) => messages.Add("Finished heating :)")
    );

    binding.When<Heater.State.Off>(
      (state) => messages.Add("Heater turned off")
    );

    // Listen to all states that inherit from Heater.State.Powered.
    binding.When<Heater.State.Powered>(
      (state) => messages.Add("Heater is powered")
    );

    binding.When<Heater.State.Idle>(
      (state) => messages.Add("Heater is idling")
    );

    binding.When<Heater.State.Heating>(
      (state) => messages.Add("Heater is heating")
    );

    heater.Input(new Heater.Input.TurnOn());

    // Dropping the temp below target should move it from idle to heating
    tempSensor.UpdateReading(66.0);
    heater.Value.ShouldBeOfType<Heater.State.Heating>();
    // Raising the temp above target should move it from heating back to idle
    tempSensor.UpdateReading(74);
    heater.Value.ShouldBeOfType<Heater.State.Idle>();

    messages.ShouldBe(new string[] {
      "Heater is powered",
      "Heater is idling",
      "Heater is powered",
      "Heater is heating",
      "Finished heating :)",
      "Heater is powered",
      "Heater is idling"
    });
  }
}
