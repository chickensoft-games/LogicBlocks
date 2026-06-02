namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.TestCases;

public partial class ToasterOven : LogicBlock
{

  public ToasterOven()
  {
    Set(new BaseState.Heating());
    Set(new BaseState.Toasting());
    Set(new BaseState.Baking());
    Set(new BaseState.DoorOpen());
  }

  public void StartLogicBlock() => Start<BaseState.Toasting>();

  public record Data
  {
    public int Temperature { get; set; }
    public int ToastColor { get; set; }
  }

  public static class Input
  {
    public readonly record struct OpenDoor;
    public readonly record struct CloseDoor(int ToastColor);
    public readonly record struct StartBaking(int Temperature);
    public readonly record struct StartToasting(int ToastColor);
  }
}
