namespace Chickensoft.Introspection.Tests.Models;

using Moq;
using Shouldly;
using Xunit;

public class TypeMetadataTest {
  [Fact]
  public void InitializesTypeMetadata() {
    var metadata = new TypeMetadata("Name");

    metadata.ShouldBeAssignableTo<ITypeMetadata>();
  }

  [Fact]
  public void InitializesConcreteTypeMetadata() {
    var metadata = new ConcreteTypeMetadata(
      Name: "Name",
      GenericTypeGetter: (r) => r.Receive<string>(),
      () => new object()
    );

    metadata.ShouldBeAssignableTo<IConcreteTypeMetadata>();
    metadata.ShouldBeAssignableTo<IClosedTypeMetadata>();
    metadata.ShouldBeAssignableTo<ITypeMetadata>();
  }

  [Fact]
  public void InitializesAbstractIntrospectiveTypeMetadata() {
    var metatype = new Mock<IMetatype>();

    var metadata = new AbstractIntrospectiveTypeMetadata(
      Name: "Name",
      GenericTypeGetter: (r) => r.Receive<string>(),
      Metatype: metatype.Object
    );

    metadata.ShouldBeAssignableTo<IIntrospectiveTypeMetadata>();
    metadata.ShouldBeAssignableTo<IClosedTypeMetadata>();
    metadata.ShouldBeAssignableTo<ITypeMetadata>();
  }

  [Fact]
  public void InitializesIntrospectiveTypeMetadata() {
    var metatype = new Mock<IMetatype>();

    var metadata = new IntrospectiveTypeMetadata(
      Name: "Name",
      GenericTypeGetter: (r) => r.Receive<string>(),
      () => new object(),
      Metatype: metatype.Object,
      Version: 1
    );

    metadata.ShouldBeAssignableTo<IIntrospectiveTypeMetadata>();
    metadata.ShouldBeAssignableTo<IConcreteTypeMetadata>();
    metadata.ShouldBeAssignableTo<IClosedTypeMetadata>();
    metadata.ShouldBeAssignableTo<ITypeMetadata>();
  }

  [Fact]
  public void InitializesAbstractIdentifiableTypeMetadata() {
    var metatype = new Mock<IMetatype>();

    var metadata = new AbstractIdentifiableTypeMetadata(
      Name: "Name",
      GenericTypeGetter: (r) => r.Receive<string>(),
      Metatype: metatype.Object,
      Id: "name"
    );

    metadata.ShouldBeAssignableTo<IIdentifiableTypeMetadata>();
    metadata.ShouldBeAssignableTo<IIntrospectiveTypeMetadata>();
    metadata.ShouldBeAssignableTo<IClosedTypeMetadata>();
    metadata.ShouldBeAssignableTo<ITypeMetadata>();
  }

  [Fact]
  public void InitializesIdentifiableTypeMetadata() {
    var metatype = new Mock<IMetatype>();

    var metadata = new IdentifiableTypeMetadata(
      Name: "Name",
      GenericTypeGetter: (r) => r.Receive<string>(),
      () => new object(),
      Metatype: metatype.Object,
      Id: "name",
      Version: 2
    );

    metadata.ShouldBeAssignableTo<IIdentifiableTypeMetadata>();
    metadata.ShouldBeAssignableTo<IIntrospectiveTypeMetadata>();
    metadata.ShouldBeAssignableTo<IConcreteTypeMetadata>();
    metadata.ShouldBeAssignableTo<IClosedTypeMetadata>();
    metadata.ShouldBeAssignableTo<ITypeMetadata>();
  }

}
