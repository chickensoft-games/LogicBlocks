namespace Chickensoft.LogicBlocks;

public abstract partial class Logic<
  TInput, TState, TOutput, THandler, TInputReturn, TUpdate
> {
  /// <summary>
  /// Logic block state interface. All states used with a logic block must
  /// implement this interface.
  /// </summary>
  public interface IStateLogic {
    /// <summary>Logic block context.</summary>
    Context Context { get; }
  }

  /// <summary>
  /// Logic block base state record. If you are using records for your logic
  /// block states, you may inherit from this record rather instead of
  /// implementing <see cref="IStateLogic"/> directly and storing a context
  /// in each state.
  /// </summary>
  public abstract record StateLogic : IStateLogic {
    /// <summary>Logic block context.</summary>
    public Context Context { get; }

    /// <summary>
    /// Creates a new instance of the logic block base state record.
    /// </summary>
    /// <param name="context">Logic block context.</param>
    public StateLogic(Context context) {
      Context = context;
    }
  }
}
