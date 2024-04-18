namespace Chickensoft.LogicBlocks.Tests.Types;

using System;
using System.Collections.Generic;
using Chickensoft.LogicBlocks;
using Chickensoft.LogicBlocks.Types;
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

    LogicBlockTypes.Initialize(_registry.Object);

    var ancestorDescendants = LogicBlockTypes.GetDescendants(typeof(Ancestor));

    ancestorDescendants.ShouldBe(new HashSet<Type> {
        typeof(Parent),
        typeof(Child),
      }, ignoreOrder: true
    );

    // Should be the exact same object reference since a repeated lookup is
    // cached.
    LogicBlockTypes.GetDescendants(typeof(Ancestor))
      .ShouldBeSameAs(ancestorDescendants);

    LogicBlockTypes.GetDescendants(typeof(Parent))
      .ShouldBe(new HashSet<Type> {
        typeof(Child),
      }, ignoreOrder: true
    );

    LogicBlockTypes.GetDescendants(typeof(Child)).ShouldBeEmpty();

    LogicBlockTypes.GetDescendants(typeof(AncestorSibling))
      .ShouldBe(new HashSet<Type> {
        typeof(ParentCousin),
        typeof(ChildCousin),
      }, ignoreOrder: true
    );

    LogicBlockTypes.GetDescendants(typeof(ParentCousin))
      .ShouldBe(new HashSet<Type> {
        typeof(ChildCousin),
      }, ignoreOrder: true
    );

    LogicBlockTypes.GetDescendants(typeof(ChildCousin)).ShouldBeEmpty();
  }

  [Fact]
  public void ObjectWithoutBaseTypeIsNonIssue() {
    // When looking up a type that has no base type — i.e., typeof(object)) —
    // we want to make sure we don't crash.
    _registry
      .Setup(reg => reg.VisibleTypes)
      .Returns(new HashSet<Type> { typeof(object) });

    LogicBlockTypes.Reset();
    LogicBlockTypes.Initialize(_registry.Object);

    LogicBlockTypes.GetDescendants(typeof(object)).ShouldBeEmpty();
  }
}
