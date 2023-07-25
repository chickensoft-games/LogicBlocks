namespace Chickensoft.LogicBlocks.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Chickensoft.LogicBlocks.Generator.Common.Models;
using Chickensoft.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Services;

public class OutputVisitor : CSharpSyntaxWalker {
  public SemanticModel Model { get; }
  public CancellationToken Token { get; }
  public ICodeService CodeService { get; }
  private readonly
    ImmutableDictionary<IOutputContext, HashSet<string>>.Builder
      _outputTypes = ImmutableDictionary
        .CreateBuilder<IOutputContext, HashSet<string>>();
  private readonly Stack<IOutputContext> _outputContexts = new();
  private IOutputContext OutputContext => _outputContexts.Peek();

  public ImmutableDictionary<IOutputContext, ImmutableHashSet<string>>
    OutputTypes => _outputTypes.ToImmutableDictionary(
      pair => pair.Key, pair => pair.Value.ToImmutableHashSet()
    );

  public OutputVisitor(
    SemanticModel model,
    CancellationToken token,
    ICodeService service,
    IOutputContext startContext
  ) {
    Model = model;
    Token = token;
    CodeService = service;
    _outputContexts.Push(startContext);
  }

  public override void VisitInvocationExpression(
    InvocationExpressionSyntax node
  ) {
    if (node.Expression is not MemberAccessExpressionSyntax memberAccess) {
      return;
    }

    var id = memberAccess.Expression;
    if (id is not IdentifierNameSyntax identifierName) { return; }

    var lhsType =
      GetModel(identifierName).GetTypeInfo(identifierName, Token).Type;
    if (lhsType is null) { return; }

    var lhsTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
      lhsType, lhsType.Name
    );
    var methodName = memberAccess.Name.Identifier.ValueText;

    var pushedContext = false;
    if (methodName == Constants.LOGIC_BLOCK_CONTEXT_ON_ENTER) {
      _outputContexts.Push(OutputContexts.OnEnter);
      pushedContext = true;
    }
    else if (methodName == Constants.LOGIC_BLOCK_CONTEXT_ON_EXIT) {
      _outputContexts.Push(OutputContexts.OnExit);
      pushedContext = true;
    }

    base.VisitInvocationExpression(node);

    if (pushedContext) {
      _outputContexts.Pop();
    }

    if (
      lhsTypeId != Constants.LOGIC_BLOCK_CONTEXT_ID ||
      methodName != Constants.LOGIC_BLOCK_CONTEXT_OUTPUT
    ) {
      return;
    }

    var args = node.ArgumentList.Arguments;

    if (args.Count != 1) {
      return;
    }

    var rhs = node.ArgumentList.Arguments[0].Expression;
    var rhsType = GetModel(rhs).GetTypeInfo(rhs, Token).Type;

    if (rhsType is null) {
      return;
    }

    var rhsTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
      rhsType, rhsType.Name
    );

    AddOutput(rhsTypeId);
  }

  public override void VisitClassDeclaration(ClassDeclarationSyntax node) { }

  private void AddOutput(string outputId) {
    if (!_outputTypes.TryGetValue(OutputContext, out var outputs)) {
      outputs = new HashSet<string>();
      _outputTypes.Add(OutputContext, outputs);
    }

    outputs.Add(outputId);
  }

  private SemanticModel GetModel(SyntaxNode node) =>
    Model.Compilation.GetSemanticModel(node.SyntaxTree);
}
