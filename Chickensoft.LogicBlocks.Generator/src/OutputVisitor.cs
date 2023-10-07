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
    ImmutableDictionary<IOutputContext, HashSet<LogicBlockOutput>>.Builder
      _outputTypes = ImmutableDictionary
        .CreateBuilder<IOutputContext, HashSet<LogicBlockOutput>>();
  private readonly Stack<IOutputContext> _outputContexts = new();
  private IOutputContext OutputContext => _outputContexts.Peek();

  public ImmutableDictionary<IOutputContext, ImmutableHashSet<LogicBlockOutput>>
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
    var methodName = "";
    if (node.Expression is MemberAccessExpressionSyntax memberAccess) {
      var id = memberAccess.Expression;
      if (id is not IdentifierNameSyntax identifierName) {
        base.VisitInvocationExpression(node);
        return;
      }

      var lhsType =
        GetModel(identifierName).GetTypeInfo(identifierName, Token).Type;
      if (lhsType is null) {
        base.VisitInvocationExpression(node);
        return;
      }

      var lhsTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        lhsType, lhsType.Name
      );
      methodName = memberAccess.Name.Identifier.ValueText;

      if (
        lhsTypeId != Constants.LOGIC_BLOCK_CONTEXT_ID ||
        methodName != Constants.LOGIC_BLOCK_CONTEXT_OUTPUT
      ) {
        base.VisitInvocationExpression(node);
        return;
      }

      var args = node.ArgumentList.Arguments;

      if (args.Count != 1) {
        base.VisitInvocationExpression(node);
        return;
      }

      var rhs = node.ArgumentList.Arguments[0].Expression;
      var rhsType = GetModel(rhs).GetTypeInfo(rhs, Token).Type;

      if (rhsType is null) {
        base.VisitInvocationExpression(node);
        return;
      }

      var rhsTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        rhsType, rhsType.Name
      );

      AddOutput(rhsTypeId, rhsType.Name);

      return;
    }

    if (node.Expression is not GenericNameSyntax genericName) {
      base.VisitInvocationExpression(node);
      return;
    }

    // void log(string message) => LogicBlocksGenerator.Log.Print(message);

    methodName = genericName.Identifier.ValueText;

    var pushedContext = false;
    if (methodName == Constants.LOGIC_BLOCK_STATE_LOGIC_ON_ENTER) {
      _outputContexts.Push(OutputContexts.OnEnter);
      pushedContext = true;
    }
    else if (methodName == Constants.LOGIC_BLOCK_STATE_LOGIC_ON_EXIT) {
      _outputContexts.Push(OutputContexts.OnExit);
      pushedContext = true;
    }

    base.VisitInvocationExpression(node);

    if (pushedContext) {
      _outputContexts.Pop();
    }
  }

  // Don't visit nested types.
  public override void VisitClassDeclaration(ClassDeclarationSyntax node) { }
  public override void VisitStructDeclaration(StructDeclarationSyntax node) { }

  private void AddOutput(string id, string name) {
    if (!_outputTypes.TryGetValue(OutputContext, out var outputs)) {
      outputs = new HashSet<LogicBlockOutput>();
      _outputTypes.Add(OutputContext, outputs);
    }

    outputs.Add(new LogicBlockOutput(id, name));
  }

  private SemanticModel GetModel(SyntaxNode node) =>
    Model.Compilation.GetSemanticModel(node.SyntaxTree);
}
