namespace Chickensoft.LogicBlocks.Generator.Tests.Types.TestCases;

[Mixin]
public interface IExampleMixin : IMixin<IExampleMixin> {
  void IMixin<IExampleMixin>.Handler() { }
}

[Mixin]
public interface IMyOtherMixin : IMixin<IMyOtherMixin> {
  void IMixin<IMyOtherMixin>.Handler() { }
}
