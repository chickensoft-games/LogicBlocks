namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System.Collections.Generic;

#pragma warning disable CS1998

public partial class FakeLogicBlockAsync {
  public interface IInput {
    public record struct InputOne(int Value1, int Value2) : IInput;
    public record struct InputTwo(string Value1, string Value2)
      : IInput;
    public record struct InputError() : IInput;
    public record struct InputUnknown() : IInput;
    public record struct GetString() : IInput;
    public record struct NoNewState() : IInput;
    public record struct InputCallback(
      Action Callback,
      Func<Context, State> Next
    ) : IInput;
    public record struct Custom(Func<Context, State> Next) : IInput;
  }

  public abstract record State(Context Context) : StateLogic(Context),
    IGet<IInput.InputOne>,
    IGet<IInput.InputTwo>,
    IGet<IInput.InputError>,
    IGet<IInput.NoNewState>,
    IGet<IInput.InputCallback>,
    IGet<IInput.GetString>,
    IGet<IInput.Custom> {
    public async Task<State> On(IInput.InputOne input) {
      Context.Output(new IOutput.OutputOne(1));
      return new StateA(Context, input.Value1, input.Value2);
    }

    public async Task<State> On(IInput.InputTwo input) {
      Context.Output(new IOutput.OutputTwo("2"));
      return new StateB(Context, input.Value1, input.Value2);
    }

    public async Task<State> On(IInput.InputError input)
      => throw new InvalidOperationException();

    public async Task<State> On(IInput.NoNewState input) {
      Context.Output(new IOutput.OutputOne(1));
      return this;
    }

    public async Task<State> On(IInput.InputCallback input) {
      input.Callback();
      return input.Next(Context);
    }

    public async Task<State> On(IInput.Custom input) => input.Next(Context);

    public async Task<State> On(IInput.GetString input) => new StateC(
      Context, Context.Get<string>()
    );

    public record StateA(Context Context, int Value1, int Value2) :
      State(Context);
    public record StateB(Context Context, string Value1, string Value2) :
      State(Context);
    public record StateC(Context Context, string Value) :
      State(Context);
    public record Custom : State {
      public Custom(Context context, Action<Context> setupCallback) :
        base(context) {
        setupCallback(context);
      }
    }
  }

  public interface IOutput {
    public record struct OutputOne(int Value) : IOutput;
    public record struct OutputTwo(string Value) : IOutput;
  }
}

public partial class FakeLogicBlockAsync
  : LogicBlockAsync<
    FakeLogicBlockAsync.IInput, FakeLogicBlockAsync.State, FakeLogicBlockAsync.IOutput
  > {
  public Func<Context, State>? InitialState { get; init; }

  public List<Exception> Exceptions { get; } = new();

  public void PublicSet<T>(T value) where T : notnull => Set(value);

  public void PublicOnTransition<TStateTypeA, TStateTypeB>(
    Transition<TStateTypeA, TStateTypeB> transitionCallback
  ) where TStateTypeA : State where TStateTypeB : State =>
    OnTransition(transitionCallback);

  protected override void OnError(Exception e) {
    Exceptions.Add(e);
    base.OnError(e);
  }

  public override State GetInitialState(Context context) =>
    InitialState?.Invoke(context) ?? new State.StateA(context, 1, 2);

  ~FakeLogicBlockAsync() { }
}

#pragma warning restore CS1998
