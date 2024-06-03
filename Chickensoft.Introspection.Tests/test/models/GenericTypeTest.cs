namespace Chickensoft.Introspection.Tests.Models;

using System;
using Shouldly;
using Xunit;

public class GenericTypeTest {
  private readonly GenericType _type = new(
    OpenType: typeof(string),
    ClosedType: typeof(string),
    Arguments: [],
    GenericTypeGetter: receiver => receiver.Receive<string>(),
    GenericTypeGetter2: (_) => throw new NotImplementedException()
  );

  [Fact]
  public void InitializesGenericType() => _type.ShouldBeOfType<GenericType>();
}
