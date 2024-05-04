namespace Chickensoft.Introspection.Generator.Tests.TestCases {
  namespace TraditionalNamespace {
    public class A {
      public sealed class AA {
        public sealed class AAA;
      }
      internal sealed class AB;
      protected internal sealed class AC;
      private sealed class AD;
      private protected sealed class AE;
      protected sealed class AF;
    }

    public class A<TA>;

    internal sealed class B;

    public sealed class GA<TA>;
    public sealed class GB<TA, TB>;
    public class GC<TA, TB, TC> {
      public sealed class GCA;
      internal sealed class GCB;
      protected internal sealed class GCC;
      private sealed class GCD;
      private protected sealed class GCE;
      protected sealed class GCF;

      public class GCG<TD>;
      public class GCH<TE, TF>;
      public class GCI<TG, TH, TI>;
      public class GCI<TG, TH, TI, TJ>;
    }
  }
}
