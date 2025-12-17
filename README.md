# AI Combat Data Collector (Unity)

> **畢業專題原型：應用深度學習模型於三元克制戰鬥系統的玩家意圖預測**
> A 3D combat prototype designed for collecting player behavior data to train Adaptive AI.

---

## 專案簡介 (Introduction)
本專案是一個基於 Unity 引擎開發的 3D 第三人稱動作遊戲原型。核心目標是實作一個「三元克制（剪刀石頭布）」的戰鬥系統，並在遊玩過程中**自動收集玩家的戰鬥數據**（如血量、距離、出招習慣、反應時間）。

這些數據將用於後續訓練深度學習模型 (Deep Learning Model)，以建立一個能夠預測玩家意圖並自動適應的 AI 對手。

---

## 核心功能 (Key Features)

### 1. 數據工程 (Data Engineering)
* **自動化數據日誌**：
    * 自動將每一回合的戰鬥狀態寫入 `.csv` 檔案 (支援 Windows/macOS 路徑)。
    * 特徵包含：雙方血量、距離、敵方行動、玩家反應、發起者、**是否為 QTE 事件**。
* **自動對戰模式 (Auto-Battle Mining)**：
    * 內建 AI 互戰開關 (`T` 鍵)，可讓電腦接管玩家操作進行快速對戰，用於生成海量訓練數據。

### 2. 戰鬥系統 (Combat System)
* **三元克制機制**：輕攻擊 (Light) > 重攻擊 (Heavy) > 格擋 (Block) > 輕攻擊。
* **QTE 反應視窗 (Reaction Window)**：
    * 敵人攻擊前具備前搖動作與**時間緩慢 (Slow Motion)** 機制。
    * 提供玩家決策空間，用於收集「人類玩家如何應對特定攻擊」的行為數據。
* **視覺回饋**：實作浮動戰鬥文字 (Floating Text) 與打擊特效，即時顯示傷害與格擋狀態。

### 3. 完整遊戲架構 (Game Architecture)
* **完整流程**：包含主選單 (Start/Quit)、戰鬥場景、以及遊戲內暫停 (ESC) 功能。
* **模組化設計**：採用 Controller/Manager 分離設計，確保戰鬥邏輯、UI 邏輯與移動邏輯解耦。

---

## 系統架構 (System Architecture)
本專案採用元件導向設計 (Component-Based Design)：

| 腳本名稱 | 職責 (Responsibility) |
| :--- | :--- |
| **CombatSystem (GameManager)** | **核心裁判**。負責判定勝負 (三元克制)、管理 QTE 流程、寫入 CSV 數據。 |
| **PlayerController** | **玩家控制**。處理 Input、控制移動、發送攻擊請求 (支援自動模式接管)。 |
| **EnemyController** | **敵人 AI**。處理自動追蹤、距離判斷、計時器主動攻擊邏輯。 |
| **CameraController** | **運鏡控制**。處理滑鼠旋轉 (Free Look) 與鎖定目標 (Lock-on) 的平滑切換。 |
| **MainMenu / PauseMenu** | **UI 管理**。處理場景切換、遊戲暫停、時間凍結 (TimeScale) 控制。 |
| **FloatingText** | **特效管理**。處理戰鬥數值的生成與飄動效果。 |

---

## 操作說明 (Controls)

| 按鍵 | 功能 | 備註 |
| :--- | :--- | :--- |
| **W / A / S / D** | 移動 (Move) | 鎖定時為平移 (Strafing) |
| **Tab / 滑鼠中鍵** | 切換鎖定 (Toggle Lock-on) | 鎖定敵人或自由視角 |
| **J / K / L** | 輕 / 重 / 格擋 (Actions) | 剪刀石頭布攻擊 |
| **T** | **切換自動對戰 (Auto-Battle)** | 開啟後電腦接管操作 (挖礦模式) |
| **ESC** | **暫停選單 (Pause)** | 暫停遊戲、回到主選單 |
| **R** | 重置遊戲 (Reset) | 手動重新開始回合 |

---

## 數據格式 (Data Format)
收集到的 `CombatData.csv` 格式範例如下：

```csv
PlayerHP, EnemyHP, EnemyAction, PlayerAction, Initiator, IsQTE
10, 10, 2, 1, Enemy, TRUE
9, 10, 1, 3, Player, FALSE
...
```

* **Action Mapping**: 0=None, 1=Light, 2=Heavy, 3=Block
* **IsQTE**: TRUE 代表該回合由敵人發起且觸發了反應視窗，FALSE 代表玩家主動攻擊。

---

## 開發日誌 (Devlog)

### Day 2 - 2025/12/17：系統優化與 UI 建置
* **UI/UX 系統**：
    * 實作 **主選單 (Main Menu)** 與 **暫停選單 (Pause Menu)**，完善遊戲循環。
    * 修正場景切換時的 `Time.timeScale` 凍結問題。
* **戰鬥體驗升級**：
    * 加入 **QTE 反應視窗**：敵人攻擊時觸發時間緩慢，讓玩家有決策時間，增加數據收集的策略性。
    * 加入 **Floating Text**：增加打擊感與資訊透明度。
* **專案重構**：
    * 整理 Assets 資料夾結構 (Scripts/Scenes/Prefabs)，並建立 Git 分支策略以區分「開發版」與「數據收集版」。

### Day 1 - 2025/12/16：專案初始化與核心邏輯
* **基礎建設**：
    * 建立 Unity 專案與 Git 版本控制 (包含 `.gitignore` 與 GitHub Actions 準備)。
    * 完成 **三元克制戰鬥系統** (剪刀石頭布邏輯：Light > Heavy > Block)。
* **角色控制**：
    * 實作 **PlayerController** (WASD 移動 + 攻擊輸入)。
    * 實作 **EnemyController** (基礎追蹤 AI + 隨機攻擊)。
    * 實作 **CameraController** (自由視角與鎖定視角的平滑切換)。
* **數據工程**：
    * 實作 CSV 寫入功能，紀錄每回合戰鬥數據 (HP, Action, Initiator)。

---