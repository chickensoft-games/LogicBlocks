namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using System;
using System.Collections.Generic;
using Sync.Primitives;

public class EnemyRepo : IDisposable
{
  public IAutoValue<int> NumEnemies => _numEnemies;

  private readonly AutoValue<int> _numEnemies = new(0);

  public void Dispose()
  {
    _numEnemies.Dispose();
    GC.SuppressFinalize(this);
  }
}

public record SubscriptionLogicState : LogicBlockState
{
  public static class Input
  {
    public readonly record struct NumEnemiesChanged(int Value);
  }
}

public class SubscriptionLogic : LogicBlock
{
  public EnemyRepo EnemyRepo => Get<EnemyRepo>();

  public override void OnStart()
  {
    // called by logic blocks when starting so you can do initial setup
    // and read from your blackboard (which isn't available in constructor)
    //
    // this is called for each start
  }

  public override IEnumerable<IDisposable> OnStartSubscriptions()
  {
    // always called right after OnStart by logic blocks

    // will get disposed automatically when stopped
    yield return EnemyRepo.NumEnemies.Bind().OnValue((numEnemies) => Input(
        new SubscriptionLogicState.Input.NumEnemiesChanged(numEnemies)
      )
    );
  }

  // likewise there is OnStop and OnStopSubscriptions if you need to manually clean
  // up stuff, but the example here doesn't require those
}
