/*
 * ----------------------------------------------------------------------------
 * 腳本名稱：CombatSystem.cs (QTE版)
 * 功能更新：加入 QTE 反應視窗，讓玩家有時間針對敵人的攻擊進行防禦或反擊。
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class CombatSystem : MonoBehaviour
{
    public enum ActionType { None, LightAttack, HeavyAttack, Block }

    [Header("控制器連結")]
    public PlayerController playerCtrl;
    public EnemyController enemyCtrl; 
    public Animator playerAnim;
    public Animator enemyAnim;
    
    [Header("UI 與 特效")]
    public Image playerHealthBar;
    public Image enemyHealthBar;
    public GameObject hitEffectPrefab;
    public GameObject qtePromptUI;
    public GameObject floatingTextPrefab;

    [Header("遊戲數值")]
    public int maxHP = 10;
    public float qteDuration = 0.5f;
    public float slowMotionScale = 0.3f;

    // --- 內部狀態 ---
    private int playerHP;
    private int enemyHP;
    private bool isGameOver = false;
    private string dataPath;
    
    // 狀態機變數
    private bool isCombatActive = false;
    private bool isInQTE = false; // ★ 是否正在等待玩家輸入
    private ActionType playerReactionInput = ActionType.None; // 暫存玩家在 QTE 期間按了什麼

    void Start()
    {
        dataPath = Application.persistentDataPath + "/CombatData.csv";
        // 如果檔案不存在，寫入標頭
        if (!File.Exists(dataPath))
        {
            // 標頭多加一欄 "IsQTE" 標記這是不是一次 QTE 數據
            string header = "PlayerHP,EnemyHP,EnemyAction,PlayerAction,Initiator,IsQTE\n";
            File.WriteAllText(dataPath, header);
        }
        
        if(qtePromptUI != null) qtePromptUI.SetActive(false); // 確保 UI 一開始是關的
        ResetGame();
    }

    void ResetGame()
    {
        playerHP = maxHP;
        enemyHP = maxHP;
        isGameOver = false;
        isCombatActive = false;
        isInQTE = false;
        Time.timeScale = 1.0f; // 確保時間恢復正常
        
        if(playerCtrl != null) playerCtrl.SetMovementLock(false);
        if(enemyCtrl != null) enemyCtrl.SetMovementLock(false);
        
        UpdateUI();
    }

    public bool CanAct()
    {
        // 如果正在 QTE 中，也視為不能隨意行動 (輸入會被 QTE 邏輯接管)
        return !isCombatActive && !isGameOver && !isInQTE;
    }

    // --- 接收玩家輸入 ---
    // PlayerController 會呼叫這個
    public void OnPlayerInput(ActionType action)
    {
        // 情況 A: 平常狀態 -> 玩家主動發起攻擊
        if (CanAct())
        {
            // 玩家先手，敵人隨機回應
            ActionType enemyReaction = (ActionType)Random.Range(1, 4);
            StartCoroutine(ResolveTurn(action, enemyReaction, "Player", false));
        }
        // 情況 B: QTE 狀態中 -> 玩家正在反應
        else if (isInQTE)
        {
            playerReactionInput = action; // 記錄下玩家按了什麼
            // 這裡不直接觸發，等 QTE 時間結束統一結算，或者你想按了立刻結算也可以 (這裡選時間到結算)
        }
    }

    // --- 接收敵人輸入 ---
    public void OnEnemyRequestAttack(ActionType enemyAction)
    {
        if (!CanAct()) return;

        // ★ 關鍵改變：敵人攻擊時，不直接結算，而是進入 QTE 流程
        StartCoroutine(QTEPhase(enemyAction));
    }

    // --- QTE 流程協程 ---
    IEnumerator QTEPhase(ActionType enemyAction)
    {
        isCombatActive = true;
        isInQTE = true;
        playerReactionInput = ActionType.None; // 重置玩家輸入

        // 1. 鎖定移動
        playerCtrl.SetMovementLock(true);
        enemyCtrl.SetMovementLock(true);

        // 2. 敵人播放攻擊前搖 (這裡先簡單播動畫，實際可能需要分段動畫)
        PlayAnimation(enemyAnim, enemyAction);
        Debug.Log("敵人發動攻擊！請反應！");

        // 3. 開啟 QTE 視窗 (UI + 慢動作)
        if(qtePromptUI) qtePromptUI.SetActive(true);
        Time.timeScale = slowMotionScale; // 時間變慢，給玩家反應機會

        // 4. 等待時間 (注意：WaitForSeconds 會受 TimeScale 影響，所以實際物理時間 = qteDuration * (1/scale))
        // 如果想固定物理時間 0.5秒，要用 unscaled time 寫法，但這裡讓它跟著變慢比較有張力
        yield return new WaitForSeconds(qteDuration * slowMotionScale); 

        // 5. 時間到，恢復正常
        Time.timeScale = 1.0f;
        if(qtePromptUI) qtePromptUI.SetActive(false);
        isInQTE = false;

        // 6. 進入結算
        // 此時 playerReactionInput 應該已經在 OnPlayerInput 被填入了
        StartCoroutine(ResolveTurn(playerReactionInput, enemyAction, "Enemy", true));
    }

    // --- 結算回合 ---
    IEnumerator ResolveTurn(ActionType pAction, ActionType eAction, string initiator, bool isQTE)
    {
        // 如果不是 QTE 進來的 (是玩家主動)，這裡才鎖定和播動畫
        if (!isQTE)
        {
            isCombatActive = true;
            playerCtrl.SetMovementLock(true);
            enemyCtrl.SetMovementLock(true);
            
            if(pAction != ActionType.None) PlayAnimation(playerAnim, pAction);
            if(eAction != ActionType.None) PlayAnimation(enemyAnim, eAction);
        }
        else
        {
            // 如果是 QTE 進來的，玩家剛剛才按鍵，現在補播玩家的動畫
            if(pAction != ActionType.None) PlayAnimation(playerAnim, pAction);
        }

        // 1. 記錄數據
        string logLine = $"{playerHP},{enemyHP},{(int)eAction},{(int)pAction},{initiator},{isQTE}\n";
        File.AppendAllText(dataPath, logLine);

        // 2. 等待接觸點 (Impact)
        yield return new WaitForSeconds(0.4f);

        // 3. 判定勝負
        int result = GetResultScore(pAction, eAction);

        // 4. 執行結果
        if (result == 1) // 玩家贏
        {
            enemyHP--;
            enemyAnim.SetTrigger("Hit");
            if(hitEffectPrefab) Instantiate(hitEffectPrefab, enemyCtrl.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Debug.Log("玩家得分！");
            // ★ 新增：敵人頭上跳出傷害
            ShowPopup(enemyCtrl.transform, "-1", Color.red);
            // ★ 新增：玩家頭上跳出 Win
            ShowPopup(playerCtrl.transform, "Hit!", Color.yellow);
        }
        else if (result == -1) // 敵人贏 (或玩家沒反應)
        {
            playerHP--;
            playerAnim.SetTrigger("Hit");
            Debug.Log("玩家受傷！");
            // ★ 新增：玩家頭上跳出傷害
            ShowPopup(playerCtrl.transform, "-1", Color.red);
        }
        else 
        {
            Debug.Log("平手/格擋成功！");
            // 可以加個特效表示格擋
            // ★ 新增：雙方頭上跳出 Block
            ShowPopup(playerCtrl.transform, "Block", Color.cyan);
            ShowPopup(enemyCtrl.transform, "Block", Color.cyan);
        }

        UpdateUI();
        CheckGameOver();

        // 5. 後搖
        yield return new WaitForSeconds(0.6f);

        // 6. 解鎖
        if (!isGameOver)
        {
            isCombatActive = false;
            playerCtrl.SetMovementLock(false);
            enemyCtrl.SetMovementLock(false);
        }
    }

    // ... (GetResultScore, PlayAnimation, UpdateUI, CheckGameOver 跟之前一樣，省略不貼以節省空間) ...
    // 請保留原本的這些輔助函式！
    
    // 為了方便你複製，這裡補上 GetResultScore 
    int GetResultScore(ActionType p, ActionType e)
    {
        if (p == ActionType.None && e != ActionType.None) return -1; // 玩家沒按 -> 輸
        if (e == ActionType.None && p != ActionType.None) return 1;  
        if (p == e) return 0; // 平手
        
        if (p == ActionType.LightAttack) return (e == ActionType.HeavyAttack) ? 1 : -1;
        else if (p == ActionType.HeavyAttack) return (e == ActionType.Block) ? 1 : -1;
        else return (e == ActionType.LightAttack) ? 1 : -1; 
    }
    
    // 請自行保留 UpdateUI, CheckGameOver, PlayAnimation
    void PlayAnimation(Animator anim, ActionType action) {
        if (action == ActionType.LightAttack) anim.SetTrigger("Light");
        else if (action == ActionType.HeavyAttack) anim.SetTrigger("Heavy");
        else if (action == ActionType.Block) anim.SetTrigger("Block");
    }
    void UpdateUI() {
        if (playerHealthBar != null) playerHealthBar.fillAmount = (float)playerHP / maxHP;
        if (enemyHealthBar != null) enemyHealthBar.fillAmount = (float)enemyHP / maxHP;
    }
    void CheckGameOver() {
        if (playerHP <= 0 || enemyHP <= 0) isGameOver = true;
    }

    // ★ 輔助函式：生成浮動文字
    void ShowPopup(Transform target, string text, Color color)
    {
        if (floatingTextPrefab != null)
        {
            // 在頭頂上方一點點生成 (Vector3.up * 2.0f)
            GameObject go = Instantiate(floatingTextPrefab, target.position + Vector3.up * 2.0f, Quaternion.identity);
            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) ft.SetText(text, color);
        }
    }
}