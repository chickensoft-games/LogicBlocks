namespace Chickensoft.LogicBlocks.Generator.Tests.Types.TestCases;

using System.Text;
using JSON = System.Text.Json;
using static System.Console;
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class JunkAttribute : Attribute { }

[LogicModel("my_type")]
public partial class MyType {
  public StringBuilder MakeStringBuilder() {
    WriteLine("Making a string builder.");
    JSON.JsonSerializer.Serialize(new { Message = "Hello, world!" });
    return new StringBuilder();
  }

  [Junk]
  [Junk]
  [Save("my_property")]
  public string MyProperty { get; set; } = "";

  [Save("optional_int")]
  public int? OptionalInt { get; set; } = 10;

#pragma warning disable IDE0001
  [Save("optional_float")]
  public Nullable<float> OptionalFloat { get; set; } = 10.0f;
#pragma warning restore IDE0001
}

public partial class MyType<T> { }

public static partial class One {
  internal partial record struct Two {
    public partial interface IThree {
      public sealed partial record Four {
        [LogicModel("nested_type")]
        public sealed partial class NestedType {
          [Junk]
          [Junk]
          [Save("my_property")]
          public string MyProperty { get; set; } = "";

          [Save("optional_int")]
          public int? OptionalInt { get; set; } = 10;

#pragma warning disable IDE0001
          [Save("optional_float")]
          public Nullable<float> OptionalFloat { get; set; } = 10.0f;
#pragma warning restore IDE0001
        }
      }
    }
  }
}
