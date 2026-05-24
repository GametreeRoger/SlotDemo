using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlotDemo.EditorTools
{
    public static class SlotDemoArtGenerator
    {
        const int MachineW = 1120;
        const int MachineH = 898;
        const string ImagesFolder = "Assets/Images";

        // Palette
        static readonly Color32 PurpleDark   = new Color32(37, 25, 63, 255);
        static readonly Color32 PurpleMid    = new Color32(70, 52, 120, 255);
        static readonly Color32 PurpleLight  = new Color32(114, 89, 181, 255);
        static readonly Color32 PurpleAccent = new Color32(160, 64, 200, 255);
        static readonly Color32 PurpleHot    = new Color32(210, 80, 230, 255);
        static readonly Color32 Gold         = new Color32(255, 208, 64, 255);
        static readonly Color32 Silver       = new Color32(200, 192, 224, 255);
        static readonly Color32 SilverDark   = new Color32(130, 120, 165, 255);
        static readonly Color32 Window       = new Color32(20, 12, 36, 255);
        static readonly Color32 WindowEdge   = new Color32(50, 35, 80, 255);
        static readonly Color32 LedPink      = new Color32(255, 107, 200, 255);
        static readonly Color32 LedCyan      = new Color32(107, 220, 255, 255);
        static readonly Color32 LedYellow    = new Color32(255, 224, 112, 255);
        static readonly Color32 LedWhite     = new Color32(255, 255, 255, 255);
        static readonly Color32 RedBall      = new Color32(220, 50, 50, 255);
        static readonly Color32 RedBallDark  = new Color32(140, 25, 25, 255);
        static readonly Color32 Transparent  = new Color32(0, 0, 0, 0);
        static readonly Color32 Shadow       = new Color32(10, 5, 20, 160);

        static readonly Color32 OrangeLight  = new Color32(255, 168, 64, 255);
        static readonly Color32 OrangeDark   = new Color32(200, 72, 24, 255);
        static readonly Color32 OrangeEdge   = new Color32(130, 40, 10, 255);
        static readonly Color32 BlueLight    = new Color32(110, 180, 255, 255);
        static readonly Color32 BlueDark     = new Color32(24, 88, 181, 255);
        static readonly Color32 BlueEdge     = new Color32(10, 40, 110, 255);

        [MenuItem("Tools/SlotDemo/Generate Art")]
        public static void GenerateAll()
        {
            GenerateMachine();
            GenerateSpinButton();
            GenerateBetButton();
            GenerateWinPopup();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "SlotDemo",
                "Art generated:\n  • machine_gen.png\n  • spin_btn.png\n  • bet_btn.png\n  • win_popup_bg.png\n\nRun Tools/SlotDemo/Build All to use them in the scene.",
                "OK");
        }

        // ───────── machine ─────────
        static void GenerateMachine()
        {
            int W = MachineW, H = MachineH;
            var px = new Color32[W * H];

            // Drop shadow under cabinet (a slightly larger, offset, dark rounded rect)
            FillRoundedRect(px, W, H, 22, 6, W - 44, H - 44, 64, Shadow);

            // Outer cabinet — vertical gradient
            FillRoundedRectVGradient(px, W, H, 18, 18, W - 36, H - 36, 60, PurpleDark, PurpleMid);

            // Inner cabinet face (inset, brighter highlight)
            FillRoundedRectVGradient(px, W, H, 50, 50, W - 100, H - 100, 48, PurpleMid, PurpleLight);

            // ----- Marquee header -----
            int marqX = 130, marqY = 720, marqW = W - 260, marqH = 130;
            FillRoundedRect(px, W, H, marqX - 4, marqY - 4, marqW + 8, marqH + 8, 34, Gold);     // outer gold trim
            FillRoundedRectVGradient(px, W, H, marqX, marqY, marqW, marqH, 30, PurpleAccent, PurpleHot);
            // marquee inner highlight band
            FillRoundedRect(px, W, H, marqX + 14, marqY + marqH - 30, marqW - 28, 8, 4, new Color32(255, 200, 240, 180));

            // ----- Reel viewing area (dark inset) -----
            // 3 reels at canvas x = -175, 0, +175 (PNG x = 385, 560, 735), centered Y = -11 → PNG y = 438
            // Each reel 150×450; combined visible x [310,810], y [213,663]
            int reelInsetX = 270, reelInsetY = 185, reelInsetW = 580, reelInsetH = 510;
            // gold/silver frame
            FillRoundedRect(px, W, H, reelInsetX - 14, reelInsetY - 14, reelInsetW + 28, reelInsetH + 28, 30, Gold);
            FillRoundedRect(px, W, H, reelInsetX - 8, reelInsetY - 8, reelInsetW + 16, reelInsetH + 16, 26, WindowEdge);
            // dark window
            FillRoundedRectVGradient(px, W, H, reelInsetX, reelInsetY, reelInsetW, reelInsetH, 22, Window, WindowEdge);
            // Vertical separator lines between reels (just inside reel slots)
            int sepW = 4;
            FillRect(px, W, H, 470 - sepW / 2, reelInsetY + 25, sepW, reelInsetH - 50, new Color32(90, 70, 130, 200));
            FillRect(px, W, H, 650 - sepW / 2, reelInsetY + 25, sepW, reelInsetH - 50, new Color32(90, 70, 130, 200));

            // ----- Control deck (bottom panel) -----
            int deckX = 130, deckY = 75, deckW = W - 260, deckH = 130;
            FillRoundedRect(px, W, H, deckX - 4, deckY - 4, deckW + 8, deckH + 8, 24, Silver);
            FillRoundedRectVGradient(px, W, H, deckX, deckY, deckW, deckH, 20, SilverDark, new Color32(80, 65, 115, 255));

            // Coin tray slot at very bottom of cabinet
            int trayX = 320, trayY = 38, trayW = W - 640, trayH = 22;
            FillRoundedRect(px, W, H, trayX, trayY, trayW, trayH, 8, Window);
            // tray inner highlight (thin line)
            FillRect(px, W, H, trayX + 8, trayY + trayH - 4, trayW - 16, 2, new Color32(150, 130, 200, 100));

            // ----- Side lever (just right of the reel frame's lower-right corner) -----
            // Reel frame outer right edge at x≈864, frame bottom at y≈171. Place the lever
            // hugging the right side of the reels in the lower portion of the reel zone.
            int leverBaseX = 895, leverBaseY = 295;
            // mount cap at stick base (looks anchored to the cabinet body)
            DrawCircle(px, W, H, leverBaseX, leverBaseY - 60, 9, SilverDark);
            // stick
            FillRect(px, W, H, leverBaseX - 4, leverBaseY - 60, 8, 60, SilverDark);
            // ball with shading
            DrawCircle(px, W, H, leverBaseX, leverBaseY + 10, 28, RedBallDark);
            DrawCircle(px, W, H, leverBaseX, leverBaseY + 14, 24, RedBall);
            DrawCircle(px, W, H, leverBaseX - 7, leverBaseY + 21, 8, new Color32(255, 180, 180, 220));   // highlight

            // ----- LED dots around perimeter -----
            int dotR = 8;
            Color32[] ledRotation = { LedPink, LedCyan, LedYellow, LedWhite };
            // Top edge (just under marquee gold trim) — skip; marquee already there.
            // Marquee outer top — small bulbs along top edge of the cabinet, above marquee
            int topY = 870;
            for (int x = 90, k = 0; x < W - 90; x += 70, k++)
                DrawDotWithGlow(px, W, H, x, topY, dotR, ledRotation[k % 4]);
            // Left edge
            for (int y = 250, k = 0; y < 700; y += 70, k++)
                DrawDotWithGlow(px, W, H, 35, y, dotR, ledRotation[(k + 1) % 4]);
            // Right edge
            for (int y = 250, k = 0; y < 700; y += 70, k++)
                DrawDotWithGlow(px, W, H, W - 35, y, dotR, ledRotation[(k + 2) % 4]);
            // Above the bottom deck (just above panel)
            int botY = 215;
            for (int x = 90, k = 0; x < W - 90; x += 70, k++)
                DrawDotWithGlow(px, W, H, x, botY, dotR, ledRotation[(k + 3) % 4]);

            SaveAsSprite(px, W, H, "machine_gen.png");
        }

        // ───────── Spin button (orange rounded rect) ─────────
        static void GenerateSpinButton()
        {
            int W = 200, H = 120;
            var px = new Color32[W * H];
            // shadow
            FillRoundedRect(px, W, H, 6, 0, W - 12, H - 6, 32, Shadow);
            // outer dark ring
            FillRoundedRect(px, W, H, 2, 6, W - 4, H - 12, 34, OrangeEdge);
            // body gradient
            FillRoundedRectVGradient(px, W, H, 8, 12, W - 16, H - 24, 30, OrangeDark, OrangeLight);
            // top sheen
            FillRoundedRect(px, W, H, 22, H - 30, W - 44, 8, 4, new Color32(255, 220, 180, 180));
            SaveAsSprite(px, W, H, "spin_btn.png");
        }

        // ───────── BET button (blue circle) ─────────
        static void GenerateBetButton()
        {
            int W = 140, H = 140;
            var px = new Color32[W * H];
            int cx = W / 2, cy = H / 2;
            // shadow
            DrawCircle(px, W, H, cx + 2, cy - 4, 62, Shadow);
            // dark outer ring
            DrawCircle(px, W, H, cx, cy, 62, BlueEdge);
            // body
            DrawCircleVGradient(px, W, H, cx, cy, 56, BlueDark, BlueLight);
            // upper sheen
            DrawCircle(px, W, H, cx, cy + 22, 30, new Color32(180, 220, 255, 90));
            DrawCircle(px, W, H, cx - 12, cy + 28, 12, new Color32(220, 240, 255, 160));
            SaveAsSprite(px, W, H, "bet_btn.png");
        }

        // ───────── Win popup background ─────────
        static void GenerateWinPopup()
        {
            int W = 600, H = 200;
            var px = new Color32[W * H];
            FillRoundedRect(px, W, H, 0, 0, W, H, 24, Gold);
            FillRoundedRectVGradient(px, W, H, 6, 6, W - 12, H - 12, 20, new Color32(20, 10, 30, 230), new Color32(60, 30, 80, 230));
            SaveAsSprite(px, W, H, "win_popup_bg.png");
        }

        // ─────────────── helpers ───────────────
        static void Plot(Color32[] px, int W, int H, int x, int y, Color32 c)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            if (c.a == 0) return;
            var dst = px[y * W + x];
            if (c.a == 255 || dst.a == 0)
            {
                // opaque source, or empty target → straight write (no premultiply)
                px[y * W + x] = c;
                return;
            }
            // src-over-dst with straight alpha (so Unity renders the sprite correctly with alphaIsTransparency)
            float sa = c.a / 255f;
            float da = dst.a / 255f;
            float outA = sa + da * (1f - sa);
            if (outA < 1e-4f) { px[y * W + x] = new Color32(0, 0, 0, 0); return; }
            float inv = 1f / outA;
            px[y * W + x] = new Color32(
                (byte)Mathf.Clamp((c.r * sa + dst.r * da * (1f - sa)) * inv, 0f, 255f),
                (byte)Mathf.Clamp((c.g * sa + dst.g * da * (1f - sa)) * inv, 0f, 255f),
                (byte)Mathf.Clamp((c.b * sa + dst.b * da * (1f - sa)) * inv, 0f, 255f),
                (byte)Mathf.Clamp(outA * 255f, 0f, 255f));
        }

        static void FillRect(Color32[] px, int W, int H, int x0, int y0, int w, int h, Color32 c)
        {
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                    Plot(px, W, H, x0 + dx, y0 + dy, c);
        }

        static void FillRoundedRect(Color32[] px, int W, int H, int x0, int y0, int w, int h, int r, Color32 c)
        {
            if (r < 0) r = 0;
            if (r > w / 2) r = w / 2;
            if (r > h / 2) r = h / 2;
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    if (!InRoundedRect(dx, dy, w, h, r)) continue;
                    Plot(px, W, H, x0 + dx, y0 + dy, c);
                }
            }
        }

        static void FillRoundedRectVGradient(Color32[] px, int W, int H, int x0, int y0, int w, int h, int r, Color32 bottom, Color32 top)
        {
            if (r < 0) r = 0;
            if (r > w / 2) r = w / 2;
            if (r > h / 2) r = h / 2;
            for (int dy = 0; dy < h; dy++)
            {
                float t = (h <= 1) ? 1f : (float)dy / (h - 1);
                Color32 c = LerpC(bottom, top, t);
                for (int dx = 0; dx < w; dx++)
                {
                    if (!InRoundedRect(dx, dy, w, h, r)) continue;
                    Plot(px, W, H, x0 + dx, y0 + dy, c);
                }
            }
        }

        static bool InRoundedRect(int dx, int dy, int w, int h, int r)
        {
            int cx, cy;
            if (dx < r && dy < r) { cx = r; cy = r; }
            else if (dx >= w - r && dy < r) { cx = w - r - 1; cy = r; }
            else if (dx < r && dy >= h - r) { cx = r; cy = h - r - 1; }
            else if (dx >= w - r && dy >= h - r) { cx = w - r - 1; cy = h - r - 1; }
            else return true;
            int ddx = dx - cx, ddy = dy - cy;
            return ddx * ddx + ddy * ddy <= r * r;
        }

        static void DrawCircle(Color32[] px, int W, int H, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r2)
                        Plot(px, W, H, cx + dx, cy + dy, c);
        }

        static void DrawCircleVGradient(Color32[] px, int W, int H, int cx, int cy, int r, Color32 bottom, Color32 top)
        {
            int r2 = r * r;
            for (int dy = -r; dy <= r; dy++)
            {
                float t = ((float)(dy + r)) / (2 * r);
                Color32 c = LerpC(bottom, top, t);
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r2)
                        Plot(px, W, H, cx + dx, cy + dy, c);
            }
        }

        static void DrawDotWithGlow(Color32[] px, int W, int H, int cx, int cy, int r, Color32 c)
        {
            // glow ring
            var glow = new Color32(c.r, c.g, c.b, 70);
            DrawCircle(px, W, H, cx, cy, r + 6, glow);
            DrawCircle(px, W, H, cx, cy, r, c);
            // white hot-spot
            DrawCircle(px, W, H, cx - r / 3, cy + r / 3, Mathf.Max(1, r / 3), new Color32(255, 255, 255, 220));
        }

        static Color32 LerpC(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(a.r + (b.r - a.r) * t),
                (byte)Mathf.RoundToInt(a.g + (b.g - a.g) * t),
                (byte)Mathf.RoundToInt(a.b + (b.b - a.b) * t),
                (byte)Mathf.RoundToInt(a.a + (b.a - a.a) * t));
        }

        static void SaveAsSprite(Color32[] px, int W, int H, string filename)
        {
            string path = ImagesFolder + "/" + filename;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.spritePixelsPerUnit = 100;
                imp.SaveAndReimport();
            }
        }
    }
}
