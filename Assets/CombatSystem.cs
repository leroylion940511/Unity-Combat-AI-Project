using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class CombatSystem : MonoBehaviour
{
    public enum ActionType { None, LightAttack, HeavyAttack, Block }

    [Header("控制器連結")]
    public PlayerController playerCtrl;
    public EnemyController enemyCtrl; // ★ 新增連結
    public Animator playerAnim;
    public Animator enemyAnim;
    
    [Header("UI 與 特效")]
    public Image playerHealthBar;
    public Image enemyHealthBar;
    public GameObject hitEffectPrefab;

    [Header("遊戲數值")]
    public int maxHP = 10;
    
    private int playerHP;
    private int enemyHP;
    private bool isGameOver = false;
    private string dataPath;
    
    // 全局戰鬥鎖 (只要有人在攻擊，兩邊都不能動)
    private bool isCombatActive = false; 

    void Start()
    {
        dataPath = Application.dataPath + "/CombatData.csv";
        if (!File.Exists(dataPath))
        {
            string header = "PlayerHP,EnemyHP,EnemyAction,PlayerAction,Initiator\n"; // 多記一個誰發起的
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

    // 提供給外部查詢：現在能打嗎？
    public bool CanAct()
    {
        return !isCombatActive && !isGameOver;
    }

    // ★ 玩家按下攻擊鍵時呼叫
    public void OnPlayerRequestAttack(ActionType playerAction)
    {
        if (!CanAct()) return;
        
        // 敵人隨機回應 (這裡未來會換成神經網路預測！)
        ActionType enemyReaction = (ActionType)Random.Range(1, 4);
        
        StartCoroutine(ResolveTurn(playerAction, enemyReaction, "Player"));
    }

    // ★ 敵人主動攻擊時呼叫
    public void OnEnemyRequestAttack(ActionType enemyAction)
    {
        if (!CanAct()) return;

        // 玩家的回應？
        // 因為敵人是主動的，玩家可能沒按鍵。
        // 這裡簡化處理：假設玩家來不及反應 (None)，或者你可以加上一個極短的 QTE 時間讓玩家按
        // 為了數據收集方便，我們先假設玩家是 None (或是隨機，模擬玩家剛好也在亂按)
        ActionType playerReaction = ActionType.None; 

        StartCoroutine(ResolveTurn(playerReaction, enemyAction, "Enemy"));
    }

    // 統一處理戰鬥回合
    IEnumerator ResolveTurn(ActionType pAction, ActionType eAction, string initiator)
    {
        isCombatActive = true; // 鎖定全場

        // 1. 停止雙方移動
        playerCtrl.SetMovementLock(true);
        enemyCtrl.SetMovementLock(true);

        // 2. 記錄數據
        string logLine = $"{playerHP},{enemyHP},{(int)eAction},{(int)pAction},{initiator}\n";
        File.AppendAllText(dataPath, logLine);

        // 3. 播放動畫 (None 代表發呆或來不及反應)
        if(pAction != ActionType.None) PlayAnimation(playerAnim, pAction);
        if(eAction != ActionType.None) PlayAnimation(enemyAnim, eAction);

        // 4. 等待接觸
        yield return new WaitForSeconds(0.4f);

        // 5. 判定勝負
        int result = GetResultScore(pAction, eAction);

        // 6. 結算
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

        // 7. 後搖
        yield return new WaitForSeconds(0.6f);

        // 8. 解鎖
        if (!isGameOver)
        {
            isCombatActive = false;
            playerCtrl.SetMovementLock(false);
            enemyCtrl.SetMovementLock(false);
        }
    }

    // 判定邏輯 (增加對 None 的處理)
    int GetResultScore(ActionType p, ActionType e)
    {
        // 如果一方沒出招 (None)，另一方只要有攻擊就算贏
        if (p == ActionType.None && (e == ActionType.LightAttack || e == ActionType.HeavyAttack)) return -1; // 玩家發呆，敵人打 -> 敵人贏
        if (e == ActionType.None && (p == ActionType.LightAttack || p == ActionType.HeavyAttack)) return 1;  // 敵人發呆，玩家打 -> 玩家贏

        if (p == e) return 0;
        if (p == ActionType.LightAttack) return (e == ActionType.HeavyAttack) ? 1 : -1;
        else if (p == ActionType.HeavyAttack) return (e == ActionType.Block) ? 1 : -1;
        else return (e == ActionType.LightAttack) ? 1 : -1; // Block
    }

    // ... (UI 和 PlayAnimation 函式保持不變) ...
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