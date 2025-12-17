// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：攝影機控制器 (第三人稱視角、鎖定系統)

using UnityEngine;

/// <summary>
/// 負責第三人稱視角 (TPS) 的運鏡控制，包含自由視角與戰鬥鎖定視角。
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("目標設定")]
    /// <summary> 攝影機跟隨的主角 (Player) </summary>
    public Transform target;
    
    /// <summary> 當前鎖定的敵人目標 (Enemy)。若為 null 則無法進入鎖定模式。 </summary>
    public Transform lockTarget;

    [Header("參數設定")]
    /// <summary> 滑鼠旋轉靈敏度 </summary>
    public float mouseSensitivity = 3.0f;
    
    /// <summary> 跟隨平滑度 (數值越大越緊跟，越小越有延遲感) </summary>
    public float smoothSpeed = 10f;
    
    /// <summary> 自由視角時，相機相對於主角的標準偏移量 (建議放在主角背後上方) </summary>
    public Vector3 offset = new Vector3(0, 2, -3);

    // --- 狀態變數 ---
    /// <summary> 
    /// 當前是否處於鎖定模式。
    /// PlayerController 會讀取此變數來決定移動模式 (自由移動 vs 平移)。
    /// </summary>
    public bool isLockedOn = false; 

    private float yaw;   // 水平旋轉角度 (Y軸)
    private float pitch; // 垂直旋轉角度 (X軸)

    void Start()
    {
        // 初始化角度為目前的相機角度，避免遊戲開始時視角跳動
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        
        // 鎖定並隱藏滑鼠游標，適合 FPS/TPS 遊戲體驗
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
    }

    /// <summary>
    /// 在 LateUpdate 更新相機位置。
    /// 原理：確保 Player 在 Update 中已經移動完畢，相機再跟上去，避免畫面抖動 (Jitter)。
    /// </summary>
    void LateUpdate()
    {
        if (target == null) return;

        // 1. 偵測鎖定輸入 (滑鼠中鍵 或 Tab)
        if (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleLock();
        }

        // 2. 根據狀態執行對應運鏡
        if (isLockedOn && lockTarget != null)
        {
            HandleLockedCamera(); // 戰鬥模式
        }
        else
        {
            HandleFreeCamera();   // 自由探索模式
        }
    }

    /// <summary>
    /// 切換鎖定狀態。會自動檢查是否有目標可供鎖定。
    /// </summary>
    void ToggleLock()
    {
        isLockedOn = !isLockedOn;
        
        // 防呆：如果想開啟鎖定但場上沒有敵人目標，則強制取消
        if (isLockedOn && lockTarget == null) 
        {
            isLockedOn = false;
            // 建議：此處未來可加入「自動搜尋最近敵人」的邏輯
        }
    }

    /// <summary>
    /// 自由視角邏輯 (Exploration Mode)。
    /// 玩家可使用滑鼠自由旋轉視角，相機平滑跟隨玩家背後。
    /// </summary>
    void HandleFreeCamera()
    {
        // 1. 讀取滑鼠輸入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 2. 計算角度 (Pitch 需反向扣除 mouseY)
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -10f, 60f); // 限制抬頭低頭角度，避免相機穿地或翻轉

        // 3. 設定相機自身的旋轉
        Vector3 targetRotation = new Vector3(pitch, yaw, 0);
        transform.eulerAngles = targetRotation;

        // 4. 計算目標位置 (主角位置 + 旋轉後的偏移量)
        // Quaternion.Euler(pitch, yaw, 0) 會把 offset 轉到正確的角度方向
        Vector3 desiredPosition = target.position + Quaternion.Euler(pitch, yaw, 0) * offset;
        
        // 5. 平滑移動 (Lerp)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 鎖定視角邏輯 (Combat Mode)。
    /// 相機強制注視敵人，並保持在主角與敵人的連線後方。
    /// </summary>
    void HandleLockedCamera()
    {
        // 1. 計算主角到敵人的方向向量
        Vector3 dirToEnemy = (lockTarget.position - target.position).normalized;
        
        // 2. 計算理想位置：在主角「背對敵人」的方向
        // 距離 3f, 高度 2f (可根據戰鬥需求微調這些魔術數字)
        Vector3 desiredPosition = target.position - (dirToEnemy * 3f) + Vector3.up * 2f; 

        // 3. 平滑移動位置
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // 4. 強制轉向：注視敵人
        // 進階優化建議：若覺得太暈，可改為 LookAt(主角與敵人的中心點)
        transform.LookAt(lockTarget.position);
    }
}