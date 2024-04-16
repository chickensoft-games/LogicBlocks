namespace Chickensoft.LogicBlocks;

using System;

/// <summary>
/// Indicates that a reference type is a model. Logic models are partial
/// classes or record classes whose property information will be generated at
/// compile-time by the LogicBlocks generator to enable serialization in
/// ahead-of-time (AOT) environments without reflection, using System.Text.Json.
/// <br />
/// While System.Text.Json has its own source generators, it requires that you
/// register types somewhere other than on the type itself, which makes creating
/// composable type hierarchies a pain and very error-prone. The LogicBlocks
/// generator allows you to work around this by tagging types with this
/// attribute and generating the necessary information System.Text.Json needs
/// to serialize and deserialize the type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LogicModelAttribute : Attribute {
  /// <summary>
  /// The identifier of the model to be used as the type discriminator during
  /// serialization and deserialization.
  /// </summary>
  public string Id { get; }

  /// <summary>
  /// Creates a new instance of the <see cref="LogicModelAttribute"/> class
  /// with the specified model identifier. Models represent groups of
  /// serializable data.
  /// </summary>
  /// <param name="id">The identifier of the model to be used as the type
  /// discriminator during serialization and deserialization.</param>
  public LogicModelAttribute(string id) {
    Id = id;
  }
}
