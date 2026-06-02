namespace Chickensoft.LogicBlocks.DiagramGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Models;
using SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Services;

[Generator]
public class DiagramGenerator : ChickensoftGenerator, IIncrementalGenerator
{
  public static Log Log { get; } = new Log();
  public static ICodeService CodeService { get; } = new CodeService();
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
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
    //
    // To debug on macOS with VSCode, you can pull open the command palette
    // and select "Debug: Attach to a .NET 5+ or .NET Core process"
    // (csharp.attachToProcess) and then search "VBCS" and select the
    // matching compiler process. Once it attaches, this will stop sleeping
    // and you're on your merry way!
    //
    // --------------------------------------------------------------------- //
    // while (!System.Diagnostics.Debugger.IsAttached) {
    //   Thread.Sleep(100);
    // }
    // System.Diagnostics.Debugger.Break();
    // --------------------------------------------------------------------- //

    var options = context.AnalyzerConfigOptionsProvider
      .Select((options, _) =>
      {
        var disabled = options.GlobalOptions.TryGetValue(
          $"build_property.{Constants.DISABLE_CSPROJ_PROP}", out var value
        ) && value.ToLower() is "true";

        return new GenerationOptions(
          disabled
        );
      });

    var logicBlockCandidates = context.SyntaxProvider.CreateSyntaxProvider(
        predicate: static (node, _) =>
          IsLogicBlockCandidate(node),
        transform: (ctx, token) =>
          GetGraph(
            DiscoverLogicDiagram, (ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel, token
          )
      )
      .Combine(options)
      .Select(
        (value, _) => new GenerationData(
          Options: value.Right,
          Result: value.Left
        )
      );

    context.RegisterImplementationSourceOutput(logicBlockCandidates, GenerateDiagram);

    var stateDiagramCandidates = context.SyntaxProvider.CreateSyntaxProvider(
        predicate: static (node, _) =>
          IsStateDiagramCandidate(node),
        transform: (ctx, token) =>
          GetGraph(
            DiscoverStateGraph, (TypeDeclarationSyntax)ctx.Node, ctx.SemanticModel, token
          )
      )
      .Combine(options)
      .Select(
        (value, _) => new GenerationData(
          Options: value.Right,
          Result: value.Left
        )
      );

    context.RegisterImplementationSourceOutput(stateDiagramCandidates, GenerateDiagram);
  }

  public void GenerateDiagram(SourceProductionContext context, GenerationData data)
  {
    if (data.Options.LogicBlocksDiagramGeneratorDisabled) { return; }
    if (data.Result is null) { return; }

    var result = data.Result switch
    {
      LogicBlockImplementation logicBlockImplementation => ConvertToUml(logicBlockImplementation),
      StateDiagramImplementation stateDiagramImplementation => ConvertStateGraphToUml(stateDiagramImplementation),
      _ => null
    };

    if (result is not IValidLogicBlockResult validResult) { return; }

    try
    {
      File.WriteAllText(validResult.FilePath, validResult.Content);
    }
    catch (Exception)
    {
      context.AddSource(
        hintName: $"{validResult.Name}.puml.g.cs",
        source: string.Join(
          "\n", validResult.Content.Split('\n').Select(line => $"// {line}")
        )
      );
    }
  }

  public static bool IsLogicBlockCandidate(SyntaxNode node)
  {
    //Only retrieve logic blocks that have the LogicBlock type and have the Start method called
    return node is ClassDeclarationSyntax classDeclaration &&
           (CodeService.InheritsFromByName(classDeclaration, Constants.LOGIC_BLOCK_TYPE_NAME) ||
            CodeService.InheritsFromByName(classDeclaration, Constants.AUTO_BLOCK_TYPE_NAME)) &&
           classDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>()
             .Any(invocation =>
               (invocation.Expression as SimpleNameSyntax)?.Identifier.Text == Constants.LOGIC_BLOCK_STATE_LOGIC_START ||
               (invocation.Expression as GenericNameSyntax)?.Identifier.Text == Constants.LOGIC_BLOCK_STATE_LOGIC_START);
  }

  public static bool IsStateDiagramCandidate(SyntaxNode node)
  {
    return node is TypeDeclarationSyntax classDeclarationSyntax &&
           CodeService.InheritsFromByName(classDeclarationSyntax, Constants.LOGIC_BLOCK_STATE) &&
           classDeclarationSyntax.AttributeLists.SelectMany(l => l.Attributes)
             .Any(attr => attr.Name.ToString() == Constants.LOGIC_BLOCK_ATTRIBUTE_NAME);
  }

