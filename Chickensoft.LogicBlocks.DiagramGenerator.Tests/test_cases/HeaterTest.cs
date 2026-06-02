namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

using System.Collections.Generic;
using Shouldly;
using Xunit;

public class HeaterTest
{
  [Fact]
  public void Runs()
  {
    var tempSensor = new TemperatureSensor();
    var heater = new Heater();
    heater.Set<ITemperatureSensor>(tempSensor);

    var finishedHeating = false;
    using var binding = heater.Bind();

    binding.OnOutput((in Heater.Output.FinishedHeating _) =>
      finishedHeating = true
    );

    heater.Start<Heater.HeaterState.Off>();
    heater.State.ShouldBeOfType<Heater.HeaterState.Off>();

    heater.Input(new Heater.Input.TargetTempChanged(73.0d));
    var state = heater.State.ShouldBeOfType<Heater.HeaterState.Off>();
    state.Get<Heater.Data>().TargetTemp.ShouldBe(73.0d);

    heater.Input(new Heater.Input.TurnOn());
    // Turning it on will go straight to heating since the air temp (72) is
    // below the target temp (73)
    heater.State.ShouldBeOfType<Heater.HeaterState.Heating>();

    // Updating air temp to above target temp should move it to idle
    tempSensor.UpdateReading(74.0d);
    heater.State.ShouldBeOfType<Heater.HeaterState.Idle>();

    finishedHeating.ShouldBeTrue();
  }

  [Fact]
  public static void BindingsRespondToHeater()
  {
    var tempSensor = new TemperatureSensor();
    var heater = new Heater();
    heater.Set<ITemperatureSensor>(tempSensor);

    using var binding = heater.Bind();

    var messages = new List<string>();

    // Handle an output produced by the heater.
    binding.OnOutput(
      (in Heater.Output.FinishedHeating _) =>
        messages.Add("Finished heating :)")
    ).OnState<Heater.HeaterState.Off>(
      _ => messages.Add("Heater turned off")
    ).OnState<Heater.HeaterState.Powered>(
      _ => messages.Add("Heater is powered")
    ).OnState<Heater.HeaterState.Idle>(
      _ => messages.Add("Heater is idling")
    ).OnState<Heater.HeaterState.Heating>(
      _ => messages.Add("Heater is heating")
    );

    heater.Start<Heater.HeaterState.Off>();

    // Listen to all states that inherit from Heater.State.Powered.

    heater.Input(new Heater.Input.TurnOn());

    // Dropping the temp below target should move it from idle to heating
    tempSensor.UpdateReading(66.0);
    heater.State.ShouldBeOfType<Heater.HeaterState.Heating>();
    // Raising the temp above target should move it from heating back to idle
    tempSensor.UpdateReading(74);
    heater.State.ShouldBeOfType<Heater.HeaterState.Idle>();

    messages.ShouldBe([
      "Heater turned off",
      "Heater is powered",
      "Heater is idling",
      "Heater is powered",
      "Heater is heating",
      "Finished heating :)",
      "Heater is powered",
      "Heater is idling"
    ]);
  }
}
