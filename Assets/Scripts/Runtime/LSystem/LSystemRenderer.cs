using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace LSystem
{
    public class LSystemRenderer : MonoBehaviour
    {
        public LSystemData data;

        public Texture2D resultTexture;
        public int textureWidth = 1024;
        public int textureHeight = 1024;
        public int padding = 20;

        public float lodPruneThreshold = 0.5f;

        public int currentDisplayIteration = 0;
        public bool isAnimating = false;

        public Color32 currentBgColor = new Color32(0, 0, 0, 0);
        public Color32 currentLineColor = new Color32(99, 102, 241, 255);

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

        public void RenderImmediate()
        {
            isAnimating = false;
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            ProcessRenderPass(data.Iterations);
        }

        public void RenderAnimated(float stepDelay)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimationRoutine(stepDelay));
        }

        private IEnumerator AnimationRoutine(float delay)
        {
            isAnimating = true;
            int targetIterations = data.Iterations;

            var wait = new WaitForSeconds(delay);

            for (int i = 1; i <= targetIterations; i++)
            {
                ProcessRenderPass(i);
                yield return wait;
            }
            isAnimating = false;
        }

        private void ProcessRenderPass(int currentIteration)
        {
            if (data == null) return;

            currentDisplayIteration = currentIteration;

            int originalIter = data.Iterations;
            data.Iterations = currentIteration;

            float drawW = textureWidth - padding * 2;
            float drawH = textureHeight - padding * 2;

            var segments = LSystemGenerator.GenerateWithLOD(data, drawW, drawH, lodPruneThreshold);

            data.Iterations = originalIter;

            if (segments.Count == 0) return;

            float startRad = data.StartAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(startRad);
            float sin = Mathf.Sin(startRad);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < segments.Count; i++)
            {
                var (f, t) = segments[i];

                Vector2 rFrom = new Vector2(f.x * cos - f.y * sin, f.x * sin + f.y * cos);
                Vector2 rTo = new Vector2(t.x * cos - t.y * sin, t.x * sin + t.y * cos);

                segments[i] = (rFrom, rTo);

                min = Vector2.Min(min, Vector2.Min(rFrom, rTo));
                max = Vector2.Max(max, Vector2.Max(rFrom, rTo));
            }

            float scale = Mathf.Min(
                drawW / Mathf.Max(max.x - min.x, 0.0001f),
                drawH / Mathf.Max(max.y - min.y, 0.0001f));

            float offsetX = padding + (drawW - (max.x - min.x) * scale) * 0.5f;
            float offsetY = padding + (drawH - (max.y - min.y) * scale) * 0.5f;

            if (resultTexture == null || resultTexture.width != textureWidth || resultTexture.height != textureHeight)
            {
                if (resultTexture != null) Destroy(resultTexture);
                resultTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            }

            NativeArray<Color32> pixels = resultTexture.GetRawTextureData<Color32>();

            Color32 bg = currentBgColor;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            Color32 lineCol = currentLineColor;
            int thick = data.Thickness;

            foreach (var (from, to) in segments)
            {
                int x0 = Mathf.RoundToInt((from.x - min.x) * scale + offsetX);
                int y0 = Mathf.RoundToInt((from.y - min.y) * scale + offsetY);
                int x1 = Mathf.RoundToInt((to.x - min.x) * scale + offsetX);
                int y1 = Mathf.RoundToInt((to.y - min.y) * scale + offsetY);

                DrawLine(pixels, textureWidth, textureHeight, x0, y0, x1, y1, lineCol, thick);
            }

            resultTexture.Apply();
            OnTextureUpdated?.Invoke(resultTexture);
        }

        public void SaveToPNG()
        {
            if (resultTexture == null) return;

            byte[] bytes = resultTexture.EncodeToPNG();
            string defaultFileName = $"Fractal_{DateTime.Now:yyyyMMdd_HHmmss}.png";

#if UNITY_WEBGL && !UNITY_EDITOR
            string base64 = Convert.ToBase64String(bytes);
            DownloadImage(defaultFileName, base64);
#else
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloadsPath = Path.Combine(userPath, "Downloads");

            if (!Directory.Exists(downloadsPath)) Directory.CreateDirectory(downloadsPath);

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

        private static void DrawLine(NativeArray<Color32> pixels, int w, int h, int x0, int y0, int x1, int y1, Color32 col, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            bool isXDominant = dx > -dy;

            int halfThick = thickness / 2;
            int thickRem = (thickness - 1) / 2;

            while (true)
            {
                if (isXDominant)
                {
                    for (int j = -halfThick; j <= thickRem; j++)
                    {
                        int py = y0 + j;
                        if (x0 >= 0 && x0 < w && py >= 0 && py < h)
                            pixels[py * w + x0] = col;
                    }
                }
                else
                {
                    for (int i = -halfThick; i <= thickRem; i++)
                    {
                        int px = x0 + i;
                        if (px >= 0 && px < w && y0 >= 0 && y0 < h)
                            pixels[y0 * w + px] = col;
                    }
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 >= dy) { if (x0 == x1) break; err += dy; x0 += sx; }
                if (e2 <= dx) { if (y0 == y1) break; err += dx; y0 += sy; }
            }
        }

        private void OnDestroy()
        {
            if (resultTexture != null)
            {
                Destroy(resultTexture);
            }
        }
    }
}