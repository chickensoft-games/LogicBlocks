namespace Chickensoft.Introspection.Tests;

using System;
using Shouldly;
using Xunit;

[Mixin]
public interface IMixin1 : IMixin<IMixin1> {
  void IMixin<IMixin1>.Handler() => IIntrospectiveTest.Called1 = true;
}

[Mixin]
public interface IMixin2 : IMixin<IMixin2> {
  void IMixin<IMixin2>.Handler() => IIntrospectiveTest.Called2 = true;
}


[Meta(typeof(IMixin1), typeof(IMixin2))]
public partial class MyTypeWithAMixin { }

public class IIntrospectiveTest {
  public static bool Called1 { get; set; }
  public static bool Called2 { get; set; }

  public IIntrospectiveTest() {
    Called1 = false;
    Called2 = false;
  }

  [Fact]
  public void MixinIsCalled() {
    IIntrospective myType = new MyTypeWithAMixin();
    myType.InvokeMixin(typeof(IMixin1));

    Called1.ShouldBeTrue();
  }

  [Fact]
  public void AllMixinsCalled() {
    IIntrospective myType = new MyTypeWithAMixin();
    myType.InvokeMixins();

    Called1.ShouldBeTrue();
    Called2.ShouldBeTrue();
  }

  [Fact]
  public void ThrowsOnMissingMixin() {
    IIntrospective myType = new MyTypeWithAMixin();
    Should.Throw<InvalidOperationException>(
      () => myType.InvokeMixin(typeof(IIntrospectiveTest))
    );
  }
}
