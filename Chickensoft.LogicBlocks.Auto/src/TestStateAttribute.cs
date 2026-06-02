namespace Chickensoft.LogicBlocks.Auto;

using System;

/// <summary>
/// Marks a state as a test state. Test states are skipped during
/// preallocation validation, allowing test doubles to be used without
/// requiring introspection metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestStateAttribute : Attribute;
