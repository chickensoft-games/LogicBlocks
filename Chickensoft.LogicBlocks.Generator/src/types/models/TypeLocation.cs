
namespace Chickensoft.LogicBlocks.Generator.Types.Models;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the location of a type inside any namespaces and/or containing
/// types.
/// </summary>
/// <param name="Namespaces">Namespaces containing the type.</param>
/// <param name="ContainingTypes">Containing type names.</param>
public record TypeLocation(
  ICollection<string> Namespaces,
  ICollection<string> ContainingTypes
) {
  /// <summary>Fully resolved namespace of the type's location.</summary>
  public string Namespace => string.Join(".", Namespaces);

  /// <summary>Type prefix, used to generate the fully qualified name.</summary>
  public string Prefix {
    get {
      var prefix = FullName;
      if (prefix is not "") {
        prefix += ".";
      }
      return prefix;
    }
  }

  /// <summary>
  /// Full name of the type that represents this location.
  /// </summary>
  public string FullName =>
    string.Join(".", Namespaces.Concat(ContainingTypes));
}
