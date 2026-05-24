using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlotDemo.EditorTools
{
    public static class SlotDemoSceneBuilder
    {
        const string PicturesPath = "Assets/Images/pictures.jpg";
        const string MachinePath = "Assets/Images/machine.jpg";
        const string MachineGenPath = "Assets/Images/machine_gen.png";
        const string SpinBtnPath = "Assets/Images/spin_btn.png";
        const string BetBtnPath = "Assets/Images/bet_btn.png";
        const string WinPopupBgPath = "Assets/Images/win_popup_bg.png";
        const string DataFolder = "Assets/Data";
        const string SymbolTablePath = "Assets/Data/SymbolTable.asset";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        const float MachineW = 1120f;
        const float MachineH = 898f;

        const float ReelXSpacing = 175f;
        const float ReelCenterY = -11f;
        const float ReelW = 150f;
        const float CellH = 150f;
        const int CellsVisible = 3;
        const float ReelH = CellH * CellsVisible;     // 450 — exactly 3 cells visible
        const int CellsPerStrip = 7;                  // 3 visible + 2 buffer above + 2 below

        const float ControlsY = -271f;
        const float SpinBtnX = 0f;

        static readonly (string name, SlotSymbol symbol, int col, int row, int weight, int multiplier)[] Symbols =
        {
            ("Cherry",     SlotSymbol.Cherry,     0, 0, 10, 5),
            ("Orange",     SlotSymbol.Orange,     1, 0, 8,  8),
            ("Bar",        SlotSymbol.Bar,        2, 0, 3,  25),
            ("Plum",       SlotSymbol.Plum,       0, 1, 8,  8),
            ("Watermelon", SlotSymbol.Watermelon, 1, 1, 5,  15),
            ("Lemon",      SlotSymbol.Lemon,      2, 1, 8,  8),
            ("BigWin",     SlotSymbol.BigWin,     0, 2, 1,  100),
            ("Banana",     SlotSymbol.Banana,     1, 2, 10, 5),
            ("Seven",      SlotSymbol.Seven,      2, 2, 2,  50),
        };

        [MenuItem("Tools/SlotDemo/Build All (DESTRUCTIVE — clears scene)")]
        public static void BuildAll()
        {
            // Safety: warn if the current scene already has a SlotMachine, since Build All deletes everything.
            var existingMachine = Object.FindFirstObjectByType<SlotMachine>();
            if (existingMachine != null)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Build All — will overwrite scene",
                    "The current scene already has a SlotMachine. Build All DELETES every root GameObject and rebuilds them from scratch — manual position/setting tweaks will be lost.\n\n" +
                    "If you only need to attach the new View components, cancel and use:\n" +
                    "    Tools/SlotDemo/Wire Views Only (non-destructive)\n\n" +
                    "Continue with a full rebuild?",
                    "Overwrite Scene", "Cancel");
                if (!proceed) return;
            }

            BackupScene();    // copies SampleScene.unity to Temp/ so the previous state can be recovered if needed

            EnsureFolders();
            SlicePicturesIfNeeded();
            AssetDatabase.Refresh();
            var table = CreateSymbolTable();
            BuildScene(table);
            EditorUtility.DisplayDialog(
                "SlotDemo",
                "Build complete.\n\nA backup of the previous scene was placed in the project's Temp/ folder.",
                "OK");
        }

        static void BackupScene()
        {
            try
            {
                if (!File.Exists(ScenePath)) return;
                string tempDir = "Temp/SlotDemoBackup";
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dest = $"{tempDir}/SampleScene_{stamp}.unity";
                File.Copy(ScenePath, dest, overwrite: true);
                Debug.Log("[SlotDemo] Scene backed up to " + dest);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SlotDemo] Could not back up scene: " + e.Message);
            }
        }

        static void SlicePicturesIfNeeded()
        {
            int spriteCount = 0;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(PicturesPath))
                if (o is Sprite) spriteCount++;

            if (spriteCount >= 9)
            {
                Debug.Log("[SlotDemo] pictures.jpg already has " + spriteCount + " sub-sprites; skipping auto-slice.");
                return;
            }
            SlicePictures();
        }

        // Map (col, row) where row 0 = bottom, row 2 = top of texture → Unity's default
        // left-to-right top-to-bottom naming scheme (pictures_0 = top-left).
        static int PictureIndex(int col, int row) => (2 - row) * 3 + col;

        [MenuItem("Tools/SlotDemo/1. Slice pictures.jpg")]
        public static void MenuSlice() { SlicePictures(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/SlotDemo/2. Create SymbolTable asset")]
        public static void MenuTable() { EnsureFolders(); CreateSymbolTable(); }

        [MenuItem("Tools/SlotDemo/3. Build Scene")]
        public static void MenuScene()
        {
            var table = AssetDatabase.LoadAssetAtPath<SymbolTable>(SymbolTablePath);
            if (table == null) { Debug.LogError("Run step 2 first."); return; }
            BuildScene(table);
        }

        [MenuItem("Tools/SlotDemo/Rebuild Reels Only (preserves rest of scene)")]
        public static void MenuRebuildReels()
        {
            var table = AssetDatabase.LoadAssetAtPath<SymbolTable>(SymbolTablePath);
            if (table == null) { Debug.LogError("SymbolTable missing — run Build All once first."); return; }

            var slot = Object.FindFirstObjectByType<SlotMachine>();
            if (slot == null) { Debug.LogError("No SlotMachine in current scene. Save your scene & open SampleScene."); return; }
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("No Canvas in current scene."); return; }

            // Capture existing reel positions so we don't reset them if user nudged them
            var oldReels = slot.reels;
            var keepPositions = new Vector2?[3];
            for (int i = 0; i < 3 && oldReels != null && i < oldReels.Length; i++)
            {
                if (oldReels[i] != null)
                {
                    var rt = oldReels[i].GetComponent<RectTransform>();
                    keepPositions[i] = rt.anchoredPosition;
                    Object.DestroyImmediate(oldReels[i].gameObject);
                }
            }

            var newReels = new Reel[3];
            float[] defaultXs = { -ReelXSpacing, 0f, ReelXSpacing };
            for (int i = 0; i < 3; i++)
            {
                var pos = keepPositions[i] ?? new Vector2(defaultXs[i], ReelCenterY);
                newReels[i] = BuildReel("Reel" + i, canvas.transform, pos, table);
            }
            slot.reels = newReels;
            slot.table = table;

            EditorUtility.SetDirty(slot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);
            Debug.Log("[SlotDemo] Rebuilt 3 reels with " + CellsPerStrip + " cells each (" + CellsVisible + " visible). Save the scene to persist.");
        }

        [MenuItem("Tools/SlotDemo/Wire Views Only (non-destructive)")]
        public static void MenuWireViews()
        {
            var slot = Object.FindFirstObjectByType<SlotMachine>();
            if (slot == null)
            {
                EditorUtility.DisplayDialog("Wire Views", "No SlotMachine in the current scene.\nRun Build All once first, or save the scene that has the machine.", "OK");
                return;
            }

            int added = 0, skipped = 0;

            // CreditsLabel
            var creditsGO = GameObject.Find("CreditsLabel");
            if (creditsGO != null)
            {
                if (creditsGO.GetComponent<SlotDemo.Views.CreditsView>() == null)
                {
                    var v = creditsGO.AddComponent<SlotDemo.Views.CreditsView>();
                    v.machine = slot; v.label = creditsGO.GetComponent<Text>();
                    added++;
                } else skipped++;
            }
            else Debug.LogWarning("[SlotDemo] CreditsLabel not found — skipped.");

            // TotalWinLabel
            var totalWinGO = GameObject.Find("TotalWinLabel");
            if (totalWinGO != null)
            {
                if (totalWinGO.GetComponent<SlotDemo.Views.TotalWinView>() == null)
                {
                    var v = totalWinGO.AddComponent<SlotDemo.Views.TotalWinView>();
                    v.machine = slot; v.label = totalWinGO.GetComponent<Text>();
                    added++;
                } else skipped++;
            }
            else Debug.LogWarning("[SlotDemo] TotalWinLabel not found — skipped.");

            // BetLabel
            var betGO = GameObject.Find("BetLabel");
            if (betGO != null)
            {
                if (betGO.GetComponent<SlotDemo.Views.BetView>() == null)
                {
                    var v = betGO.AddComponent<SlotDemo.Views.BetView>();
                    v.machine = slot; v.label = betGO.GetComponent<Text>();
                    added++;
                } else skipped++;
            }
            else Debug.LogWarning("[SlotDemo] BetLabel not found — skipped.");

            // SpinButton
            var spinGO = GameObject.Find("SpinButton");
            if (spinGO != null)
            {
                if (spinGO.GetComponent<SlotDemo.Views.SpinButtonView>() == null)
                {
                    var v = spinGO.AddComponent<SlotDemo.Views.SpinButtonView>();
                    v.machine = slot; v.button = spinGO.GetComponent<Button>();
                    added++;
                } else skipped++;
            }
            else Debug.LogWarning("[SlotDemo] SpinButton not found — skipped.");

            // BetButton
            var betBtnGO = GameObject.Find("BetButton");
            if (betBtnGO != null)
            {
                if (betBtnGO.GetComponent<SlotDemo.Views.BetButtonView>() == null)
                {
                    var v = betBtnGO.AddComponent<SlotDemo.Views.BetButtonView>();
                    v.machine = slot; v.button = betBtnGO.GetComponent<Button>();
                    added++;
                } else skipped++;
            }
            else Debug.LogWarning("[SlotDemo] BetButton not found — skipped.");

            // WinPopup
            var popupGO = GameObject.Find("WinPopup");
            if (popupGO != null)
            {
                if (popupGO.GetComponent<SlotDemo.Views.WinPopupView>() == null)
                {
                    var v = popupGO.AddComponent<SlotDemo.Views.WinPopupView>();
                    v.machine = slot;
                    v.group = popupGO.GetComponent<CanvasGroup>();
                    var winText = popupGO.transform.Find("WinText");
                    if (winText != null) v.text = winText.GetComponent<Text>();
                    added++;
                } else skipped++;
            }
            else Debug.LogWarning("[SlotDemo] WinPopup not found — skipped.");

            EditorUtility.SetDirty(slot);
            EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);
            Debug.Log($"[SlotDemo] Wire Views: added {added}, already-present {skipped}. Save the scene (Ctrl/Cmd+S) to persist.");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets", "Data");
        }

        static void SlicePictures()
        {
            var importer = AssetImporter.GetAtPath(PicturesPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[SlotDemo] pictures.jpg importer not found at " + PicturesPath);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.isReadable = false;
            importer.mipmapEnabled = false;

            const int W = 612, H = 580;
            const int colW = W / 3;
            int[] rowHeights = { 193, 193, 194 };
            int[] rowYs = { 0, 193, 386 };

            var metas = new List<SpriteMetaData>();
            foreach (var s in Symbols)
            {
                int idx = PictureIndex(s.col, s.row);
                var m = new SpriteMetaData
                {
                    name = "pictures_" + idx,
                    rect = new Rect(s.col * colW, rowYs[s.row], colW, rowHeights[s.row]),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                };
                metas.Add(m);
            }

#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
            importer.SaveAndReimport();

            Debug.Log("[SlotDemo] Sliced pictures.jpg into " + metas.Count + " sprites.");
        }

        static SymbolTable CreateSymbolTable()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SymbolTable>(SymbolTablePath);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<SymbolTable>();
                AssetDatabase.CreateAsset(existing, SymbolTablePath);
            }

            var spriteByName = new Dictionary<string, Sprite>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(PicturesPath))
            {
                if (obj is Sprite sp) spriteByName[sp.name] = sp;
            }

            var entries = new List<SymbolTable.Entry>();
            foreach (var s in Symbols)
            {
                int idx = PictureIndex(s.col, s.row);
                string lookupName = "pictures_" + idx;
                spriteByName.TryGetValue(lookupName, out var sprite);
                if (sprite == null)
                    Debug.LogWarning("[SlotDemo] Missing sprite " + lookupName + " for " + s.symbol + " — re-slice pictures.jpg.");
                entries.Add(new SymbolTable.Entry
                {
                    symbol = s.symbol,
                    sprite = sprite,
                    weight = s.weight,
                    multiplier = s.multiplier,
                });
            }
            existing.entries = entries.ToArray();

            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        static void BuildScene(SymbolTable table)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            // Camera
            var camGO = new GameObject("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.13f, 0.13f, 0.18f);
            cam.orthographic = true;
            camGO.transform.position = new Vector3(0, 0, -10);

            // EventSystem
            var esGO = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif

            // Canvas
            var canvasGO = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(MachineW, MachineH);
            scaler.matchWidthOrHeight = 0.5f;

            // MachineBG — prefer the generated art if available
            var machineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MachineGenPath)
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>(MachinePath);
            var bgGO = NewUI("MachineBG", canvasGO.transform);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = machineSprite;
            bgImg.raycastTarget = false;
            SetRect(bgGO, Vector2.zero, new Vector2(MachineW, MachineH));

            // Title on the marquee (only meaningful when machine_gen.png is used; benign with machine.jpg too)
            BuildLabel("Title", canvasGO.transform, new Vector2(0f, 335f), new Vector2(700f, 100f), "LUCKY SLOT", 64, new Color(1f, 0.95f, 0.6f));

            // Reels
            var reels = new Reel[3];
            float[] reelXs = { -ReelXSpacing, 0f, ReelXSpacing };
            for (int i = 0; i < 3; i++)
            {
                reels[i] = BuildReel("Reel" + i, canvasGO.transform, new Vector2(reelXs[i], ReelCenterY), table);
            }

            // Button sprites (generated). If missing, buttons fall back to flat-color shapes.
            var spinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpinBtnPath);
            var betSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BetBtnPath);

            // BET button (left of Spin)
            var betBtnGO = NewUI("BetButton", canvasGO.transform);
            var betImg = betBtnGO.AddComponent<Image>();
            if (betSprite != null) { betImg.sprite = betSprite; betImg.color = Color.white; }
            else { betImg.color = new Color(0.1f, 0.4f, 0.9f, 1f); }
            betImg.preserveAspect = true;
            betImg.raycastTarget = true;
            var betBtn = betBtnGO.AddComponent<Button>();
            betBtn.targetGraphic = betImg;
            SetRect(betBtnGO, new Vector2(-220f, ControlsY), new Vector2(140f, 140f));

            // Spin button
            var spinBtnGO = NewUI("SpinButton", canvasGO.transform);
            var spinImg = spinBtnGO.AddComponent<Image>();
            if (spinSprite != null) { spinImg.sprite = spinSprite; spinImg.color = Color.white; }
            else { spinImg.color = new Color(0.9f, 0.45f, 0.15f, 1f); }
            spinImg.preserveAspect = true;
            spinImg.raycastTarget = true;
            var spinBtn = spinBtnGO.AddComponent<Button>();
            spinBtn.targetGraphic = spinImg;
            SetRect(spinBtnGO, new Vector2(SpinBtnX, ControlsY), new Vector2(200f, 120f));

            // Bet label (overlays Bet button)
            var betLabel = BuildLabel("BetLabel", canvasGO.transform, new Vector2(-220f, ControlsY), new Vector2(140f, 50f), "BET 1", 30, Color.white);

            // Spin label
            BuildLabel("SpinLabel", canvasGO.transform, new Vector2(SpinBtnX, ControlsY), new Vector2(200f, 60f), "SPIN", 48, Color.white);

            // Total Win label (right of Spin)
            var totalWinLabel = BuildLabel("TotalWinLabel", canvasGO.transform, new Vector2(290f, ControlsY + 6f), new Vector2(220f, 60f), "0", 36, new Color(1f, 0.95f, 0.4f));
            BuildLabel("TotalWinCaption", canvasGO.transform, new Vector2(290f, ControlsY + 40f), new Vector2(220f, 30f), "TOTAL WIN", 20, new Color(1f, 0.85f, 0.4f));

            // Credits display (left side of deck, mirrors TotalWin on the right)
            var creditsLabel = BuildLabel("CreditsLabel", canvasGO.transform, new Vector2(-365f, ControlsY + 6f), new Vector2(200f, 60f), "1000", 36, new Color(0.6f, 1f, 0.6f));
            BuildLabel("CreditsCaption", canvasGO.transform, new Vector2(-365f, ControlsY + 40f), new Vector2(200f, 30f), "CREDITS", 20, new Color(0.5f, 0.9f, 0.5f));

            // Win popup
            var winPopupGO = NewUI("WinPopup", canvasGO.transform);
            var cg = winPopupGO.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            SetRect(winPopupGO, new Vector2(0f, 80f), new Vector2(600f, 200f));

            var winBg = NewUI("Bg", winPopupGO.transform);
            var winBgImg = winBg.AddComponent<Image>();
            var winPopupSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WinPopupBgPath);
            if (winPopupSprite != null) { winBgImg.sprite = winPopupSprite; winBgImg.color = Color.white; }
            else { winBgImg.color = new Color(0f, 0f, 0f, 0.55f); }
            winBgImg.raycastTarget = false;
            SetRect(winBg, Vector2.zero, new Vector2(600f, 200f));

            var winText = BuildLabel("WinText", winPopupGO.transform, Vector2.zero, new Vector2(600f, 200f), "WIN +0", 80, new Color(1f, 0.9f, 0.2f));

            // SlotMachine controller (model + input only — no view refs)
            var controllerGO = new GameObject("SlotMachine");
            var controller = controllerGO.AddComponent<SlotMachine>();
            controller.table = table;
            controller.reels = reels;
            controller.spinButton = spinBtn;
            controller.betButton = betBtn;
            controller.betSteps = new[] { 1, 5, 10, 50, 100 };
            controller.startingCredits = 1000;
            controller.reelStopDurations = new[] { 1.4f, 1.9f, 2.4f };

            // Views subscribe to controller events. Each view sits on the GameObject of the widget it updates.
            var creditsView = creditsLabel.gameObject.AddComponent<SlotDemo.Views.CreditsView>();
            creditsView.machine = controller;
            creditsView.label = creditsLabel;

            var totalWinView = totalWinLabel.gameObject.AddComponent<SlotDemo.Views.TotalWinView>();
            totalWinView.machine = controller;
            totalWinView.label = totalWinLabel;

            var betView = betLabel.gameObject.AddComponent<SlotDemo.Views.BetView>();
            betView.machine = controller;
            betView.label = betLabel;

            var spinView = spinBtnGO.AddComponent<SlotDemo.Views.SpinButtonView>();
            spinView.machine = controller;
            spinView.button = spinBtn;

            var betBtnView = betBtnGO.AddComponent<SlotDemo.Views.BetButtonView>();
            betBtnView.machine = controller;
            betBtnView.button = betBtn;

            var popupView = winPopupGO.AddComponent<SlotDemo.Views.WinPopupView>();
            popupView.machine = controller;
            popupView.group = cg;
            popupView.text = winText;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[SlotDemo] Scene built.");
        }

        static Reel BuildReel(string name, Transform parent, Vector2 pos, SymbolTable table)
        {
            var reelGO = NewUI(name, parent);
            var bg = reelGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = false;
            reelGO.AddComponent<RectMask2D>();
            SetRect(reelGO, pos, new Vector2(ReelW, ReelH));

            var stripGO = NewUI("Strip", reelGO.transform);
            SetRect(stripGO, Vector2.zero, new Vector2(ReelW, CellH * CellsPerStrip));

            int centerIndex = CellsPerStrip / 2;
            var cells = new Image[CellsPerStrip];
            for (int i = 0; i < CellsPerStrip; i++)
            {
                var cellGO = NewUI("Cell_" + i, stripGO.transform);
                var img = cellGO.AddComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                float y = (centerIndex - i) * CellH;     // cell[centerIndex] sits at y=0 (mask center / payline)
                SetRect(cellGO, new Vector2(0, y), new Vector2(ReelW, CellH));
                cells[i] = img;
            }

            var reel = reelGO.AddComponent<Reel>();
            reel.table = table;
            reel.strip = stripGO.GetComponent<RectTransform>();
            reel.cells = cells;
            reel.cellHeight = CellH;
            reel.topSpeed = 2400f;
            reel.decelEndSpeed = 600f;

            // pre-fill cell sprites so the editor view is not empty
            if (table != null && table.entries != null && table.entries.Length > 0)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    var entry = table.entries[i % table.entries.Length];
                    cells[i].sprite = entry.sprite;
                }
            }

            return reel;
        }

        static Text BuildLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color)
        {
            var go = NewUI(name, parent);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(2, -2);

            SetRect(go, pos, size);
            return t;
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetRect(GameObject go, Vector2 anchoredPos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
