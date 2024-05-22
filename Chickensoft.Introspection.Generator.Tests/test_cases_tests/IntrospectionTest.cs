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

  private static readonly Dictionary<Type, ITypeMetadata> _visibleTypes = new() {
    [typeof(Ancestor)] = new Mock<ITypeMetadata>().Object,
    [typeof(Parent)] = new Mock<ITypeMetadata>().Object,
    [typeof(Child)] = new Mock<ITypeMetadata>().Object,
    [typeof(AncestorSibling)] = new Mock<ITypeMetadata>().Object,
    [typeof(ParentCousin)] = new Mock<ITypeMetadata>().Object,
    [typeof(ChildCousin)] = new Mock<ITypeMetadata>().Object,
  };
  private readonly Mock<ITypeRegistry> _registry;

  public LogicBlockTypeUtilsTest() {
    _registry = new Mock<ITypeRegistry>();
  }

  [Fact]
  public void GetDescendantSubtypes() {
    _registry.Setup(reg => reg.VisibleTypes).Returns(_visibleTypes);

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
      .Returns(new Dictionary<Type, ITypeMetadata> {
        [typeof(LogicBlockTypeUtilsTest)] = new Mock<ITypeMetadata>().Object,
      });

    Types.InternalGraph.Reset();
    Types.Graph.Register(_registry.Object);

    Types.Graph
    .GetDescendantSubtypes(typeof(LogicBlockTypeUtilsTest))
    .ShouldBeEmpty();
  }
}
