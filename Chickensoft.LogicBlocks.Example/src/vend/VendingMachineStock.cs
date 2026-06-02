namespace Chickensoft.LogicBlocks.Example.Vend;

using System.Collections.Generic;
using System.Linq;

public interface IVendingMachineStock
{
  IReadOnlyDictionary<ItemType, int> Quantities { get; }
  IReadOnlyDictionary<ItemType, int> Prices { get; }

  /// <summary>
  /// Gets the quantity of a specific item type in stock.
  /// </summary>
  int GetQuantity(ItemType type);

  /// <summary>
  /// Gets the price of a specific item type.
  /// </summary>
  int GetPrice(ItemType type);

  /// <summary>
  /// Checks if an item type is available in stock.
  /// </summary>
  bool HasItem(ItemType type);

  /// <summary>
  /// Checks if there are any items available in stock.
  /// </summary>
  bool HasAnyItems();

  /// <summary>
  /// Dispenses an item of a specific type, reducing its stock quantity.
  /// </summary>
  void Dispense(ItemType type);
}

// Just a domain layer repository that manages the stock for a vending machine.
public class VendingMachineStock : IVendingMachineStock
{
  private readonly Dictionary<ItemType, int> _quantities;
  private readonly Dictionary<ItemType, int> _prices;

  public IReadOnlyDictionary<ItemType, int> Quantities => _quantities;
  public IReadOnlyDictionary<ItemType, int> Prices => _prices;

  public VendingMachineStock(
    IReadOnlyDictionary<ItemType, int> quantities,
    IReadOnlyDictionary<ItemType, int> prices
  )
  {
    // Collection triggers erroneous warnings since we aren't on net11 and
    // can't use new `with` collection expression arguments :P
#pragma warning disable IDE0028
    _quantities = new(quantities);
    _prices = new(prices);
#pragma warning restore IDE0028
  }

  public int GetQuantity(ItemType type) => _quantities[type];
  public int GetPrice(ItemType type) => _prices[type];
  public bool HasItem(ItemType type) => _quantities[type] > 0;
  public bool HasAnyItems() => _quantities.Values.Any(qty => qty > 0);
  public void Dispense(ItemType type) => _quantities[type]--;
}
