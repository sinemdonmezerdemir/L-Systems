using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace LSystem
{
    /// <summary>
    /// Handles the rasterization of L-System segments into a 2D Texture.
    /// Implements memory-efficient texture reuse and event-based updates.
    /// </summary>
    public class LSystemRenderer : MonoBehaviour
    {
        public LSystemData data;

        public Texture2D resultTexture;
        public int textureWidth = 1024;
        public int textureHeight = 1024;
        public int padding = 20;

        public int currentDisplayIteration = 0;
        public bool isAnimating = false;

        // --- THEME CONFIGURATION ---
        public Color32 currentBgColor = new Color32(0, 0, 0, 0);
        public Color32 currentLineColor = new Color32(99, 102, 241, 255);

        // Event to notify listeners (like UI) when a new frame is rendered
        public event Action<Texture2D> OnTextureUpdated;

        private Coroutine animationCoroutine;

        [DllImport("__Internal")]
        private static extern void DownloadImage(string filename, string base64Data);

        public void SetThemeColors(bool isLight)
        {
            if (isLight)
            {
                currentBgColor = new Color32(255, 255, 255, 255);
                currentLineColor = new Color32(0, 0, 0, 255);
            }
            else
            {
                currentBgColor = new Color32(0, 0, 0, 0);
                currentLineColor = new Color32(99, 102, 241, 255);
            }
        }

        public void DrawDirect()
        {
            isAnimating = false;
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            ExecuteDraw(data.iterations);
        }

        public void DrawAnimated(float stepDelay)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimationRoutine(stepDelay));
        }

        private IEnumerator AnimationRoutine(float delay)
        {
            isAnimating = true;
            int targetIterations = data.iterations;
            for (int i = 1; i <= targetIterations; i++)
            {
                ExecuteDraw(i);
                yield return new WaitForSeconds(delay);
            }
            isAnimating = false;
        }

        private void ExecuteDraw(int currentIteration)
        {
            if (data == null) return;

            currentDisplayIteration = currentIteration;

            int originalIter = data.iterations;
            data.iterations = currentIteration;
            string lString = LSystemGenerator.Generate(data);
            data.iterations = originalIter;

            var segments = LSystemGenerator.Interpret(lString, data.segmentLength, data.angle, Vector2.zero, data.startAngle);
            if (segments.Count == 0) return;

            // Calculate bounding box for dynamic scaling
            Vector2 min = segments[0].from, max = segments[0].from;
            foreach (var (f, t) in segments)
            {
                min = Vector2.Min(min, f); min = Vector2.Min(min, t);
                max = Vector2.Max(max, f); max = Vector2.Max(max, t);
            }

            float drawW = textureWidth - padding * 2;
            float drawH = textureHeight - padding * 2;
            float scale = Mathf.Min(
                drawW / Mathf.Max(max.x - min.x, 0.0001f),
                drawH / Mathf.Max(max.y - min.y, 0.0001f));

            float offsetX = padding + (drawW - (max.x - min.x) * scale) * 0.5f;
            float offsetY = padding + (drawH - (max.y - min.y) * scale) * 0.5f;

            // OPTIMIZATION: Reuse texture memory if dimensions are the same
            if (resultTexture == null || resultTexture.width != textureWidth || resultTexture.height != textureHeight)
            {
                if (resultTexture != null) Destroy(resultTexture);
                resultTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            }

            NativeArray<Color32> pixels = resultTexture.GetRawTextureData<Color32>();

            // Optimized background fill
            Color32 bg = currentBgColor;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            Color32 line = currentLineColor;
            foreach (var (from, to) in segments)
            {
                int x0 = Mathf.RoundToInt((from.x - min.x) * scale + offsetX);
                int y0 = Mathf.RoundToInt((max.y - from.y) * scale + offsetY);
                int x1 = Mathf.RoundToInt((to.x - min.x) * scale + offsetX);
                int y1 = Mathf.RoundToInt((max.y - to.y) * scale + offsetY);
                DrawLine(pixels, textureWidth, textureHeight, x0, y0, x1, y1, line);
            }

            resultTexture.Apply();

            // Notify UI that a new frame is ready
            OnTextureUpdated?.Invoke(resultTexture);
        }

        public void SaveToPNG()
        {
            if (resultTexture == null) return;

            byte[] bytes = resultTexture.EncodeToPNG();
            string defaultFileName = $"Fractal_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";

#if UNITY_WEBGL && !UNITY_EDITOR
            string base64 = System.Convert.ToBase64String(bytes);
            DownloadImage(defaultFileName, base64);
#else
            string userPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string downloadsPath = Path.Combine(userPath, "Downloads");

            if (!Directory.Exists(downloadsPath))
                Directory.CreateDirectory(downloadsPath);

#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save Fractal", downloadsPath, defaultFileName, "png");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllBytes(path, bytes);
                Debug.Log($"[LSystem] Image saved to: {path}");
            }
#else
            string path = Path.Combine(downloadsPath, defaultFileName);
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[LSystem] Image saved to: {path}");
#endif
#endif
        }

        private static void DrawLine(NativeArray<Color32> pixels, int w, int h, int x0, int y0, int x1, int y1, Color32 col)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                if (x0 >= 0 && x0 < w && y0 >= 0 && y0 < h) pixels[y0 * w + x0] = col;
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 >= dy) { if (x0 == x1) break; err += dy; x0 += sx; }
                if (e2 <= dx) { if (y0 == y1) break; err += dx; y0 += sy; }
            }
        }
    }
}