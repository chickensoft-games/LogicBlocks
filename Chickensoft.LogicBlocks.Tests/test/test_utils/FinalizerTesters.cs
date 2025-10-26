namespace Chickensoft.LogicBlocks.Tests.TestUtils;

using System;

public static class Utils
{
  public static void ClearWeakReference(WeakReference weakReference)
  {
    weakReference.Target = null;
    GC.Collect();
    GC.WaitForPendingFinalizers();
  }
}
