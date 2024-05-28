namespace Chickensoft.Introspection.Generator.Tests.TestCases;

[Meta, Id("versioned_model")]
public abstract partial record VersionedModel;

[Meta, Version(1)]
public partial record VersionedModel1 : VersionedModel;

[Meta, Version(2)]
public partial record VersionedModel2 : VersionedModel;

[Meta, Version(3)]
public partial record VersionedModel3 : VersionedModel;

[Meta, Version(0)]
public partial record VersionedModel0 : VersionedModel;
