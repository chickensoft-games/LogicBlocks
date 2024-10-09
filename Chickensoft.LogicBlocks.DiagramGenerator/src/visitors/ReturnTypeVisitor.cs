namespace Chickensoft.LogicBlocks.DiagramGenerator;

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
  /// <summary>Type of the current state.</summary>
  public INamedTypeSymbol StateType { get; }
  private readonly HashSet<string> _returnTypes = new();

  public ImmutableHashSet<string> ReturnTypes => [.. _returnTypes];

  public ReturnTypeVisitor(
    SemanticModel model,
    CancellationToken token,
    ICodeService codeService,
    INamedTypeSymbol stateBaseType,
    INamedTypeSymbol stateType
  ) {
    Model = model;
    Token = token;
    CodeService = codeService;
    StateBaseType = stateBaseType;
    StateType = stateType;
  }

  public override void VisitReturnStatement(ReturnStatementSyntax node)
    => AddExpressionToReturnTypes(node.Expression);

  public override void VisitArrowExpressionClause(
    ArrowExpressionClauseSyntax node
  ) => AddExpressionToReturnTypes(node.Expression);

  private void AddExpressionToReturnTypes(ExpressionSyntax? expression) {
    // Recurse into other expressions, looking for return types.
    if (expression is not ExpressionSyntax expressionSyntax) {
      return;
    }

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

    if (expression is MemberAccessExpressionSyntax memberAccess) {
      AddExpressionToReturnTypes(memberAccess.Expression);
      return;
    }

    // Recursive base case.
    // Look for To<State>() and ToSelf() method calls and glean type information
    // based on that.

    ITypeSymbol? type = default;

    if (
      expression is InvocationExpressionSyntax invocation
    ) {
      if (
        invocation.Expression is GenericNameSyntax generic &&
        generic.Identifier.Text == "To" &&
        generic.TypeArgumentList.Arguments.Count == 1
      ) {
        var genericType = generic.TypeArgumentList.Arguments[0];
        type = GetModel(genericType).GetTypeInfo(genericType, Token).Type;
      }
      else if (
        invocation.Expression is IdentifierNameSyntax id &&
        id.Identifier.Text == "ToSelf"
      ) {
        type = StateType;
      }
      else {
        AddExpressionToReturnTypes(invocation.Expression);
        return;
      }
    }

    // Make sure type is provided.
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
