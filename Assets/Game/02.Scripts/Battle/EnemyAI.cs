using System.Collections.Generic;
using System.Linq;

public static class EnemyAI
{
    public static BattleActorRuntime PickTarget(EnemyAIProfile profile, List<BattleActorRuntime> allies)
    {
        var aliveAllies = allies.Where(a => !a.IsDead).ToList();
        if (aliveAllies.Count == 0) return null;

        switch (profile)
        {
            case EnemyAIProfile.BasicFocusWeak:
                // HP 비율/절대값 중 택1. 여기선 절대값 낮은 대상
                return aliveAllies.OrderBy(a => a.hp).First();
            case EnemyAIProfile.BasicRandom:
            default:
                return aliveAllies[UnityEngine.Random.Range(0, aliveAllies.Count)];
        }
    }
}
