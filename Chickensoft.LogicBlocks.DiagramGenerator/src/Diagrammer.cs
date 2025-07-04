namespace Chickensoft.LogicBlocks.DiagramGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Chickensoft.LogicBlocks.DiagramGenerator.Models;
using Chickensoft.LogicBlocks.DiagramGenerator.Services;
using Chickensoft.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class Diagrammer : ChickensoftGenerator, IIncrementalGenerator {
  public static Log Log { get; } = new Log();
  public ICodeService CodeService { get; } = new CodeService();

  // #pragma warning disable
  // private static bool _logsFlushed;
  // #pragma warning restore

  public void Initialize(IncrementalGeneratorInitializationContext context) {
    // We don't output any static sources. If we did, this is how we'd do it.
    // // Add post initialization sources
    // // (source code that is always generated regardless)
    // foreach (var postInitSource in Constants.PostInitializationSources) {
    //   context.RegisterPostInitializationOutput(
    //     (context) => context.AddSource(
    //       hintName: $"{postInitSource.Key}.cs",
    //       source: postInitSource.Value.Clean()
    //     )
    //   );
    // }

    // If you need to debug the source generator, uncomment the following line
    // and use Visual Studio 2022 on Windows to attach to debugging next time
    // the source generator process is started by running `dotnet build` in
    // the project consuming the source generator
    //
    // --------------------------------------------------------------------- //
    // System.Diagnostics.Debugger.Launch();
    // --------------------------------------------------------------------- //
    //
    // You can debug a source generator in Visual Studio on Windows by
    // simply uncommenting the Debugger.Launch line above.

    // Otherwise...
    // To debug on macOS with VSCode, you can pull open the command palette
    // and select "Debug: Attach to a .NET 5+ or .NET Core process"
    // (csharp.attachToProcess) and then search "VBCS" and select the
    // matching compiler process. Once it attaches, this will stop sleeping
    // and you're on your merry way!

    // --------------------------------------------------------------------- //
    // while (!System.Diagnostics.Debugger.IsAttached) {
    //   Thread.Sleep(100);
    // }
    // System.Diagnostics.Debugger.Break();
    // --------------------------------------------------------------------- //

    var options = context.AnalyzerConfigOptionsProvider
      .Select((options, _) => {
        var disabled = options.GlobalOptions.TryGetValue(
          $"build_property.{Constants.DISABLE_CSPROJ_PROP}", out var value
        ) && value.ToLower() is "true";

        return new GenerationOptions(
          LogicBlocksDiagramGeneratorDisabled: disabled
        );
      });

    var logicBlockCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) =>
        IsLogicBlockCandidate(node),
      transform: (context, token) =>
        GetStateGraph(
          (ClassDeclarationSyntax)context.Node, context.SemanticModel, token
        )
    )
    .Where(logicBlockImplementation => logicBlockImplementation is not null)
    .Combine(options)
    .Select(
      (value, token) => new GenerationData(
        Options: value.Right,
        Result: ConvertStateGraphToUml(
          value.Right, value.Left!, token
        )
      )
    );

    context.RegisterImplementationSourceOutput(
      source: logicBlockCandidates,
      action: (
        context,
        data
      ) => {
        var disabled = data.Options.LogicBlocksDiagramGeneratorDisabled;
        if (disabled) { return; }

        var possibleResult = data.Result;

        if (possibleResult is not LogicBlockOutputResult result) { return; }

        // Since we need to output non-C# files, we have to write files to
        // the disk ourselves. This also allows us to output files that are
        // in the same directory as the source file.

        var destFile = result.FilePath;
        var content = result.Content;

        try {
          File.WriteAllText(destFile, content);
        }
        catch (Exception) {
          // If we can't write a file next to the source file, create a
          // commented out C# file with the UML content in it.
          //
          // This allows the source generator to be unit-tested.
          context.AddSource(
            hintName: $"{result.Name}.puml.g.cs",
            source: string.Join(
              "\n", result.Content.Split('\n').Select(line => $"// {line}")
            )
          );
        }
      }
    );

    Log.Print("Done finding candidates");

    // When debugging source generators, it can be nice to output a log file.
    // This is a total hack to print out a single file.
    // --------------------------------------------------------------------- //
    // var syntax = context.SyntaxProvider.CreateSyntaxProvider(
    //   predicate: (syntaxNode, _) => syntaxNode is CompilationUnitSyntax,
    //   transform: (syntaxContext, _) => syntaxContext.Node
    // );
    // context.RegisterImplementationSourceOutput(
    //   syntax,
    //   (ctx, _) => {
    //     if (_logsFlushed) { return; }
    //     ctx.AddSource(
    //       "LOG", SourceText.From(Log.Contents, Encoding.UTF8)
    //     );
    //     _logsFlushed = true;
    //   }
    // );
    // --------------------------------------------------------------------- //
  }

  public static bool IsLogicBlockCandidate(SyntaxNode node) =>
    node is ClassDeclarationSyntax classDeclaration &&
    classDeclaration.AttributeLists.SelectMany(list => list.Attributes)
      .Any(attribute =>
        attribute.Name.ToString() == Constants.LOGIC_BLOCK_ATTRIBUTE_NAME &&
        attribute.ArgumentList is AttributeArgumentListSyntax argumentList &&
        argumentList.Arguments.Any(
          arg =>
            arg.NameEquals is NameEqualsSyntax nameEquals &&
            nameEquals.Name.ToString() == "Diagram" &&
            arg.Expression is LiteralExpressionSyntax literalExpression &&
            literalExpression.Token.ValueText == "true"
        )
      );

  public LogicBlockImplementation? GetStateGraph(
    ClassDeclarationSyntax logicBlockClassDecl,
    SemanticModel model,
    CancellationToken token
  ) {
    try {
      return DiscoverStateGraph(logicBlockClassDecl, model, token);
    }
    catch (Exception e) {
      Log.Print($"Exception occurred: {e}");
      return null;
    }
  }

  /// <summary>
  /// Looks at a logic block subclass, finds the logic block type in its
  /// inheritance hierarchy, and builds a state graph from it based on the
  /// state type given to the logic block type in the inheritance hierarchy.
  /// </summary>
  /// <param name="logicBlockClassDecl">Logic block class declaration.</param>
  /// <param name="model">Semantic model.</param>
  /// <param name="token">Cancellation token.</param>
  /// <returns>Logic block graph.</returns>
  public LogicBlockImplementation? DiscoverStateGraph(
    ClassDeclarationSyntax logicBlockClassDecl,
    SemanticModel model,
    CancellationToken token
  ) {
    var filePath = logicBlockClassDecl.SyntaxTree.FilePath;
    var destFile = Path.ChangeExtension(filePath, ".g.puml");

    Log.Print($"File path: {filePath}");
    Log.Print($"Dest file: {destFile}");

    var semanticSymbol = model.GetDeclaredSymbol(logicBlockClassDecl, token);

    if (semanticSymbol is not INamedTypeSymbol symbol) {
      return null;
    }

    var concreteState = (INamedTypeSymbol)symbol
      .GetAttributes()
      .FirstOrDefault(
        attr => attr.AttributeClass?.Name ==
          Constants.LOGIC_BLOCK_ATTRIBUTE_NAME_FULL
      )
      ?.ConstructorArguments[0]
      .Value!;

    // Search the logic block for all state subtypes, found recursively
    var stateSubtypes = CodeService.GetAllNestedTypesRecursively(
      symbol,
      (type) => CodeService.GetAllBaseTypes(type).Any(
          (baseType) => SymbolEqualityComparer.Default.Equals(
            baseType, concreteState
          ) || (
            concreteState.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(
              baseType, concreteState.OriginalDefinition
            )
          )
        )
      );

    // Base state becomes the root
    var root = new LogicBlockGraph(
      id: CodeService.GetNameFullyQualifiedWithoutGenerics(
        concreteState, concreteState.Name
      ),
      name: concreteState.Name,
      baseId: CodeService.GetNameFullyQualifiedWithoutGenerics(
        concreteState, concreteState.Name
      )
    );

    var stateTypesById = new Dictionary<string, INamedTypeSymbol> {
      [root.Id] = concreteState
    };

    var stateGraphsById = new Dictionary<string, LogicBlockGraph> {
      [root.Id] = root
    };

    var subtypesByBaseType =
      new Dictionary<string, IList<INamedTypeSymbol>>();

    foreach (var subtype in stateSubtypes) {
      if (token.IsCancellationRequested) {
        return null;
      }

      var baseType = subtype.BaseType;

      if (baseType is not INamedTypeSymbol namedBaseType) {
        continue;
      }

      var baseTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        namedBaseType, namedBaseType.Name
      );

      if (!subtypesByBaseType.ContainsKey(baseTypeId)) {
        subtypesByBaseType[baseTypeId] = [];
      }

      subtypesByBaseType[baseTypeId].Add(subtype);
    }

    // Find initial state
    var getInitialStateMethod = symbol.GetMembers()
      .FirstOrDefault(
        member => member is IMethodSymbol method &&
          member.Name == Constants.LOGIC_BLOCK_GET_INITIAL_STATE
      );

    HashSet<string> initialStateIds = [];

    if (
      getInitialStateMethod is IMethodSymbol initialStateMethod &&
      initialStateMethod.DeclaringSyntaxReferences.Select(
        (syntaxRef) => syntaxRef.GetSyntax(token)
      ).OfType<MethodDeclarationSyntax>() is
        IEnumerable<MethodDeclarationSyntax> initialStateMethodSyntaxes
    ) {
      foreach (var initialStateMethodSyntax in initialStateMethodSyntaxes) {
        var initialStateVisitor = new ReturnTypeVisitor(
          model, token, CodeService, concreteState, symbol
        );
        initialStateVisitor.Visit(initialStateMethodSyntax);
        initialStateIds.UnionWith(initialStateVisitor.ReturnTypes);
      }
    }

    // Convert the subtypes into a graph by recursively building the graph
    // from the base state.
    LogicBlockGraph buildGraph(
      INamedTypeSymbol type, INamedTypeSymbol baseType
    ) {
      var typeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        type, type.Name
      );

      var graph = new LogicBlockGraph(
        id: typeId,
        name: type.Name,
        baseId: CodeService.GetNameFullyQualifiedWithoutGenerics(
          baseType, baseType.Name
        )
      );

      stateTypesById[typeId] = type;
      stateGraphsById[typeId] = graph;

      var subtypes = subtypesByBaseType.ContainsKey(typeId)
        ? subtypesByBaseType[typeId]
        : [];

      foreach (var subtype in subtypes) {
        graph.Children.Add(buildGraph(subtype, type));
      }

      return graph;
    }

    if (subtypesByBaseType.ContainsKey(root.BaseId)) {
      // Only try to build graph of subtypes if there are any.
      root.Children.AddRange(subtypesByBaseType[root.BaseId]
        .Select((stateType) => buildGraph(stateType, concreteState))
      );
    }

    foreach (var state in stateGraphsById.Values) {
      state.Data = GetStateGraphData(
        stateTypesById[state.Id], model, token, concreteState
      );
    }

    var implementation = new LogicBlockImplementation(
      FilePath: destFile,
      Id: CodeService.GetNameFullyQualified(symbol, symbol.Name),
      Name: symbol.Name,
      InitialStateIds: [.. initialStateIds],
      Graph: root,
      StatesById: stateGraphsById.ToImmutableDictionary()
    );

    Log.Print("Graph: " + implementation.Graph);

    return implementation;
  }

  public ILogicBlockResult ConvertStateGraphToUml(
    GenerationOptions options,
    LogicBlockImplementation implementation,
    CancellationToken token
  ) {
    var sb = new StringBuilder();

    // need to build up the uml string describing the state graph
    var graph = implementation.Graph;

    var transitions = new List<string>();
    foreach (
      var stateId in implementation.StatesById.OrderBy(id => id.Key)
    ) {
      var state = stateId.Value;
      foreach (
        var inputToStates in state.Data.InputToStates.OrderBy(id => id.Key)
      ) {
        var inputId = inputToStates.Key;
        foreach (var destStateId in inputToStates.Value.OrderBy(id => id)) {
          var dest = implementation.StatesById[destStateId];
          transitions.Add(
            $"{state.UmlId} --> " +
            $"{dest.UmlId} : {state.Data.Inputs[inputId].Name}"
          );
        }
      }
    }

    transitions.Sort();

    var initialStates = new List<string>();
    // State descriptions are added at the end of the document outside
    // of the state declaration. Mermaid doesn't support state descriptions
    // when they are nested inside the state, so we just flatten it out.
    //
    // In our case, we use state descriptions to show what outputs are produced
    // by the state, and when.
    var stateDescriptions = new List<string>();

    foreach (
      var initialStateId in implementation.InitialStateIds.OrderBy(id => id)
    ) {
      initialStates.Add(
        "[*] --> " + implementation.StatesById[initialStateId].UmlId
      );
    }

    var states =
      WriteGraph(implementation.Graph, implementation, stateDescriptions, 0);

    stateDescriptions.Sort();

    var text = Format($"""
    @startuml {implementation.Name}
    {states}

    {transitions}

    {stateDescriptions}

    {initialStates}
    @enduml
    """);

    return new LogicBlockOutputResult(
      FilePath: implementation.FilePath,
      Name: implementation.Name,
      Content: text
    );
  }

  private IEnumerable<string> WriteGraph(
    LogicBlockGraph graph,
    LogicBlockImplementation impl,
    List<string> stateDescriptions,
    int t
  ) {
    var lines = new List<string>();

    var isMultilineState = graph.Children.Count > 0;

    var isRoot = graph == impl.Graph;

    if (isMultilineState) {
      if (isRoot) {
        lines.Add(
          $"{Tab(t)}state \"{impl.Name} State\" as {graph.UmlId} {{"
        );
      }
      else {
        lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId} {{");
      }
    }
    else if (isRoot) {
      lines.Add($"{Tab(t)}state \"{impl.Name} State\" as {graph.UmlId}");
    }
    else {
      lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId}");
    }

    foreach (var child in graph.Children.OrderBy(child => child.Name)) {
      lines.AddRange(
        WriteGraph(child, impl, stateDescriptions, t + 1)
      );
    }

    foreach (
      var outputContext in
        graph.Data.Outputs.Keys.OrderBy(key => key.DisplayName)
    ) {
      var outputs = graph.Data.Outputs[outputContext]
        .Select(output => output.Name)
        .OrderBy(output => output);
      var line = string.Join(", ", outputs);
      stateDescriptions.Add(
        $"{graph.UmlId} : {outputContext.DisplayName} → {line}"
      );
    }

    if (isMultilineState) { lines.Add($"{Tab(t)}}}"); }
    return lines;
  }

  public LogicBlockGraphData GetStateGraphData(
      INamedTypeSymbol type,
      SemanticModel model,
      CancellationToken token,
      INamedTypeSymbol stateBaseType
    ) {
    // type is the state type

    var inputsBuilder = ImmutableDictionary
      .CreateBuilder<string, LogicBlockInput>();
    var inputToStatesBuilder = ImmutableDictionary
      .CreateBuilder<string, ImmutableHashSet<string>>();
    var outputsBuilder = ImmutableDictionary
      .CreateBuilder<IOutputContext, ImmutableHashSet<LogicBlockOutput>>();

    // Get all of the handled inputs by looking at the implemented input
    // handler interfaces.

    var handledInputInterfaces = type.AllInterfaces.Where(
      (interfaceType) => CodeService.GetNameFullyQualifiedWithoutGenerics(
        interfaceType, interfaceType.Name
      ) is
        Constants.LOGIC_BLOCK_INPUT_INTERFACE_ID &&
        interfaceType.TypeArguments.Length == 1
    );

    var interfaces = new HashSet<INamedTypeSymbol>(
      type.Interfaces, SymbolEqualityComparer.Default
    );

    // Get all syntax nodes comprising this type declaration.
    var syntaxNodes = type.DeclaringSyntaxReferences
      .Select(syntaxRef => syntaxRef.GetSyntax(token));

    // Find constructors for the type, filtering out any constructors for nested
    // types.
    var constructorNodes = syntaxNodes
      .SelectMany(syntaxNode => syntaxNode.ChildNodes())
      .OfType<ConstructorDeclarationSyntax>().ToList();

    var inputHandlerMethods = new List<MethodDeclarationSyntax>();

    var outputVisitor = new OutputVisitor(
      model, token, CodeService, OutputContexts.None
    );
    foreach (var constructor in constructorNodes) {
      // Collect outputs from every syntax node comprising the state type.
      outputVisitor.Visit(constructor);
    }
    outputsBuilder.AddRange(outputVisitor.OutputTypes);

    foreach (var handledInputInterface in handledInputInterfaces) {
      var interfaceMembers = handledInputInterface.GetMembers();
      var inputTypeSymbol = handledInputInterface.TypeArguments[0];
      if (inputTypeSymbol is not INamedTypeSymbol inputType) {
        continue;
      }
      if (interfaceMembers.Length == 0) { continue; }
      var implementation = type.FindImplementationForInterfaceMember(
        interfaceMembers[0]
      );
      if (implementation is not IMethodSymbol methodSymbol) {
        continue;
      }

      var onTypeItself = interfaces.Contains(handledInputInterface);

      if (!onTypeItself) {
        // method is not on the current type (so it must be implemented on a
        // base type).
        //
        // we have to check for this case since Roslyn doesn't return
        // overridden methods on the derived type when asking for an interface's
        // member implementation method — we have to look up the overrides
        // ourselves :/

        // find any equivalent, overridden method on the current derived type
        methodSymbol = type.GetMembers()
          .OfType<IMethodSymbol>()
          .FirstOrDefault(
            member => SymbolEqualityComparer.Default.Equals(
              member.OverriddenMethod, methodSymbol
            )
          );

        if (methodSymbol is null) {
          continue;
        }
      }

      var handlerMethodSyntaxes = methodSymbol
        .DeclaringSyntaxReferences
        .Select(syntaxRef => syntaxRef.GetSyntax(token))
        .OfType<MethodDeclarationSyntax>()
        .ToImmutableArray();

      foreach (var methodSyntax in handlerMethodSyntaxes) {
        inputHandlerMethods.Add(methodSyntax);
        var inputId = CodeService.GetNameFullyQualifiedWithoutGenerics(
          inputType, inputType.Name
        );
        var outputContext = OutputContexts.OnInput(inputType.Name);
        var modelForSyntax =
          model.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);
        var returnTypeVisitor = new ReturnTypeVisitor(
          modelForSyntax, token, CodeService, stateBaseType, type
        );
        outputVisitor = new OutputVisitor(
          modelForSyntax, token, CodeService, outputContext
        );

        returnTypeVisitor.Visit(methodSyntax);
        outputVisitor.Visit(methodSyntax);

        if (outputVisitor.OutputTypes.ContainsKey(outputContext)) {
          outputsBuilder.Add(
            outputContext, outputVisitor.OutputTypes[outputContext]
          );
        }

        inputsBuilder.Add(
          inputId,
          new LogicBlockInput(Id: inputId, Name: inputType.Name)
        );

        inputToStatesBuilder.Add(
          inputId,
          returnTypeVisitor.ReturnTypes
        );
      }
    }

    // find methods on type that aren't input handlers or constructors
    var allOtherMethods = syntaxNodes
      .SelectMany(syntaxNode => syntaxNode.ChildNodes())
      .OfType<MethodDeclarationSyntax>()
      .Where(
        methodSyntax => !inputHandlerMethods.Contains(methodSyntax)
      );

    foreach (var otherMethod in allOtherMethods) {
      Log.Print("Examining method: " + otherMethod.Identifier.Text);
      var outputContext = OutputContexts.Method(otherMethod.Identifier.Text);

      var modelForSyntax = model.Compilation.GetSemanticModel(
        otherMethod.SyntaxTree
      );

      outputVisitor = new OutputVisitor(
        modelForSyntax, token, CodeService, outputContext
      );
      outputVisitor.Visit(otherMethod);

      if (outputVisitor.OutputTypes.ContainsKey(outputContext)) {
        outputsBuilder.Add(
          outputContext, outputVisitor.OutputTypes[outputContext]
        );
      }
    }

    var inputs = inputsBuilder.ToImmutable();

    var inputToStates = inputToStatesBuilder.ToImmutable();

    foreach (var input in inputToStates.Keys) {
      Log.Print(
        $"{type.Name} + {input.Split('.').Last()} -> " +
        $"{string.Join(", ", inputToStates[input].Select(
          s => s.Split('.').Last())
        )}"
      );
    }

    var outputs = outputsBuilder.ToImmutable();

    return new LogicBlockGraphData(
      Inputs: inputs,
      InputToStates: inputToStates,
      Outputs: outputs
    );
  }
}
