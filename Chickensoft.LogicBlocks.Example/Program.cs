#pragma warning disable IDE0010

namespace Chickensoft.LogicBlocks.Example;

using System.Text.RegularExpressions;

public static class Program {
  public const string IMAGE = """
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
 :::.________________.::<  >::
 ::|##################|:::::::
 ::|==================|:::::::
 :::::::::::::::::::::::::::::
""";

  public static readonly Dictionary<ItemType, int> Totals = new() {
    [ItemType.Juice] = 6,
    [ItemType.Water] = 6,
    [ItemType.Candy] = 6
  };

  public const char EMPTY = '_';

  public static readonly Dictionary<ItemType, char> Chars = new() {
    [ItemType.Juice] = 'J',
    [ItemType.Water] = 'W',
    [ItemType.Candy] = 'C'
  };

  public static readonly VendingMachineStock Stock = new(new(Totals));

  private static long _transactionStartedTime;
  private const long TRANSACTION_TIMEOUT = 5_000; // 5 seconds
  private static bool _isTransactionUnderway;

  private static bool _isVending;
  private static long _vendingStartedTime;
  private const long VENDING_TIME = 3_000; // 3 seconds to vend

  // Keep a buffer of the last 3 outputs to show them on the screen.
  private const int MAX_OUTPUTS = 3;
  private static readonly Queue<object> _lastFewOutputs =
    new(MAX_OUTPUTS);

