global using System.Xml;

namespace BaseClasses.A {
  using Chickensoft.Introspection;


  [Meta]
  public partial class BaseClass {
    public required string Name { get; init; }
  }
}

namespace BaseClasses.A.B.DeeplyNamespaced {
  using Chickensoft.Introspection;

  public partial class Container {
    public partial class Container2 {
      [Meta]
      public partial class DeeplyNestedBaseClass {
        public required string DeepName { get; init; }
      }
    }
  }
}

namespace BaseClasses.B {
  using BaseClasses.A;
  using Chickensoft.Introspection;

  [Meta]
  public partial class Child : BaseClass { }
}

namespace BaseClasses.A.B.UsingDeeplyNamespaced {
  using BaseClasses.A.B.DeeplyNamespaced;
  using Chickensoft.Introspection;

  [Meta]
  public partial class Child : Container.Container2.DeeplyNestedBaseClass { }

  [Meta]
  public partial class SomeChild :
    OtherNamespace.Altogether.A.B.C.D.SomeBaseClass;

  public partial class Group1 {
    public partial class Group2 {
      public partial class Child : Container.Container2.DeeplyNestedBaseClass { }
    }
  }
}

namespace AlternativeNamespace {
  using Chickensoft.Introspection;
  using OtherNamespace.Altogether;

  [Meta]
  public partial class ChildBaseNotFullyQualified : A.B.C.D.SomeBaseClass { }

  [Meta]
  public partial class ChildBaseFullyQualified :
    OtherNamespace.Altogether.A.B.C.D.SomeBaseClass { }

}

namespace OtherNamespace.Altogether {
  using Chickensoft.Introspection;

  [Meta]
  public partial class ChildBaseFullyQualified :
    OtherNamespace.Altogether.A.B.C.D.SomeBaseClass { }

  [Meta]
  public partial class ChildBaseFullyQualified2 : A.B.C.D.SomeBaseClass { }

  public partial class A {
    [Meta]
    public partial class ChildBaseFullyQualified : B.C.D.SomeBaseClass { }
    public partial class B {
      [Meta]
      public partial class ChildBaseFullyQualified : C.D.SomeBaseClass { }
      public partial class C {
        [Meta]
        public partial class ChildBaseFullyQualified : D.SomeBaseClass { }
        public partial class D {
          [Meta]
          public partial class SomeBaseClass {
            public required string Identifier { get; init; }
          }
        }
      }
    }
  }
}

namespace AliasedStuff {
  namespace Nested {
    using Chickensoft.Introspection;
    using X = OtherNamespace.Altogether.A;
    using Y = OtherNamespace.Altogether.A.B;

    [Meta]
    public partial class DirectChild : X { }

    [Meta]
    public partial class Child : X.B.C.D.SomeBaseClass { }

    [Meta]
    public partial class Child2 : Y.C.D.SomeBaseClass { }

    [Meta]
    public partial class ExtendsTypeFromSomewhereElse : System.Collections.ArrayList;
  }
}


namespace StaticUsings {
  using Chickensoft.Introspection;
  using static OtherNamespace.Altogether.A;

  [Meta]
  public partial class ChildWithStaticBaseRef : B.C.D.SomeBaseClass { }
}


namespace One {
  namespace Two {
    using Chickensoft.Introspection;

    [Meta]
    public partial class A { }
  }

  public partial class B : Two.A { }
}
