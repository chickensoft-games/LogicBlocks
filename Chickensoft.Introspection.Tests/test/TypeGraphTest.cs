namespace Chickensoft.Introspection.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.Introspection;
using Moq;
using Shouldly;
using Xunit;

public class TypeGraphAncestryTest {
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

  public TypeGraphAncestryTest() {
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
        [typeof(TypeGraphAncestryTest)] = new Mock<ITypeMetadata>().Object,
      });

    Types.InternalGraph.Reset();
    Types.Graph.Register(_registry.Object);

    Types.Graph
    .GetDescendantSubtypes(typeof(TypeGraphAncestryTest))
    .ShouldBeEmpty();
  }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class TagAttribute : Attribute {
  public string Name { get; }

  public TagAttribute(string name) {
    Name = name;
  }
}

public partial class TypeGraphMemberMetadataTest {
  [Meta, Tag("model")]
  public partial class Model {
    public string? Name { get; init; }
    [Tag("age")]
    public required int Age { get; init; }
  }

  [Meta, Tag("child")]
  public partial class ChildModel : Model {
    public string? ChildName { get; init; }
  }

  [Fact]
  public void ComputesPropertyMetadataForDerivedTypes() {
    var props = Types.Graph.GetProperties(typeof(ChildModel)).ToList();

    // Props are in alphabetical order for stable & predictable orderings.

    props[0].Name.ShouldBe("Age");
    props[0]
      .Attributes[typeof(TagAttribute)].Single()
      .ShouldBeOfType<TagAttribute>().Name.ShouldBe("age");

    props[1].Name.ShouldBe("ChildName");

    props[2].Name.ShouldBe("Name");
  }
}
