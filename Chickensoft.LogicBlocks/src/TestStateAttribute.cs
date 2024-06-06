namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Represents a logic block state that's only used for testing purposes. This
/// can be placed on test states that extend abstract states you wish to test.
/// </summary>
[AttributeUsage(
  AttributeTargets.Class, AllowMultiple = false, Inherited = true
)]
public partial class TestStateAttribute : Attribute;
