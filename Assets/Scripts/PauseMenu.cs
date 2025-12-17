// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：暫停選單控制 (Pause Menu Controller)

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 管理遊戲的暫停、繼續、返回主選單與退出功能。
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>
    /// 全域變數，紀錄目前遊戲是否處於暫停狀態。
    /// 其他腳本可透過 PauseMenu.isPaused 讀取此狀態。
    /// </summary>
    public static bool isPaused = false;

    /// <summary>
    /// 暫停選單的 UI 面板 (Canvas 或 Panel)。
    /// </summary>
    public GameObject pauseMenuUI;

    void Update()
    {
        // 偵測 ESC 鍵輸入
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    /// <summary>
    /// 繼續遊戲：隱藏選單並恢復時間流動。
    /// </summary>
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // 恢復時間流動 (正常速度)
        isPaused = false;
    }

    /// <summary>
    /// 暫停遊戲：顯示選單並凍結時間。
    /// </summary>
    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // 凍結時間 (遊戲邏輯停止)
        isPaused = true;
    }

    /// <summary>
    /// 返回主選單。
    /// </summary>
    public void LoadMenu()
    {
        // ★ 重要修正：切換場景前必須先恢復時間流動！
        // 如果不設回 1f，進入主選單後時間仍是凍結的，會導致動畫或邏輯卡住。
        Time.timeScale = 1f; 
        isPaused = false;
        
        // 載入 Build Settings 中 Index 為 0 的場景 (通常是 MainMenu)
        SceneManager.LoadScene(0); 
    }

    /// <summary>
    /// 退出遊戲應用程式。
    /// 注意：此功能在 Unity 編輯器中無效，僅在打包後的執行檔有效。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting Game... (應用程式退出)");
        Application.Quit();
    }
}