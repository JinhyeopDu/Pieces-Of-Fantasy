using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFileName = "pof_save.json";

    public static string SavePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static void Save(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveManager] Save failed: data is null.");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Saved: {SavePath}");
#endif
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("[SaveManager] Load failed: save file does not exist.");
            return null;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);

#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Loaded: {SavePath}");
#endif
        return data;
    }
}