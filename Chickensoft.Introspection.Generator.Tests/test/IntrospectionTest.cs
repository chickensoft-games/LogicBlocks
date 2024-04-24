namespace Chickensoft.LogicBlocks.Tests.Types;

using System;
using System.Collections.Generic;
using Chickensoft.Introspection;
using Moq;
using Shouldly;
using Xunit;


public class LogicBlockTypeUtilsTest {
  public class Ancestor;
  public class Parent : Ancestor;
  public class Child : Parent;

  public class AncestorSibling;
  public class ParentCousin : AncestorSibling;
  public class ChildCousin : ParentCousin;

  private static readonly HashSet<Type> _visibleTypes = new() {
    typeof(Ancestor),
    typeof(Parent),
    typeof(Child),
    typeof(AncestorSibling),
    typeof(ParentCousin),
    typeof(ChildCousin),
  };

  private readonly Mock<ITypeRegistry> _registry;

  public LogicBlockTypeUtilsTest() {
    _registry = new Mock<ITypeRegistry>();
  }


  [Fact]
  public void GetsDescendants() {
    _registry.Setup(reg => reg.VisibleTypes).Returns(_visibleTypes);
    _registry.Setup(reg => reg.VisibleInstantiableTypes)
      .Returns(new Dictionary<Type, Func<object>>());
    _registry.Setup(reg => reg.Metatypes)
      .Returns(new Dictionary<Type, IMetatype>());

    Types.Register(_registry.Object);

    var ancestorDescendants = Types.GetDescendants(typeof(Ancestor));

    ancestorDescendants.ShouldBe(new HashSet<Type> {
        typeof(Parent),
        typeof(Child),
      }, ignoreOrder: true
    );

    // Should be the exact same object reference since a repeated lookup is
    // cached.
    Types.GetDescendants(typeof(Ancestor))
      .ShouldBeSameAs(ancestorDescendants);

    Types.GetDescendants(typeof(Parent))
      .ShouldBe(new HashSet<Type> {
        typeof(Child),
      }, ignoreOrder: true
    );

    Types.GetDescendants(typeof(Child)).ShouldBeEmpty();

    Types.GetDescendants(typeof(AncestorSibling))
      .ShouldBe(new HashSet<Type> {
        typeof(ParentCousin),
        typeof(ChildCousin),
      }, ignoreOrder: true
    );

    Types.GetDescendants(typeof(ParentCousin))
      .ShouldBe(new HashSet<Type> {
        typeof(ChildCousin),
      }, ignoreOrder: true
    );

    Types.GetDescendants(typeof(ChildCousin)).ShouldBeEmpty();
  }

  [Fact]
  public void ObjectWithoutBaseTypeIsNonIssue() {
    // When looking up a type that has no base type — i.e., typeof(object)) —
    // we want to make sure we don't crash.
    _registry
      .Setup(reg => reg.VisibleTypes)
      .Returns(new HashSet<Type> { typeof(object) });
    _registry.Setup(reg => reg.VisibleInstantiableTypes)
      .Returns(new Dictionary<Type, Func<object>>());
    _registry.Setup(reg => reg.Metatypes)
      .Returns(new Dictionary<Type, IMetatype>());

    Types.Reset();
    Types.Register(_registry.Object);

    Types.GetDescendants(typeof(object)).ShouldBeEmpty();
  }
}
