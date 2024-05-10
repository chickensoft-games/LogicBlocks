namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

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
