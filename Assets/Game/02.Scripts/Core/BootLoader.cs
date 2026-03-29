using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        if (GameContext.I == null)
            new GameObject("GameContext").AddComponent<GameContext>();

        AudioManager.I?.ForceApplyCurrentSettings();

        SceneManager.LoadScene("Title");
    }
}
