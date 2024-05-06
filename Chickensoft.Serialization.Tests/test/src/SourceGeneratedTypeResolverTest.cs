namespace Chickensoft.Serialization.Tests;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Shouldly;
using Xunit;

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

public record Painting(string Title, Artist Artist, List<string> Tags, Dictionary<string, List<HashSet<int>>>? Data = null) :
  Work(Title, Artist, Tags);

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

public class SourceGeneratedTypeResolverTest {
  [Fact]
  public void SerializesWithSourceGeneration() {
    // This is just a simple test that uses the vanilla System.Text.Json
    // features. Later, we'll verify that we can mix and match the
    // Chickensoft serialization system with System.Text.Json.
    var options = new JsonSerializerOptions {
      Converters = { new JsonStringEnumConverter() },
      WriteIndented = true
    };

    options.TypeInfoResolverChain.Add(ArtContext.Default);

    var artist = new Artist("Leonardo da Vinci", 50, Medium.Paint);
    var painting = new Painting("Mona Lisa", artist, ["portrait", "famous"]);

    var defaultOptions = new JsonSerializerOptions();

    var json = JsonSerializer.Serialize<Work>(painting, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "painting",
        "Data": null,
        "Title": "Mona Lisa",
        "Artist": {
          "Name": "Leonardo da Vinci",
          "Age": 50,
          "Medium": "Paint"
        },
        "Tags": [
          "portrait",
          "famous"
        ]
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );
  }
}
