namespace Chickensoft.LogicBlocks.Tests;

using System.Runtime.CompilerServices;

public static class Utils
{
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static WeakReference<T> MakeWeakRef<T>() where T : class, new() =>
    new (new T(), true);
}
