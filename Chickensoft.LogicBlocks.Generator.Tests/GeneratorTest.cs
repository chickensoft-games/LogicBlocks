namespace Chickensoft.LogicBlocks.Generator.Tests;

using Shouldly;
using Xunit;

public class GeneratorTest {
  [Fact]
  public void GeneratesUml() {
    var contents = Tester.LoadFixture("ToasterOven.cs");
    var result = Tester.Generate(contents);

    result.Outputs["ToasterOven.puml.g.cs"].ShouldBe("""
    @startuml ToasterOven
    state "ToasterOven State" as State {
      state Heating {
        state Toasting {
          Toasting : OnEnter → SetTimer
          Toasting : OnExit → ResetTimer
        }
        state Baking {
          Baking : OnEnter → SetTemperature
          Baking : OnExit → SetTemperature
        }
        Heating : OnEnter → TurnHeaterOn
        Heating : OnExit → TurnHeaterOff
      }
      state DoorOpen {
        DoorOpen : OnEnter → TurnLampOn
        DoorOpen : OnExit → TurnLampOff
      }
    }

    Baking --> Toasting : StartToasting
    DoorOpen --> Toasting : CloseDoor
    Heating --> DoorOpen : OpenDoor
    Toasting --> Baking : StartBaking

    [*] --> Toasting
    @enduml
    """.Clean());
  }
}
