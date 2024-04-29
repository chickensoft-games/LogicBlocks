namespace Chickensoft.LogicBlocks.Generator;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Chickensoft.LogicBlocks.Generator.Types.Models;
using Chickensoft.LogicBlocks.Generator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// This generator exists to list types in the developer's codebase for use
/// with polymorphic serialization and deserialization or automatic state
/// creation and registration.
/// <br />
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-8-0#configure-polymorphism-with-the-contract-model
/// <br />
/// Additionally, JSON Serialization can be tested by disabling Reflection:
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0#disable-reflection-defaults
/// <br />
/// For background on AOT/iOS Environments and STJ:
/// https://github.com/dotnet/runtime/issues/31326
/// </summary>
[Generator]
public class TypeGenerator : IIncrementalGenerator {
  public void Initialize(IncrementalGeneratorInitializationContext context) {
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
    //   Thread.Sleep(500);
    // }
    // System.Diagnostics.Debugger.Break();
    // --------------------------------------------------------------------- //

    // Because of partial type declarations, we may need to combine some
    // type declarations into one.
    var incrementalGenerationData = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsTypeCandidate,
      transform: ResolveDeclaredTypeInfo
    )
    .Collect()
    .Select((declaredTypes, _) => {
      var typesByFullName = declaredTypes
        .GroupBy((type) => type.FullName);

      var uniqueTypes = typesByFullName
        .Select(
          // Combine non-unique type entries together.
          group => group.Aggregate(
            (DeclaredType typeA, DeclaredType typeB) =>
              typeA.MergePartialDefinition(typeB)
          )
        )
        .OrderBy(type => type.FullName) // Sort for deterministic output
        .ToDictionary(
          g => g.FullName,
          g => g
        );

      var tree = new TypeResolutionTree();
      tree.AddDeclaredTypes(uniqueTypes);

      var visibleTypeIds = tree.GetVisibleTypes();
      var concreteVisibleTypeIds = tree.GetVisibleTypes(
        predicate:
          static (type) => type.IsConcrete && type.OpenGenerics.Length == 0,
        searchGenericTypes: false
      );

      var visibleTypesBuilder =
        ImmutableDictionary.CreateBuilder<string, DeclaredType>();
      var concreteVisibleTypesBuilder =
        ImmutableDictionary.CreateBuilder<string, DeclaredType>();
      var metatypesBuilder =
        ImmutableDictionary.CreateBuilder<string, DeclaredType>();
      var mixinsBuilder =
        ImmutableDictionary.CreateBuilder<string, DeclaredType>();

      // Build up relevant registries to enable convenient and performant
      // introspection.
      foreach (var type in uniqueTypes.Values) {
        if (visibleTypeIds.Contains(type.FullName)) {
          visibleTypesBuilder.Add(type.FullName, type);
        }

        if (concreteVisibleTypeIds.Contains(type.FullName)) {
          concreteVisibleTypesBuilder.Add(type.FullName, type);
        }

        if (type.CanGenerateMetatypeInfo) {
          metatypesBuilder.Add(type.FullName, type);
        }

        if (type.HasMixinAttribute) {
          mixinsBuilder.Add(type.FullName, type);
        }
      }

      return new GenerationData(
        Metatypes: metatypesBuilder.ToImmutable(),
        VisibleTypes: visibleTypesBuilder.ToImmutable(),
        ConcreteVisibleTypes: concreteVisibleTypesBuilder.ToImmutable(),
        Mixins: mixinsBuilder.ToImmutable()
      );
    });

