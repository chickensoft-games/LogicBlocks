namespace Chickensoft.Serialization.Tests.Fixtures;

using Chickensoft.Introspection;

[Meta, Id("person")]
public partial record Person {
  [Save("name")]
  public string Name { get; set; } = default!;

  [Save("age")]
  public int Age { get; set; }

  [Save("pet")]
  public Pet Pet { get; set; } = default!;

  // Shouldn't be saved since no [Save] attribute is present.
  public bool Valid { get; set; } = true;
}

public enum PetType {
  Dog,
  Cat,
}

[Meta]
public abstract partial record Pet {
  [Save("name")]
  public string Name { get; set; } = "";

  public PetType Type { get; set; }
}

[Meta, Id("dog")]
public partial record Dog : Pet {
  public Dog() {
    Type = PetType.Dog;
  }

  [Save("bark_volume")]
  public int BarkVolume { get; set; }
}

[Meta, Id("cat")]
public partial record Cat : Pet {
  public Cat() {
    Type = PetType.Cat;
  }

  [Save("purr_volume")]
  public int PurrVolume { get; set; }
}
