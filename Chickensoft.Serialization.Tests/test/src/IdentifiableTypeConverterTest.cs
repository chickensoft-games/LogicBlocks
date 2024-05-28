namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using Chickensoft.Collections;
using Chickensoft.Serialization.Tests.Fixtures;
using Chickensoft.Serializer.Tests.Fixtures;
using Shouldly;
using Xunit;

public partial class IdentifiableTypeConverterTest {
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

    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new IdentifiableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(person, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "person",
        "$v": 1,
        "age": 30,
        "name": "John Doe",
        "pet": {
          "$type": "dog",
          "$v": 1,
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

  [Fact]
  public void InitPropertiesSerialize() {
    var model = new InitPropertyModel() {
      Name = "Jane Doe",
      Age = 30,
      Descriptions = new() {
        "One",
        "Two",
        "Three"
      }
    };

    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = new SerializableTypeResolver(),
      Converters = { new IdentifiableTypeConverter(new Blackboard()) }
    };

    var json = JsonSerializer.Serialize(model, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "init_property_model",
        "$v": 1,
        "age": 30,
        "descriptions": [
          "One",
          "Two",
          "Three"
        ],
        "name": "Jane Doe"
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var deserializedModel =
      JsonSerializer.Deserialize<InitPropertyModel>(json, options);

    deserializedModel.ShouldBeEquivalentTo(model);
  }
}
