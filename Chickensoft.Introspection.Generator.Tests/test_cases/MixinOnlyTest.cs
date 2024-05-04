namespace Chickensoft.Introspection.Generator.Tests.TestCases;

[Mixin]
public interface ISomeMixin : IMixin<ISomeMixin> {
  void IMixin<ISomeMixin>.Handler() { }
}

[Meta(typeof(ISomeMixin))]
public partial class SomeType;
