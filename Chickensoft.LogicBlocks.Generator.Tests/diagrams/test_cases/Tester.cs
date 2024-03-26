namespace Chickensoft.LogicBlocks.Generator.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Chickensoft.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public readonly record struct GeneratorOutput(
  IDictionary<string, string> Outputs,
  ImmutableArray<Diagnostic> Diagnostics
);

public static class TestStringExtensions {
  public static string Clean(this string text) => string.Join(
    "\n",
    text.NormalizeLineEndings("\n").Split('\n').Select(line => $"// {line}")
  ).NormalizeLineEndings();
}

public static class Tester {
  public static SemanticModel GetSemanticModel(string code) {
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    return CSharpCompilation
      .Create("AssemblyName")
      .AddSyntaxTrees(tree)
      .GetSemanticModel(tree);
  }

  public static GeneratorOutput Generate(params string[] sources) {
    var syntaxTrees = sources.Select(
      source => CSharpSyntaxTree.ParseText(source)
    );

    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(assembly => !assembly.IsDynamic)
      .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
      .Cast<MetadataReference>();

    var compilation = CSharpCompilation.Create(
      assemblyName: "SourceGeneratorTests",
      syntaxTrees: syntaxTrees,
      references: references,
      options: new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary
      )
    );

    var generator = new LogicBlocksDiagramGenerator();

    CSharpGeneratorDriver.Create(generator)
      .RunGeneratorsAndUpdateCompilation(
        compilation,
        out var outputCompilation,
        out var diagnostics
      );

    var outputs = new Dictionary<string, string>();
    foreach (var output in outputCompilation.SyntaxTrees) {
      var text = output.ToString();
      if (text is not null && !sources.Contains(text)) {
        outputs.Add(Path.GetFileName(output.FilePath), text);
      }
    }

    return new GeneratorOutput(
      Outputs: outputs.ToImmutableDictionary(), Diagnostics: diagnostics
    );
  }

  /// <summary>
  /// Parses the given code and returns the first node of the given type within
  /// the syntax tree.
  /// </summary>
  /// <param name="code">Source code string.</param>
  /// <typeparam name="T">Type of the node to find in the tree.</typeparam>
  /// <returns>First matching node within the tree of type
  /// <typeparamref name="T" />.</returns>
  public static T Parse<T>(string code) where T : SyntaxNode
    => (T)CSharpSyntaxTree
      .ParseText(code)
      .GetRoot()
      .DescendantNodes()
      .First(node => node is T);

  /// <summary>
  /// Parses the given code and returns the first node of the given type within
  /// the syntax tree and the semantic model for the code.
  /// </summary>
  /// <param name="code">Source code string.</param>
  /// <param name="symbol">Symbol for the node found in the tree.</param>
  /// <typeparam name="TNode">Type of the node to find in the tree.</typeparam>
  /// <typeparam name="TSymbol">Type of symbol to find.</typeparam>
  /// <returns>First matching node within the tree of type
  /// <typeparamref name="TNode" />.</returns>
  public static TNode Parse<TNode, TSymbol>(string code, out TSymbol symbol)
    where TNode : SyntaxNode
    where TSymbol : ISymbol {
    var tree = CSharpSyntaxTree.ParseText(code);
    var node = (TNode)tree
      .GetRoot()
      .DescendantNodes()
      .First(node => node is TNode);

    symbol = GetSymbol<TNode, TSymbol>(tree, node);

    return node;
  }

  /// <summary>
  /// Given a syntax tree and a node of a given type, returns the symbol for
  /// that node.
  /// </summary>
  /// <param name="tree">Syntax tree.</param>
  /// <param name="node">Syntax node within the tree.</param>
  /// <typeparam name="TNode">Type of the syntax node given.</typeparam>
  /// <typeparam name="TSymbol">Type of the symbol to get.</typeparam>
  /// <returns>Symbol associated with the syntax tree node.</returns>
  public static TSymbol GetSymbol<TNode, TSymbol>(SyntaxTree tree, TNode node)
    where TNode : SyntaxNode
    where TSymbol : ISymbol {
    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(assembly => !assembly.IsDynamic)
      .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
      .Cast<MetadataReference>();

    return (TSymbol)CSharpCompilation
    .Create("AssemblyName")
    .AddReferences(references)
    .AddSyntaxTrees(tree)
    .GetSemanticModel(tree)
    .GetDeclaredSymbol(node)!;
  }

  private static string CallerFilePath(
    [CallerFilePath] string? callerFilePath = null
  ) => callerFilePath ??
  throw new ArgumentNullException(nameof(callerFilePath));

  public static string LoadFixture(string relativePathInProject) =>
    File.ReadAllText(Path.Join(ProjectDirectory(), relativePathInProject));

  private static string ProjectDirectory() =>
    Path.GetDirectoryName(CallerFilePath())!;
}
