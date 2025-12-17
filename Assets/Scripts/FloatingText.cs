using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 2.0f;
    public float destroyTime = 1.0f;
    public TextMesh textMesh;

    void Start()
    {
        // 1. 自動抓取身上的 TextMesh
        if (textMesh == null) textMesh = GetComponent<TextMesh>();

        // 2. 設定自毀時間
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 3. 往上飄移
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 4. (選用) 永遠面向攝影機，不然轉視角會穿幫
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }

    // 給外部呼叫的設定函式
    public void SetText(string text, Color color)
    {
        if (textMesh == null) textMesh = GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
    }
}