  public static int Main(string[] args) {
    var machine = new VendingMachine(Stock);
    var shouldContinue = true;
    var lastState = machine.Value;
    // Outputs that need to be processed.
    var outputs = new Queue<object>();

    Console.CancelKeyPress += (_, _) => shouldContinue = false;
    machine.OnOutput += (output) => {
      AddOutputToBuffer(output);
      outputs.Enqueue(output);
    };

    void update() {
      while (outputs.Count > 0) {
        var output = outputs.Dequeue();
        ProcessOutput(output);
      }
      ShowOverview();
      ShowState(machine.Value);
      Console.WriteLine("");
      Console.Write("> ");
      ShowOutputs(_lastFewOutputs);
      lastState = machine.Value;
    }

    update();

    while (shouldContinue) {
      if (machine.Value != lastState || outputs.Count > 0) {
        update();
        lastState = machine.Value;
      }

      // Update timers
      if (_isVending) {
        var time = GetMs() - _vendingStartedTime;
        if (time > VENDING_TIME) {
          _isVending = false;
          machine.Input(new VendingMachine.Input.VendingCompleted());
        }
        else {
          PrintCountdown(time, VENDING_TIME);
        }
      }

      if (_isTransactionUnderway) {
        var time = GetMs() - _transactionStartedTime;
        if (time > TRANSACTION_TIMEOUT) {
          _isTransactionUnderway = false;
          machine.Input(new VendingMachine.Input.TransactionTimedOut());
        }
        else {
          PrintCountdown(time, TRANSACTION_TIMEOUT);
        }
      }

      // Don't do anything else if there's nothing to do or we're vending.
      while (_isVending && Console.KeyAvailable) {
        // Flush keys
        Console.ReadKey(false);
      }

      if (_isVending || !Console.KeyAvailable) {
        continue;
      }

      var key = Console.ReadKey(false);

      // Move cursor back one
      Console.Write("\b");

      int? digit = null;

      switch (key.Key) {
        case ConsoleKey.Escape:
          Console.Write("q "); // Fixes formatting.
          goto case ConsoleKey.Q;
        case ConsoleKey.Q:
          Console.WriteLine("Done. Thanks for using the vending machine!");
          shouldContinue = false;
          break;
        case ConsoleKey.J:
          machine.Input(
            new VendingMachine.Input.SelectionEntered(ItemType.Juice)
          );
          break;
        case ConsoleKey.W:
          machine.Input(
            new VendingMachine.Input.SelectionEntered(ItemType.Water)
          );
          break;
        case ConsoleKey.C:
          machine.Input(
            new VendingMachine.Input.SelectionEntered(ItemType.Candy)
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

      if (digit is int cash) {
        machine.Input(new VendingMachine.Input.PaymentReceived(cash));
      }
    }

    return 0;
  }

  private static void ShowState(VendingMachine.State state) {
    Console.WriteLine("");
    Console.WriteLine(" -- Vending Machine State --");
    Console.WriteLine($"   :: {state}");
    Console.WriteLine("");
  }

  private static void ShowOverview() {
    Console.Clear();

    Console.Write(
      PlaceBesideImage(
        ReplaceToReflectQuantities(IMAGE, Stock.Stock),
        "           ** VENDING MACHINE **",
        "",
        "     A LogicBlocks State Machine Example",
        "",
        "There are 3 types of items in the vending machine: ",
        "",
        $" {Chars[ItemType.Juice]} = " +
          $"Juice ( {VendingMachine.Prices[ItemType.Juice]:C} )",
        $" {Chars[ItemType.Water]} = Water " +
          $"( {VendingMachine.Prices[ItemType.Water]:C} )",
        $" {Chars[ItemType.Candy]} = Candy " +
          $"( {VendingMachine.Prices[ItemType.Candy]:C} )",
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

  private static void ShowOutputs(Queue<object> outputs) {
    if (outputs.Count == 0) { return; }
    Console.WriteLine(" -- Last 3 Outputs (Most Recent -> Oldest) --");
    var i = 1;
    foreach (var output in outputs.Reverse()) {
      Console.WriteLine($"   {i++} :: {output}");
    }
  }

  private static void ProcessOutput(object output) {
    if (output is VendingMachine.Output.BeginVending) {
      _vendingStartedTime = GetMs();
      _isTransactionUnderway = false;
      _isVending = true;
    }
    else if (
      output is
      VendingMachine.Output.TransactionStarted or
      VendingMachine.Output.RestartTransactionTimeOutTimer
    ) {
      _transactionStartedTime = GetMs();
      _isTransactionUnderway = true;
    }
    else if (output is VendingMachine.Output.ClearTransactionTimeOutTimer) {
      _isTransactionUnderway = false;
    }
  }

  private static long GetMs() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

  // Show time formatted as 00.0s
  private static void PrintCountdown(long timeMs, long durationMs) =>
    Console.Write(
      $"\r{(durationMs - timeMs) / 1000d:00}s".ToCharArray(), 0, 3
    );

  private static string PlaceBesideImage(string image, params string[] text) {
    var imageLines = image.Split("\n").Select(line => line.Trim()).ToArray();
    var imageWidth = imageLines.Max(line => line.Length);
    var lines = new List<string>();
    var index = 0;
    var iterations = Math.Max(imageLines.Length, text.Length);
    var showText = true;
    var showImage = true;

    while (index < iterations) {
      if (index >= imageLines.Length) {
        showImage = false;
      }
      if (index >= text.Length) {
        showText = false;
      }

      if (showImage && showText) {
        lines.Add($"{imageLines[index]} {text[index]}");
      }
      else if (showImage) {
        lines.Add(imageLines[index]);
      }
      else if (showText) {
        lines.Add($"{new string(' ', imageWidth)} {text[index]}");
      }

      index++;
    }

    return string.Join("\n", lines);
  }

  private static string ReplaceToReflectQuantities(
    string image, Dictionary<ItemType, int> quantities
  ) {
    var result = image;
    foreach ((var type, var qty) in quantities) {
      result = ReplaceNTimes(
        result, Chars[type], EMPTY, Totals[type] - qty
      );
    }
    return result;
  }

  private static string ReplaceNTimes(string text, char a, char b, int n) {
    if (n == 0) { return text; }
    var result = text;
    var regex = new Regex(Regex.Escape(a.ToString()));
    for (var i = 0; i < n; i++) {
      result = regex.Replace(result, b.ToString(), 1);
    }
    return result;
  }

  private static void AddOutputToBuffer(object output) {
    _lastFewOutputs.Enqueue(output);
    if (_lastFewOutputs.Count > MAX_OUTPUTS) {
      _lastFewOutputs.Dequeue();
    }
  }
}

#pragma warning restore IDE0010
