namespace Chickensoft.Serialization.Tests.Fixtures;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum Medium {
  Paint,
  Pencil,
  Digital,
}

public record Artist(string Name, int Age, Medium Medium);

[JsonDerivedType(typeof(Painting), "painting")]
[JsonDerivedType(typeof(Drawing), "drawing")]
[JsonDerivedType(typeof(Image), "image")]
public abstract record Work(string Title, Artist Artist, List<string> Tags);

public record Painting(
  string Title,
  Artist Artist,
  List<string> Tags,
  Dictionary<string, List<HashSet<int>>>? Data = null
) : Work(Title, Artist, Tags);

public record Drawing(string Title, Artist Artist, List<string> Tags) :
  Work(Title, Artist, Tags);

public record Image(string Title, Artist Artist, List<string> Tags) :
  Work(Title, Artist, Tags);

[JsonSourceGenerationOptions(
  GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(Artist))]
[JsonSerializable(typeof(Work))]
[JsonSerializable(typeof(Medium))]
public partial class ArtContext : JsonSerializerContext;
