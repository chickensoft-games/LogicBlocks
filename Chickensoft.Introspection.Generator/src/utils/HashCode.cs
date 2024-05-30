namespace Chickensoft.Introspection.Generator.Utils;

public static class HashCode {
  private const int SEED = 1009;
  private const int FACTOR = 9176;

  // Based on https://stackoverflow.com/a/34006336.
  // HashCode is not available in netstandard2.0, so this is sufficient.
  public static int Combine(params object?[] values) {
    var hash = SEED;
    foreach (var obj in values) {
      hash = (hash * FACTOR) + (obj is null ? 0 : obj.GetHashCode());
    }
    return hash;
  }
}
