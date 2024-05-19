namespace Chickensoft.Serialization.Tests.Fixtures;

using System.Text.Json.Serialization;
using Chickensoft.Introspection;

[JsonSerializable(typeof(Activity))]
[JsonSerializable(typeof(CampCounselor))]
public partial class MixAndMatchContext : JsonSerializerContext;

public record Activity(string Place, string Description);

public record CampCounselor {
  // Not explicitly listed as serializable on the context because it is handled
  // by the Chickensoft serialization system.
  [JsonPropertyName("person")]
  public Person Person { get; set; } = default!;

  [JsonPropertyName("activity")]
  public Activity Activity { get; set; } = default!;
}

[Meta, Id("camp_instructor")]
public partial record CampInstructor {
  [Save("person")]
  public Person Person { get; set; } = default!;

  [Save("activity")]
  public Activity Activity { get; set; } = default!;
}
