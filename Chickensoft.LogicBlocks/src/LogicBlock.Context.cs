namespace Chickensoft.LogicBlocks;

using System;

public abstract partial class LogicBlock<TState>
{
  /// <summary>Logic block context provided to each logic block state.</summary>
  internal readonly struct DefaultContext : IContext
  {
    public LogicBlock<TState> Logic { get; }

    /// <summary>
    /// Creates a new logic block context for the given logic block.
    /// </summary>
    /// <param name="logic">Logic block.</param>
    public DefaultContext(
      LogicBlock<TState> logic
    )
    {
      Logic = logic;
    }

    /// <inheritdoc />
    public void Input<TInputType>(in TInputType input)
      where TInputType : struct => Logic.Input(input);

    /// <inheritdoc />
    public void Output<TOutputType>(in TOutputType output)
      where TOutputType : struct => Logic.OutputValue(output);

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : class =>
      Logic.Get<TDataType>();

    /// <inheritdoc />
    public void AddError(Exception e) => Logic.AddError(e);

    /// <inheritdoc />
    public override bool Equals(object? obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Logic);
  }

  internal class ContextAdapter : IContext, IContextAdapter
  {
    public IContext? Context { get; private set; }

    public void Adapt(IContext context) => Context = context;
    public void Clear() => Context = null;
    public Action<Exception>? OnError =>
      Context is DefaultContext defaultContext
        ? defaultContext.Logic.AddError : null;

    /// <inheritdoc />
    public void Input<TInputType>(in TInputType input)
      where TInputType : struct
    {
      if (Context is not IContext context)
      {
        throw new InvalidOperationException(
          "Cannot add input to a logic block with an uninitialized context."
        );
      }

      context.Input(input);
    }

    /// <inheritdoc />
    public void Output<TOutputType>(in TOutputType output)
    where TOutputType : struct
    {
      if (Context is not { } context)
      {
        throw new InvalidOperationException(
          "Cannot add output to a logic block with an uninitialized context."
        );
      }

      context.Output(in output);
    }

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : class =>
      Context is not IContext context
        ? throw new InvalidOperationException(
          "Cannot get value from a logic block with an uninitialized context."
        )
        : context.Get<TDataType>();

    /// <inheritdoc />
    public void AddError(Exception e)
    {
      if (Context is not IContext context)
      {
        throw new InvalidOperationException(
          "Cannot add error to a logic block with an uninitialized context."
        );
      }

      context.AddError(e);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Context);
  }
}
