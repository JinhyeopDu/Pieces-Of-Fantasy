using System.Collections;
using TMPro;
using UnityEngine;

public class BattleCenterMessageUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float defaultDuration = 1.2f;

    private Coroutine showCo;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        root.SetActive(false);
    }

    public void ShowMessage(string message, float duration = -1f)
    {
        if (root == null)
            root = gameObject;

        if (messageText != null)
            messageText.text = message;

        if (showCo != null)
        {
            StopCoroutine(showCo);
            showCo = null;
        }

        root.SetActive(true);
        showCo = StartCoroutine(ShowCo(duration > 0f ? duration : defaultDuration));
    }

    private IEnumerator ShowCo(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (root != null)
            root.SetActive(false);

        showCo = null;
    }

    public void HideImmediate()
    {
        if (showCo != null)
        {
            StopCoroutine(showCo);
            showCo = null;
        }

        if (root != null)
            root.SetActive(false);
    }
}