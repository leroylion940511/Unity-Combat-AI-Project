using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;
    public GameObject pauseMenuUI;

    void Update()
    {
        // 偵測 ESC 鍵
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

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // 恢復時間流動
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // 凍結時間
        isPaused = true;
    }

    public void LoadMenu()
    {
        // ★ 關鍵：切換場景前一定要把時間恢復！
        // 不然主選單或其他場景會被凍結
        Time.timeScale = 1f; 
        isPaused = false;
        
        // 載入 Index 0 的場景 (通常是 MainMenu)
        SceneManager.LoadScene(0); 
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}