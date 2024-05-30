namespace Chickensoft.Introspection.Generator.Tests.Models;

using System.Linq;
using Chickensoft.Introspection.Generator.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Xunit;

public class TypeGeneratorTest {

  [Fact]
  public void GetConstruction() =>
    TypeGenerator.GetConstruction(
      SyntaxFactory.StructDeclaration("MyStruct")
    ).ShouldBe(Construction.Struct);

  [Fact]
  public void PropertiesWithoutAccessorList() {
    var code = """
    [Meta]
    public partial class MyType {
      public int Property { }
    }
    """;

    var tree = CSharpSyntaxTree.ParseText(code);

    var typeDeclaration = tree
      .GetRoot()
      .DescendantNodes()
      .OfType<ClassDeclarationSyntax>()
      .First();

    return;
  }
}
