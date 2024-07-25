namespace Chickensoft.LogicBlocks.Tests;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Shouldly;
using Xunit;

public class TestListener<TState> : LogicBlockListener<TState>
where TState : StateLogic<TState> {
  public TestListener(LogicBlock<TState> logicBlock) : base(logicBlock) { }

  public event Action<object>? OnInput;
  public event Action<TState>? OnState;
  public event Action<object>? OnOutput;
  public event Action<Exception>? OnError;

  protected override void ReceiveInput<TInputType>(in TInputType input) =>
    OnInput?.Invoke(input);

  protected override void ReceiveState(TState state) =>
    OnState?.Invoke(state);

  protected override void ReceiveOutput<TOutputType>(in TOutputType output) =>
    OnOutput?.Invoke(output);

  protected override void ReceiveException(Exception e) =>
    OnError?.Invoke(e);
}

// Don't run in parallel with other LogicBlock tests.
// Global introspection state is shared.
[Collection("LogicBlock")]
public partial class LogicBlockTest {
  [Meta, Id("test_obj")]
  public partial class TestObject;

  public static TestListener<TStateType> Listen<TStateType>(
    LogicBlock<TStateType> logicBlock
  ) where TStateType : StateLogic<TStateType> => new(logicBlock);

  [Fact]
  public void Initializes() {
    var block = new FakeLogicBlock();
    block.Value.ShouldBe(block.GetInitialState().State);
  }

  [Fact]
  public void GetsAndSetsBlackboardData() {
    var block = new EmptyLogicBlock();
    var context = new FakeContext();
    var obj = new TestObject();
    block.Has<string>().ShouldBeFalse();
    block.HasObject(typeof(string)).ShouldBeFalse();
    block.Set("data");
    block.Has<string>().ShouldBeTrue();
    block.HasObject(typeof(string)).ShouldBeTrue();
    block.Get<string>().ShouldBe("data");
    block.GetObject(typeof(string)).ShouldBe("data");
    block.Overwrite("string");
    block.Get<string>().ShouldBe("string");
    block.OverwriteObject(typeof(string), "overwritten");
    block.GetObject(typeof(string)).ShouldBe("overwritten");
    block.SetObject(typeof(int), 5);
    block.GetObject(typeof(int)).ShouldBe(5);
    block.SaveObject(typeof(TestObject), () => obj, null);

    block.Types.ShouldContain(typeof(string));
    block.Types.ShouldContain(typeof(int));
    block.SavedTypes.ShouldBe(
      [typeof(EmptyLogicBlock.State), typeof(TestObject)], ignoreOrder: true
    );
    block.TypesToSave.ShouldBe([typeof(TestObject)]);

    // Can't change values once set.
    Should.Throw<DuplicateNameException>(() => block.Set("other"));
    Should.Throw<KeyNotFoundException>(() => block.Get<string[]>());
    Should.Throw<KeyNotFoundException>(() => block.GetObject(typeof(string[])));
  }

