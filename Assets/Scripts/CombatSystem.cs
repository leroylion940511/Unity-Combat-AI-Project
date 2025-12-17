// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：戰鬥系統核心 (QTE 反應機制、勝負判定、戰鬥數據收集)

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO; // 用於 CSV 檔案讀寫

public class CombatSystem : MonoBehaviour
{
    /// <summary>
    /// 定義戰鬥動作類型 (剪刀石頭布機制)
    /// None: 無動作/未反應
    /// Light: 輕攻擊 (克制 Heavy)
    /// Heavy: 重攻擊 (克制 Block)
    /// Block: 格擋 (克制 Light)
    /// </summary>
    public enum ActionType { None, LightAttack, HeavyAttack, Block }

    [Header("控制器連結")]
    public PlayerController playerCtrl;
    public EnemyController enemyCtrl; 
    public Animator playerAnim;
    public Animator enemyAnim;
    
    [Header("UI 與 特效")]
    public Image playerHealthBar;
    public Image enemyHealthBar;
    public GameObject hitEffectPrefab;    // 打擊特效
    public GameObject qtePromptUI;        // QTE 提示視窗 (感嘆號或按鍵提示)
    public GameObject floatingTextPrefab; // 浮動文字 (傷害數值/狀態)

    [Header("遊戲數值")]
    public int maxHP = 10;
    
    /// <summary> QTE 給予玩家的反應時間 (秒)，會受 slowMotionScale 影響實際體感時長 </summary>
    public float qteDuration = 0.5f;
    
    /// <summary> QTE 期間的時間流逝倍率 (0.3 = 30% 速度，即慢動作) </summary>
    public float slowMotionScale = 0.3f;

    // --- 內部狀態 ---
    private int playerHP;
    private int enemyHP;
    private bool isGameOver = false;
    private string dataPath; // CSV 檔案路徑
    
    // --- 狀態機變數 ---
    private bool isCombatActive = false; // 是否正在進行回合演練 (播放動畫中)
    private bool isInQTE = false;        // ★ 是否處於 QTE 等待輸入階段
    private ActionType playerReactionInput = ActionType.None; // 暫存 QTE 期間玩家按下的指令

    void Start()
    {
        // 設定數據儲存路徑 (通常在 AppData 或 Documents 下)
        dataPath = Application.persistentDataPath + "/CombatData.csv";
        
        // 如果檔案不存在，寫入 CSV 標頭
        if (!File.Exists(dataPath))
        {
            // 標頭包含：雙方血量、雙方動作、發起者、是否為 QTE 事件
            string header = "PlayerHP,EnemyHP,EnemyAction,PlayerAction,Initiator,IsQTE\n";
            File.WriteAllText(dataPath, header);
        }
        
        if(qtePromptUI != null) qtePromptUI.SetActive(false); 
        ResetGame();
    }

    /// <summary>
    /// 重置遊戲狀態 (血量、UI、鎖定解除)。
    /// </summary>
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

    /// <summary>
    /// 檢查玩家目前是否可以輸入指令。
    /// </summary>
    public bool CanAct()
    {
        // 注意：如果在 QTE 中，也視為 "Cannot Act" (因為輸入邏輯會被 QTE 接管)
        return !isCombatActive && !isGameOver && !isInQTE;
    }

    // ========================================================================
    //                               輸入處理
    // ========================================================================

    /// <summary>
    /// 接收玩家輸入 (由 PlayerController 呼叫)。
    /// </summary>
    /// <param name="action">玩家按下的動作 (J/K/L)</param>
    public void OnPlayerInput(ActionType action)
    {
        // 情況 A: 平常狀態 -> 玩家主動發起攻擊
        if (CanAct())
        {
            // 玩家先手，敵人隨機回應 (未來可改接 AI 模型)
            ActionType enemyReaction = (ActionType)Random.Range(1, 4);
            StartCoroutine(ResolveTurn(action, enemyReaction, "Player", false));
        }
        // 情況 B: QTE 狀態中 -> 玩家正在對敵人的攻擊做出反應
        else if (isInQTE)
        {
            playerReactionInput = action; // 記錄下玩家按了什麼
            // 這裡選擇不立刻中斷 QTE，而是等時間結束統一結算
            // 若想要「一按就反應」，可在這裡直接呼叫 StopCoroutine 或是設旗標
        }
    }

    /// <summary>
    /// 接收敵人攻擊請求 (由 EnemyController/AI 呼叫)。
    /// </summary>
    public void OnEnemyRequestAttack(ActionType enemyAction)
    {
        if (!CanAct()) return;

        // ★ 關鍵機制：敵人攻擊時，不直接結算，而是進入 QTE 流程
        StartCoroutine(QTEPhase(enemyAction));
    }

    // ========================================================================
    //                               核心流程
    // ========================================================================

    /// <summary>
    /// QTE 流程協程：鎖定 -> 慢動作 -> 等待輸入 -> 恢復。
    /// </summary>
    IEnumerator QTEPhase(ActionType enemyAction)
    {
        isCombatActive = true;
        isInQTE = true;
        playerReactionInput = ActionType.None; // 重置玩家輸入

        // 1. 鎖定雙方移動
        playerCtrl.SetMovementLock(true);
        enemyCtrl.SetMovementLock(true);

        // 2. 敵人播放攻擊前搖 (提示玩家要防禦了)
        PlayAnimation(enemyAnim, enemyAction);
        Debug.Log("敵人發動攻擊！QTE 開始！");

        // 3. 開啟 QTE 視窗 (UI + 慢動作)
        if(qtePromptUI) qtePromptUI.SetActive(true);
        Time.timeScale = slowMotionScale; // 時間變慢

        // 4. 等待反應時間 
        // 註：WaitForSeconds 受 TimeScale 影響。
        // 實際物理等待時間 = qteDuration * (1 / slowMotionScale)
        // 這裡直接用變慢後的秒數等待，營造緊張感
        yield return new WaitForSeconds(qteDuration * slowMotionScale); 

        // 5. 時間到，恢復正常
        Time.timeScale = 1.0f;
        if(qtePromptUI) qtePromptUI.SetActive(false);
        isInQTE = false;

        // 6. 進入結算
        // 將玩家在 QTE 期間輸入的 playerReactionInput 傳入
        StartCoroutine(ResolveTurn(playerReactionInput, enemyAction, "Enemy", true));
    }

