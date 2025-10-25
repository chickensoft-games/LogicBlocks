namespace Chickensoft.LogicBlocks.DiagramGenerator.Tests.Models;

using System.Collections.Generic;
using System.Collections.Immutable;
using Chickensoft.LogicBlocks.DiagramGenerator.Models;
using Shouldly;
using Xunit;

public class ModelsTest
{
  public class LogicBlockSubclassTest
  {
    [Fact]
    public void Initializes()
    {
      var id = "id";
      var name = "name";
      var baseId = "baseId";

      var subclass = new LogicBlockSubclass(id, name, baseId);

      subclass.Id.ShouldBe(id);
      subclass.Name.ShouldBe(name);
      subclass.BaseId.ShouldBe(baseId);
    }
  }

  public class LogicBlockImplementationTest
  {
    [Fact]
    public void Equality()
    {
      var impl = new LogicBlockImplementation(
        "file",
        "id",
        "name",
        [],
        new LogicBlockGraph(
          "id", "name", "baseId", []
        ),
        new Dictionary<string, LogicBlockGraph>().ToImmutableDictionary()
      );

      var identical = new LogicBlockImplementation(
        "file",
        "id",
        "name",
        [],
        new LogicBlockGraph(
          "id", "name", "baseId", []
        ),
        new Dictionary<string, LogicBlockGraph>().ToImmutableDictionary()
      );

      var differentId = new LogicBlockImplementation(
        "file",
        "id_other",
        "name",
        [],
        new LogicBlockGraph(
          "id", "name", "baseId", []
        ),
        new Dictionary<string, LogicBlockGraph>().ToImmutableDictionary()
      );

      var differentName = new LogicBlockImplementation(
        "file",
        "id",
        "name_other",
        [],
        new LogicBlockGraph(
          "id", "name", "baseId", []
        ),
        new Dictionary<string, LogicBlockGraph>().ToImmutableDictionary()
      );

      var differentGraph = new LogicBlockImplementation(
        "file",
        "id",
        "name_other",
        [],
        new LogicBlockGraph(
          "id", "name", "baseId", [
            new("id", "name", "baseId", [])
          ]
        ),
        new Dictionary<string, LogicBlockGraph>().ToImmutableDictionary()
      );

      impl.Equals(null).ShouldBeFalse();
      impl.Equals(impl).ShouldBeTrue();
      impl.Equals(identical).ShouldBeTrue();
      impl.Equals(differentId).ShouldBeFalse();
      impl.Equals(differentName).ShouldBeFalse();
      impl.Equals(differentGraph).ShouldBeFalse();

      impl.GetHashCode().ShouldBeOfType<int>();
    }
  }

  public class OutputContextsTest
  {
    [Fact]
    public void InitializesContexts()
    {
      OutputContexts.None.DisplayName.ShouldBe(nameof(OutputContexts.None));
      OutputContexts.OnEnter.DisplayName.ShouldBe(nameof(OutputContexts.OnEnter));
      OutputContexts.OnExit.DisplayName.ShouldBe(nameof(OutputContexts.OnExit));
      OutputContexts.OnInput("Input").DisplayName.ShouldBe("OnInput");
      OutputContexts.Method("Method").DisplayName.ShouldBe("Method()");
    }
  }
}
