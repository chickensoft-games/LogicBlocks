namespace Chickensoft.LogicBlocks.DiagramGenerator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorUtils;

public class ReturnTypeVisitor : CSharpSyntaxWalker
{
  public SemanticModel Model { get; }
  public CancellationToken Token { get; }
  public ICodeService CodeService { get; }
  /// <summary>Type of the current state.</summary>
  public INamedTypeSymbol StateType { get; }
  private readonly INamedTypeSymbol? _stateBaseType;
  private readonly HashSet<string> _returnTypes = [];

  public ImmutableHashSet<string> ReturnTypes => [.. _returnTypes];

  public ReturnTypeVisitor(
    SemanticModel model,
    CancellationToken token,
    ICodeService codeService,
    INamedTypeSymbol stateType,
    INamedTypeSymbol? stateBaseType = null
  )
  {
    Model = model;
    Token = token;
    CodeService = codeService;
    StateType = stateType;
    _stateBaseType = stateBaseType;
  }

  public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    => AddExpressionToReturnTypes(node.Expression);

  public override void VisitArgument(ArgumentSyntax node)
    => AddExpressionToReturnTypes(node.Expression);

  public override void VisitReturnStatement(ReturnStatementSyntax node)
    => AddExpressionToReturnTypes(node.Expression);

  public override void VisitArrowExpressionClause(
    ArrowExpressionClauseSyntax node
  ) => AddExpressionToReturnTypes(node.Expression);

  private void AddExpressionToReturnTypes(ExpressionSyntax? expression)
  {
    // Recurse into other expressions, looking for return types.
    if (expression is null)
    {
      return;
    }

    if (expression is ConditionalExpressionSyntax conditional)
    {
      AddExpressionToReturnTypes(conditional.WhenTrue);
      AddExpressionToReturnTypes(conditional.WhenFalse);
      return;
    }

    if (expression is SwitchExpressionSyntax @switch)
    {
      foreach (var arm in @switch.Arms)
      {
        AddExpressionToReturnTypes(arm.Expression);
      }

      return;
    }

    if (expression is BinaryExpressionSyntax binary)
    {
      AddExpressionToReturnTypes(binary.Left);
      AddExpressionToReturnTypes(binary.Right);
      return;
    }

    if (expression is MemberAccessExpressionSyntax memberAccess)
    {
      AddExpressionToReturnTypes(memberAccess.Expression);
      return;
    }

    ITypeSymbol? typeSymbol = null;

    // Handle To<T>() and ToSelf() transition invocations
    if (expression is InvocationExpressionSyntax invocation)
    {
      if (
        invocation.Expression is GenericNameSyntax generic &&
        generic.Identifier.Text == "To" &&
        generic.TypeArgumentList.Arguments.Count == 1
      )
      {
        var genericType = generic.TypeArgumentList.Arguments[0];
        typeSymbol = GetModel(genericType).GetTypeInfo(genericType, Token).Type;
      }
      else if (
        invocation.Expression is IdentifierNameSyntax invId &&
        invId.Identifier.Text == "ToSelf"
      )
      {
        typeSymbol = StateType;
      }
      else
      {
        AddExpressionToReturnTypes(invocation.Expression);
        return;
      }
    }

    if (expression is TypeOfExpressionSyntax typeOfExpr)
    {
      typeSymbol = Model.GetTypeInfo(typeOfExpr.Type).Type;
    }

    // Make sure type is provided.
    if (typeSymbol is null)
    {
      return;
    }

    // Filter by stateBaseType when provided (for state diagram transition detection).
    if (_stateBaseType is not null && !typeSymbol.InheritsFromOrEquals(_stateBaseType))
    {
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