  public T? GetGraph<T>(
    Func<TypeDeclarationSyntax, SemanticModel, CancellationToken, T?> discoverFunc,
    TypeDeclarationSyntax logicBlockClassDecl,
    SemanticModel model,
    CancellationToken token
  )
  {
    try
    {
      return discoverFunc(logicBlockClassDecl, model, token);
    }
    catch (Exception e)
    {
      Log.Print($"Exception occurred: {e}");
      return default;
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
  public LogicBlockImplementation? DiscoverLogicDiagram(
    TypeDeclarationSyntax logicBlockClassDecl,
    SemanticModel model,
    CancellationToken token
  )
  {
    var filePath = logicBlockClassDecl.SyntaxTree.FilePath;
    var destFile = Path.ChangeExtension(filePath, ".g.puml");

    Log.Print($"File path: {filePath}");
    Log.Print($"Dest file: {destFile}");

    var semanticSymbol = model.GetDeclaredSymbol(logicBlockClassDecl, token);

    if (semanticSymbol is null)
    {
      return null;
    }

    HashSet<string> initialStateIds = [];

    var startMethodArgs = semanticSymbol.DeclaringSyntaxReferences
      .SelectMany(x =>
        x.GetSyntax()
          .DescendantNodes()
          .OfType<InvocationExpressionSyntax>()
          .Where(invocation =>
            (invocation.Expression as SimpleNameSyntax)?.Identifier.Text == Constants.LOGIC_BLOCK_STATE_LOGIC_START &&
            invocation.ArgumentList.Arguments is { Count: not 0 } args&&
            model.GetTypeInfo(args[0].Expression).Type?.ToDisplayString() == Constants.SYSTEMTYPE)
          .Select(syntax => syntax.ArgumentList.Arguments[0])
        );

    foreach (var arg in startMethodArgs)
    {
      var initialStateVisitor = new ReturnTypeVisitor(
        model, token, CodeService, semanticSymbol
      );
      initialStateVisitor.Visit(arg);
      initialStateIds.UnionWith(initialStateVisitor.ReturnTypes);
    }

    // Handle Start<T>() generic invocations — extract the type argument directly.
    var genericStartTypeArgs = semanticSymbol.DeclaringSyntaxReferences
      .SelectMany(x =>
        x.GetSyntax()
          .DescendantNodes()
          .OfType<InvocationExpressionSyntax>()
          .Where(invocation =>
            invocation.Expression is GenericNameSyntax generic &&
            generic.Identifier.Text == Constants.LOGIC_BLOCK_STATE_LOGIC_START &&
            generic.TypeArgumentList.Arguments.Count == 1)
          .Select(invocation =>
            ((GenericNameSyntax)invocation.Expression).TypeArgumentList.Arguments[0])
        );

    foreach (var typeArg in genericStartTypeArgs)
    {
      if (model.GetTypeInfo(typeArg, token).Type is INamedTypeSymbol typeSymbol)
      {
        initialStateIds.Add(
          CodeService.GetNameFullyQualifiedWithoutGenerics(typeSymbol, typeSymbol.Name)
        );
      }
    }

    HashSet<string> dependencyIds = [];

    foreach (var tree in model.Compilation.SyntaxTrees)
    {
      if (token.IsCancellationRequested) { break; }
      var treeModel = model.Compilation.GetSemanticModel(tree);
      var setArgs = tree.GetRoot(token)
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Where(invocation =>
        {
          switch (invocation.Expression)
          {
            case SimpleNameSyntax { Identifier.Text:
              Constants.LOGIC_BLOCK_STATE_LOGIC_SET or
              Constants.LOGIC_BLOCK_STATE_LOGIC_SET_OBJECT or
              Constants.LOGIC_BLOCK_STATE_LOGIC_OVERWRITE or
              Constants.LOGIC_BLOCK_STATE_LOGIC_OVERWRITE_OBJECT
            }:
              {
                var enclosing = treeModel.GetEnclosingSymbol(invocation.SpanStart, token);
                return SymbolEqualityComparer.Default.Equals(
                  enclosing?.ContainingType, semanticSymbol
                );
              }
            case MemberAccessExpressionSyntax { Name.Identifier.Text:
              Constants.LOGIC_BLOCK_STATE_LOGIC_SET or
              Constants.LOGIC_BLOCK_STATE_LOGIC_SET_OBJECT or
              Constants.LOGIC_BLOCK_STATE_LOGIC_OVERWRITE or
              Constants.LOGIC_BLOCK_STATE_LOGIC_OVERWRITE_OBJECT
            } memberAccess:
              {
                var receiverType = treeModel.GetTypeInfo(memberAccess.Expression).Type;
                return SymbolEqualityComparer.Default.Equals(
                         receiverType, semanticSymbol
                        ) ||
                       semanticSymbol.AllInterfaces.Contains(
                         receiverType, SymbolEqualityComparer.Default
                        );
              }
            default:
              return false;
          }
        })
        .Select(invocation => invocation.ArgumentList.Arguments[0]);

      foreach (var arg in setArgs)
      {
        var initialStateVisitor = new SetDependencyVisitor(
          treeModel, token, CodeService, semanticSymbol
        );
        initialStateVisitor.Visit(arg);
        dependencyIds.UnionWith(initialStateVisitor.ReturnTypes);
      }
    }

    var implementation = new LogicBlockImplementation(
      FilePath: destFile,
      Id: CodeService.GetNameFullyQualified(semanticSymbol, semanticSymbol.Name),
      Name: semanticSymbol.Name,
      InitialStateIds: [.. initialStateIds],
      DependencyIds: [.. dependencyIds]
    );

    return implementation;
  }

  public ILogicBlockResult ConvertToUml(LogicBlockImplementation implementation)
  {
    static string ShortName(string id) =>
      id.Replace("global::", "").Split('.').Last();

    var name = implementation.Name;
    var initialStateNames = implementation.InitialStateIds
      .Select(ShortName)
      .OrderBy(n => n)
      .ToList();
    var dependencyNames = implementation.DependencyIds
      .Select(ShortName)
      .OrderBy(n => n)
      .ToList();

    var sb = new StringBuilder();
    sb.AppendLine($"@startuml {name}");
    sb.AppendLine($"state {name} {{");
    foreach (var stateName in initialStateNames)
    {
      sb.AppendLine($"  state {stateName}");
    }
    sb.AppendLine("}");
    sb.AppendLine();
    foreach (var stateName in initialStateNames)
    {
      sb.AppendLine($"[*] --> {stateName}");
    }
    if (dependencyNames.Count > 0)
    {
      sb.AppendLine();
      sb.AppendLine($"{name} : Dependencies:");
      foreach (var dep in dependencyNames)
      {
        sb.AppendLine($"{name} : - {dep}");
      }
    }
    sb.Append("@enduml");

    return new LogicBlockOutputResult(
      FilePath: implementation.FilePath,
      Name: name,
      Content: sb.ToString()
    );
  }

  /// <summary>
  /// Looks at a logic block subclass, finds the logic block type in its
  /// inheritance hierarchy, and builds a state graph from it based on the
  /// state type given to the logic block type in the inheritance hierarchy.
  /// </summary>
  /// <param name="stateClassDecl">Logic block class declaration.</param>
  /// <param name="model">Semantic model.</param>
  /// <param name="token">Cancellation token.</param>
  /// <returns>Logic block graph.</returns>
 public StateDiagramImplementation? DiscoverStateGraph(
    TypeDeclarationSyntax stateClassDecl,
    SemanticModel model,
    CancellationToken token
  )
  {
    var filePath = stateClassDecl.SyntaxTree.FilePath;
    var destFile = Path.ChangeExtension(filePath, ".g.puml");

    Log.Print($"File path: {filePath}");
    Log.Print($"Dest file: {destFile}");

    var semanticSymbol = model.GetDeclaredSymbol(stateClassDecl, token);

    if (semanticSymbol is null)
    {
      return null;
    }

    var concreteState = semanticSymbol;

    // Search for all types that inherit from this state
    var stateSubtypes = CodeService.GetAllDerivedTypes(
      concreteState,
      model.Compilation,
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

    var root = new LogicBlockGraph(
      id: CodeService.GetNameFullyQualifiedWithoutGenerics(concreteState, concreteState.Name),
      name: concreteState.Name,
      baseId: CodeService.GetNameFullyQualifiedWithoutGenerics(concreteState, concreteState.Name)
    );

    var stateTypesById = new Dictionary<string, INamedTypeSymbol> { [root.Id] = concreteState };
    var stateGraphsById = new Dictionary<string, LogicBlockGraph> { [root.Id] = root };
    var subtypesByBaseType = new Dictionary<string, IList<INamedTypeSymbol>>();

    foreach (var subtype in stateSubtypes)
    {
      if (token.IsCancellationRequested) { return null; }

      var baseType = subtype.BaseType;
      if (baseType is null) { continue; }

      var baseTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        baseType, baseType.Name
      );

      if (!subtypesByBaseType.ContainsKey(baseTypeId))
      {
        subtypesByBaseType[baseTypeId] = [];
      }

      subtypesByBaseType[baseTypeId].Add(subtype);
    }

    LogicBlockGraph buildGraph(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
      var typeId = CodeService.GetNameFullyQualifiedWithoutGenerics(type, type.Name);
      var graph = new LogicBlockGraph(
        id: typeId,
        name: type.Name,
        baseId: CodeService.GetNameFullyQualifiedWithoutGenerics(baseType, baseType.Name)
      );

      stateTypesById[typeId] = type;
      stateGraphsById[typeId] = graph;

      if (subtypesByBaseType.TryGetValue(typeId, out var subtypes))
      {
        foreach (var subtype in subtypes)
        {
          graph.Children.Add(buildGraph(subtype, type));
        }
      }

      return graph;
    }

    if (subtypesByBaseType.TryGetValue(root.BaseId, out var rootChildren))
    {
      root.Children.AddRange(rootChildren.Select(st => buildGraph(st, concreteState)));
    }

    foreach (var state in stateGraphsById.Values)
    {
      state.Data = GetStateGraphData(stateTypesById[state.Id], model, token, concreteState);
    }

    var implementation = new StateDiagramImplementation(
      FilePath: destFile,
      Id: CodeService.GetNameFullyQualified(semanticSymbol, semanticSymbol.Name),
      Name: semanticSymbol.Name,
      Graph: root,
      StatesById: stateGraphsById.ToImmutableDictionary()
    );

    Log.Print("Graph: " + implementation.Graph);

    return implementation;
  }

  public ILogicBlockResult ConvertStateGraphToUml(StateDiagramImplementation implementation)
  {
    var transitions = new List<string>();
    foreach (var stateId in implementation.StatesById.OrderBy(id => id.Key))
    {
      var state = stateId.Value;
      foreach (var inputToStates in state.Data.InputToStates.OrderBy(id => id.Key))
      {
        var inputId = inputToStates.Key;
        foreach (var destStateId in inputToStates.Value.OrderBy(id => id))
        {
          if (!implementation.StatesById.TryGetValue(destStateId, out var dest))
          {
            continue;
          }

          transitions.Add(
            $"{state.UmlId} --> {dest.UmlId} : {state.Data.Inputs[inputId].Name}"
          );
        }
      }
    }

    transitions.Sort();

    var stateDescriptions = new List<string>();

    foreach (var stateId in implementation.StatesById.OrderBy(id => id.Key))
    {
      var state = stateId.Value;
      foreach (
        var outputContext in state.Data.Outputs.Keys.OrderBy(key => key.DisplayName)
      )
      {
        var outputs = state.Data.Outputs[outputContext]
          .Select(output => output.Name)
          .OrderBy(output => output);
        var line = string.Join(", ", outputs);
        stateDescriptions.Add($"{state.UmlId} : {outputContext.DisplayName} → {line}");
      }
    }

    stateDescriptions.Sort();

    var states = WriteStateDiagramGraph(implementation.Graph, 0);

    var sb = new StringBuilder();
    sb.AppendLine($"@startuml {implementation.Name}");
    foreach (var line in states) { sb.AppendLine(line); }

    if (transitions.Count > 0)
    {
      sb.AppendLine();
      foreach (var t in transitions) { sb.AppendLine(t); }
    }

    if (stateDescriptions.Count > 0)
    {
      sb.AppendLine();
      foreach (var d in stateDescriptions) { sb.AppendLine(d); }
    }

    sb.Append("@enduml");

    return new LogicBlockOutputResult(
      FilePath: implementation.FilePath,
      Name: implementation.Name,
      Content: sb.ToString()
    );
  }

  private IEnumerable<string> WriteStateDiagramGraph(
    LogicBlockGraph graph,
    int t
  )
  {
    var lines = new List<string>();

    var isMultilineState = graph.Children.Count > 0;

    if (isMultilineState)
    {
      lines.Add(
        $"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId} {{"
      );
    }
    else
    {
      lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId}");
    }

    foreach (var child in graph.Children.OrderBy(child => child.Name))
    {
      lines.AddRange(
        WriteStateDiagramGraph(child, t + 1)
      );
    }

    if (isMultilineState)
    { lines.Add($"{Tab(t)}}}"); }
    return lines;
  }

  public LogicBlockGraphData GetStateGraphData(
    INamedTypeSymbol type,
    SemanticModel model,
    CancellationToken token,
    INamedTypeSymbol stateBaseType
  )
  {
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
      .Select(syntaxRef => syntaxRef.GetSyntax(token)).ToList();

    // Find constructors for the type, filtering out any constructors for nested
    // types.
    var constructorNodes = syntaxNodes
      .SelectMany(syntaxNode => syntaxNode.ChildNodes())
      .OfType<ConstructorDeclarationSyntax>().ToList();

    var inputHandlerMethods = new List<MethodDeclarationSyntax>();

    var outputVisitor = new OutputVisitor(
      model, token, CodeService, OutputContexts.None
    );
    foreach (var constructor in constructorNodes)
    {
      // Collect outputs from every syntax node comprising the state type.
      outputVisitor.Visit(constructor);
    }
    outputsBuilder.AddRange(outputVisitor.OutputTypes);

    foreach (var handledInputInterface in handledInputInterfaces)
    {
      var interfaceMembers = handledInputInterface.GetMembers();
      var inputTypeSymbol = handledInputInterface.TypeArguments[0];
      if (inputTypeSymbol is not INamedTypeSymbol inputType)
      {
        continue;
      }
      if (interfaceMembers.Length == 0)
      { continue; }
      var implementation = type.FindImplementationForInterfaceMember(
        interfaceMembers[0]
      );
      if (implementation is not IMethodSymbol methodSymbol)
      {
        continue;
      }

      var onTypeItself = interfaces.Contains(handledInputInterface);

      if (!onTypeItself)
      {
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

        if (methodSymbol is null)
        {
          continue;
        }
      }

      var handlerMethodSyntaxes = methodSymbol
        .DeclaringSyntaxReferences
        .Select(syntaxRef => syntaxRef.GetSyntax(token))
        .OfType<MethodDeclarationSyntax>()
        .ToImmutableArray();

      foreach (var methodSyntax in handlerMethodSyntaxes)
      {
        inputHandlerMethods.Add(methodSyntax);
        var inputId = CodeService.GetNameFullyQualifiedWithoutGenerics(
          inputType, inputType.Name
        );
        var outputContext = OutputContexts.OnInput(inputType.Name);
        var modelForSyntax =
          model.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);
        var returnTypeVisitor = new ReturnTypeVisitor(
          modelForSyntax, token, CodeService, type, stateBaseType
        );
        outputVisitor = new OutputVisitor(
          modelForSyntax, token, CodeService, outputContext
        );

        returnTypeVisitor.Visit(methodSyntax);
        outputVisitor.Visit(methodSyntax);

        if (outputVisitor.OutputTypes.ContainsKey(outputContext))
        {
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

    foreach (var otherMethod in allOtherMethods)
    {
      Log.Print("Examining method: " + otherMethod.Identifier.Text);
      var outputContext = OutputContexts.Method(otherMethod.Identifier.Text);

      var modelForSyntax = model.Compilation.GetSemanticModel(
        otherMethod.SyntaxTree
      );

      outputVisitor = new OutputVisitor(
        modelForSyntax, token, CodeService, outputContext
      );
      outputVisitor.Visit(otherMethod);

      if (outputVisitor.OutputTypes.ContainsKey(outputContext))
      {
        outputsBuilder.Add(
          outputContext, outputVisitor.OutputTypes[outputContext]
        );
      }
    }

    var inputs = inputsBuilder.ToImmutable();

    var inputToStates = inputToStatesBuilder.ToImmutable();

    foreach (var input in inputToStates.Keys)
    {
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
