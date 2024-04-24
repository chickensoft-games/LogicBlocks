namespace Chickensoft.LogicBlocks;

/// <summary>
/// Common, non-generic base type for all logic blocks. This exists to allow
/// all logic blocks in a codebase to be identified by inspecting the derived
/// types computed from the generated type registry that the logic blocks
/// generator produces.
/// </summary>
public abstract class LogicBlockBase;
