namespace Chickensoft.LogicBlocks.Tests.Fixtures;
#pragma warning disable CS1998

public partial class FakeLogicBlockAsync {
  public static class Input {
    public readonly record struct InputOne(int Value1, int Value2);
    public readonly record struct InputTwo(string Value1, string Value2);
    public readonly record struct InputThree(string Value1, string Value2);
    public readonly record struct InputError;
    public readonly record struct InputUnknown;
    public readonly record struct GetString;
    public readonly record struct NoNewState;
    public readonly record struct SelfInput(object Input);
    public readonly record struct InputCallback(
      Func<Task> Callback,
      Func<IContext, Task<State>> Next
    );
    public readonly record struct Custom(Func<IContext, State> Next);
  }

  public abstract record State(IContext Context) : StateLogic(Context),
    IGet<Input.InputOne>,
    IGet<Input.InputTwo>,
    IGet<Input.InputThree>,
    IGet<Input.InputError>,
    IGet<Input.NoNewState>,
    IGet<Input.InputCallback>,
    IGet<Input.GetString>,
    IGet<Input.SelfInput>,
    IGet<Input.Custom> {
    public async Task<State> On(Input.InputOne input) {
      Context.Output(new Output.OutputOne(1));
      return new StateA(Context, input.Value1, input.Value2);
    }

    public async Task<State> On(Input.InputTwo input) {
      Context.Output(new Output.OutputTwo("2"));
      return new StateB(Context, input.Value1, input.Value2);
    }

    public async Task<State> On(Input.InputThree input) => new StateD(
      Context, input.Value1, input.Value2
    );

    public async Task<State> On(Input.InputError input)
      => throw new InvalidOperationException();

    public async Task<State> On(Input.NoNewState input) {
      Context.Output(new Output.OutputOne(1));
      return this;
    }

    public async Task<State> On(Input.InputCallback input) {
      await input.Callback();
      return await input.Next(Context);
    }

    public async Task<State> On(Input.Custom input) => input.Next(Context);

    public async Task<State> On(Input.GetString input) => new StateC(
      Context, Context.Get<string>()
    );

    public async Task<State> On(Input.SelfInput input) {
      // Can't await input in an async logic block — would deadlock.
      Context.Input(input.Input);
      // Return our current state in the meantime.
      return this;
    }

    public record StateA(IContext Context, int Value1, int Value2) :
      State(Context);
    public record StateB(IContext Context, string Value1, string Value2) :
      State(Context);
    public record StateC(IContext Context, string Value) :
      State(Context);
    public record StateD(IContext Context, string Value1, string Value2) :
      State(Context);
    public record Custom : State {
      public Custom(IContext context, Action<IContext> setupCallback) :
        base(context) {
        setupCallback(context);
      }
    }

    public record OnEnterState : State {
      public OnEnterState(IContext context, Func<State?, Task> onEnter) :
        base(context) {
        OnEnter<OnEnterState>(onEnter);
      }
    }
  }

  public static class Output {
    public readonly record struct OutputOne(int Value);
    public readonly record struct OutputTwo(string Value);
  }
}

public partial class FakeLogicBlockAsync :
LogicBlockAsync<FakeLogicBlockAsync.State> {
  public Func<IContext, State>? InitialState { get; init; }

  public List<Exception> Exceptions { get; } = new();

  public override State GetInitialState(IContext context) =>
    InitialState?.Invoke(context) ?? new State.StateA(context, 1, 2);

  private readonly Action<Exception>? _onError;

  public FakeLogicBlockAsync(Action<Exception>? onError = null) {
    _onError = onError;
  }

#pragma warning disable RCS1259
  ~FakeLogicBlockAsync() { }
#pragma warning restore RCS1259

  protected override void HandleError(Exception e) {
    Exceptions.Add(e);
    _onError?.Invoke(e);
    base.HandleError(e);
  }
}

#pragma warning restore CS1998
