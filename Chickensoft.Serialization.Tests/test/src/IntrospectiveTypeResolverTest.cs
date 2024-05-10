namespace Chickensoft.Serialization.Tests;

using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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

    var resolver = new IntrospectiveTypeResolver();

    var options = new JsonSerializerOptions {
      WriteIndented = true,
      TypeInfoResolver = resolver
    };

    var json = JsonSerializer.Serialize(person, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "name": "John Doe",
        "age": 30,
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

  [Fact]
  public void CreatesCorrectTypeInfo() {
    var resolver = new IntrospectiveTypeResolver();
    var options = new JsonSerializerOptions {
      WriteIndented = true
    };

    var personType = typeof(Person);
    var personTypeInfo = resolver.GetTypeInfo(personType, options);

    personTypeInfo!.Properties.Select(p => p.Name).ShouldBe(
      ["name", "age", "pet"], ignoreOrder: true
    );
    personTypeInfo.Type.ShouldBe(personType);

    var petType = typeof(Pet);
    var petTypeInfo = resolver.GetTypeInfo(petType, options);

    petTypeInfo.ShouldNotBeNull();
    petTypeInfo.Type.ShouldBe(petType);
    petTypeInfo.PolymorphismOptions!.DerivedTypes.ShouldBe(
      [
        new JsonDerivedType(typeof(Dog), "dog"),
        new JsonDerivedType(typeof(Cat), "cat"),
      ],
      comparer: JsonDerivedTypeComparer.Instance,
      ignoreOrder: true
    );

    var dogType = typeof(Dog);
    var dogTypeInfo = resolver.GetTypeInfo(dogType, options);

    dogTypeInfo.ShouldNotBeNull();
    dogTypeInfo.Type.ShouldBe(dogType);

    var catType = typeof(Cat);
    var catTypeInfo = resolver.GetTypeInfo(catType, options);

    catTypeInfo.ShouldNotBeNull();
    catTypeInfo.Type.ShouldBe(catType);
  }
}
