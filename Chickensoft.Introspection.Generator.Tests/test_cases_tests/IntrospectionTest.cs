namespace Chickensoft.Introspection.Generator.Tests.Types;

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

  private static readonly Dictionary<Type, string> _visibleTypes = new() {
    [typeof(Ancestor)] = nameof(Ancestor),
    [typeof(Parent)] = nameof(Parent),
    [typeof(Child)] = nameof(Child),
    [typeof(AncestorSibling)] = nameof(AncestorSibling),
    [typeof(ParentCousin)] = nameof(ParentCousin),
    [typeof(ChildCousin)] = nameof(ChildCousin),
  };

  private readonly Mock<ITypeRegistry> _registry;

  public LogicBlockTypeUtilsTest() {
    _registry = new Mock<ITypeRegistry>();
  }

  [Fact]
  public void GetDescendantSubtypes() {
    _registry.Setup(reg => reg.VisibleTypes).Returns(_visibleTypes);
    _registry.Setup(reg => reg.ConcreteVisibleTypes)
      .Returns(new Dictionary<Type, Func<object>>());
    _registry.Setup(reg => reg.Metatypes)
      .Returns(new Dictionary<Type, IMetatype>());

    Types.Graph.Register(_registry.Object);

    var ancestorDescendants =
      Types.Graph.GetDescendantSubtypes(typeof(Ancestor));

    ancestorDescendants.ShouldBe(new HashSet<Type> {
        typeof(Parent),
        typeof(Child),
      }, ignoreOrder: true
    );

    // Should be the exact same object reference since a repeated lookup is
    // cached.
    Types.Graph.GetDescendantSubtypes(typeof(Ancestor))
      .ShouldBeSameAs(ancestorDescendants);

    Types.Graph.GetDescendantSubtypes(typeof(Parent))
      .ShouldBe(new HashSet<Type> {
        typeof(Child),
      }, ignoreOrder: true
    );

    Types.Graph.GetDescendantSubtypes(typeof(Child)).ShouldBeEmpty();

    Types.Graph.GetDescendantSubtypes(typeof(AncestorSibling))
      .ShouldBe(new HashSet<Type> {
        typeof(ParentCousin),
        typeof(ChildCousin),
      }, ignoreOrder: true
    );

    Types.Graph.GetDescendantSubtypes(typeof(ParentCousin))
      .ShouldBe(new HashSet<Type> {
        typeof(ChildCousin),
      }, ignoreOrder: true
    );

    Types.Graph.GetDescendantSubtypes(typeof(ChildCousin)).ShouldBeEmpty();
  }

  [Fact]
  public void ObjectWithoutBaseTypeIsNonIssue() {
    // When looking up a type that has no base type — i.e., typeof(object)) —
    // we want to make sure we don't crash.
    _registry
      .Setup(reg => reg.VisibleTypes)
      .Returns(new Dictionary<Type, string> {
        [typeof(LogicBlockTypeUtilsTest)] = nameof(LogicBlockTypeUtilsTest)
      });
    _registry.Setup(reg => reg.ConcreteVisibleTypes)
      .Returns(new Dictionary<Type, Func<object>>());
    _registry.Setup(reg => reg.Metatypes)
      .Returns(new Dictionary<Type, IMetatype>());

    Types.InternalGraph.Reset();
    Types.Graph.Register(_registry.Object);

    Types.Graph
    .GetDescendantSubtypes(typeof(LogicBlockTypeUtilsTest))
    .ShouldBeEmpty();
  }
}