    context.RegisterSourceOutput(
      source: incrementalGenerationData,
      action: static (
        SourceProductionContext context,
        GenerationData data
      ) => {
        GenerateTypeRegistry(context, data);
        OutputMetatypesAndReportDiagnostics(context, data);
      }
    );
  }

  public static void OutputMetatypesAndReportDiagnostics(
    SourceProductionContext context,
    GenerationData data
  ) {
    // A metatype is a class generated by the source generator that contains
    // information about the class it is generated inside of.
    foreach (var type in data.VisibleTypes.Values) {
      foreach (var diagnostic in type.Diagnostics) {
        context.ReportDiagnostic(diagnostic);
      }

      if (!type.CanGenerateMetatypeInfo) {
        continue;
      }

      var usings = type.Usings
        .Where(u => !u.IsGlobal) // Globals are universally available
        .OrderBy(u => u.TypeName)
        .ThenBy(u => u.IsGlobal)
        .ThenBy(u => u.IsStatic)
        .ThenBy(u => u.IsAlias)
        .Select(@using => @using.CodeString).ToList();

      var code = CreateCodeWriter();

      WriteFileStart(code);

      code.WriteLine($"namespace {type.Location.Namespace};\n");

      foreach (var usingDirective in usings) {
        code.WriteLine(usingDirective);
      }

      if (usings.Count > 0) { code.WriteLine(); }

      // Nest it inside all the containing types
      foreach (var containingType in type.Location.ContainingTypes) {
        code.WriteLine($"{containingType.CodeString} {{");
        code.Indent++;
      }

      // Apply mixin interfaces to the type.
      var mixins = type.Mixins.Length > 0
        ? ", " + string.Join(", ", type.Mixins)
        : "";

      // Nest it inside the type itself
      code.WriteLine(
        $"{type.Reference.CodeString} : " +
        $"{Constants.INTROSPECTIVE}{mixins} {{"
      );
      code.Indent++;

      // Add a mixin state bucket to the type itself.
      code.WriteLine(
        $"public {Constants.DATA_TABLE} MixinState {{ get; }} = new();"
      );
      code.WriteLine();

      // Add a metatype accessor to the type for convenience
      code.WriteLine(
        $"public {Constants.METATYPE} Metatype " +
        $"=> TypeRegistry.Instance.Metatypes[typeof({type.Reference.Name})];"
      );
      code.WriteLine();

      code.WriteLine(
        $"public record {Constants.METATYPE_IMPL} : {Constants.METATYPE} {{"
      );
      code.Indent++;
      OutputMetatypeInformation(type, code);
      code.Indent--;

      // Close braces
      for (var i = code.Indent; i >= 0; i--) {
        code.WriteLine("}");
        code.Indent--;
      }

      WriteFileEnd(code);

      context.AddSource(
        hintName: $"{type.Filename}.g.cs",
        source: code.InnerWriter.ToString()
      );
    }
  }

  private static void WriteFileStart(IndentedTextWriter code) {
    code.WriteLine("#pragma warning disable");
    code.WriteLine("#nullable enable");
  }

  private static void WriteFileEnd(IndentedTextWriter code) {
    code.WriteLine("#nullable restore");
    code.WriteLine("#pragma warning restore");
  }

  private static void OutputMetatypeInformation(
    DeclaredType type, IndentedTextWriter code
  ) {
    var fullName = type.Reference.NameWithOpenGenerics;

    GenerateMetatypeId(type.Id, code);
    code.WriteLine();
    GenerateProperties(type, code);
    code.WriteLine();
    GenerateMetatypeAttributes(type.Attributes, code);
    code.WriteLine();
    GenerateMixins(type, code);
    code.WriteLine();
    GenerateMixinHandlers(type, code);
  }

  public static void GenerateMixins(
    DeclaredType type,
    IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IReadOnlyList<System.Type> Mixins { get; } = new System.Collections.Generic.List<System.Type>() {");
    code.Indent++;
    GenerateMixinsEntries(type.Mixins, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  public static void GenerateMixinsEntries(
    ImmutableArray<string> mixins, IndentedTextWriter code
  ) {
    for (var i = 0; i < mixins.Length; i++) {
      var isLast = i == mixins.Length - 1;
      var mixin = mixins[i];
      code.Write($"typeof({mixin})");
      if (!isLast) { code.Write(","); }
      code.WriteLine();
    }
  }

  private static void GenerateMixinHandlers(
    DeclaredType type, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IReadOnlyDictionary<System.Type, System.Action<object>> MixinHandlers { get; } = new System.Collections.Generic.Dictionary<System.Type, System.Action<object>>() {");
    code.Indent++;
    AddMixinHandlerEntries(type, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  public static void GenerateMetatypeId(string id, IndentedTextWriter code) =>
    code.WriteLine($"public string Id => {id};");

  public static void GenerateMetatypeAttributes(
    ImmutableArray<DeclaredAttribute> attributes, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IReadOnlyDictionary<System.Type, System.Attribute[]> Attributes { get; } = new System.Collections.Generic.Dictionary<System.Type, System.Attribute[]>() {");

    GenerateAttributeMapping(attributes, code);

    code.WriteLine("};");
  }

  public static void GenerateProperties(
    DeclaredType type, IndentedTextWriter code
  ) {
    code.WriteLine(
      "public System.Collections.Generic.IReadOnlyList<" +
      $"{Constants.PROPERTY_METADATA}> Properties {{ get; }} = " +
      "new System.Collections.Generic.List<" +
      $"{Constants.PROPERTY_METADATA}>() {{"
    );

    for (var i = 0; i < type.Properties.Length; i++) {
      var isLast = i == type.Properties.Length - 1;

      var property = type.Properties[i];

      code.Indent++;
      GeneratePropertyMetadata(type, property, code);
      code.Indent--;

      if (!isLast) { code.Write(","); }

      code.WriteLine();
    }

    code.WriteLine("};");
  }

  public static void GeneratePropertyMetadata(
    DeclaredType type, DeclaredProperty property, IndentedTextWriter code
  ) {
    code.WriteLine($"new {Constants.PROPERTY_METADATA}(");
    code.Indent++;

    var propertyValue = "value" + (property.IsNullable ? "" : "!");

    code.WriteLine($"Name: \"{property.Name}\",");
    code.WriteLine($"Type: typeof({property.Type}),");
    code.WriteLine($"Getter: (object obj) => (({type.Reference.Name})obj).{property.Name},");
    code.WriteLine($"Setter: (object obj, object? value) => (({type.Reference.Name})obj).{property.Name} = ({property.Type}){propertyValue},");
    code.WriteLine($"GenericTypeGetter: (ITypeReceiver receiver) => receiver.Receive<{property.Type}>(),");
    code.WriteLine("AttributesByType: new System.Collections.Generic.Dictionary<System.Type, System.Attribute[]>() {");

    GenerateAttributeMapping(property.Attributes, code);

    code.WriteLine("}");

    code.Indent--;

    code.Write(")");
  }

  public static void GenerateAttributeMapping(
    ImmutableArray<DeclaredAttribute> attributeUsages,
    IndentedTextWriter code
  ) {
    var attributesByName = attributeUsages
      .GroupBy(attr => attr.Name)
      .ToDictionary(
        group => group.Key,
        group => group.ToImmutableArray()
      );

    code.Indent++;

    var i = 0;

    foreach (var attribute in attributesByName.Keys) {
      var attributes = attributesByName[attribute];
      var isLast = i == attributesByName.Count - 1;

      code.WriteLine($"[typeof({attribute}Attribute)] = new System.Attribute[] {{");

      code.Indent++;
      GenerateAttributeMappingEntry(attributes, code);
      code.Indent--;

      code.Write("}");
      if (!isLast) { code.Write(","); }
      code.WriteLine();

      i++;
    }

    code.Indent--;
  }

  public static void GenerateAttributeMappingEntry(
    ImmutableArray<DeclaredAttribute> attributes, IndentedTextWriter code
  ) {
    var i = 0;

    foreach (var attribute in attributes) {
      var isLast = i == attributes.Length - 1;

      code.Write($"new {attribute.Name}Attribute(");
      code.Write(string.Join(", ", attribute.ConstructorArgs));
      code.Write(")");
      if (attribute.InitializerArgs.Length > 0) {
        code.Write(" { ");
        code.Write(string.Join(", ", attribute.InitializerArgs));
        code.Write(" }");
      }

      if (!isLast) { code.Write(","); }
      code.WriteLine();

      i++;
    }
  }

  public static void GenerateTypeRegistry(
    SourceProductionContext context,
    GenerationData data
  ) {
    var code = CreateCodeWriter();

    WriteFileStart(code);

    code.WriteLine(
      "public partial class TypeRegistry : " +
      $"{Constants.TYPE_REGISTRY_INTERFACE} {{"
    );
    code.WriteLine();

    code.Indent++;
    code.WriteLine(
      $"public static {Constants.TYPE_REGISTRY_INTERFACE} Instance " +
      "{ get; } = new TypeRegistry();"
    );
    code.WriteLine();
    CreateVisibleTypesProperty(data.VisibleTypes, code);
    CreateConcreteVisibleTypesProperty(data.ConcreteVisibleTypes, code);
    CreateMetatypesProperty(data.Metatypes, code);
    CreateModuleInitializer(code);
    code.Indent--;
    code.WriteLine("}");

    WriteFileEnd(code);

    context.AddSource(
      hintName: "TypeRegistry.g.cs",
      source: code.InnerWriter.ToString()
    );
  }

  public static void CreateModuleInitializer(IndentedTextWriter code) {
    code.WriteLine("[System.Runtime.CompilerServices.ModuleInitializer]");
    code.WriteLine(
      "internal static void Initialize() => " +
      $"{Constants.TYPES_GRAPH}.Register(TypeRegistry.Instance);"
    );
  }

  public static DeclaredType ResolveDeclaredTypeInfo(
    GeneratorSyntaxContext context, CancellationToken _
  ) {
    var typeDecl = (TypeDeclarationSyntax)context.Node;

    var name = typeDecl.Identifier.ValueText;
    var construction = GetConstruction(typeDecl);
    var isPartial = IsPartial(typeDecl);
    var typeParameters = GetTypeParameters(typeDecl);

    var reference = new TypeReference(
      Name: name,
      Construction: construction,
      IsPartial: isPartial,
      TypeParameters: typeParameters
    );

    var location = GetLocation(typeDecl);
    var kind = GetKind(typeDecl);
    var hasIntrospectiveAttribute = HasIntrospectiveAttribute(typeDecl);
    var hasMixinAttribute = HasMixinAttribute(typeDecl);
    var isTopLevelAccessible = IsTopLevelAccessible(typeDecl);

    var diagnostics = new HashSet<Diagnostic>();

    if (
      hasIntrospectiveAttribute && (
        !isTopLevelAccessible || // Must be top-level accessible
        !isPartial || // Must be partial
        typeParameters.Length > 0 // Must be non-generic
      )
    ) {
      diagnostics.Add(
        Diagnostics.InvalidIntrospectiveType(
          typeDecl,
          name
        )
      );
    }

    var usings = GetUsings(typeDecl);
    var properties = GetProperties(typeDecl);
    var attributes = GetAttributes(typeDecl.AttributeLists);
    var mixins = GetMixins(typeDecl);

    return new DeclaredType(
      Reference: reference,
      Location: location,
      Usings: usings,
      Kind: kind,
      HasIntrospectiveAttribute: hasIntrospectiveAttribute,
      HasMixinAttribute: hasMixinAttribute,
      IsTopLevelAccessible: isTopLevelAccessible,
      Properties: properties,
      Attributes: attributes,
      Mixins: mixins,
      Diagnostics: diagnostics.ToImmutableHashSet()
    );
  }

  private static void CreateVisibleTypesProperty(
    ImmutableDictionary<string, DeclaredType> types, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IReadOnlyDictionary<System.Type, string> VisibleTypes { get; } = new System.Collections.Generic.Dictionary<System.Type, string>() {");
    code.Indent++;
    AddTypeEntries(types, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  private static void CreateConcreteVisibleTypesProperty(
    ImmutableDictionary<string, DeclaredType> types, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IReadOnlyDictionary<System.Type, System.Func<object>> ConcreteVisibleTypes { get; } = new System.Collections.Generic.Dictionary<System.Type, System.Func<object>>() {");
    code.Indent++;
    AddConcreteTypeEntries(types, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  private static void CreateMetatypesProperty(
    ImmutableDictionary<string, DeclaredType> types, IndentedTextWriter code
  ) {
    code.WriteLine(
      "public System.Collections.Generic.IReadOnlyDictionary<System.Type, " +
      $"{Constants.METATYPE}> Metatypes {{ get; }} = new " +
      "System.Collections.Generic.Dictionary<System.Type, " +
      $"{Constants.METATYPE}>() {{"
    );
    code.Indent++;
    AddMetatypeEntries(types, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  private static void AddTypeEntries(
    ImmutableDictionary<string, DeclaredType> types, IndentedTextWriter code
  ) {
    var i = 0;
    foreach (var type in types.Values) {
      var typeName = type.FullName;
      var isLast = i == types.Count - 1;
      code.WriteLine($"[typeof({typeName})] = \"{type.Reference.NameWithOpenGenerics}\"{(isLast ? "" : ",")}");
      i++;
    }
  }

  private static void AddConcreteTypeEntries(
    ImmutableDictionary<string, DeclaredType> types,
    IndentedTextWriter code
  ) {
    var i = 0;

    foreach (var type in types.Values) {
      var typeName = type.FullName;
      var isLast = i == types.Count - 1;
      code.WriteLine(
        $"[typeof({typeName})] = () => " +
        $"System.Activator.CreateInstance<{typeName}>(){(isLast ? "" : ",")}"
      );
      i++;
    }
  }

  private static void AddMetatypeEntries(
    ImmutableDictionary<string, DeclaredType> types,
    IndentedTextWriter code
  ) {
    var i = 0;
    foreach (var type in types.Values) {
      var typeName = type.FullName;
      var isLast = i == types.Count - 1;
      code.WriteLine(
        $"[typeof({typeName})] = " +
        $"new {typeName}.{Constants.METATYPE_IMPL}(){(isLast ? "" : ",")}"
      );
      i++;
    }
  }

  private static void AddMixinHandlerEntries(
    DeclaredType type,
    IndentedTextWriter code
  ) {
    var i = 0;
    foreach (var mixinType in type.Mixins) {
      var isLast = i == type.Mixins.Length - 1;
      code.WriteLine(
        $"[typeof({mixinType})] = (obj) => " +
        $"(({mixinType})obj).Handler(){(isLast ? "" : ",")}"
      );
      i++;
    }
  }

  // We identify all type declarations and filter them out later by visibility
  // based on all the information about the type from any partial declarations
  // of the same type that we discover, as well as visibility information about
  // any containing types.
  public static bool IsTypeCandidate(SyntaxNode node, CancellationToken _) =>
      node is TypeDeclarationSyntax;

  public static DeclaredTypeKind GetKind(TypeDeclarationSyntax typeDecl) {
    if (typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword)) {
      // We know abstract types aren't interfaces or static classes.
      return DeclaredTypeKind.AbstractType;
    }
    if (typeDecl is ClassDeclarationSyntax classDecl) {
      return classDecl.Modifiers.Any(SyntaxKind.StaticKeyword)
        ? DeclaredTypeKind.StaticClass
        : DeclaredTypeKind.ConcreteType;
    }
    else if (typeDecl is InterfaceDeclarationSyntax) {
      return DeclaredTypeKind.Interface;
    }
    return DeclaredTypeKind.ConcreteType;
  }

  public static Construction GetConstruction(TypeDeclarationSyntax typeDecl) {
    if (typeDecl is ClassDeclarationSyntax classDecl) {
      return classDecl.Modifiers.Any(SyntaxKind.StaticKeyword)
        ? Construction.StaticClass
        : Construction.Class;
    }
    else if (typeDecl is InterfaceDeclarationSyntax) {
      return Construction.Interface;
    }
    else if (typeDecl is RecordDeclarationSyntax recordDecl) {
      return recordDecl.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
        ? Construction.RecordStruct
        : Construction.RecordClass;
    }
    return Construction.Class;
  }

  public static ImmutableArray<string> GetTypeParameters(
    TypeDeclarationSyntax typeDecl
  ) =>
    typeDecl.TypeParameterList?.Parameters
      .Select(p => p.Identifier.ValueText)
      .ToImmutableArray()
      ?? ImmutableArray<string>.Empty;

  /// <summary>
  /// True if the type declaration is explicitly marked as visible at the
  /// top-level of the project. Doesn't check containing types, so this alone
  /// is not sufficient to determine overall visibility.
  /// </summary>
  /// <param name="typeDecl">Type declaration syntax.</param>
  /// <returns>True if marked as `public` or `internal`.</returns>
  public static bool IsTopLevelAccessible(TypeDeclarationSyntax typeDecl) =>
    typeDecl.Modifiers.Any(m =>
      m.IsKind(SyntaxKind.PublicKeyword) ||
      m.IsKind(SyntaxKind.InternalKeyword)
    );

  public static bool IsPartial(TypeDeclarationSyntax typeDecl) =>
    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

  public static bool HasIntrospectiveAttribute(
    TypeDeclarationSyntax typeDecl
  ) => HasAttributeNamed(typeDecl, Constants.INTROSPECTIVE_ATTRIBUTE_NAME);

  public static bool HasMixinAttribute(TypeDeclarationSyntax typeDecl) =>
    HasAttributeNamed(typeDecl, Constants.MIXIN_ATTRIBUTE_NAME);

  private static bool HasAttributeNamed(
    TypeDeclarationSyntax typeDecl, string name
  ) =>
    typeDecl.AttributeLists.Any(
      list => list.Attributes.Any(attr => attr.Name.ToString() == name)
    );

  /// <summary>
  /// Determines where a type is located within the source code.
  /// <br />
  /// https://stackoverflow.com/a/61409409
  /// </summary>
  /// <param name="source">Type declaration syntax.</param>
  /// <returns>Fully qualified name.</returns>
  /// <exception cref="ArgumentNullException />
  public static TypeLocation GetLocation(TypeDeclarationSyntax source) {
    var namespaces = new LinkedList<string>();
    var types = new LinkedList<TypeReference>();
    for (
      var parent = source.Parent; parent is not null; parent = parent.Parent
    ) {
      if (parent is BaseNamespaceDeclarationSyntax @namespace) {
        foreach (
          var namespacePart in @namespace.Name.ToString().Split('.').Reverse()
        ) {
          namespaces.AddFirst(namespacePart);
        }
      }
      else if (parent is TypeDeclarationSyntax type) {
        var typeParameters = type.TypeParameterList?.Parameters
            .Select(p => p.Identifier.ValueText)
            .ToImmutableArray()
            ?? ImmutableArray<string>.Empty;

        var construction = GetConstruction(type);
        var isPartial = IsPartial(type);

        var containingType = new TypeReference(
          Name: type.Identifier.ValueText,
          Construction: construction,
          IsPartial: isPartial,
          TypeParameters: typeParameters
        );

        types.AddFirst(containingType);
      }
    }

    return new TypeLocation(namespaces, types);
  }

  public static ImmutableHashSet<UsingDirective> GetUsings(
    TypeDeclarationSyntax type
  ) {
    var allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
    foreach (var parent in type.Ancestors(false)) {
      if (parent is BaseNamespaceDeclarationSyntax ns) {
        allUsings = allUsings.AddRange(ns.Usings);
      }
      else if (parent is CompilationUnitSyntax comp) {
        allUsings = allUsings.AddRange(comp.Usings);
      }
    }
    return allUsings
      .Select(@using => new UsingDirective(
          Alias: @using.Alias?.Name.NormalizeWhitespace().ToString(),
          TypeName: @using.Name.NormalizeWhitespace().ToString(),
          IsGlobal: @using.GlobalKeyword is { ValueText: "global" },
          IsStatic: @using.StaticKeyword is { ValueText: "static" },
          IsAlias: @using.Alias != default
        )
      )
      .ToImmutableHashSet();
  }

  public static ImmutableArray<DeclaredProperty> GetProperties(
    TypeDeclarationSyntax type
  ) {
    var properties = ImmutableArray.CreateBuilder<DeclaredProperty>();
    foreach (var property in type.Members.OfType<PropertyDeclarationSyntax>()) {
      var isPartial = property.Modifiers.Any(SyntaxKind.PartialKeyword);

      if (isPartial) { continue; } // Partial properties are unsupported.

      var propertyAttributes = GetAttributes(property.AttributeLists);

      if (propertyAttributes.Length == 0) {
        // Only record information about properties marked with attributes.
        continue;
      }

      var hasSetter = property.AccessorList?.Accessors
        .Any(accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
        ?? false;

      var isNullable =
        property.Type is NullableTypeSyntax ||
        (property.Type is GenericNameSyntax generic && generic.Identifier.ValueText == "Nullable");

      properties.Add(
        new DeclaredProperty(
          Name: property.Identifier.ValueText,
          Type: property.Type.NormalizeWhitespace().ToString(),
          HasSetter: hasSetter,
          IsNullable: isNullable,
          Attributes: propertyAttributes
        )
      );
    }
    return properties.ToImmutable();
  }

  public static ImmutableArray<string> GetMixins(
    TypeDeclarationSyntax typeDecl
  ) {
    var mixins = ImmutableArray.CreateBuilder<string>();

    foreach (var attributeList in typeDecl.AttributeLists) {
      foreach (var attr in attributeList.Attributes) {
        if (attr.Name.ToString() == Constants.INTROSPECTIVE_ATTRIBUTE_NAME) {
          mixins.AddRange(
            attr.ArgumentList?.Arguments
              .Select(arg => arg.Expression)
              .OfType<TypeOfExpressionSyntax>()
              .Select(arg => arg.Type.NormalizeWhitespace().ToString())
              .ToImmutableArray() ?? ImmutableArray<string>.Empty
          );
        }
      }
    }

    return mixins.ToImmutable();
  }

  public static ImmutableArray<DeclaredAttribute> GetAttributes(
    SyntaxList<AttributeListSyntax> attributeLists
  ) {
    var attributes = ImmutableArray.CreateBuilder<DeclaredAttribute>();

    foreach (var attr in attributeLists) {
      foreach (var arg in attr.Attributes) {
        var initializerArgs = ImmutableArray.CreateBuilder<string>();
        var constructorArgs = ImmutableArray.CreateBuilder<string>();
        var name = arg.Name.NormalizeWhitespace().ToString();

        foreach (
          var argExpr in arg.ArgumentList?.Arguments ??
            SyntaxFactory.SeparatedList<AttributeArgumentSyntax>()
        ) {
          var exp = argExpr.Expression.NormalizeWhitespace().ToString();

          if (argExpr.NameEquals is { } nameEquals) {
            initializerArgs.Add($"{nameEquals.Name.NormalizeWhitespace()} = {exp}");
          }
          else {
            constructorArgs.Add(exp);
          }
        }

        attributes.Add(new DeclaredAttribute(
          Name: name,
          ConstructorArgs: constructorArgs.ToImmutable(),
          InitializerArgs: initializerArgs.ToImmutable()
        ));
      }
    }

    return attributes.ToImmutable();
  }

  // public static ImmutableArray<AttributeUsage> GetAttributes(
  //   SyntaxList<AttributeListSyntax> attributeLists
  // ) => attributeLists
  //   .SelectMany(list => list.Attributes)
  //   .Select(attr => new AttributeUsage(
  //     Name: attr.Name.NormalizeWhitespace().ToString(),
  //     ArgExpressions: attr.ArgumentList?.Arguments
  //       .Select(
  //         arg => arg.NameEquals is { } nameEquals
  //           ? $"{nameEquals.Name.NormalizeWhitespace()}: {arg.Expression.NormalizeWhitespace()}"
  //           : arg.NormalizeWhitespace().ToString()
  //       )
  //       .ToImmutableArray()
  //       ?? ImmutableArray<string>.Empty
  //   ))
  //   .ToImmutableArray();

  public static IndentedTextWriter CreateCodeWriter() =>
    new(new StringWriter(), "  ");
}
