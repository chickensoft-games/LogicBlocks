namespace Chickensoft.Introspection.Generator.Utils;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.Introspection.Generator.Models;
using Microsoft.CodeAnalysis;

public static class Diagnostics {
  public const string ERR_PREFIX = "INTROSPECTION";
  public const string ERR_CATEGORY = "Chickensoft.Introspection.Generator";

  public static Diagnostic TypeNotVisible(
    Location location,
    string name,
    IEnumerable<DeclaredType> offendingTypes
  ) => Diagnostic.Create(
    new(
      $"{ERR_PREFIX}_000",
      "Introspective Type Global Namespace Visibility",
      messageFormat:
        "Cannot determine if the introspective type `{0}` is visible from the " +
        "global namespace. Please make sure the type **and all of its " +
        "containing types** have `public` or `internal` accessibility " +
        "modifiers in the file that contains the [" +
        Constants.INTROSPECTIVE_ATTRIBUTE_NAME + "] attribute on the type. " +
        "The following types are not indicated to be fully visible: {1}.",
      category: ERR_CATEGORY,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true
    ),
    location,
    name,
    string.Join(",", offendingTypes.Select(t => $"`{t.Reference.NameWithGenerics}`"))
  );

  public static Diagnostic TypeNotFullyPartial(
    Location location,
    string name,
    IEnumerable<DeclaredType> offendingTypes
  ) => Diagnostic.Create(
    new(
      $"{ERR_PREFIX}_001",
      "Introspective Type Not Fully Partial",
      messageFormat:
        "Introspective type `{0}` is not fully partial. Please make sure the " +
        "type **and all of its containing types** are marked as partial. " +
        "The following types still need to be marked as partial: {1}.",
      category: ERR_CATEGORY,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true
    ),
    location,
    name,
    string.Join(",", offendingTypes.Select(t => $"`{t.Reference.NameWithGenerics}`"))
  );

  public static Diagnostic TypeIsGeneric(
    Location location,
    string name,
    IEnumerable<DeclaredType> offendingTypes
  ) => Diagnostic.Create(
    new(
      $"{ERR_PREFIX}_002",
      "Introspective Type Is Generic",
      messageFormat:
        "Introspective type `{0}` cannot be generic. Please make sure the " +
        "type **and all of its containing types** are not generic. " +
        "The following types are generic: {1}.",
      category: ERR_CATEGORY,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true
    ),
    location,
    name,
    string.Join(",", offendingTypes.Select(t => $"`{t.Reference.NameWithGenerics}`"))
  );

  public static Diagnostic TypeDoesNotHaveUniqueId(
    Location location,
    string name,
    DeclaredType existingType
  ) => Diagnostic.Create(
    new(
      $"{ERR_PREFIX}_003",
      "Introspective Type Id Reused",
      messageFormat:
        "Introspective type `{0}` does not have a unique id. The following " +
        "type shares the same id: `{1}`.",
      category: ERR_CATEGORY,
      DiagnosticSeverity.Error,
      isEnabledByDefault: true
    ),
    location,
    name,
    existingType.FullName
  );
}
