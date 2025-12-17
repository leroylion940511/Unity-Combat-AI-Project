// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：主選單控制器 (開始遊戲、離開遊戲)

using UnityEngine;
using UnityEngine.SceneManagement; // ★ 必須引用此命名空間才能進行場景切換

/// <summary>
/// 管理主選單 (Main Menu) 的按鈕事件。
/// 需掛載於 Canvas 或空物件上，並將按鈕的 OnClick 事件綁定至此腳本的方法。
/// </summary>
public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// 開始遊戲按鈕的功能。
    /// 邏輯：讀取 Build Settings 列表中的「下一個」場景。
    /// </summary>
    public void PlayGame()
    {
        // SceneManager.GetActiveScene().buildIndex 取得當前場景編號 (主選單通常是 0)
        // LoadScene 載入編號 + 1 的場景 (通常是遊戲關卡，即 1)
        // ★ 注意：請務必在 File -> Build Settings 中將場景加入列表，否則會報錯。
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// 離開遊戲按鈕的功能。
    /// </summary>
    public void QuitGame()
    {
        // 顯示 Log 以便在 Unity 編輯器中確認按鈕是否運作
        Debug.Log("遊戲已關閉 (Quit Game)"); 
        
        // 關閉應用程式 (僅在打包後的 .exe 或 .app 中有效)
        Application.Quit();
    }
}