    /// <summary>
    /// 回合結算協程：播放動畫、計算傷害、顯示特效、寫入數據。
    /// </summary>
    IEnumerator ResolveTurn(ActionType pAction, ActionType eAction, string initiator, bool isQTE)
    {
        // 如果是玩家主動攻擊 (非 QTE)，這裡才需要執行鎖定和播動畫
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
            // 如果是 QTE 結算，敵人動畫已經在 QTEPhase 播了，現在補播玩家的反應動畫
            if(pAction != ActionType.None) PlayAnimation(playerAnim, pAction);
        }

        // 1. 記錄數據到 CSV
        // 格式：PlayerHP, EnemyHP, EnemyAction, PlayerAction, Initiator, IsQTE
        string logLine = $"{playerHP},{enemyHP},{(int)eAction},{(int)pAction},{initiator},{isQTE}\n";
        File.AppendAllText(dataPath, logLine);

        // 2. 等待動畫接觸點 (Impact Point)
        yield return new WaitForSeconds(0.4f);

        // 3. 判定勝負 (-1:玩家輸, 0:平手, 1:玩家贏)
        int result = GetResultScore(pAction, eAction);

        // 4. 執行結果 (扣血、特效、浮動文字)
        if (result == 1) // 玩家贏
        {
            enemyHP--;
            enemyAnim.SetTrigger("Hit"); // 敵人受傷動畫
            
            if(hitEffectPrefab) 
                Instantiate(hitEffectPrefab, enemyCtrl.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            
            ShowPopup(enemyCtrl.transform, "-1", Color.red);    // 敵人扣血
            ShowPopup(playerCtrl.transform, "Hit!", Color.yellow); // 玩家成功
        }
        else if (result == -1) // 敵人贏 (或玩家 QTE 逾時未按)
        {
            playerHP--;
            playerAnim.SetTrigger("Hit"); // 玩家受傷動畫
            ShowPopup(playerCtrl.transform, "-1", Color.red); // 玩家扣血
        }
        else 
        {
            // 平手 或 格擋成功
            ShowPopup(playerCtrl.transform, "Block", Color.cyan);
            ShowPopup(enemyCtrl.transform, "Block", Color.cyan);
        }

        UpdateUI();
        CheckGameOver();

        // 5. 後搖時間 (讓動畫播完)
        yield return new WaitForSeconds(0.6f);

        // 6. 解鎖控制 (如果遊戲還沒結束)
        if (!isGameOver)
        {
            isCombatActive = false;
            playerCtrl.SetMovementLock(false);
            enemyCtrl.SetMovementLock(false);
        }
    }

    // ========================================================================
    //                               輔助函式
    // ========================================================================

    /// <summary>
    /// 三元克制邏輯判定。
    /// </summary>
    /// <returns>1: 玩家贏, 0: 平手, -1: 敵人贏</returns>
    int GetResultScore(ActionType p, ActionType e)
    {
        if (p == ActionType.None && e != ActionType.None) return -1; // QTE 沒反應 -> 輸
        if (e == ActionType.None && p != ActionType.None) return 1;  // 敵人發呆 -> 贏
        if (p == e) return 0; // 平手
        
        // Light > Heavy > Block > Light
        if (p == ActionType.LightAttack) return (e == ActionType.HeavyAttack) ? 1 : -1;
        else if (p == ActionType.HeavyAttack) return (e == ActionType.Block) ? 1 : -1;
        else return (e == ActionType.LightAttack) ? 1 : -1; // p is Block
    }
    
    void PlayAnimation(Animator anim, ActionType action) 
    {
        if (action == ActionType.LightAttack) anim.SetTrigger("Light");
        else if (action == ActionType.HeavyAttack) anim.SetTrigger("Heavy");
        else if (action == ActionType.Block) anim.SetTrigger("Block");
    }

    void UpdateUI() 
    {
        if (playerHealthBar != null) playerHealthBar.fillAmount = (float)playerHP / maxHP;
        if (enemyHealthBar != null) enemyHealthBar.fillAmount = (float)enemyHP / maxHP;
    }

    void CheckGameOver() 
    {
        if (playerHP <= 0 || enemyHP <= 0) 
        {
            isGameOver = true;
            Debug.Log("Game Over!");
            // 此處可加入開啟結算選單的邏輯
        }
    }

    /// <summary>
    /// 生成浮動文字 (Floating Text)。
    /// </summary>
    void ShowPopup(Transform target, string text, Color color)
    {
        if (floatingTextPrefab != null)
        {
            // 在目標頭頂上方一點點生成
            GameObject go = Instantiate(floatingTextPrefab, target.position + Vector3.up * 2.0f, Quaternion.identity);
            
            // 取得 FloatingText 腳本並設定數值
            // 注意：需確保 Prefab 上有掛載 FloatingText 腳本
            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) 
            {
                ft.SetText(text, color);
            }
        }
    }
}