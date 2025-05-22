namespace Chickensoft.LogicBlocks.CodeFixes;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chickensoft.LogicBlocks.CodeFixes.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Shared, ExportCodeFixProvider(
  LanguageNames.CSharp, Name = nameof(LogicBlockAttributeCodeFixProvider)
)]
public class LogicBlockAttributeCodeFixProvider : CodeFixProvider {
  public sealed override ImmutableArray<string> FixableDiagnosticIds {
    get;
  } = [Diagnostics.MissingLogicBlockAttributeDescriptor.Id];

  public sealed override FixAllProvider GetFixAllProvider() =>
    WellKnownFixAllProviders.BatchFixer;

  public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
    var root = await context
      .Document
      .GetSyntaxRootAsync(context.CancellationToken)
      .ConfigureAwait(false);

    var diagnostic = context.Diagnostics[0];
    var diagnosticSpan = diagnostic.Location.SourceSpan;

    var classDeclaration = root?
      .FindToken(diagnosticSpan.Start)
      .Parent
      ?.DescendantNodesAndSelf()
      .OfType<ClassDeclarationSyntax>()
      .FirstOrDefault();

    if (classDeclaration == null) {
      return;
    }

    context.RegisterCodeFix(
      CodeAction.Create(
        title: "Add [LogicBlock] attribute",
        createChangedDocument: c => AddLogicBlockAttribute(
          context.Document,
          classDeclaration,
          c
        ),
        equivalenceKey: "AddParameterlessConstructor"
      ),
      diagnostic
    );
  }

  private async Task<Document> AddLogicBlockAttribute(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    CancellationToken cancellationToken
  ) {
    if (
      !(
        classDeclaration.Identifier.ValueText is { } identifier &&
        classDeclaration
          .BaseList?
          .Types
          .FirstOrDefault()
          ?.Type is GenericNameSyntax genericName &&
          genericName
            .TypeArgumentList
            .Arguments
            .FirstOrDefault()
            ?.NormalizeWhitespace().ToString() is { } stateType
      )
    ) {
      // Unable to retrieve the state type.
      return document;
    }

    if (stateType.StartsWith(identifier + ".")) {
      // Attributes are scoped inside nested classes, interestingly enough,
      // so we can simplify the type.
      stateType = stateType.Substring(identifier.Length + 1);
    }

    if (stateType.StartsWith("I")) {
      // If it's an interface, drop the I in hopes they've named their concrete
      // state class consistently. If they have, this will save the developer
      // time and trouble.
      stateType = stateType.Substring(1);
    }

    var attribute = SyntaxFactory.Attribute(
      SyntaxFactory.ParseName("LogicBlock")
    ).WithArgumentList(
      SyntaxFactory.ParseAttributeArgumentList(
        $"(typeof({stateType}))"
      )
    );

    var leadingTrivia = classDeclaration
      .Modifiers
      .FirstOrDefault()
      .LeadingTrivia;

    // Any preceding trivia becomes preceding trivia for the attribute list.
    var newClassDeclaration = classDeclaration
      .AddAttributeLists(
        SyntaxFactory.AttributeList(
          SyntaxFactory.SingletonSeparatedList(attribute)
        ).WithLeadingTrivia(leadingTrivia)
      ).WithModifiers(
        classDeclaration.Modifiers
        .Replace(
          classDeclaration.Modifiers.FirstOrDefault(),
          classDeclaration
            .Modifiers
            .First()
            .WithLeadingTrivia(SyntaxTriviaList.Empty)
        )
      );

    var root = (
      await document
        .GetSyntaxRootAsync(cancellationToken)
        .ConfigureAwait(false)
    )!;
    var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
    return document.WithSyntaxRoot(newRoot);
  }
}
