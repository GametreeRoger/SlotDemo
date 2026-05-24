# SlotDemo — Unity 6 3-Reel 老虎機

## 啟動方式

1. 在 Unity Hub 打開這個專案（Unity 6000.4.6f1+）
2. 等待編譯完成（Assets/Scripts、Assets/Editor）
3. 點選 menu：**Tools → SlotDemo → Build All**
   - 這會自動：
     - 把 `Assets/Images/pictures.jpg` 切成 9 張 sprite（Cherry、Orange、Bar、Plum、Watermelon、Lemon、BigWin、Banana、Seven）
     - 建立 `Assets/Data/SymbolTable.asset`（含倍率與權重資料）
     - 重建 `Assets/Scenes/SampleScene.unity`，產生 Canvas、機台背景、三輪 reel、Spin / BET 按鈕、文字 label 與中獎彈窗
4. 開啟 SampleScene，按 **Play**

> 若需要重新建構（例如改完倍率資料），可以再按一次 **Tools → SlotDemo → Build All**，會把整個場景清掉重做。也提供分步驟的子選單。

## 玩法

- **Spin**：扣除當前 BET 後，三個轉輪垂直滾動，依序停下（reel0 ≈1.4s、reel1 ≈1.9s、reel2 ≈2.4s）
- **BET**：每點一次循環 1 → 5 → 10 → 50 → 100 → 1
- **中獎**：三輪相同圖樣即中，依下表倍率乘上目前 BET，彈窗顯示 `WIN +N`
- **Credits** 顯示在畫面下方，歸零無法再 Spin

### 倍率與權重

| Symbol     | Weight | 三連線倍率 |
|------------|--------|-----------|
| Cherry     | 10     | 5x        |
| Banana     | 10     | 5x        |
| Orange     | 8      | 8x        |
| Plum       | 8      | 8x        |
| Lemon      | 8      | 8x        |
| Watermelon | 5      | 15x       |
| Bar        | 3      | 25x       |
| Seven      | 2      | 50x       |
| BigWin     | 1      | 100x      |

要調整，直接在 Project 視窗點 `Assets/Data/SymbolTable.asset`，在 Inspector 改 entries 即可（無需改 code，無需重建場景）。

## 微調

`Tools → SlotDemo → Build All` 會用固定座標放置元件。如果機台圖案上的 Spin / BET / 視窗位置稍微對不上，直接在 Scene view 拖拉就好：

- **Canvas → MachineBG**：背景貼圖，1120 × 898
- **Canvas → Reel0 / Reel1 / Reel2**：三個轉輪視窗，預設置於 x=-260 / 0 / 260，y=-11
- **Canvas → SpinButton / BetButton**：透明點擊區，蓋在機台對應按鈕上
- **Canvas → BetLabel / TotalWinLabel / CreditsLabel**：UI 文字

調整完按 `Ctrl/Cmd+S` 存檔；下次按 Build All 會被覆寫，所以記得備份你滿意的座標。

## 檔案結構

```
Assets/
├── Images/
│   ├── machine.jpg     (機台外觀)
│   └── pictures.jpg    (Build 後切成 9 個 sprite)
├── Scenes/
│   └── SampleScene.unity
├── Scripts/
│   ├── SlotSymbol.cs       (enum)
│   ├── SymbolTable.cs      (ScriptableObject — sprite + weight + multiplier)
│   ├── Reel.cs             (單輪滾動 + 停在指定 symbol)
│   └── SlotMachine.cs      (主控：bet、credit、win 評分、UI 綁定)
├── Editor/
│   └── SlotDemoSceneBuilder.cs   (一鍵建構場景的 menu 工具)
└── Data/
    └── SymbolTable.asset   (Build 後自動產生)
```
