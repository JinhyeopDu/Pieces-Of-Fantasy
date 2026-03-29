using UnityEngine;

public class EnemyAnimatorSwitcher : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator; // 비우면 Root의 Animator 자동 탐색

    [Header("Controllers")]
    public RuntimeAnimatorController explorationController;
    public RuntimeAnimatorController battleController;

    void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();

        if (!animator)
            animator = GetComponentInChildren<Animator>(true);

        if (!animator)
            Debug.LogError($"[EnemyAnimatorSwitcher] Animator not found on {name}", this);
    }

    public void UseExploration()
    {
        if (!animator || !explorationController) return;
        animator.runtimeAnimatorController = explorationController;
        animator.Rebind();
        animator.Update(0f);
    }

    public void UseBattle()
    {
        if (!animator || !battleController) return;
        animator.runtimeAnimatorController = battleController;
        animator.Rebind();
        animator.Update(0f);
    }
}
