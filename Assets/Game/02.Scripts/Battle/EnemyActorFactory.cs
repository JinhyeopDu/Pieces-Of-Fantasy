using UnityEngine;

public static class EnemyActorFactory
{
    // EnemyData → BattleActorRuntime로 변환
    public static BattleActorRuntime CreateEnemy(EnemyData ed)
    {
        if (ed == null) return null;

        int level = ed.ResolveSpawnLevel();
        var cd = ed.baseStats; // CharacterData 재사용
        if (cd == null) return null;

        // 새 생성자 사용
        var actor = new BattleActorRuntime(cd, enemy: true);

        // 레벨 세팅
        actor.level = Mathf.Max(1, level);

        // 스탯 산출(임시 공식)
        // TODO: 나중에 EnemyData쪽에 공식 고정 함수로 빼는 것을 권장
        actor.maxHp = Mathf.Max(1, cd.baseHP + cd.hpPerLevel * (actor.level - 1));
        actor.hp = actor.maxHp;
        actor.sp = 0;

        actor.atk = cd.baseATK + cd.atkPerLevel * (actor.level - 1);
        actor.def = cd.baseDEF + cd.defPerLevel * (actor.level - 1);
        actor.spd = cd.baseSPD + cd.spdPerLevel * (actor.level - 1);

        // (선택) 로그
        Debug.Log($"[EnemyActorFactory] {ed.name} lv={actor.level} " +
                  $"HP={actor.hp}/{actor.maxHp} ATK={actor.atk} DEF={actor.def} SPD={actor.spd}");

        return actor;
    }
}