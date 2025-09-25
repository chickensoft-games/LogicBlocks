namespace Chickensoft.LogicBlocks.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using Chickensoft.LogicBlocks.Analyzers.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LogicBlockAttributeAnalyzer : DiagnosticAnalyzer {
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
    get;
  } = [Diagnostics.MissingLogicBlockAttributeDescriptor];

  public override void Initialize(AnalysisContext context) {
    context.EnableConcurrentExecution();

    context.ConfigureGeneratedCodeAnalysis(
      GeneratedCodeAnalysisFlags.Analyze |
      GeneratedCodeAnalysisFlags.ReportDiagnostics
    );

    context.RegisterSyntaxNodeAction(
      AnalyzeClassDeclaration,
      SyntaxKind.ClassDeclaration
    );
  }

  private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
    var classDeclaration = (ClassDeclarationSyntax)context.Node;

    if (
        classDeclaration.BaseList?.Types.FirstOrDefault()?.Type is not GenericNameSyntax baseLogicBlock ||
        !baseLogicBlock.Identifier.ValueText.EndsWith("LogicBlock")
    ) {
      // Only analyze types that appear to be logic blocks.
      return;
    }

    // We can't apply the LogicBlock attribute to generic LogicBlock types
    // if one of the generics is the state type
    if (
      classDeclaration.TypeParameterList is not null
        && classDeclaration.TypeParameterList.Parameters.Count > 0
        && classDeclaration.TypeParameterList.Parameters.Any(
          parameter =>
            baseLogicBlock.TypeArgumentList.Arguments.Any(
              argument =>
                argument is IdentifierNameSyntax identifierArg
                  && identifierArg.Identifier.ValueText == parameter.Identifier.ValueText
            )
        )
    ) {
      return;
    }

    var attributes = classDeclaration.AttributeLists.SelectMany(
      list => list.Attributes
    ).Where(
      attribute => attribute.Name.ToString() == "LogicBlock"
    );

    if (!attributes.Any()) {
      context.ReportDiagnostic(
        Diagnostics.MissingLogicBlockAttribute(
          classDeclaration.GetLocation(),
          classDeclaration.Identifier.ValueText
        )
      );
    }
  }
}
