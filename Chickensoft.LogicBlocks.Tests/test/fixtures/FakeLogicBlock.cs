namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

[StateDiagram(typeof(State))]
public partial class FakeLogicBlock {
  public static class Input {
    public readonly record struct InputOne(int Value1, int Value2);
    public readonly record struct InputTwo(string Value1, string Value2);
    public readonly record struct InputThree(string Value1, string Value2);
    public readonly record struct InputError;
    public readonly record struct InputUnknown;
    public readonly record struct GetString;
    public readonly record struct NoNewState;
    public readonly record struct SelfInput(InputOne Input);
    public readonly record struct InputCallback(
      Action Callback,
      Func<IContext, State> Next
    );
    public readonly record struct Custom(Func<IContext, State> Next);
  }

  public abstract record State : StateLogic<State>,
    IGet<Input.InputOne>,
    IGet<Input.InputTwo>,
    IGet<Input.InputThree>,
    IGet<Input.InputError>,
    IGet<Input.NoNewState>,
    IGet<Input.InputCallback>,
    IGet<Input.GetString>,
    IGet<Input.SelfInput>,
    IGet<Input.Custom> {
    public State On(in Input.InputOne input) {
      Context.Output(new Output.OutputOne(1));
      return new StateA(input.Value1, input.Value2);
    }

    public State On(in Input.InputTwo input) {
      Context.Output(new Output.OutputTwo("2"));
      return new StateB(input.Value1, input.Value2);
    }

    public State On(in Input.InputThree input) => new StateD(
      input.Value1, input.Value2
    );

    public State On(in Input.InputError input)
      => throw new InvalidOperationException();

    public State On(in Input.NoNewState input) {
      Context.Output(new Output.OutputOne(1));
      return this;
    }

    public State On(in Input.InputCallback input) {
      input.Callback();
      return input.Next(Context);
    }

    public State On(in Input.Custom input) => input.Next(Context);

    public State On(in Input.GetString input) => new StateC(
      Context.Get<string>()
    );

    public State On(in Input.SelfInput input) {
      Context.Input(input.Input);
      return this;
    }

    public record StateA(int Value1, int Value2) : State;
    public record StateB(string Value1, string Value2) : State;
    public record StateC(string Value) : State;
    public record StateD(string Value1, string Value2) : State;

    public record NothingState : State;

    public record Custom : State {
      public Custom(IContext context, Action<IContext> setupCallback) {
        setupCallback(context);
      }
    }

    public record OnEnterState : State {
      public OnEnterState(IContext context, Action<State?> onEnter) {
        this.OnEnter(onEnter);
      }
    }

    public record OnExitState : State {
      public OnExitState(IContext context, Action<State?> onExit) {
        this.OnExit(onExit);
      }
    }
  }

  public static class Output {
    public readonly record struct OutputOne(int Value);
    public readonly record struct OutputTwo(string Value);
  }
}

public partial class FakeLogicBlock : LogicBlock<FakeLogicBlock.State> {
  public Func<State>? InitialState { get; init; }

  public List<Exception> Exceptions { get; } = new();

  public void PublicSet<T>(T value) where T : class => Set(value);

  public void PublicOverwrite<T>(T value) where T : class =>
    Overwrite(value);

  public override State GetInitialState() =>
    InitialState?.Invoke() ?? new State.StateA(1, 2);

  private readonly Action<Exception>? _onError;

  public FakeLogicBlock(Action<Exception>? onError = null) {
    _onError = onError;
  }

  ~FakeLogicBlock() { }

  protected override void HandleError(Exception e) {
    Exceptions.Add(e);
    _onError?.Invoke(e);
    base.HandleError(e);
  }
}
