namespace Chickensoft.LogicBlocks.Tests.TestUtils;

public static class Utils {
  public static void ClearWeakReference(WeakReference weakReference) {
    weakReference.Target = null;
    GC.Collect();
    GC.WaitForPendingFinalizers();
  }
}
