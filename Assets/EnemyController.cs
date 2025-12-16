using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("基本設定")]
    public float moveSpeed = 3.5f;
    public float chaseRange = 8.0f; // 超過這個距離會追
    public float attackRange = 2.0f; // 進入這個距離準備攻擊
    
    [Header("主動攻擊 AI")]
    public float attackCooldownMin = 2.0f;
    public float attackCooldownMax = 4.0f;
    private float attackTimer;

    [Header("連結")]
    public Animator anim;
    public Transform playerTrans; // 追蹤目標
    public CombatSystem combatSystem;

    private bool canMove = true;

    void Start()
    {
        // 初始化攻擊計時器
        ResetAttackTimer();
    }

    public void SetMovementLock(bool isLocked)
    {
        canMove = !isLocked;
        if (!canMove) anim.SetBool("IsWalking", false);
    }

    void Update()
    {
        if (!canMove || playerTrans == null) return;

        float distance = Vector3.Distance(transform.position, playerTrans.position);

        // --- 1. 移動邏輯 (簡單的狀態機) ---
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
            // 距離夠近：停止移動 (或者可以在這裡寫左右繞圈)
            anim.SetBool("IsWalking", false);
        }

        // --- 2. 主動攻擊邏輯 ---
        // 只有在距離夠近時才倒數攻擊
        if (distance <= attackRange + 0.5f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                // 時間到！發動攻擊
                PerformAIAction();
                ResetAttackTimer();
            }
        }
    }

    void PerformAIAction()
    {
        // 這裡暫時隨機出招 (未來可以改成根據玩家血量或其他邏輯)
        int rand = Random.Range(1, 3); // 1=Light, 2=Heavy (AI通常不主動格擋)
        
        CombatSystem.ActionType action = (CombatSystem.ActionType)rand;
        
        // 告訴裁判：我要打玩家！
        combatSystem.OnEnemyRequestAttack(action);
    }

    void ResetAttackTimer()
    {
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }
}