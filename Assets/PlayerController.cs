/*
 * ----------------------------------------------------------------------------
 * 腳本名稱：PlayerController.cs
 * 專案名稱：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
 * 作者：張志晨 (B11202076) - 中華大學資工系
 * 日期：2025/12/16
 * * 功能描述：
 * 本腳本負責「玩家身體」的所有控制邏輯，包含：
 * 1. 移動控制：處理 WASD 輸入，支援「自由視角移動」與「鎖定視角平移 (Strafing)」兩種模式。
 * 2. 動畫同步：根據移動速度更新 Animator 的 IsWalking 參數。
 * 3. 攻擊請求：偵測 J/K/L 輸入，並向 GameManager 發送攻擊請求，自身不處理傷害判定。
 * 4. 行動鎖定：提供接口讓 CombatSystem 在戰鬥演出時鎖住玩家移動。
 * ----------------------------------------------------------------------------
 */

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("數值設定")]
    public float moveSpeed = 4.0f;  // 移動速度
    public float rotateSpeed = 10f; // 轉身速度

    [Header("元件連結")]
    public Animator anim;            // 自身的 Animator
    public CameraController camCtrl; // 為了知道是否鎖定
    public Transform mainCameraTrans;// 為了知道鏡頭方向
    
    // 連結 GameManager，方便呼叫戰鬥請求
    public CombatSystem combatSystem; 

    // --- 內部狀態 ---
    private bool canMove = true; // 是否允許移動 (由 CombatSystem 控制)

    /// <summary>
    /// 設定是否鎖住移動 (用於攻擊硬直或過場動畫)
    /// </summary>
    public void SetMovementLock(bool isLocked)
    {
        canMove = !isLocked;
        // 如果被鎖住，強制停止走路動畫，避免滑步
        if (!canMove) anim.SetBool("IsWalking", false);
    }

    void Update()
    {
        // 1. 處理攻擊輸入 (即使不能移動，有時候也能預輸入，但這裡先簡單處理)
        CheckAttackInput();

        // 2. 處理移動
        if (canMove) HandleMovement();
    }

    void CheckAttackInput()
    {
        // 只有在系統允許行動時 (例如沒有在硬直中) 才偵測
        if (combatSystem.CanAct())
        {
            if (Input.GetKeyDown(KeyCode.J)) combatSystem.OnPlayerRequestAttack(CombatSystem.ActionType.LightAttack);
            else if (Input.GetKeyDown(KeyCode.K)) combatSystem.OnPlayerRequestAttack(CombatSystem.ActionType.HeavyAttack);
            else if (Input.GetKeyDown(KeyCode.L)) combatSystem.OnPlayerRequestAttack(CombatSystem.ActionType.Block);
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        // 判斷是否有輸入
        bool isMoving = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);
        anim.SetBool("IsWalking", isMoving);

        if (!isMoving) return;

        // 根據相機鎖定狀態決定移動方式
        if (camCtrl.isLockedOn && camCtrl.lockTarget != null)
        {
            // --- 鎖定模式 (Strafing) ---
            // 強制面向敵人
            Vector3 lookPos = camCtrl.lockTarget.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);

            // 平移移動
            Vector3 moveDir = (transform.forward * v) + (transform.right * h);
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        else
        {
            // --- 自由模式 (Free Roam) ---
            // 以攝影機為前方的相對移動
            Vector3 camForward = mainCameraTrans.forward;
            camForward.y = 0; 
            camForward.Normalize();
            Vector3 camRight = mainCameraTrans.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 moveDir = (camForward * v) + (camRight * h);

            if (moveDir.magnitude > 0.1f)
            {
                Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotateSpeed * Time.deltaTime);
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
        }
    }
}