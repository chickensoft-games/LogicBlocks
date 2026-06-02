namespace Chickensoft.LogicBlocks.Auto;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Introspection;

using static Introspection.Types;

public partial class AutoBlock
{
  /// <summary>
  /// Cache of pristine reference states, keyed by state type. Used by the
  /// serialization system to determine whether a state has diverged from its
  /// default values and actually needs to be serialized. Only one reference
  /// state is created per type across the entire application lifetime.
  /// </summary>
  internal static ConcurrentDictionary<Type, object> ReferenceStates { get; } =
    new();

  /// <summary>
  /// Preallocates all concrete state types in the state hierarchy rooted at
  /// <typeparamref name="TState"/>. Each concrete state is instantiated and
  /// placed on the blackboard. For serializable (identifiable) logic blocks,
  /// states are also registered for serialization and validated to ensure they
  /// have the required introspection metadata.
  /// </summary>
  /// <typeparam name="TState">Base state type whose hierarchy should be
  /// preallocated.</typeparam>
  /// <exception cref="LogicBlockException" />
  public void Preallocate<TState>() where TState : LogicBlockState =>
    Preallocate(typeof(TState));

  /// <summary>
  /// Preallocates all concrete state types in the state hierarchy rooted at
  /// <paramref name="baseStateType"/>. Each concrete state is instantiated and
  /// placed on the blackboard. For serializable (identifiable) logic blocks,
  /// states are also registered for serialization and validated to ensure they
  /// have the required introspection metadata.
  /// </summary>
  /// <param name="baseStateType">Base state type whose hierarchy should be
  /// preallocated.</param>
  /// <exception cref="LogicBlockException" />
  public void Preallocate(Type baseStateType)
  {
    var logicType = GetType();
    var metadata = Graph.GetMetadata(logicType);

    // Determine if this logic block is serializable (identifiable).
    // If it is, we enforce that all concrete states are also identifiable.
    var isIdentifiable = metadata is IIdentifiableTypeMetadata;

    var descendantStateTypes = Graph.GetDescendantSubtypes(baseStateType);

    // Only allocate the validation set if we need it for serializable blocks.
    var stateTypesNeedingAttention = isIdentifiable
      ? new HashSet<Type>(descendantStateTypes.Count + 1)
      : null;

    void cacheReferenceState(Type type, IConcreteTypeMetadata concreteMetadata)
    {
      // Cache a pristine version of the state. Only done once per state type
      // (not per logic block instance). Used by the serialization system to
      // determine if it really needs to save a state.
      if (!ReferenceStates.ContainsKey(type))
      {
        ReferenceStates.TryAdd(type, concreteMetadata.Factory());
      }
    }

    void discoverState(Type type)
    {
      if (isIdentifiable)
      {
        // Serializable logic block path.
        var stateMetadata = Graph.GetMetadata(type);

        // Skip test states.
        if (
          stateMetadata is IIntrospectiveTypeMetadata iMetadata &&
          iMetadata.Metatype.Attributes.ContainsKey(typeof(TestStateAttribute))
        )
        {
          return;
        }

        if (stateMetadata is IIdentifiableTypeMetadata)
        {
          if (stateMetadata is IConcreteTypeMetadata concreteMetadata)
          {
            cacheReferenceState(type, concreteMetadata);

            // Register state for serialization. States are only saved if they
            // have diverged from the reference state.
            SaveObject(
              type: type,
              factory: concreteMetadata.Factory,
              referenceValue: ReferenceStates[type]
            );

            // Force state instance onto the blackboard. Do as much heap
            // allocation as possible during setup instead of during execution.
            OverwriteObject(type, concreteMetadata.Factory());
          }
        }
        else if (stateMetadata is not IIntrospectiveTypeMetadata)
        {
          // State is not even introspective — flag it.
          stateTypesNeedingAttention!.Add(type);
        }
        else if (stateMetadata is IConcreteTypeMetadata)
        {
          // Concrete introspective types on serializable logic blocks MUST
          // be identifiable types.
          stateTypesNeedingAttention!.Add(type);
        }

        return;
      }

      // Non-serializable logic block path.
      if (Graph.GetMetadata(type) is IConcreteTypeMetadata concreteNonSerializable)
      {
        cacheReferenceState(type, concreteNonSerializable);
        OverwriteObject(type, concreteNonSerializable.Factory());
      }
    }

    discoverState(baseStateType);

    foreach (var stateType in descendantStateTypes)
    {
      discoverState(stateType);
    }

    if (!isIdentifiable)
    { return; }

    if (stateTypesNeedingAttention!.Count == 0)
    { return; }

    var statesNeedingAttention = string.Join(", ", stateTypesNeedingAttention);

    throw new LogicBlockException(
      $"Automatic logic block `{logicType}` has states that are not " +
      $"serializable. Please ensure the following types have the " +
      $"[{nameof(MetaAttribute)}] and [{nameof(IdAttribute)}] attributes: " +
      $"{statesNeedingAttention}."
    );
  }
}
