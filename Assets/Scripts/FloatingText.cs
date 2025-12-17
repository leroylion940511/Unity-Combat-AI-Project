// 作者：張志晨 (B11202076)
// 單位：中華大學資工系
// 專案：畢業專題 - 應用深度學習模型於三元克制戰鬥系統
// 日期：2025/12/17
// 功能：浮動文字特效 (處理傷害數值或狀態文字的飄移與銷毀)

using UnityEngine;

/// <summary>
/// 掛載於浮動文字 Prefab 上。
/// 負責控制文字生成後的向上飄移、自動銷毀以及始終面向攝影機 (Billboard)。
/// </summary>
public class FloatingText : MonoBehaviour
{
    [Header("動態設定")]
    /// <summary> 文字向上飄移的速度 </summary>
    public float moveSpeed = 2.0f;
    
    /// <summary> 文字存活時間 (秒)，時間到自動銷毀 </summary>
    public float destroyTime = 1.0f;
    
    [Header("元件連結")]
    /// <summary> 顯示文字的 Mesh 元件 (若是用 TextMeshPro 需改為 TMP_Text) </summary>
    public TextMesh textMesh;

    // --- 內部緩存 ---
    private Camera mainCam;

    void Start()
    {
        // 1. 自動抓取身上的 TextMesh (如果 Inspector 沒拉的話)
        if (textMesh == null) 
            textMesh = GetComponent<TextMesh>();

        // 2. 緩存主攝影機 (避免在 Update 中頻繁呼叫 Camera.main 造成效能浪費)
        mainCam = Camera.main;

        // 3. 設定自毀倒數
        // 這會確保物件不會無限累積在場景中吃記憶體
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 4. 控制位移：持續往世界座標的上方飄移
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 5. Billboard 效果：確保文字永遠面向攝影機
        // 這樣玩家旋轉視角時，文字才不會變扁或被看到背面
        if (mainCam != null)
        {
            // 計算「從攝影機指向文字」的方向向量
            // 使用 LookRotation 讓文字的 Z 軸對齊這個向量，也就是正面朝向攝影機
            transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
        }
    }

    /// <summary>
    /// 初始化文字內容與顏色 (供 CombatSystem 呼叫)。
    /// </summary>
    /// <param name="text">要顯示的字串 (如 "-1", "Block")</param>
    /// <param name="color">文字顏色 (傷害=紅, 格擋=青, 勝利=黃)</param>
    public void SetText(string text, Color color)
    {
        // 防呆：如果外部呼叫太快，Start 可能還沒執行，這裡再確保一次元件存在
        if (textMesh == null) 
            textMesh = GetComponent<TextMesh>();
            
        textMesh.text = text;
        textMesh.color = color;
    }
}