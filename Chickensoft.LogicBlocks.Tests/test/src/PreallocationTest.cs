namespace Chickensoft.LogicBlocks.Tests;

using System;
using System.Collections.Generic;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks.Tests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

// Don't run in parallel with other LogicBlock tests.
// Global introspection state is shared.
[Collection("LogicBlock")]
public class PreallocationTest : IDisposable {
  public Mock<ITypeGraph> Graph { get; set; } = new();
  public Mock<ILogicBlock<EmptyLogicBlock.State>> Logic { get; set; } = new();

  // Runs before each test
  public PreallocationTest() {
    EmptyLogicBlock.Graph = Graph.Object;
  }

  // Runs after each test
#pragma warning disable CA1816
  public void Dispose() =>
    EmptyLogicBlock.Graph = EmptyLogicBlock.DefaultGraph;
#pragma warning restore CA1816

  [Fact]
  public void DoesNothingIfLogicBlockIsNotIntrospective() {
    Graph
      .Setup(g => g.IsIntrospectiveType(Logic.Object.GetType()))
      .Returns(false);

    Should.NotThrow(() => EmptyLogicBlock.PreallocateStates(Logic.Object));
  }

  [Fact]
  public void DoesNothingIfAttributesIsEmpty() {
    Graph
      .Setup(g => g.IsIntrospectiveType(Logic.Object.GetType()))
      .Returns(true);

    var metatype = new Mock<IMetatype>();
    var attributes = new Dictionary<Type, Attribute[]>();

    metatype
      .Setup(m => m.Attributes)
      .Returns(attributes);

    Graph
      .Setup(g => g.GetMetatype(Logic.Object.GetType()))
      .Returns(metatype.Object);

    Should.NotThrow(() => EmptyLogicBlock.PreallocateStates(Logic.Object));
  }

  [Fact]
  public void DoesNothingIfFirstAttributeIsNotLogicBlockAttribute() {
    Graph
      .Setup(g => g.IsIntrospectiveType(Logic.Object.GetType()))
      .Returns(true);

    var metatype = new Mock<IMetatype>();

    // The introspection generator will never produce malformed
    // metadata like this, but we still need to test the code path for
    // null safety.
    var attributes = new Dictionary<Type, Attribute[]>() {
      [typeof(LogicBlockAttribute)] = [new ObsoleteAttribute()]
    };

    metatype
      .Setup(m => m.Attributes)
      .Returns(attributes);

    Graph
      .Setup(g => g.GetMetatype(Logic.Object.GetType()))
      .Returns(metatype.Object);

    Should.NotThrow(() => EmptyLogicBlock.PreallocateStates(Logic.Object));
  }

  [Fact]
  public void ThrowsWhenBaseStateOrSubstatesAreNotIntrospective() {
    Graph
      .Setup(g => g.IsIntrospectiveType(Logic.Object.GetType()))
      .Returns(true);

    var metatype = new Mock<IMetatype>();

    Graph
      .Setup(g => g.GetMetatype(Logic.Object.GetType()))
      .Returns(metatype.Object);

    // The introspection generator will never produce malformed
    // metadata like this, but we still need to test the code path for
    // null safety.
    var attributes = new Dictionary<Type, Attribute[]>() {
      [typeof(LogicBlockAttribute)] = [
        new LogicBlockAttribute(typeof(EmptyLogicBlock.State))
      ]
    };

    metatype
      .Setup(m => m.Attributes)
      .Returns(attributes);

    Graph
      .Setup(g => g.IsIntrospectiveType(typeof(EmptyLogicBlock.State)))
      .Returns(false);

    Graph.Setup(g => g.IsConcrete(typeof(EmptyLogicBlock.State)))
      .Returns(true);

    Logic.Setup(logic => logic.SaveObject(
      typeof(EmptyLogicBlock.State),
      It.IsAny<Func<object>>()
    ));

    Logic
      .Setup(logic => logic.GetObject(typeof(EmptyLogicBlock.State)))
      .Returns<EmptyLogicBlock.State>(default!);

    Graph.Setup(g => g.GetDescendantSubtypes(typeof(EmptyLogicBlock.State)))
      .Returns(new HashSet<Type>() { typeof(object) });

    Graph.Setup(g => g.IsIntrospectiveType(typeof(object)))
      .Returns(false);

    Graph.Setup(g => g.IsConcrete(typeof(object)))
      .Returns(false);

    Should.Throw<LogicBlockException>(
      () => EmptyLogicBlock.PreallocateStates(Logic.Object)
    );
  }

