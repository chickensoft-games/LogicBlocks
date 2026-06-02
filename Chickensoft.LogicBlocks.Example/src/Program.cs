namespace Chickensoft.LogicBlocks.Example;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vend;

public static class Program
{
  public const string IMAGE =
  """
    :::::::::::::::::::::::::::::
    :::::::::::::::::::::::::::::
    ::                   :::___::
    ::   JJ   WW   CC    :::---::
    ::  XXXXXXXXXXXXXXX  :::___::
    ::                   ::::::::
    ::   JJ   WW   CC    ::::::::
    ::  XXXXXXXXXXXXXXX  :::...::
    ::                   ::<  >::
    ::   JJ   WW   CC    ::::::::
    ::  XXXXXXXXXXXXXXX  :::...::
    ::                   :::...::
    ::._________________.::<  >::
    ::|##################|:::::::
    ::|==================|:::::::
    :::::::::::::::::::::::::::::
  """;

  public static readonly Dictionary<ItemType, int> Quantities = new()
  {
    [ItemType.Juice] = 6,
    [ItemType.Water] = 6,
    [ItemType.Candy] = 6
  };

  public static readonly Dictionary<ItemType, int> Prices = new()
  {
    [ItemType.Juice] = 4,
    [ItemType.Water] = 2,
    [ItemType.Candy] = 6
  };

  public const char EMPTY = '_';

  public static readonly Dictionary<ItemType, char> Chars = new()
  {
    [ItemType.Juice] = 'J',
    [ItemType.Water] = 'W',
    [ItemType.Candy] = 'C'
  };

  public static readonly IVendingMachineStock Stock = new VendingMachineStock(
    Quantities, Prices
  );

  // Keep a buffer of the last few outputs to show them on the screen.
  private const int MAX_OUTPUTS = 3;

  private static readonly Queue<object> _lastFewOutputs =
    new(MAX_OUTPUTS);

  public static int Main(string[] args)
  {
    var shouldContinue = true;
    var shouldRender = true;
    int? timeRemaining = null;

    Console.CancelKeyPress += (_, _) => shouldContinue = false;

    var vendingLogic = new VendingLogic();
    vendingLogic.Set(Stock);

    using var binding = vendingLogic.Bind();

    void defaultHandler(object output)
    {
      AddOutputToBuffer(output);
      shouldRender = true;
    }


    binding
      .OnOutput((in VendingState.Output.Dispensed output) =>
        defaultHandler(output)
      )
      .OnOutput((in VendingState.Output.OutOfStockNotification output) =>
        defaultHandler(output)
      )
      .OnOutput((in VendingState.Output.TransactionStarted output) =>
        defaultHandler(output)
      )
      .OnOutput((in VendingState.Output.DispenseCash output) =>
        defaultHandler(output)
      )
      .OnOutput((in VendingState.Output.ShowWelcomeMessage output) =>
        defaultHandler(output)
      )
      .OnOutput((in VendingState.Output.Countdown output) =>
      {
        timeRemaining = output.SecondsRemaining;
        defaultHandler(output);
      })
      .OnOutput((in VendingState.Output.CountdownFinished output) =>
      {
        timeRemaining = null;
        defaultHandler(output);
      });

    // observe every state change since states are derived from VendingState
    binding.OnState((VendingState _) => shouldRender = true);

    vendingLogic.Start<VendingState.Idle>();

    var lastState = vendingLogic.State;

    void render()
    {
      shouldRender = false;
      ShowOverview();
      ShowState(vendingLogic);
      ShowOutputs(_lastFewOutputs);
      Console.Write("   ");
      ShowTimeRemaining(timeRemaining);
      Console.Write("> ");
      lastState = vendingLogic.State;
    }

    render();

    while (shouldContinue)
    {
      if (shouldRender)
      {
        render();
        lastState = vendingLogic.State;
      }

      var ticks = Environment.TickCount;

      vendingLogic.Input(new VendingState.Input.Tick(ticks));

      if (!Console.KeyAvailable)
      {
        continue;
      }

      var key = Console.ReadKey(false);

      // Move cursor back one
      Console.Write("\b");

      int? digit = null;

      switch (key.Key)
      {
        case ConsoleKey.Escape:
          Console.Write("q "); // Fixes formatting.
          goto case ConsoleKey.Q;
        case ConsoleKey.Q:
          Console.WriteLine("Done. Thanks for using the vending machine!");
          shouldContinue = false;
          break;
        case ConsoleKey.J:
          vendingLogic.Input(
            new VendingState.Input.SelectionEntered(ItemType.Juice, ticks)
          );
          break;
        case ConsoleKey.W:
          vendingLogic.Input(
            new VendingState.Input.SelectionEntered(ItemType.Water, ticks)
          );
          break;
        case ConsoleKey.C:
          vendingLogic.Input(
            new VendingState.Input.SelectionEntered(ItemType.Candy, ticks)
          );
          break;
        case ConsoleKey.D0:
        case ConsoleKey.NumPad0:
          digit = 0;
          break;
        case ConsoleKey.D1:
        case ConsoleKey.NumPad1:
          digit = 1;
          break;
        case ConsoleKey.D2:
        case ConsoleKey.NumPad2:
          digit = 2;
          break;
        case ConsoleKey.D3:
        case ConsoleKey.NumPad3:
          digit = 3;
          break;
        case ConsoleKey.D4:
        case ConsoleKey.NumPad4:
          digit = 4;
          break;
        case ConsoleKey.D5:
        case ConsoleKey.NumPad5:
          digit = 5;
          break;
        case ConsoleKey.D6:
        case ConsoleKey.NumPad6:
          digit = 6;
          break;
        case ConsoleKey.D7:
        case ConsoleKey.NumPad7:
          digit = 7;
          break;
        case ConsoleKey.D8:
        case ConsoleKey.NumPad8:
          digit = 8;
          break;
        case ConsoleKey.D9:
        case ConsoleKey.NumPad9:
          digit = 9;
          break;
        default:
          break;
      }

      if (digit is int cash)
      {
        vendingLogic.Input(new VendingState.Input.CashReceived(cash, ticks));
      }
    }

    return 0;
  }

