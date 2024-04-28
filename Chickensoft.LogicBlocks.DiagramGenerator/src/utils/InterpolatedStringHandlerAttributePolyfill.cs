// Polyfill the InterpolatedStringHandlerAttribute to allow us to have
// custom string interpolation handlers in netstandard2.0
//
// Surprisingly, this works!

namespace System.Runtime.CompilerServices;

using System;

[AttributeUsage(
  AttributeTargets.Class | AttributeTargets.Struct,
  AllowMultiple = false,
  Inherited = false
)]
public sealed class InterpolatedStringHandlerAttribute : Attribute;
