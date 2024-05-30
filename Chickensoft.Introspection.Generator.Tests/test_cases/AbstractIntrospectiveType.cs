namespace Chickensoft.Introspection.Generator.Tests.TestCases;

[Meta]
public abstract partial record AbstractType;

[Meta, Id("concrete_child_one")]
public partial record ConcreteChildOne : AbstractType;

[Meta, Id("concrete_child_two")]
public partial record ConcreteChildTwo : AbstractType;