  private static void ShowState(VendingLogic machine)
  {
    Console.WriteLine("");
    Console.WriteLine(" -- Vending Machine State --");
    Console.WriteLine($"   :: {machine.State?.GetType().Name ?? "<null>"}");
    Console.WriteLine("");
    Console.WriteLine(" -- Vending Machine Data --");
    Console.WriteLine($"   :: {machine.Get<VendingData>()}");
  }

  private static void ShowOverview()
  {
    Console.Clear();

    Console.Write(
      PlaceBesideImage(
        ReplaceToReflectQuantities(IMAGE, Stock.Quantities),
        "           ** VENDING MACHINE **",
        "",
        "     A LogicBlocks State Machine Example",
        "",
        "There are 3 types of items in the vending machine: ",
        "",
        $" {Chars[ItemType.Juice]} = " +
        $"Juice ( {Prices[ItemType.Juice]:C} )",
        $" {Chars[ItemType.Water]} = Water " +
        $"( {Prices[ItemType.Water]:C} )",
        $" {Chars[ItemType.Candy]} = Candy " +
        $"( {Prices[ItemType.Candy]:C} )",
        "",
        "To interact, do one of the following:",
        "",
        " * Enter a selection: `j`, `w`, or `c`",
        " * Insert cash: `0` - `9`",
        " * Wait for the transaction to time out (if selection was made)",
        " * Wait for the vending to complete (if vending)"
      )
    );
    Console.WriteLine("");
    Console.WriteLine("");
    Console.WriteLine("Press `q` or `escape` to quit.");
  }

  private static void ShowOutputs(Queue<object> outputs)
  {
    if (outputs.Count == 0)
    { return; }

    Console.WriteLine("");
    Console.WriteLine(
      $" -- Last {MAX_OUTPUTS} Outputs (Most Recent -> Oldest) --"
    );
    var i = 1;
    foreach (var output in outputs.Reverse())
    {
      Console.WriteLine($"   {i++} :: {output}");
    }

    var remaining = MAX_OUTPUTS - outputs.Count;

    while (remaining-- > 0)
    {
      Console.WriteLine($"   {i++} :: <empty>");
    }

    Console.WriteLine("");
  }

  private static void ShowTimeRemaining(int? seconds)
  {
    if (seconds is not { } remaining)
    { return; }

    Console.Write("\b\b\b");
    Console.Write(remaining.ToString("00") + " ");
  }

  private static string PlaceBesideImage(string image, params string[] text)
  {
    var imageLines = image.Split("\n").Select(line => line.Trim()).ToArray();
    var imageWidth = imageLines.Max(line => line.Length);
    var lines = new List<string>();
    var index = 0;
    var iterations = Math.Max(imageLines.Length, text.Length);
    var showText = true;
    var showImage = true;

    while (index < iterations)
    {
      if (index >= imageLines.Length)
      {
        showImage = false;
      }

      if (index >= text.Length)
      {
        showText = false;
      }

      if (showImage && showText)
      {
        lines.Add($"{imageLines[index]} {text[index]}");
      }
      else if (showImage)
      {
        lines.Add(imageLines[index]);
      }
      else if (showText)
      {
        lines.Add($"{new string(' ', imageWidth)} {text[index]}");
      }

      index++;
    }

    return string.Join("\n", lines);
  }

  private static string ReplaceToReflectQuantities(
    string image, IReadOnlyDictionary<ItemType, int> quantities
  )
  {
    var result = image;
    foreach (var (type, qty) in quantities)
    {
      result = ReplaceNTimes(
        result, Chars[type], EMPTY, Quantities[type] - qty
      );
    }

    return result;
  }

  private static string ReplaceNTimes(string text, char a, char b, int n)
  {
    if (n == 0)
    { return text; }

    var result = text;
    var regex = new Regex(Regex.Escape(a.ToString()));
    for (var i = 0; i < n; i++)
    {
      result = regex.Replace(result, b.ToString(), 1);
    }

    return result;
  }

  private static void AddOutputToBuffer(object output)
  {
    _lastFewOutputs.Enqueue(output);
    while (_lastFewOutputs.Count > MAX_OUTPUTS)
    {
      _lastFewOutputs.Dequeue();
    }
  }
}
