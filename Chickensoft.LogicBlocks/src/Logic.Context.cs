namespace Chickensoft.LogicBlocks;

using System;

public abstract partial class Logic<TState, THandler, TInputReturn, TUpdate> {
  /// <summary>Logic block context provided to each logic block state.</summary>
  internal readonly struct DefaultContext : IContext {
    public Logic<TState, THandler, TInputReturn, TUpdate> Logic { get; }

    /// <summary>
    /// Creates a new logic block context for the given logic block.
    /// </summary>
    /// <param name="logic">Logic block.</param>
    public DefaultContext(
      Logic<TState, THandler, TInputReturn, TUpdate> logic
    ) {
      Logic = logic;
    }

    /// <inheritdoc />
    public void Input<TInputType>(TInputType input)
      where TInputType : notnull => Logic.Input(input);

    /// <inheritdoc />
    public void Output(in object output) => Logic.OutputValue(in output);

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : notnull =>
      Logic.Get<TDataType>();

    /// <inheritdoc />
    public void AddError(Exception e) => Logic.AddError(e);

    /// <inheritdoc />
    public override bool Equals(object obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Logic);
  }

  internal class ContextAdapter : IContext {
    public IContext? Context { get; private set; }

    public void Adapt(IContext context) => Context = context;
    public void Clear() => Context = null;

    /// <inheritdoc />
    public void Input<TInputType>(TInputType input)
      where TInputType : notnull {
      if (Context is not IContext context) {
        throw new InvalidOperationException(
          "Cannot add input to a logic block with an uninitialized context."
        );
      }

      context.Input(input);
    }

    /// <inheritdoc />
    public void Output(in object output) {
      if (Context is not IContext context) {
        throw new InvalidOperationException(
          "Cannot add output to a logic block with an uninitialized context."
        );
      }

      context.Output(in output);
    }

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : notnull {
      if (Context is not IContext context) {
        throw new InvalidOperationException(
          "Cannot get value from a logic block with an uninitialized context."
        );
      }

      return context.Get<TDataType>();
    }

    /// <inheritdoc />
    public void AddError(Exception e) {
      if (Context is not IContext context) {
        throw new InvalidOperationException(
          "Cannot add error to a logic block with an uninitialized context."
        );
      }

      context.AddError(e);
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Context);
  }
}
