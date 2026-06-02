namespace Chickensoft.LogicBlocks.Tests;

using System.Collections;
using Fixtures;
using LogicBlocks;
using Shouldly;

public class HistoryTest
{
  private sealed record A : LogicBlockState
  {
  }

  private sealed record B : LogicBlockState
  {
  }

  private sealed record C : LogicBlockState
  {
  }

  private sealed record D : LogicBlockState
  {
  }

  private readonly Type _a = typeof(A);
  private readonly Type _b = typeof(B);
  private readonly Type _c = typeof(C);
  private readonly Type _d = typeof(D);

  [Fact]
  public void InitializesAndStoresEntries()
  {
    var history = new History();

    history.Count.ShouldBe(0);
    history.IsEmpty.ShouldBeTrue();

    history.Push(_a);
    history.Count.ShouldBe(1);
    history.IsEmpty.ShouldBeFalse();
    history.Peek().ShouldBe(_a);

    history.Pop().ShouldBe(_a);
    history.Count.ShouldBe(0);
    history.IsEmpty.ShouldBeTrue();
    history.Peek().ShouldBeNull();
  }

  [Fact]
  public void ToArrayPreservesOrder()
  {
    var history = new History([_a, _b, _c]);
    history.Push(_d);

    var array = history.ToArray();
    array.Length.ShouldBe(4);
    array.ShouldBe([_a, _b, _c, _d]);

    var newHistory = new History(array);

    newHistory.Pop().ShouldBe(_d);
    newHistory.Pop().ShouldBe(_c);
    newHistory.Pop().ShouldBe(_b);
    newHistory.Pop().ShouldBe(_a);

    newHistory.IsEmpty.ShouldBeTrue();
  }

  [Fact]
  public void InitializeDropsOldestIfOverCapacity()
  {
    var history = new History([_a, _b, _c, _d], 3);

    history.ToArray().ShouldBe([_b, _c, _d]);
  }

  [Fact]
  public void PushAtMaxCapacityEvictsOldest()
  {
    var history = new History(maxCapacity: 2);

    history.Push(_a);
    history.Push(_b);
    history.Push(_c);

    history.Count.ShouldBe(2);
    history.ToArray().ShouldBe([_b, _c]);
  }

  [Fact]
  public void PopReturnsNullWhenEmpty()
  {
    var history = new History();

    history.Pop().ShouldBeNull();
  }

  [Fact]
  public void PeekReturnsNullWhenEmpty()
  {
    var history = new History();

    history.Peek().ShouldBeNull();
  }

  [Fact]
  public void ClearRemovesAllEntries()
  {
    var history = new History([_a, _b, _c]);

    history.Clear();

    history.Count.ShouldBe(0);
    history.IsEmpty.ShouldBeTrue();
  }

  [Fact]
  public void EnumerationYieldsItemsInOrder()
  {
    var history = new History([_a, _b, _c]);
    var items = new List<Type>();

    foreach (var item in history)
    {
      items.Add(item);
    }

    items.ShouldBe([_a, _b, _c]);
  }

  [Fact]
  public void NullMaxCapacityAllowsUnboundedGrowth()
  {
    var history = new History(maxCapacity: null);

    history.Push(_a);
    history.Push(_b);
    history.Push(_c);
    history.Push(_d);

    history.Count.ShouldBe(4);
  }

  [Fact]
  public void NullMaxHistoryOnLogicBlockCreatesUnboundedHistory()
  {
    using var logic = new TestLogicBlock(maxHistoryCapacity: null);

    logic.History.MaxCapacity.ShouldBeNull();

    // Push more than the default capacity (8) to prove it's unbounded
    for (var i = 0; i < 20; i++)
    {
      logic.History.Push(typeof(A));
    }

    logic.History.Count.ShouldBe(20);
  }

  [Fact]
  public void DefaultLogicBlockHistoryCapacityIsEight()
  {
    using var logic = new TestLogicBlock();

    logic.History.MaxCapacity.ShouldBe(LogicBlock.MAX_HISTORY_DEFAULT);
  }

  [Fact]
  public void NonGenericEnumeratorWorks()
  {
    var history = new History([_a, _b]);
    IEnumerable enumerable = history;
    var items = new List<object>();

    foreach (var item in enumerable)
    {
      items.Add(item);
    }

    items.Count.ShouldBe(2);
  }
}
