namespace Chickensoft.Introspection.Generator.Models;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.Introspection.Generator.Utils;
using Microsoft.CodeAnalysis;

/// <summary>
/// Represents a declared type.
/// </summary>
/// <param name="Reference">Type reference, including the name, construction,
/// type parameters, and whether or not the type is partial.</param>
/// <param name="SyntaxLocation">Syntax node location (used for diagnostics).
/// </param>
/// <param name="Location">Location of the type in the source code, including
/// namespaces and containing types.</param>
/// <param name="Usings">Using directives that are in scope for the type.
/// </param>
/// <param name="Kind">Kind of the type.</param>
/// <param name="IsStatic">True if the type is static. Static types can't
/// provide generic type retrieval.</param>
/// <param name="HasIntrospectiveAttribute">True if the type was tagged with the
/// MetatypeAttribute.</param>
/// <param name="HasMixinAttribute">True if the type is tagged with the mixin
/// attribute.</param>
/// <param name="IsPublicOrInternal">True if the public or internal
/// visibility modifier was seen on the type.</param>
/// <param name="Properties">Properties declared on the type.</param>
/// <param name="Attributes">Attributes declared on the type.</param>
/// <param name="Mixins">Mixins that are applied to the type.</param>
public record DeclaredType(
  TypeReference Reference,
  Location SyntaxLocation,
  TypeLocation Location,
  ImmutableHashSet<UsingDirective> Usings,
  DeclaredTypeKind Kind,
  bool IsStatic,
  bool HasIntrospectiveAttribute,
  bool HasMixinAttribute,
  bool IsPublicOrInternal,
  ImmutableArray<DeclaredProperty> Properties,
  ImmutableArray<DeclaredAttribute> Attributes,
  ImmutableArray<string> Mixins
) {
  /// <summary>Output filename (only works for non-generic types).</summary>
  public string Filename => FullNameOpen.Replace('.', '_');

  /// <summary>
  /// Fully qualified, open generic name, as determined based on syntax nodes
  /// only.
  /// </summary>
  public string FullNameOpen => Location.Prefix + Reference.SimpleNameOpen;

  /// <summary>
  /// Fully qualified, closed generic name, as determined based on syntax nodes
  /// only.
  /// </summary>
  public string FullNameClosed => Location.Prefix + Reference.SimpleNameClosed;

  /// <summary>
  /// True if the metatype information can be generated for this type.
  /// </summary>
  public bool CanGenerateMetatypeInfo =>
    HasIntrospectiveAttribute &&
    Location.IsFullyPartialOrNotNested &&
    !IsGeneric;

  /// <summary>True if the type has a version attribute.</summary>
  public bool HasVersionAttribute =>
    VersionAttribute.Value is not null;

  /// <summary>
  /// True if the type is generic. A type is generic if it has type parameters
  /// or is nested inside any containing types that have type parameters.
  /// </summary>
  public bool IsGeneric =>
    Reference.TypeParameters.Length > 0 ||
    Location.IsInGenericType;

  /// <summary>
  /// Identifier of the type. Types tagged with the [Meta] attribute can also
  /// be tagged with the optional [Id] attribute, which allows a custom string
  /// identifier to be given as the type's id.
  /// </summary>
  public string? Id => IdAttribute?.Value?.ConstructorArgs.FirstOrDefault();

  /// <summary>
  /// Version of the type. Types tagged with the [Meta] attribute can also be
  /// tagged with the optional [Id] attribute, which allows a custom version
  /// number to be given as the type's version.
  /// </summary>
  public int Version {
    get {
      if (Kind == DeclaredTypeKind.AbstractType) {
        // Abstract types don't have versions.
        return -1;
      }

      return int.TryParse(
        VersionAttribute?.Value?.ConstructorArgs.FirstOrDefault() ?? "1",
        out var version
      ) ? version : 1;
    }
  }

  /// <summary>
  /// Whether or not the declared type was given a specific identifier.
  /// </summary>
  public bool HasId => IdAttribute.Value is not null;

  /// <summary>
  /// Validates that the DeclaredType of this type and its containing types
  /// satisfy the given predicate. Returns a list of types that do not satisfy
  /// the predicate.
  /// </summary>
  /// <param name="allTypes">Table of type full names with open generics to
  /// the declared type they represent.</param>
  /// <param name="predicate">Predicate each type must satisfy.</param>
  /// <returns>Enumerable of types that do not satisfy the predicate.</returns>
  public IEnumerable<DeclaredType> ValidateTypeAndContainingTypes(
    IDictionary<string, DeclaredType> allTypes,
    Func<DeclaredType, bool> predicate
  ) {
    // Have to reconstruct the full names of the containing types from our
    // type reference and location information.
    var fullName = Location.Namespace;
    var containingTypeFullNames = new Dictionary<TypeReference, string>();

    foreach (var containingType in Location.ContainingTypes) {
      fullName +=
        (fullName.Length == 0 ? "" : ".") +
        containingType.SimpleNameOpen;

      containingTypeFullNames[containingType] = fullName;
    }

    var typesToValidate =
      new[] { this }.Concat(Location.ContainingTypes.Select(
        (typeRef) => allTypes[containingTypeFullNames[typeRef]]
      )
    );

    return typesToValidate.Where((type) => !predicate(type));
  }

  private Lazy<DeclaredAttribute?> IntrospectiveAttribute { get; } = new(
    () => Attributes
      .FirstOrDefault(
        (attr) => attr.Name == Constants.INTROSPECTIVE_ATTRIBUTE_NAME
      )
    );

  private Lazy<DeclaredAttribute?> IdAttribute { get; } = new(
    () => Attributes
      .FirstOrDefault((attr) => attr.Name == Constants.ID_ATTRIBUTE_NAME)
    );

  private Lazy<DeclaredAttribute?> VersionAttribute { get; } = new(
    () => Attributes
      .FirstOrDefault((attr) => attr.Name == Constants.VERSION_ATTRIBUTE_NAME)
  );

  private enum DeclaredTypeState {
    Unsupported,
    Type,
    ConcreteType,
    AbstractIntrospectiveType,
    ConcreteIntrospectiveType,
    AbstractIdentifiableType,
    ConcreteIdentifiableType
  }

  private DeclaredTypeState GetState(bool knownToBeAccessibleFromGlobalScope) {
    if (Kind is DeclaredTypeKind.Interface or DeclaredTypeKind.StaticClass) {
      // Can't generate metadata about interfaces or static classes.
      return DeclaredTypeState.Unsupported;
    }

    if (!knownToBeAccessibleFromGlobalScope) {
      // Can't generate metadata about types that aren't visible from the
      // global scope.
      return DeclaredTypeState.Unsupported;
    }

    if (IsGeneric) {
      // Can't construct generic types because we wouldn't know the type
      // parameters to use.
      return DeclaredTypeState.Type;
    }

    if (HasIntrospectiveAttribute) {
      if (HasId) {
        return Kind is DeclaredTypeKind.ConcreteType
          ? DeclaredTypeState.ConcreteIdentifiableType
          : DeclaredTypeState.AbstractIdentifiableType;
      }
      return Kind is DeclaredTypeKind.ConcreteType
        ? DeclaredTypeState.ConcreteIntrospectiveType
        : DeclaredTypeState.AbstractIntrospectiveType;
    }
    // Non-generic, non-introspective type that's visible from the global scope.
    return Kind is DeclaredTypeKind.ConcreteType
      ? DeclaredTypeState.ConcreteType
      : DeclaredTypeState.Type;
  }

  /// <summary>
  /// Merge this partial type definition with another partial type definition
  /// for the same type.
  /// </summary>
  /// <param name="declaredType">Declared type representing the same type.
  /// </param>
  /// <returns>Updated representation of the declared type.</returns>
  public DeclaredType MergePartialDefinition(
    DeclaredType declaredType
  ) => new(
    Reference.MergePartialDefinition(declaredType.Reference),
    HasIntrospectiveAttribute ? SyntaxLocation : declaredType.SyntaxLocation,
    Location,
    Usings.Union(declaredType.Usings),
    Kind,
    IsStatic || declaredType.IsStatic,
    HasIntrospectiveAttribute || declaredType.HasIntrospectiveAttribute,
    HasMixinAttribute || declaredType.HasMixinAttribute,
    IsPublicOrInternal || declaredType.IsPublicOrInternal,
    Properties
      .ToImmutableHashSet()
      .Union(declaredType.Properties)
      .ToImmutableArray(),
    Attributes.Concat(declaredType.Attributes).ToImmutableArray(),
    Mixins.Concat(declaredType.Mixins).ToImmutableArray()
  );

  public bool WriteMetadata(
    IndentedTextWriter writer,
    bool knownToBeAccessibleFromGlobalScope
  ) {
    const string prefix = "Chickensoft.Introspection";
    var name = $"\"{Reference.SimpleNameClosed}\"";
    var genericTypeGetter = $"(r) => r.Receive<{FullNameClosed}>()";
    var factory = $"() => System.Activator.CreateInstance<{FullNameClosed}>()";
    var metatype = $"new {FullNameClosed}.{Constants.METATYPE_IMPL}()";
    var id = $"{Id ?? ""}";
    var version = $"{Version}";

    switch (GetState(knownToBeAccessibleFromGlobalScope)) {
      case DeclaredTypeState.Type:
        writer.Write($"new {prefix}.TypeMetadata({name})");
        return true;
      case DeclaredTypeState.ConcreteType:
        writer.Write(
          $"new {prefix}.ConcreteTypeMetadata({name}, {genericTypeGetter}, " +
          $"{factory})"
        );
        return true;
      case DeclaredTypeState.AbstractIntrospectiveType:
        writer.Write(
          $"new {prefix}.AbstractIntrospectiveTypeMetadata(" +
          $"{name}, {genericTypeGetter}, {metatype})"
        );
        return true;
      case DeclaredTypeState.ConcreteIntrospectiveType:
        writer.Write(
          $"new {prefix}.IntrospectiveTypeMetadata(" +
          $"{name}, {genericTypeGetter}, {factory}, {metatype}, {version})"
        );
        return true;
      case DeclaredTypeState.AbstractIdentifiableType:
        writer.Write(
          $"new {prefix}.AbstractIdentifiableTypeMetadata(" +
          $"{name}, {genericTypeGetter}, {metatype}, {id})"
        );
        return true;
      case DeclaredTypeState.ConcreteIdentifiableType:
        writer.Write(
          $"new {prefix}.IdentifiableTypeMetadata(" +
          $"{name}, {genericTypeGetter}, {factory}, {metatype}, {id}, " +
          $"{version})"
        );
        return true;
      case DeclaredTypeState.Unsupported:
      default:
        break;
    }
    return false;
  }

  public void WriteMetatype(IndentedTextWriter writer) {
    writer.WriteLine($"namespace {Location.Namespace};\n");

    var usings = Usings
      .Where(u => !u.IsGlobal) // Globals are universally available
      .OrderBy(u => u.TypeName)
      .ThenBy(u => u.IsGlobal)
      .ThenBy(u => u.IsStatic)
      .ThenBy(u => u.IsAlias)
      .Select(@using => @using.CodeString).ToList();

    foreach (var usingDirective in usings) {
      writer.WriteLine(usingDirective);
    }

    if (usings.Count > 0) { writer.WriteLine(); }

    // Nest our metatype inside all the containing types
    foreach (var containingType in Location.ContainingTypes) {
      writer.WriteLine($"{containingType.CodeString} {{");
      writer.Indent++;
    }

    // Apply mixin interfaces to the type.
    var mixins = Mixins.Length > 0 ? ", " + string.Join(", ", Mixins) : "";

    // Types which have been given an explicit user-defined id
    // are marked as IIdentifiable to allow the serializer to reject
    // introspective types without explicitly given id's.
    var identifiable = HasId ? $", {Constants.IDENTIFIABLE}" : "";

    var initProperties = Properties.Where(prop => prop.IsInit).ToArray();

    // Nest inside us.
    writer.WriteLine(
      $"{Reference.CodeString} : " +
      $"{Constants.INTROSPECTIVE}{identifiable}{mixins} {{"
    );
    writer.Indent++;

    // Add a mixin state bucket to the type itself.
    writer.WriteLine(
      $"public {Constants.MIXIN_BLACKBOARD} MixinState {{ get; }} = new();"
    );
    writer.WriteLine();

    // Add a metatype accessor to the type for convenience
    writer.WriteLine(
      $"public {Constants.METATYPE} Metatype " +
      "=> ((Chickensoft.Introspection.IIntrospectiveTypeMetadata)" +
      "Chickensoft.Introspection.Types.Graph.GetMetadata" +
      $"(typeof({Reference.SimpleName}))).Metatype;"
    );
    writer.WriteLine();

    writer.WriteLine(
      $"public class {Constants.METATYPE_IMPL} : {Constants.METATYPE} {{"
    );

    writer.Indent++; // metatype contents

    // Type property
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      $"public System.Type Type => typeof({Reference.SimpleNameClosed});"
    );
    // ----------------------------------------------------------------- //

    // HasInitProperties property
    // ----------------------------------------------------------------- //
    var hasInitProperties = initProperties.Any();

    writer.WriteLine(
      "public bool HasInitProperties { get; } = " +
      $"{(hasInitProperties ? "true" : "false")};"
    );
    // ----------------------------------------------------------------- //

    writer.WriteLine();

    // Properties property
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      "public System.Collections.Generic.IReadOnlyList<" +
      $"{Constants.PROPERTY_METADATA}> Properties {{ get; }} = " +
      "new System.Collections.Generic.List<" +
      $"{Constants.PROPERTY_METADATA}>() {{"
    );

    writer.WriteCommaSeparatedList(
      Properties.OrderBy(p => p.Name),
      (property) => property.Write(writer, Reference.SimpleNameClosed),
      multiline: true
    );

    writer.WriteLine("};"); // close properties list
    // ----------------------------------------------------------------- //

    writer.WriteLine();

    // Attributes property
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      "public System.Collections.Generic.IReadOnlyDictionary" +
      "<System.Type, System.Attribute[]> Attributes { get; } = " +
      "new System.Collections.Generic.Dictionary" +
      "<System.Type, System.Attribute[]>() {"
    );
    DeclaredAttribute.WriteAttributeMap(writer, Attributes);
    writer.WriteLine("};"); // close attributes dictionary
    // ----------------------------------------------------------------- //

    writer.WriteLine();

    // Mixins property
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      "public System.Collections.Generic.IReadOnlyList<System.Type> " +
      "Mixins { get; } = new System.Collections.Generic.List<System.Type>() {"
    );

    var orderedMixins = Mixins.OrderBy(m => m);

    writer.WriteCommaSeparatedList(
      orderedMixins,
      (mixin) => writer.Write($"typeof({mixin})"),
      multiline: true
    );

    writer.WriteLine("};"); // close mixins list
    // ----------------------------------------------------------------- //

    writer.WriteLine();

    // MixinHandlers property
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      "public System.Collections.Generic.IReadOnlyDictionary" +
      "<System.Type, System.Action<object>> MixinHandlers { get; } = " +
      "new System.Collections.Generic.Dictionary" +
      "<System.Type, System.Action<object>>() {"
    );

    writer.WriteCommaSeparatedList(
      orderedMixins,
      (mixin) => writer.Write(
        $"[typeof({mixin})] = (obj) => (({mixin})obj).Handler()"
      ),
      multiline: true
    );

    writer.WriteLine("};"); // close mixin handlers dictionary
    writer.WriteLine();
    // ----------------------------------------------------------------- //

    writer.WriteLine();

    // Generate constructor for init properties, if needed
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      "public object Construct(" +
      "System.Collections.Generic.IReadOnlyDictionary<string, object?>? " +
      "args = null) {"
    );
    writer.Indent++;

    if (initProperties.Length == 0) {
      if (Kind is DeclaredTypeKind.ConcreteType) {
        writer.WriteLine($"return new {Reference.SimpleNameClosed}();");
      }
      else {
        writer.WriteLine(
          "throw new System.NotImplementedException(" +
          $"\"{Reference.SimpleNameClosed} is not instantiable.\"" +
          ");"
        );
      }

      goto CLOSE_CONSTRUCT_METHOD; /* yay! a goto! */
    }

    writer.WriteLine(
      $"args = args ?? throw new System.ArgumentNullException(" +
      $"nameof(args), \"Constructing {Reference.SimpleNameClosed} requires " +
      "init args.\");"
    );
    writer.WriteLine($"return new {Reference.SimpleNameClosed}() {{");

    var propStrings = Properties
      .Where(prop => prop.HasSetter)
      .Select(
        (prop) =>
          $"{prop.Name} = ({prop.GenericType.ClosedType})args[\"{prop.Name}\"]"
      );

    writer.WriteCommaSeparatedList(
      propStrings,
      (prop) => writer.Write(prop),
      multiline: true
    );

    writer.WriteLine("};"); // close init args

    CLOSE_CONSTRUCT_METHOD:
    writer.Indent--; // close construct method
    writer.WriteLine("}");
    // ----------------------------------------------------------------- //

    // Generate constructor for init properties, if needed
    // ----------------------------------------------------------------- //
    writer.WriteLine("public override bool Equals(object obj) => true;");
    writer.WriteLine(
      "public override int GetHashCode() => base.GetHashCode();"
    );
    // ----------------------------------------------------------------- //

    writer.Indent--; // close metatype contents

    // Close nested types
    for (var i = writer.Indent; i >= 0; i--) {
      writer.WriteLine("}");
      writer.Indent--;
    }
  }
}
