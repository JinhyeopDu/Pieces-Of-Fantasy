using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;

    [Header("Scene")]
    public string currentSceneName;
    public string returnExplorationSceneName;

    [Header("Party")]
    public List<CharacterSaveData> party = new();
    public int activePartyIndex = 0;

    [Header("Inventory")]
    public List<InventoryItemSaveData> inventory = new();

    [Header("Tutorial")]
    public bool tutorialMoveDone;
    public bool tutorialSprintDone;
    public bool tutorialCharacterOpenDone;
    public bool tutorialLevelUpDone;
    public bool tutorialSecretArtDone;
    public bool tutorialBattleDone;
    public bool tutorialInventoryDone;
    public bool tutorialQuestDone;

    [Header("Quest / Ending")]
    public bool endingShown;
    public bool allQuestsCompleted;

    [Header("Quest")]
    public string currentQuestId;
    public int currentQuestValue;
    public bool currentQuestCompleted;
    public bool currentQuestRewardClaimed;
    public List<string> completedQuestIds = new();

    [Header("Points / Battle")]
    public int secretArtPoints;
    public int secretArtPointsMax;
    public int battleSkillPoints;
    public int battleSkillPointMax;

    [Header("Optional UI / Overlay")]
    public bool inventorySortEnabled;
    public bool isUIBlockingLook;

    [Header("Optional World State")]
    public bool hasWorldPosition;
    public SerializableVector3 worldPosition;

    [Header("Respawn / Defeat")]
    public List<DefeatedSpawnSaveData> defeatedSpawns = new();
    public List<string> defeatedUniqueIds = new();
}

[Serializable]
public class CharacterSaveData
{
    public string characterId;

    public int level;
    public int exp;
    public int hp;
    public int maxHp;
    public int sp;
    public int promotionStage;

    public int atk;
    public int def;
    public int spd;

    public int permAtkAdd;
    public int permDefAdd;
    public int permSpdAdd;

    public int tempAtkAdd;
    public int tempDefAdd;
    public int tempSpdAdd;

    public bool secretArtReady;

    public List<string> tempAtkSources = new();
    public List<string> tempDefSources = new();
    public List<string> tempSpdSources = new();
    public List<string> tempMaxHpSources = new();
}

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int count;
}

[Serializable]
public class DefeatedSpawnSaveData
{
    public string spawnId;
    public float remainingSeconds;
}

[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static SerializableVector3 FromVector3(Vector3 v)
    {
        return new SerializableVector3(v.x, v.y, v.z);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}