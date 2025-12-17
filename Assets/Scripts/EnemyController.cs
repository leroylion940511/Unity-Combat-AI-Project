/*
 * ----------------------------------------------------------------------------
 * 腳本名稱：EnemyController.cs
 * 專案名稱：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
 * 作者：張志晨 (B11202076) - 中華大學資工系
 * 日期：2025/12/16
 * * 功能描述：
 * 本腳本負責「敵人 AI」的行為邏輯，模擬一個基礎的戰鬥對手：
 * 1. 自動導航：計算與玩家距離，過遠則追蹤，進入攻擊範圍則停止或繞圈。
 * 2. 主動攻擊：內建計時器，時間到後隨機選擇攻擊動作並向 GameManager 發起請求。
 * 3. 未來擴充：此處的 Random 邏輯將來可替換為深度學習模型的預測輸出。
 * ----------------------------------------------------------------------------
 */

using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("基本設定")]
    public float moveSpeed = 3.5f;
    public float chaseRange = 8.0f;  // 超過這個距離會追
    public float attackRange = 2.0f; // 進入這個距離準備攻擊
    
    [Header("主動攻擊 AI")]
    public float attackCooldownMin = 2.0f;
    public float attackCooldownMax = 4.0f;
    private float attackTimer;

    [Header("連結")]
    public Animator anim;
    public Transform playerTrans; // 追蹤目標
    public CombatSystem combatSystem;

    // --- 內部狀態 ---
    private bool canMove = true;

    void Start()
    {
        ResetAttackTimer();
    }

    /// <summary>
    /// 設定是否鎖住移動 (由 CombatSystem 呼叫)
    /// </summary>
    public void SetMovementLock(bool isLocked)
    {
        canMove = !isLocked;
        if (!canMove) anim.SetBool("IsWalking", false);
    }

    void Update()
    {
        if (!canMove || playerTrans == null) return;

        float distance = Vector3.Distance(transform.position, playerTrans.position);

        // --- 1. 移動邏輯 (簡單狀態機) ---
        // 永遠面向玩家
        Vector3 lookPos = playerTrans.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (distance > attackRange)
        {
            // 距離太遠：追擊
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            anim.SetBool("IsWalking", true);
        }
        else
        {
            // 距離夠近：停止移動
            anim.SetBool("IsWalking", false);
        }

        // --- 2. 主動攻擊邏輯 ---
        // 只有在距離夠近且可以行動時才倒數
        if (distance <= attackRange + 0.5f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                PerformAIAction();
                ResetAttackTimer();
            }
        }
    }

    void PerformAIAction()
    {
        // 目前使用隨機邏輯 (1=Light, 2=Heavy)
        // 未來在此處接入神經網路模型
        int rand = Random.Range(1, 3); 
        CombatSystem.ActionType action = (CombatSystem.ActionType)rand;
        
        // 向裁判發起攻擊請求
        combatSystem.OnEnemyRequestAttack(action);
    }

    void ResetAttackTimer()
    {
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }
}