using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("數值設定")]
    public float moveSpeed = 4.0f;
    public float rotateSpeed = 10f;

    [Header("元件連結")]
    public Animator anim;
    public CameraController camCtrl;
    public Transform mainCameraTrans;
    
    // 這裡我們直接連結 GameManager，方便呼叫戰鬥
    public CombatSystem combatSystem; 

    private bool canMove = true;

    public void SetMovementLock(bool isLocked)
    {
        canMove = !isLocked;
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
        bool isMoving = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);
        anim.SetBool("IsWalking", isMoving);

        if (!isMoving) return;

        if (camCtrl.isLockedOn && camCtrl.lockTarget != null)
        {
            // 鎖定模式：面向敵人 + 平移
            Vector3 lookPos = camCtrl.lockTarget.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);

            Vector3 moveDir = (transform.forward * v) + (transform.right * h);
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        else
        {
            // 自由模式：轉向移動
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