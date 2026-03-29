using System.Collections;
using UnityEngine;

public class SettingsOpenDelay : MonoBehaviour
{
    public SettingsPanelController panel;

    public void OpenDelayed()
    {
        Debug.Log("[SettingsOpenDelay] OpenDelayed called");
        if (panel == null)
        {
            Debug.LogError("[SettingsOpenDelay] panel is NULL (Inspector에 패널 연결 필요)");
            return;
        }
        StartCoroutine(OpenNextFrame());
    }

    private IEnumerator OpenNextFrame()
    {
        Debug.Log("[SettingsOpenDelay] Coroutine started");
        yield return null; // 입력이 끝난 다음 프레임
        Debug.Log("[SettingsOpenDelay] Calling panel.Open()");
        panel.Open();
    }
}