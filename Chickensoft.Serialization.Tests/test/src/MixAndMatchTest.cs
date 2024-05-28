namespace Chickensoft.Serialization.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Chickensoft.Collections;
using Chickensoft.Serialization.Tests.Fixtures;
using DeepEqual.Syntax;
using Shouldly;
using Xunit;

public class MixAndMatchTest {
  [Fact]
  public void CanUseChickensoftModelsFromOutermostSTJModel() {
    var options = new JsonSerializerOptions {
      Converters = {
        new JsonStringEnumConverter(),
        new IdentifiableTypeConverter(new Blackboard())
      },
      WriteIndented = true,
      TypeInfoResolverChain = {
        // If mixing and matching, always provide the chickensoft type
        // resolver as the first resolver in the chain :)
        new SerializableTypeResolver(),
        // Generated serialization contexts:
        MixAndMatchContext.Default
      }
    };

    var campCounselor = new CampCounselor {
      Person = new Person {
        Name = "Alice Doe",
        Age = 30,
        Pet = new Dog {
          Name = "Fido",
          BarkVolume = 11,
        },
      },
      Activity = new Activity("Park", "Walking the dog"),
    };

    var json = JsonSerializer.Serialize(campCounselor, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "person": {
          "$type": "person",
          "$v": 1,
          "age": 30,
          "name": "Alice Doe",
          "pet": {
            "$type": "dog",
            "$v": 1,
            "bark_volume": 11,
            "name": "Fido"
          }
        },
        "activity": {
          "Place": "Park",
          "Description": "Walking the dog"
        }
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var campCounselorAgain =
      JsonSerializer.Deserialize<CampCounselor>(json, options);

    campCounselorAgain.ShouldDeepEqual(campCounselor);
  }

  [Fact]
  public void CanUseSTJModelsFromOutermostChickensoftModel() {
    var options = new JsonSerializerOptions {
      Converters = {
        new JsonStringEnumConverter(),
        new IdentifiableTypeConverter(new Blackboard())
      },
      WriteIndented = true,
      TypeInfoResolverChain = {
        // If mixing and matching, always provide the chickensoft type
        // resolver as the first resolver in the chain :)
        new SerializableTypeResolver(),
        // Generated serialization contexts:
        MixAndMatchContext.Default
      }
    };


    var campInstructor = new CampInstructor {
      Person = new Person {
        Name = "Alice Doe",
        Age = 30,
        Pet = new Dog {
          Name = "Fido",
          BarkVolume = 11,
        },
      },
      Activity = new Activity("Park", "Walking the dog"),
    };

    var json = JsonSerializer.Serialize(campInstructor, options);

    json.ShouldBe(
      /*lang=json,strict*/
      """
      {
        "$type": "camp_instructor",
        "$v": 1,
        "activity": {
          "Place": "Park",
          "Description": "Walking the dog"
        },
        "person": {
          "$type": "person",
          "$v": 1,
          "age": 30,
          "name": "Alice Doe",
          "pet": {
            "$type": "dog",
            "$v": 1,
            "bark_volume": 11,
            "name": "Fido"
          }
        }
      }
      """,
      options: StringCompareShould.IgnoreLineEndings
    );

    var campInstructorAgain =
      JsonSerializer.Deserialize<CampInstructor>(json, options);

    campInstructorAgain.ShouldDeepEqual(campInstructor);
  }
}
