/*
 * ----------------------------------------------------------------------------
 * 腳本名稱：CameraController.cs
 * 專案名稱：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
 * 作者：張志晨 (B11202076) - 中華大學資工系
 * 日期：2025/12/16
 * * 功能描述：
 * 本腳本負責「攝影機 (Camera)」的運鏡控制，實現第三人稱視角 (TPS) 的邏輯：
 * 1. 自由視角 (Free Look)：非戰鬥時，玩家可使用滑鼠自由旋轉視角，相機平滑跟隨玩家背後。
 * 2. 鎖定視角 (Lock-on)：戰鬥時 (按 Tab/中鍵)，相機強制注視敵人，輔助 PlayerController 進行平移移動 (Strafing)。
 * 3. 平滑運鏡：在 LateUpdate 中計算位置，確保主角移動完畢後才移動相機，避免畫面抖動 (Jitter)。
 * ----------------------------------------------------------------------------
 */

using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("目標")]
    public Transform target;       // 主角 (Player)
    public Transform lockTarget;   // 敵人 (Enemy)

    [Header("設定")]
    public float mouseSensitivity = 3.0f; // 滑鼠靈敏度
    public float smoothSpeed = 10f;       // 跟隨平滑度 (數值越大越黏)
    public Vector3 offset = new Vector3(0, 2, -3); // 相機相對於主角的標準距離

    // 狀態變數 (public 供 PlayerController 讀取以決定移動模式)
    public bool isLockedOn = false; 

    private float yaw;   // 水平旋轉角度 (Y軸)
    private float pitch; // 垂直旋轉角度 (X軸)

    void Start()
    {
        // 初始化角度為目前的相機角度
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        
        //隱藏滑鼠游標
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
    }

    // 使用 LateUpdate 確保 Player 已經移動完畢，相機才跟上去，防止畫面抖動
    void LateUpdate()
    {
        if (target == null) return;

        // 1. 偵測鎖定輸入 (按下滑鼠中鍵 或 Tab)
        if (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleLock();
        }

        if (isLockedOn && lockTarget != null)
        {
            // --- 鎖定模式 (Combat Mode) ---
            HandleLockedCamera();
        }
        else
        {
            // --- 自由模式 (Exploration Mode) ---
            HandleFreeCamera();
        }
    }

    void ToggleLock()
    {
        isLockedOn = !isLockedOn;
        
        // 防呆：如果想鎖定但場上沒有敵人，就取消鎖定
        if(isLockedOn && lockTarget == null) 
        {
            isLockedOn = false; 
        }
    }

    void HandleFreeCamera()
    {
        // 讀取滑鼠移動輸入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -10f, 60f); // 限制抬頭低頭角度，避免相機翻轉或穿地

        // 計算目標旋轉
        Vector3 targetRotation = new Vector3(pitch, yaw, 0);
        transform.eulerAngles = targetRotation;

        // 計算目標位置 (主角位置 + 旋轉後的偏移量)
        Vector3 desiredPosition = target.position + Quaternion.Euler(pitch, yaw, 0) * offset;
        
        // 平滑移動 (Lerp)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    void HandleLockedCamera()
    {
        // 戰鬥視角邏輯：
        // 保持在主角身後，但強制看護敵人
        
        Vector3 dirToEnemy = (lockTarget.position - target.position).normalized;
        
        // 計算理想位置：在主角與敵人連線的反方向 (背後)
        // 距離 3f, 高度 2f (可根據需求微調)
        Vector3 desiredPosition = target.position - (dirToEnemy * 3f) + Vector3.up * 2f; 

        // 平滑移動位置
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // 強制轉向：注視敵人
        // (進階優化：如果覺得太暈，可以改為 LookAt 主角與敵人的中心點)
        transform.LookAt(lockTarget.position);
    }
}