  [Fact]
  public void InvokesInputEvent() {
    var block = new FakeLogicBlock();
    using var listener = Listen(block);

    var called = 0;
    var input = new FakeLogicBlock.Input.InputOne(2, 3);

    void handler(object input) {
      input.ShouldBe(input);
      called++;
    }

    listener.OnInput += handler;

    block.IsStarted.ShouldBeFalse();
    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    block.IsStarted.ShouldBeTrue();
    called.ShouldBe(1);

    listener.OnInput -= handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);
  }

  [Fact]
  public void InvokesOutputEvent() {
    var block = new FakeLogicBlock();
    using var listener = Listen(block);

    var called = 0;
    var output = new FakeLogicBlock.Output.OutputOne(2);

    void handler(object output) {
      output.ShouldBe(output);
      called++;
    }

    listener.OnOutput += handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);

    listener.OnOutput -= handler;

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(1);
  }

  [Fact]
  public void InvokesStateEvent() {
    var block = new FakeLogicBlock();
    using var listener = Listen(block);

    var called = 0;
    var state = new FakeLogicBlock.State.StateA() { Value1 = 2, Value2 = 3 };

    void handler(FakeLogicBlock.State state) {
      state.ShouldBe(state);
      called++;
    }

    listener.OnState += handler;

    called.ShouldBe(0);

    block.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    called.ShouldBe(2); // Start state -> second state = 2 states visited

    listener.OnState -= handler;
  }

  [Fact]
  public void InvokesErrorEvent() {
    var block = new FakeLogicBlock();
    using var listener = Listen(block);

    var called = 0;

    void handler(Exception e) => called++;

    listener.OnError += handler;

    block.Input(new FakeLogicBlock.Input.InputError());

    called.ShouldBe(1);

    listener.OnError -= handler;
  }

  [Fact]
  public void ThrowingFromHandleErrorStopsExecution() {
    var block = new FakeLogicBlock((e) => throw e);

    Should.Throw<InvalidOperationException>(
      () => block.Input(new FakeLogicBlock.Input.InputError())
    );
  }

  [Fact]
  public void DoesNothingOnUnhandledInput() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.DefaultContext(block);
    block.Input(new FakeLogicBlock.Input.InputUnknown());
    block.Value.ShouldBe(block.GetInitialState().State);
  }

  [Fact]
  public void CallsEnterAndExitOnStatesInProperOrder() {
    var logic = new TestMachine();
    using var listener = Listen(logic);
    var context = new TestMachine.DefaultContext(logic);

    var outputs = new List<object>();

    void onOutput(object output) => outputs.Add(output);

    listener.OnOutput += onOutput;

    logic.Value.ShouldBeOfType<TestMachine.State.Deactivated>();
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Blooped)
    );
    logic.Input(
      new TestMachine.Input.Deactivate()
    );
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Bopped)
    );
    // Repeating previous state should do nothing.
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Bopped)
    );
    logic.Input(
      new TestMachine.Input.Activate(SecondaryState.Blooped)
    );
    logic.Input(
      new TestMachine.Input.Deactivate()
    );

    listener.OnOutput -= onOutput;

    outputs.ShouldBe(new object[] {
      new TestMachine.Output.Deactivated(),
      new TestMachine.Output.DeactivatedCleanUp(),
      new TestMachine.Output.Activated(),
      new TestMachine.Output.Blooped(),
      new TestMachine.Output.BloopedCleanUp(),
      new TestMachine.Output.ActivatedCleanUp(),
      new TestMachine.Output.Deactivated(),
      new TestMachine.Output.DeactivatedCleanUp(),
      new TestMachine.Output.Activated(),
      new TestMachine.Output.Bopped(),
      new TestMachine.Output.BoppedCleanUp(),
      new TestMachine.Output.Blooped(),
      new TestMachine.Output.BloopedCleanUp(),
      new TestMachine.Output.ActivatedCleanUp(),
      new TestMachine.Output.Deactivated(),
    });
  }

  [Fact]
  public void ReturnsCurrentValueIfProcessingInputs() {
    var block = new FakeLogicBlock();
    var context = new FakeLogicBlock.DefaultContext(block);
    var called = false;
    var value = block.Input(new FakeLogicBlock.Input.InputCallback(
      () => {
        // This gets run from the input handler of InputCallback.
        var value = block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));
        value.ShouldBe(block.GetInitialState().State);
        called = true;
      },
      (context) => new FakeLogicBlock.Transition(
        new FakeLogicBlock.State.StateA() { Value1 = 2, Value2 = 3 }
      )
    ));
    called.ShouldBeTrue();
  }

  [Fact]
  public void InvokesErrorEventFromUpdateHandler() {
    var block = new FakeLogicBlock();
    using var listener = Listen(block);

    var called = 0;

    void handler(Exception e) => called++;

    listener.OnError += handler;

    block.Input(new FakeLogicBlock.Input.Custom(
      (context) => new FakeLogicBlock.Transition(
        new FakeLogicBlock.State.OnEnterState() {
          Callback = (previous) =>
            throw new InvalidOperationException("Error from OnEnter")
        }
      )
    ));

    called.ShouldBe(1);

    listener.OnError -= handler;
  }

  [Fact]
  public void InvokesErrorEventFromUpdateHandlerManually() {
    var state = new FakeLogicBlock.State.OnEnterState() {
      Callback =
        (previous) => throw new InvalidOperationException("Error from OnEnter")
    };

    Should.Throw<InvalidOperationException>(() => state.Enter());
  }

  [Fact]
  public void StateCanAddInputUsingContext() {
    var logic = new FakeLogicBlock();
    var input = new FakeLogicBlock.Input.InputOne(5, 6);

    logic.Input(new FakeLogicBlock.Input.SelfInput(input));

    logic.Value.ShouldBeOfType<FakeLogicBlock.State.StateA>();
  }

  [Fact]
  public void StartsManuallyAndIgnoresStartWhenProcessing() {
    var enterCalled = false;
    var context = new FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () =>
        new FakeLogicBlock.Transition(new FakeLogicBlock.State.OnEnterState() {
          Callback = (previous) => enterCalled = true
        })
    };

    // LogicBlocks shouldn't call entrance handlers for the initial state.
    enterCalled.ShouldBeFalse();

    block.Value.Enter();

    enterCalled.ShouldBeTrue();
  }

  [Fact]
  public void StartEntersState() {
    var enterCalled = false;
    var context = new FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () =>
        new FakeLogicBlock.Transition(new FakeLogicBlock.State.OnEnterState() {
          Callback = (previous) => enterCalled = true
        })
    };

    enterCalled.ShouldBeFalse();

    block.Start();
    block.Start(); // Should do nothing.

    enterCalled.ShouldBeTrue();
  }

  [Fact]
  public void StartDoesNothingIfProcessing() {
    var context = new FakeContext();
    var logic = new FakeLogicBlock();
    logic.InitialState = () => new FakeLogicBlock.Transition(
      new FakeLogicBlock.State.OnEnterState() {
        Callback = (previous) => logic.Start()
      }
    );

    logic.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    logic.Value.ShouldBeOfType<FakeLogicBlock.State.StateA>();
  }

  [Fact]
  public void StopExitsState() {
    var exitCalled = false;
    var context = new FakeContext();
    var block = new FakeLogicBlock() {
      InitialState = () => new FakeLogicBlock.Transition(
        new FakeLogicBlock.State.OnExitState() {
          Callback = (previous) => exitCalled = true
        }
      )
    };

    exitCalled.ShouldBeFalse();

    block.Start();
    block.Stop();
    block.Stop(); // Should do nothing.

    exitCalled.ShouldBeTrue();
  }

  [Fact]
  public void StopDoesNothingIfProcessing() {
    var context = new FakeContext();
    var logic = new FakeLogicBlock();
    logic.InitialState = () => new FakeLogicBlock.Transition(
      new FakeLogicBlock.State.OnExitState() {
        Callback = (previous) => logic.Stop()
      }
    );

    logic.Input(new FakeLogicBlock.Input.InputOne(2, 3));
    logic.IsStarted.ShouldBeTrue();
  }

  [Fact]
  public void CreatesFakeContext() {
    var inputs = new List<int> { 1, 2, 3 };
    var outputs = new List<int> { 1, 2 };
    var errors = new List<Exception> {
      new InvalidOperationException(),
      new InvalidCastException()
    };

    var context = new FakeContext();

    context.Set(new InvalidOperationException());

    inputs.ForEach((i) => context.Input(i));
    outputs.ForEach((o) => context.Output(o));
    errors.ForEach((e) => context.AddError(e));

    context.Inputs.Cast<int>().ShouldBe(inputs);
    context.Outputs.ShouldBe(outputs.Select(static t => t as object));
    context.Errors.ShouldBe(errors);

    context.Set("c");
    context.Get<string>().ShouldBe("c");
    context.Get<InvalidOperationException>().ShouldNotBeNull();

    Should.Throw<InvalidOperationException>(
      () => context.Get<IndexOutOfRangeException>()
    );

    context.Reset();

    context.Inputs.ShouldBeEmpty();
    context.Outputs.ShouldBeEmpty();
    context.Errors.ShouldBeEmpty();

    Should.Throw<InvalidOperationException>(() => context.Get<string>());
  }

  [Fact]
  public void FirstInputStartsLogicBlockIfNeeded() {
    var logic = new InputOnInitialState();
    var inputs = new List<object>();

    using var listener = Listen(logic);
    void handler(object input) => inputs.Add(input);
    listener.OnInput += handler;

    logic.Input(new InputOnInitialState.Input.Start());

    listener.OnInput -= handler;

    inputs.ShouldBe(new object[] {
      new InputOnInitialState.Input.Start(),
      new InputOnInitialState.Input.Initialize()
    });
  }

  [Fact]
  public void ForceResetThrowsIfProcessing() {
    var logic = new FakeLogicBlock();
    logic.InitialState = () => new FakeLogicBlock.Transition(
      new FakeLogicBlock.State.OnEnterState() {
        Callback = (_) => logic.ForceReset(
          new FakeLogicBlock.State.StateC() { Value = "value" })
      }
    );

    Should.NotThrow(() => logic.Start());
  }

  [Fact]
  public void ForceResetChangesStateAndProcessesInputs() {
    var logic = new FakeLogicBlock();

    var state = logic.ForceReset(
      new FakeLogicBlock.State.OnEnterState() {
        Callback =
          (_) => logic.Input(new FakeLogicBlock.Input.InputTwo("b", "c"))
      }
    );

    state.ShouldBeOfType<FakeLogicBlock.State.StateB>();
  }

  [Fact]
  public void AddsErrorToItself() {
    var e = new InvalidOperationException();
    var state = new FakeLogicBlock.State.AddErrorOnEnterState() { E = e };

    var context = state.CreateFakeContext();
    state.Enter();

    context.Errors.ShouldBe([e]);
  }

  [Fact]
  public void DefaultContextAddsError() {
    var logic = new FakeLogicBlock();

    Should.NotThrow(
      () => logic.Context.AddError(new InvalidOperationException())
    );
  }

  [Fact]
  public void RestoreStateRestoresState() {
    var logic = new FakeLogicBlock();
    var state = new FakeLogicBlock.State.StateC() { Value = "c" };

    logic.RestoreState(state);

    logic.Value.ShouldBe(state);
  }

  [Fact]
  public void RestoreStateThrowsIfStateAlreadyExists() {
    var logic = new FakeLogicBlock();
    var state = new FakeLogicBlock.State.StateC() { Value = "c" };

    logic.Start();

    Should.Throw<LogicBlockException>(() => logic.RestoreState(state));
  }

  public class LogicBlockEquality {
    private sealed record TestValue(int Value);

    [Fact]
    public void NotEqualToNonLogicBlock() {
      var logic = new FakeLogicBlock();
      var obj = new object();

      logic.Equals(obj).ShouldBeFalse();
    }

    [Fact]
    public void NotEqualToDifferentLogicBlock() {
      var logic = new FakeLogicBlock();
      var other = new MyLogicBlock();

      logic.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void NotEqualIfBlackboardsAreDifferent() {
      var logic = new FakeLogicBlock();
      var other = new FakeLogicBlock();

      logic.Set("data");
      other.Set("other");

      logic.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void NotEqualIfStatesAreDifferent() {
      var logic = new FakeLogicBlock();
      var other = new FakeLogicBlock();

      logic.Start();
      other.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

      logic.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void NotEqualIfBlackboardsAreDifferentLengths() {
      var logic = new FakeLogicBlock();
      var other = new FakeLogicBlock();

      logic.Set("data");
      other.Set("data");
      other.Set(new object());

      logic.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void NotEqualIfBlackboardTypesAreDifferent() {
      var logic = new FakeLogicBlock();
      var other = new FakeLogicBlock();

      logic.Set("data");
      other.Set(new object());

      logic.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void EqualIfBlackboardValuesAreEquivalent() {
      var logic = new FakeLogicBlock();
      var other = new FakeLogicBlock();

      logic.Set(new TestValue(1));
      logic.Set("hello");

      other.Set("hello");
      other.Set(new TestValue(1));

      logic.Equals(other).ShouldBeTrue();
    }

    [Fact]
    public void HashCodesAreDifferentForEquivalentLogicBlocks() {
      var logic = new FakeLogicBlock();
      var other = new FakeLogicBlock();

      logic.GetHashCode().ShouldNotBe(other.GetHashCode());
    }

    [Fact]
    public void RestoreFromThrowsIfOtherIsNotStarted() {
      var logic = new MyLogicBlock();
      var other = new MyLogicBlock();

      // Other is not yet started, so nothing to restore.
      Should.Throw<LogicBlockException>(() => logic.RestoreFrom(other));
    }

    [Fact]
    public void RestoreFromCopiesStateAndBlackboard() {
      var logic = new FakeLogicBlock();

      var data = "data";

      logic.Set(data);
      logic.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

      var other = new FakeLogicBlock();

      other.RestoreFrom(logic);

      other.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();
      other.Get<string>().ShouldBe(data);
    }
  }
}
