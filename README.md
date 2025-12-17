# 🎮 AI Combat Data Collector (Unity)

> **畢業專題原型：應用深度學習模型於三元克制戰鬥系統的玩家意圖預測**
> A 3D combat prototype designed for collecting player behavior data to train Adaptive AI.

## 📖 專案簡介 (Introduction)
本專案是一個基於 Unity 引擎開發的 3D 第三人稱動作遊戲原型。核心目標是實作一個「三元克制（剪刀石頭布）」的戰鬥系統，並在遊玩過程中**自動收集玩家的戰鬥數據**（如血量、距離、出招習慣）。

這些數據將用於後續訓練深度學習模型 (Deep Learning Model)，以建立一個能夠預測玩家意圖並自動適應的 AI 對手。

## ✨ 核心功能 (Key Features)
* **數據收集系統 (Data Logging)**：
    * 自動將每一回合的戰鬥狀態記錄為 `.csv` 檔案。
    * 特徵包含：雙方血量、距離、敵方行動、玩家反應、發起者標記。
* **模組化架構 (Modular Architecture)**：
    * 採用 Controller/Manager 分離設計，確保戰鬥邏輯與移動邏輯解耦。
* **戰鬥機制 (Combat Mechanics)**：
    * **三元克制**：輕攻擊 (Light) > 重攻擊 (Heavy) > 格擋 (Block) > 輕攻擊。
    * **鎖定視角 (Lock-on System)**：支援自由視角與戰鬥鎖定視角的切換 (Strafing movement)。
    * **主動 AI**：敵人具備追蹤、繞圈與主動發起攻擊的基礎行為機。

## 🛠️ 系統架構 (System Architecture)
本專案採用元件導向設計 (Component-Based Design)：

| 腳本名稱 | 職責 (Responsibility) |
| :--- | :--- |
| **GameManager (CombatSystem)** | **裁判與大腦**。負責判定勝負、管理雙方血量、寫入 CSV 數據、控制回合流程 (Turn-based logic)。 |
| **PlayerController** | **玩家控制**。處理 Input (WASD/JKL)、控制角色移動與動畫、向裁判發送攻擊請求。 |
| **EnemyController** | **敵人 AI**。處理自動追蹤、距離判斷、計時器主動攻擊邏輯。 |
| **CameraController** | **運鏡控制**。處理滑鼠旋轉 (Free Look) 與鎖定目標 (Lock-on) 的平滑切換。 |

## 🎮 操作說明 (Controls)

| 按鍵 | 功能 | 備註 |
| :--- | :--- | :--- |
| **W / A / S / D** | 移動 (Move) | 鎖定時為平移 (Strafing) |
| **Tab / 滑鼠中鍵** | 切換鎖定 (Toggle Lock-on) | 鎖定敵人或自由視角 |
| **J** | 輕攻擊 (Light Attack) | 速度快，剋制重攻擊 |
| **K** | 重攻擊 (Heavy Attack) | 傷害高，剋制格擋 |
| **L** | 格擋 (Block) | 防禦，剋制輕攻擊 |
| **R** | 重置遊戲 (Reset) | 重新開始回合 |

## 📊 數據格式 (Data Format)
收集到的 `CombatData.csv` 格式範例如下：

```csv
PlayerHP, EnemyHP, EnemyAction, PlayerAction, Initiator
10, 10, 2, 1, Enemy
9, 10, 1, 3, Player
...
```
* **Action Mapping: 0=None, 1=Light, 2=Heavy, 3=Block**
