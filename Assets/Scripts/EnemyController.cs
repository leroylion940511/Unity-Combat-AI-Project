// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：敵人 AI 控制器 (自動導航、攻擊排程)

using UnityEngine;

/// <summary>
/// 負責敵人 AI 的行為邏輯，包含追蹤玩家與發起攻擊請求。
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("基本設定")]
    /// <summary> 敵人移動速度 </summary>
    public float moveSpeed = 3.5f;
    
    /// <summary> 追蹤範圍：超過此距離會開始移動靠近玩家 </summary>
    public float chaseRange = 8.0f;
    
    /// <summary> 攻擊範圍：進入此距離會停止移動並準備攻擊 </summary>
    public float attackRange = 2.0f;
    
    [Header("主動攻擊 AI")]
    /// <summary> 攻擊冷卻時間最小值 (秒) </summary>
    public float attackCooldownMin = 2.0f;
    /// <summary> 攻擊冷卻時間最大值 (秒) </summary>
    public float attackCooldownMax = 4.0f;
    
    /// <summary> 攻擊倒數計時器 </summary>
    private float attackTimer;

    [Header("元件連結")]
    public Animator anim;
    /// <summary> 追蹤目標 (玩家) </summary>
    public Transform playerTrans;
    /// <summary> 戰鬥系統核心 (用於發送攻擊請求) </summary>
    public CombatSystem combatSystem;

    // --- 內部狀態 ---
    /// <summary> 是否允許移動 (由 CombatSystem 控制，QTE 或受傷時為 false) </summary>
    private bool canMove = true;

    void Start()
    {
        ResetAttackTimer();
    }

    /// <summary>
    /// 設定是否鎖住移動 (用於 QTE、攻擊硬直或過場動畫)。
    /// </summary>
    /// <param name="isLocked">true = 鎖定; false = 解鎖</param>
    public void SetMovementLock(bool isLocked)
    {
        canMove = !isLocked;
        // 鎖定時強制停止走路動畫
        if (!canMove) anim.SetBool("IsWalking", false);
    }

    void Update()
    {
        // 若被鎖定或找不到玩家，則不執行 AI
        if (!canMove || playerTrans == null) return;

        float distance = Vector3.Distance(transform.position, playerTrans.position);

        // --- 1. 面向與移動邏輯 (簡單狀態機) ---
        
        // 永遠面向玩家 (Y軸旋轉)
        Vector3 lookPos = playerTrans.position;
        lookPos.y = transform.position.y; // 保持水平，不抬頭低頭
        transform.LookAt(lookPos);

        if (distance > attackRange)
        {
            // 距離大於攻擊範圍：追擊
            // 注意：直接修改 position 沒有物理碰撞推擠效果，若需避障建議改用 NavMeshAgent
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            anim.SetBool("IsWalking", true);
        }
        else
        {
            // 進入攻擊範圍：停止移動，準備戰鬥
            anim.SetBool("IsWalking", false);
        }

        // --- 2. 主動攻擊邏輯 ---
        
        // 只有在「距離夠近」且「目前並非冷卻或硬直中」才進行攻擊倒數
        // 這裡加個 +0.5f 緩衝，避免在邊緣反覆抖動
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

    /// <summary>
    /// 執行 AI 決策：選擇動作並通知 CombatSystem。
    /// 未來將在此處接入深度學習模型 (Model Inference)。
    /// </summary>
    void PerformAIAction()
    {
        // 當前邏輯：隨機選擇攻擊
        // Random.Range(int min, int max) 不包含 max。
        // 所以 (1, 3) 只會回傳 1 (Light) 或 2 (Heavy)。
        // 敵人目前設定為「主動攻擊型」，不會主動發起格擋 (ActionType.Block = 3)。
        int rand = Random.Range(1, 3); 
        
        CombatSystem.ActionType action = (CombatSystem.ActionType)rand;
        
        // 向裁判 (CombatSystem) 發起攻擊請求，這會觸發 QTE 流程
        combatSystem.OnEnemyRequestAttack(action);
    }

    /// <summary>
    /// 重置攻擊冷卻時間 (隨機區間)。
    /// </summary>
    void ResetAttackTimer()
    {
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }
}