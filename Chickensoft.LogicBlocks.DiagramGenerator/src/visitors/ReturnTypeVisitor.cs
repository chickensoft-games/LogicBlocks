namespace Chickensoft.LogicBlocks.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Chickensoft.LogicBlocks.DiagramGenerator.Services;
using Chickensoft.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class ReturnTypeVisitor : CSharpSyntaxWalker {
  public SemanticModel Model { get; }
  public CancellationToken Token { get; }
  public ICodeService CodeService { get; }
  public INamedTypeSymbol StateBaseType { get; }
  private readonly HashSet<string> _returnTypes = new();

  public ImmutableHashSet<string> ReturnTypes => _returnTypes.ToImmutableHashSet();

  public ReturnTypeVisitor(
    SemanticModel model,
    CancellationToken token,
    ICodeService codeService,
    INamedTypeSymbol stateBaseType
  ) {
    Model = model;
    Token = token;
    CodeService = codeService;
    StateBaseType = stateBaseType;
  }

  public override void VisitThisExpression(ThisExpressionSyntax node) =>
    AddExpressionToReturnTypes(node);

  public override void VisitObjectCreationExpression(
    ObjectCreationExpressionSyntax node
  ) => AddExpressionToReturnTypes(node);

  public override void VisitReturnStatement(ReturnStatementSyntax node)
    => AddExpressionToReturnTypes(node.Expression);

  public override void VisitArrowExpressionClause(
    ArrowExpressionClauseSyntax node
  ) => AddExpressionToReturnTypes(node.Expression);

  private void AddExpressionToReturnTypes(ExpressionSyntax? expression) {
    if (expression is not ExpressionSyntax expressionSyntax) {
      return;
    }

    var type = GetModel(expression).GetTypeInfo(expression, Token).Type;

    if (expression is ConditionalExpressionSyntax conditional) {
      AddExpressionToReturnTypes(conditional.WhenTrue);
      AddExpressionToReturnTypes(conditional.WhenFalse);
      return;
    }

    if (expression is SwitchExpressionSyntax @switch) {
      foreach (var arm in @switch.Arms) {
        AddExpressionToReturnTypes(arm.Expression);
      }

      return;
    }

    if (expression is BinaryExpressionSyntax binary) {
      AddExpressionToReturnTypes(binary.Left);
      AddExpressionToReturnTypes(binary.Right);
      return;
    }

    if (type is not ITypeSymbol typeSymbol) {
      return;
    }

    // Make sure type is a subtype of the state.
    if (!type.InheritsFromOrEquals(StateBaseType)) {
      return;
    }

    var returnTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
      typeSymbol, typeSymbol.Name
    );

    _returnTypes.Add(returnTypeId);
  }

  private SemanticModel GetModel(SyntaxNode node) =>
    Model.Compilation.GetSemanticModel(node.SyntaxTree);
}
