// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：玩家控制器 (移動、動畫同步、攻擊輸入偵測)

using UnityEngine;

/// <summary>
/// 負責玩家角色的核心控制，包含移動邏輯 (WASD) 與攻擊指令發送 (J/K/L)。
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("數值設定")]
    /// <summary> 玩家移動速度 </summary>
    public float moveSpeed = 4.0f;
    /// <summary> 玩家轉身速度 (自由視角模式用) </summary>
    public float rotateSpeed = 10f;

    [Header("元件連結")]
    /// <summary> 自身的 Animator 元件 </summary>
    public Animator anim;
    /// <summary> 攝影機控制器 (用於判斷鎖定狀態) </summary>
    public CameraController camCtrl;
    /// <summary> 主攝影機 Transform (用於計算相對移動方向) </summary>
    public Transform mainCameraTrans;
    /// <summary> 戰鬥系統核心 (用於發送攻擊請求) </summary>
    public CombatSystem combatSystem;

    // --- 內部狀態 ---
    /// <summary> 是否允許移動 (由 CombatSystem 控制，例如攻擊硬直時為 false) </summary>
    private bool canMove = true;

    /// <summary>
    /// 設定是否鎖住移動 (用於攻擊硬直、QTE 或過場動畫)。
    /// </summary>
    /// <param name="isLocked">true = 鎖定 (無法移動); false = 解鎖</param>
    public void SetMovementLock(bool isLocked)
    {
        canMove = !isLocked;
        
        // 如果被鎖住，強制重置走路動畫參數，避免原地滑步
        if (!canMove) 
        {
            anim.SetBool("IsWalking", false);
        }
    }

    void Update()
    {
        // 1. 處理攻擊輸入 
        // (即使被鎖定移動，有時仍需允許輸入以進行連段或 QTE，故獨立處理)
        CheckAttackInput();

        // 2. 處理移動邏輯
        if (canMove) 
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// 偵測攻擊按鍵 (J/K/L) 並通知 CombatSystem。
    /// </summary>
    void CheckAttackInput()
    {
        // 註：這裡不檢查 combatSystem.CanAct()，
        // 因為 QTE 狀態下 (CannotAct) 可能仍需要接收輸入。
        // 具體的行動判斷交給 CombatSystem 內部邏輯處理。
        
        if (Input.GetKeyDown(KeyCode.J)) 
            combatSystem.OnPlayerInput(CombatSystem.ActionType.LightAttack);
            
        else if (Input.GetKeyDown(KeyCode.K)) 
            combatSystem.OnPlayerInput(CombatSystem.ActionType.HeavyAttack);
            
        else if (Input.GetKeyDown(KeyCode.L)) 
            combatSystem.OnPlayerInput(CombatSystem.ActionType.Block);
    }

    /// <summary>
    /// 處理角色移動與旋轉 (支援鎖定視角與自由視角)。
    /// </summary>
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        // 判斷是否有輸入 (設定 Deadzone 防止手把漂移)
        bool isMoving = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);
        anim.SetBool("IsWalking", isMoving);

        if (!isMoving) return;

        // 根據相機鎖定狀態決定移動方式
        if (camCtrl.isLockedOn && camCtrl.lockTarget != null)
        {
            // --- 鎖定模式 (Strafing) ---
            // 1. 強制面向敵人
            Vector3 lookPos = camCtrl.lockTarget.position;
            lookPos.y = transform.position.y; // 保持水平旋轉
            transform.LookAt(lookPos);

            // 2. 進行平移移動 (相對於角色的前後左右)
            Vector3 moveDir = (transform.forward * v) + (transform.right * h);
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        else
        {
            // --- 自由模式 (Free Roam) ---
            // 1. 計算相對於攝影機的移動方向
            Vector3 camForward = mainCameraTrans.forward;
            camForward.y = 0; 
            camForward.Normalize();
            
            Vector3 camRight = mainCameraTrans.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 moveDir = (camForward * v) + (camRight * h);

            // 2. 轉身並移動
            if (moveDir.magnitude > 0.1f)
            {
                Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotateSpeed * Time.deltaTime);
                
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
        }
    }
}