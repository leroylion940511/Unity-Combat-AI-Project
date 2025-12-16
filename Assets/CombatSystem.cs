/*
 * ----------------------------------------------------------------------------
 * 腳本名稱：CombatSystem.cs (掛載於 GameManager)
 * 專案名稱：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
 * 作者：張志晨 (B11202076) - 中華大學資工系
 * 日期：2025/12/16
 * * 功能描述：
 * 本專案的核心中控系統，擔任「裁判」角色 (God Object for Logic)。
 * 1. 戰鬥流程管理：接收雙方攻擊請求，鎖定全場，執行回合制動畫演出。
 * 2. 判定邏輯：執行剪刀石頭布 (Light > Heavy > Block > Light) 勝負判定。
 * 3. 數據收集：將每一回合的狀態 (HP, Action, Initiator) 寫入 CSV 檔案。
 * 4. UI 更新：管理血條顯示與遊戲結束狀態。
 * * 數據存檔位置：
 * - Windows: %userprofile%/AppData/LocalLow/DefaultCompany/ProjectName/CombatData.csv
 * - Mac: ~/Library/Application Support/DefaultCompany/ProjectName/CombatData.csv
 * ----------------------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class CombatSystem : MonoBehaviour
{
    // 定義動作列舉
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

    [Header("遊戲數值")]
    public int maxHP = 10;
    
    // --- 內部狀態 ---
    private int playerHP;
    private int enemyHP;
    private bool isGameOver = false;
    private string dataPath;
    private bool isCombatActive = false; // 全局戰鬥鎖

    void Start()
    {
        // 使用 PersistentDataPath 確保在 Build 出來的遊戲中也能寫入
        dataPath = Application.persistentDataPath + "/CombatData.csv";
        Debug.Log("數據存檔路徑: " + dataPath); // 開發時方便查看

        // 如果檔案不存在，寫入 CSV 標頭
        if (!File.Exists(dataPath))
        {
            string header = "PlayerHP,EnemyHP,EnemyAction,PlayerAction,Initiator\n";
            File.WriteAllText(dataPath, header);
        }
        ResetGame();
    }

    void ResetGame()
    {
        playerHP = maxHP;
        enemyHP = maxHP;
        isGameOver = false;
        isCombatActive = false;
        
        if(playerCtrl != null) playerCtrl.SetMovementLock(false);
        if(enemyCtrl != null) enemyCtrl.SetMovementLock(false);
        
        UpdateUI();
    }

    /// <summary>
    /// 外部查詢：目前是否允許發起攻擊？
    /// </summary>
    public bool CanAct()
    {
        return !isCombatActive && !isGameOver;
    }

    /// <summary>
    /// 玩家發起攻擊時呼叫
    /// </summary>
    public void OnPlayerRequestAttack(ActionType playerAction)
    {
        if (!CanAct()) return;
        
        // 敵人隨機回應 (未來此處可讓 AI 根據數據預測來格擋)
        ActionType enemyReaction = (ActionType)Random.Range(1, 4);
        
        StartCoroutine(ResolveTurn(playerAction, enemyReaction, "Player"));
    }

    /// <summary>
    /// 敵人主動攻擊時呼叫
    /// </summary>
    public void OnEnemyRequestAttack(ActionType enemyAction)
    {
        if (!CanAct()) return;

        // 玩家回應設為 None (模擬玩家未反應，或需加入 QTE 機制)
        ActionType playerReaction = ActionType.None; 

        StartCoroutine(ResolveTurn(playerReaction, enemyAction, "Enemy"));
    }

    /// <summary>
    /// 核心協程：處理戰鬥回合的完整流程
    /// </summary>
    IEnumerator ResolveTurn(ActionType pAction, ActionType eAction, string initiator)
    {
        isCombatActive = true; // 鎖定全場

        // 1. 停止雙方移動
        playerCtrl.SetMovementLock(true);
        enemyCtrl.SetMovementLock(true);

        // 2. 記錄數據 (Append 到 CSV)
        string logLine = $"{playerHP},{enemyHP},{(int)eAction},{(int)pAction},{initiator}\n";
        File.AppendAllText(dataPath, logLine);

        // 3. 播放動畫
        if(pAction != ActionType.None) PlayAnimation(playerAnim, pAction);
        if(eAction != ActionType.None) PlayAnimation(enemyAnim, eAction);

        // 4. 等待接觸點 (Impact Time)
        yield return new WaitForSeconds(0.4f);

        // 5. 判定勝負
        int result = GetResultScore(pAction, eAction);

        // 6. 結算傷害與特效
        if (result == 1) // 玩家贏
        {
            enemyHP--;
            enemyAnim.SetTrigger("Hit");
            if(hitEffectPrefab) Instantiate(hitEffectPrefab, enemyCtrl.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Debug.Log("玩家得分！");
        }
        else if (result == -1) // 敵人贏
        {
            playerHP--;
            playerAnim.SetTrigger("Hit");
            Debug.Log("玩家受傷！");
        }

        UpdateUI();
        CheckGameOver();

        // 7. 後搖時間 (Recovery Time)
        yield return new WaitForSeconds(0.6f);

        // 8. 解鎖 (如果遊戲還沒結束)
        if (!isGameOver)
        {
            isCombatActive = false;
            playerCtrl.SetMovementLock(false);
            enemyCtrl.SetMovementLock(false);
        }
    }

    /// <summary>
    /// 三元克制邏輯：1=PlayerWin, -1=EnemyWin, 0=Draw
    /// </summary>
    int GetResultScore(ActionType p, ActionType e)
    {
        // 處理一方未出招的情況 (None)
        if (p == ActionType.None && (e != ActionType.None)) return -1; // 玩家發呆 -> 輸
        if (e == ActionType.None && (p != ActionType.None)) return 1;  // 敵人發呆 -> 贏

        if (p == e) return 0;
        
        // 剪刀石頭布邏輯
        if (p == ActionType.LightAttack) return (e == ActionType.HeavyAttack) ? 1 : -1;
        else if (p == ActionType.HeavyAttack) return (e == ActionType.Block) ? 1 : -1;
        else return (e == ActionType.LightAttack) ? 1 : -1; // Block
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
        if (playerHP <= 0 || enemyHP <= 0) isGameOver = true;
    }
}