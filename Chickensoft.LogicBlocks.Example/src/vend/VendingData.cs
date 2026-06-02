namespace Chickensoft.LogicBlocks.Example.Vend;

// Data shared between states
public partial record VendingData
{
  public ItemType ItemType { get; set; }

  public int Price { get; set; }

  public int AmountReceived { get; set; }
  public int StartTime { get; set; }
  public int ElapsedSeconds { get; set; }

  public override string ToString() =>
    $"Data(Type: {ItemType}, Price: {Price}, AmountReceived: {AmountReceived})";
}
