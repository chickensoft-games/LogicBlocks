namespace Chickensoft.LogicBlocks.DiagramGenerator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorUtils;

public class SetDependencyVisitor : CSharpSyntaxWalker
{
  public SemanticModel Model { get; }
  public CancellationToken Token { get; }
  public ICodeService CodeService { get; }
  /// <summary>Type of the current state.</summary>
  public INamedTypeSymbol StateType { get; }
  private readonly HashSet<string> _returnTypes = [];

  public ImmutableHashSet<string> ReturnTypes => [.. _returnTypes];

  public SetDependencyVisitor(
    SemanticModel model,
    CancellationToken token,
    ICodeService codeService,
    INamedTypeSymbol stateType
  )
  {
    Model = model;
    Token = token;
    CodeService = codeService;
    StateType = stateType;
  }

  public override void VisitArgument(ArgumentSyntax node)
    => AddExpressionToReturnTypes(node.Expression);

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

    ITypeSymbol? type = default;

    if (expression is TypeOfExpressionSyntax typeOfExpr)
    {
      type = Model.GetTypeInfo(typeOfExpr.Type).Type;
    }

    if (expression is ObjectCreationExpressionSyntax objectCreation)
    {
      type = Model.GetTypeInfo(objectCreation).Type;
    }

    if (expression is IdentifierNameSyntax identifierName)
    {
      type = Model.GetTypeInfo(identifierName).Type;
    }

    // Make sure type is provided.
    if ((type is INamedTypeSymbol typeSymbol &&
         CodeService.GetAllBaseTypes(typeSymbol).Any(baseType =>
           baseType.Name == Constants.LOGIC_BLOCK_STATE)) || type is null)
    {
      return;
    }

    var returnTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
      type, type.Name
    );

    _returnTypes.Add(returnTypeId);
  }
}
