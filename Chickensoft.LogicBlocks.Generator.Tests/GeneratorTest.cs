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
    state "ToasterOven State" as Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State {
      state "Heating" as Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Heating {
        state "Toasting" as Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting
        state "Baking" as Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Baking
      }
      state "DoorOpen" as Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_DoorOpen
    }

    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Baking --> Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting : StartToasting
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_DoorOpen --> Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting : CloseDoor
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Heating --> Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_DoorOpen : OpenDoor
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting --> Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Baking : StartBaking

    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Baking : OnEnter → SetTemperature
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Baking : OnExit → SetTemperature
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_DoorOpen : OnEnter → TurnLampOn
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_DoorOpen : OnExit → TurnLampOff
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Heating : OnEnter → TurnHeaterOn
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Heating : OnExit → TurnHeaterOff
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting : OnEnter → SetTimer
    Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting : OnExit → ResetTimer

    [*] --> Chickensoft_LogicBlocks_Generator_Tests_ToasterOven_State_Toasting
    @enduml
    """.Clean());
  }
}
