namespace Chickensoft.Serialization.Tests;

using System.Collections.Generic;
using System.Text.Json;
using Chickensoft.Introspection;
using DeepEqual.Syntax;
using Shouldly;
using Xunit;

public partial class CollectionsTest {
  [Meta, Id("book")]
  public partial record Book {
    [Save("title")]
    public string Title { get; set; } = default!;

    [Save("author")]
    public string Author { get; set; } = default!;

    [Save("related_books")]
    public Dictionary<string, List<HashSet<string>>> RelatedBooks { get; set; }
      = default!;
  }

  [Fact]
  public void SerializesCollections() {
    var book = new Book {
      Title = "The Book",
      Author = "The Author",
      RelatedBooks = new Dictionary<string, List<HashSet<string>>> {
        ["Title A"] = new List<HashSet<string>> {
          new() { "Author A", "Author B" },
          new() { "Author C", "Author D" },
        },
        ["Title B"] = new List<HashSet<string>> {
          new() { "Author E", "Author F" },
          new() { "Author G", "Author H" },
          new()
        },
        ["Title C"] = new()
      },
    };

    var resolver = new IntrospectiveTypeResolver();

    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = resolver
    };

    var json = JsonSerializer.Serialize(book, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "title": "The Book",
        "author": "The Author",
        "related_books": {
          "Title A": [
            [
              "Author A",
              "Author B"
            ],
            [
              "Author C",
              "Author D"
            ]
          ],
          "Title B": [
            [
              "Author E",
              "Author F"
            ],
            [
              "Author G",
              "Author H"
            ],
            []
          ],
          "Title C": []
        }
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var bookAgain = JsonSerializer.Deserialize<Book>(json, options);

    bookAgain.ShouldDeepEqual(book);
  }
}
