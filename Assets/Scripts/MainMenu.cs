using UnityEngine;
using UnityEngine.SceneManagement; // ★ 這行一定要加，才能切換場景

public class MainMenu : MonoBehaviour
{
    // 按下 "Start Game" 執行這段
    public void PlayGame()
    {
        // 讀取列表中的下一個場景 (通常是索引值 1 的場景)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // 按下 "Quit" 執行這段
    public void QuitGame()
    {
        Debug.Log("遊戲已關閉！"); // 在編輯器裡看不到關閉，所以印個 Log 確認
        Application.Quit();
    }
}