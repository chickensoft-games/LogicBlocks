namespace Chickensoft.Introspection.Generator.Tests.TestCases;

using Chickensoft.Introspection;

[Mixin]
public interface IExampleMixin : IMixin<IExampleMixin> {
  void IMixin<IExampleMixin>.Handler() { }
}

[Mixin]
public interface IMyOtherMixin : IMixin<IMyOtherMixin> {
  void IMixin<IMyOtherMixin>.Handler() { }
}
