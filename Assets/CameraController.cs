using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("目標")]
    public Transform target;       // 主角 (Player)
    public Transform lockTarget;   // 敵人 (Enemy)

    [Header("設定")]
    public float mouseSensitivity = 3.0f; // 滑鼠靈敏度
    public float smoothSpeed = 10f;       // 跟隨平滑度
    public Vector3 offset = new Vector3(0, 2, -3); // 相機與主角的距離

    // 狀態變數
    public bool isLockedOn = false; // 是否鎖定中 (給 CombatSystem 讀取)

    private float yaw;   // 水平旋轉角度
    private float pitch; // 垂直旋轉角度

    void Start()
    {
        // 初始化角度
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        
        // (選用) 隱藏滑鼠游標，玩起來比較像遊戲
        // Cursor.lockState = CursorLockMode.Locked; 
        // Cursor.visible = false;
    }

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
            // --- 鎖定模式 (Look At Enemy) ---
            HandleLockedCamera();
        }
        else
        {
            // --- 自由模式 (Free Look) ---
            HandleFreeCamera();
        }
    }

    void ToggleLock()
    {
        isLockedOn = !isLockedOn;
        
        // 如果鎖定但在距離外或沒有敵人，可能需要取消 (這裡先簡化，直接切換)
        if(isLockedOn && lockTarget == null) 
        {
            isLockedOn = false; // 沒敵人不能鎖
        }
    }

    void HandleFreeCamera()
    {
        // 讀取滑鼠移動
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -10f, 60f); // 限制抬頭低頭角度，避免翻轉

        // 計算旋轉
        Vector3 targetRotation = new Vector3(pitch, yaw, 0);
        transform.eulerAngles = targetRotation;

        // 計算位置 (跟隨主角 + 旋轉偏移)
        // 這裡用簡單的算法：位置 = 主角位置 + 旋轉 * 偏移量
        Vector3 desiredPosition = target.position + Quaternion.Euler(pitch, yaw, 0) * offset;
        
        // 平滑移動
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    void HandleLockedCamera()
    {
        // 相機位置：在主角屁股後面一點，稍微抬高
        // 為了簡單，我們這裡直接讓相機看著「敵人」，但位置保持在主角身後
        
        Vector3 dirToEnemy = (lockTarget.position - target.position).normalized;
        
        // 計算理想位置：主角位置 - (指向敵人的方向 * 距離) + 高度
        Vector3 desiredPosition = target.position - (dirToEnemy * 3f) + Vector3.up * 2f; // 3f是距離，2f是高度

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // 強制看著敵人
        // 為了不讓相機劇烈晃動，我們看「主角和敵人的中間點」通常比較舒服，這裡先直接看敵人
        transform.LookAt(lockTarget.position);
    }
}