  [Fact]
  public void ThrowsWhenLogicBlockAlreadyHasStateTypeOnBlackboard() {
    Graph
      .Setup(g => g.IsIntrospectiveType(Logic.Object.GetType()))
      .Returns(true);

    var metatype = new Mock<IMetatype>();

    Graph
      .Setup(g => g.GetMetatype(Logic.Object.GetType()))
      .Returns(metatype.Object);

    // The introspection generator will never produce malformed
    // metadata like this, but we still need to test the code path for
    // null safety.
    var attributes = new Dictionary<Type, Attribute[]>() {
      [typeof(LogicBlockAttribute)] = [
        new LogicBlockAttribute(typeof(EmptyLogicBlock.State))
      ]
    };

    metatype
      .Setup(m => m.Attributes)
      .Returns(attributes);

    Graph
      .Setup(g => g.IsIntrospectiveType(typeof(EmptyLogicBlock.State)))
      .Returns(false);

    Graph.Setup(g => g.IsConcrete(typeof(EmptyLogicBlock.State)))
      .Returns(true);

    Logic.Setup(logic => logic.SaveObject(
      typeof(EmptyLogicBlock.State),
      It.IsAny<Func<object>>()
    ));

    Logic
      .Setup(logic => logic.GetObject(typeof(EmptyLogicBlock.State)))
      .Returns<EmptyLogicBlock.State>(default!);

    Graph.Setup(g => g.GetDescendantSubtypes(typeof(EmptyLogicBlock.State)))
      .Returns(new HashSet<Type>() { typeof(object) });

    Graph.Setup(g => g.IsIntrospectiveType(typeof(object)))
      .Returns(true);

    Graph.Setup(g => g.IsConcrete(typeof(object)))
      .Returns(true);

    Logic.Setup(logic => logic.HasObject(typeof(object)))
      .Returns(true);

    Should.Throw<LogicBlockException>(
      () => EmptyLogicBlock.PreallocateStates(Logic.Object)
    );
  }

  [Fact]
  public void AddsStatesToTheBlackboard() {
    Graph
      .Setup(g => g.IsIntrospectiveType(Logic.Object.GetType()))
      .Returns(true);

    var metatype = new Mock<IMetatype>();

    Graph
      .Setup(g => g.GetMetatype(Logic.Object.GetType()))
      .Returns(metatype.Object);

    // The introspection generator will never produce malformed
    // metadata like this, but we still need to test the code path for
    // null safety.
    var attributes = new Dictionary<Type, Attribute[]>() {
      [typeof(LogicBlockAttribute)] = [
        new LogicBlockAttribute(typeof(EmptyLogicBlock.State))
      ]
    };

    metatype
      .Setup(m => m.Attributes)
      .Returns(attributes);

    Graph
      .Setup(g => g.IsIntrospectiveType(typeof(EmptyLogicBlock.State)))
      .Returns(true);

    Graph.Setup(g => g.IsConcrete(typeof(EmptyLogicBlock.State)))
      .Returns(true);

    Logic.Setup(logic => logic.SaveObject(
      typeof(EmptyLogicBlock.State),
      It.IsAny<Func<object>>()
    ));

    Logic
      .Setup(logic => logic.GetObject(typeof(EmptyLogicBlock.State)))
      .Returns<EmptyLogicBlock.State>(default!);

    Graph.Setup(g => g.GetDescendantSubtypes(typeof(EmptyLogicBlock.State)))
      .Returns(new HashSet<Type>() { });

    Should.NotThrow(() => EmptyLogicBlock.PreallocateStates(Logic.Object));
  }
}
