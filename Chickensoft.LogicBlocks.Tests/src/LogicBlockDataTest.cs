namespace Chickensoft.LogicBlocks.Tests;

using Collections;
using Fixtures;
using Shouldly;

public class LogicBlockDataTest
{
  private static LogicBlockData MakeData(
    Type? stateType = null,
    Blackboard? bb = null,
    History? history = null
  )
  {
    bb ??= new Blackboard();
    return new LogicBlockData(
      stateType ?? typeof(TestLogicBlockState), bb, history ?? new History()
    );
  }

  [Fact]
  public void EqualsSameReference()
  {
    var a = MakeData();

    a.Equals(a).ShouldBeTrue();
  }

  [Fact]
  public void EqualsNull()
  {
    var a = MakeData();

    a.Equals(null).ShouldBeFalse();
  }

  [Fact]
  public void EqualsNonLogicBlockData()
  {
    var a = MakeData();

    a.Equals("not data").ShouldBeFalse();
  }

  [Fact]
  public void EqualsDifferentStateType()
  {
    var a = MakeData(stateType: typeof(TestLogicBlockState));
    var b = MakeData(stateType: typeof(LightSwitchState.PoweredOn));

    a.Equals(b).ShouldBeFalse();
  }

  [Fact]
  public void EqualsDifferentBlackboardCount()
  {
    var bb1 = new Blackboard();
    var bb2 = new Blackboard();
    bb2.Set("extra");

    var a = MakeData(bb: bb1);
    var b = MakeData(bb: bb2);

    a.Equals(b).ShouldBeFalse();
  }

  [Fact]
  public void EqualsMissingBlackboardType()
  {
    var bb1 = new Blackboard();
    bb1.Set("hello");

    var bb2 = new Blackboard();
    bb2.Set(new List<int>());

    var a = MakeData(bb: bb1);
    var b = MakeData(bb: bb2);

    a.Equals(b).ShouldBeFalse();
  }

  [Fact]
  public void EqualsDifferentBlackboardValue()
  {
    var bb1 = new Blackboard();
    bb1.Set("hello");

    var bb2 = new Blackboard();
    bb2.Set("world");

    var a = MakeData(bb: bb1);
    var b = MakeData(bb: bb2);

    a.Equals(b).ShouldBeFalse();
  }

  [Fact]
  public void EqualsDifferentHistoryCount()
  {
    var h1 = new History();
    var h2 = new History([typeof(TestLogicBlockState)]);

    var a = MakeData(history: h1);
    var b = MakeData(history: h2);

    a.Equals(b).ShouldBeFalse();
  }

  [Fact]
  public void EqualsDifferentHistoryEntry()
  {
    var a = MakeData(history: new History([typeof(TestLogicBlockState)]));
    var b = MakeData(history: new History([typeof(LightSwitchState.PoweredOn)]));

    a.Equals(b).ShouldBeFalse();
  }

  [Fact]
  public void EqualsEquivalentData()
  {
    var bb1 = new Blackboard();
    bb1.Set("hello");
    var bb2 = new Blackboard();
    bb2.Set("hello");

    var a = MakeData(bb: bb1);
    var b = MakeData(bb: bb2);

    a.Equals(b).ShouldBeTrue();
  }

  [Fact]
  public void GetHashCodeReturnsInt()
  {
    var a = MakeData();

    a.GetHashCode().ShouldBeOfType<int>();
  }

  [Fact]
  public void GetHashCodeDiffersForDifferentData()
  {
    var bb1 = new Blackboard();
    bb1.Set("hello");
    var bb2 = new Blackboard();
    bb2.Set("world");

    var a = MakeData(bb: bb1);
    var b = MakeData(bb: bb2);

    a.GetHashCode().ShouldNotBe(b.GetHashCode());
  }

  [Fact]
  public void EqualsMatchingHistoryEntries()
  {
    var a = MakeData(history: new History([typeof(TestLogicBlockState)]));
    var b = MakeData(history: new History([typeof(TestLogicBlockState)]));

    a.Equals(b).ShouldBeTrue();
  }

  [Fact]
  public void IEquatableEqualsRedirectsToObjectEquals()
  {
    var a = MakeData();
    var b = MakeData();

#pragma warning disable CA1859
    IEquatable<LogicBlockData> equatable = a;
#pragma warning restore CA1859
    equatable.Equals(b).ShouldBeTrue();
  }

  [Fact]
  public void GetHashCodeReturnsCachedValueOnSecondCall()
  {
    var a = MakeData();

    var hash1 = a.GetHashCode();
    var hash2 = a.GetHashCode();

    hash1.ShouldBe(hash2);
  }

  [Fact]
  public void GetHashCodeIncludesHistoryEntries()
  {
    var h = new History([typeof(TestLogicBlockState)]);
    var a = MakeData(history: h);
    var b = MakeData();

    // Hash with history should differ from hash without history
    a.GetHashCode().ShouldNotBe(b.GetHashCode());
  }
}
