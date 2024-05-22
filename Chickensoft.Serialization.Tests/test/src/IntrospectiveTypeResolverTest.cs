namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.LogicBlocks.Serialization;
using Chickensoft.Serialization.Tests.Fixtures;
using Shouldly;
using Xunit;

public partial class IntrospectiveTypeResolverTest {
  [Fact]
  public void SerializesAndDeserializes() {
    var person = new Person {
      Name = "John Doe",
      Age = 30,
      Pet = new Dog {
        Name = "Fido",
        BarkVolume = 11,
      },
    };

    var resolver = new SerializableTypeResolver();

    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = resolver,
      Converters = { new IdentifiableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(person, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "person",
        "age": 30,
        "name": "John Doe",
        "pet": {
          "$type": "dog",
          "bark_volume": 11,
          "name": "Fido"
        }
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var deserializedPerson =
      JsonSerializer.Deserialize<Person>(json, options);

    deserializedPerson.ShouldBe(person);
  }
}
