using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSystem
{
    public static class LSystemGenerator
    {
        private struct CacheKey : IEquatable<CacheKey>
        {
            public char symbol;
            public int depth;

            public CacheKey(char s, int d) { symbol = s; depth = d; }
            public bool Equals(CacheKey other) => symbol == other.symbol && depth == other.depth;
            public override int GetHashCode() => (symbol.GetHashCode() * 397) ^ depth;
        }

        private struct SubTreeData
        {
            public Vector2 endPos;
            public int endDirOffset;
            public float maxRadius;
            public bool hasDraws;
        }

        private static readonly Dictionary<CacheKey, SubTreeData> cache = new Dictionary<CacheKey, SubTreeData>(1024);
        private static readonly Stack<(Vector2, int)> sharedStack = new Stack<(Vector2, int)>(512);

        private static Dictionary<char, string> rulesDict;
        private static float angleDeg;

        public static List<(Vector2 from, Vector2 to)> GenerateWithLOD(
            LSystemData data, float viewWidth, float viewHeight, float prunePixelThreshold)
        {
            var segments = new List<(Vector2, Vector2)>(16384);

            if (data == null || string.IsNullOrEmpty(data.axiom)) return segments;

            rulesDict = data.GetRuleDictionary();
            angleDeg = data.angle;
            cache.Clear();

            sharedStack.Clear();
            SubTreeData axiomData = ComputeStringTopology(data.axiom, data.iterations);

            float safeDiameter = axiomData.maxRadius * 2f;
            float preScale = Mathf.Min(viewWidth, viewHeight) / Mathf.Max(safeDiameter, 0.0001f);

            sharedStack.Clear();
            Vector2 currentPos = Vector2.zero;
            int currentDir = 0;
            ProcessStringWithPruning(data.axiom, data.iterations, ref currentPos, ref currentDir, segments, preScale, prunePixelThreshold);

            return segments;
        }

        private static SubTreeData ComputeSubTree(char symbol, int depth)
        {
            var key = new CacheKey(symbol, depth);
            if (cache.TryGetValue(key, out var cached)) return cached;

            SubTreeData result = new SubTreeData { hasDraws = false, maxRadius = 0f };

            if (depth == 0 || !rulesDict.ContainsKey(symbol))
            {
                if (symbol == 'F' || symbol == 'G' || symbol == 'f')
                {
                    result.endPos = new Vector2(1, 0);
                    result.endDirOffset = 0;
                    result.maxRadius = 1f;
                    result.hasDraws = (symbol != 'f');
                }
                else if (symbol == '+') result.endDirOffset = 1;
                else if (symbol == '-') result.endDirOffset = -1;

                cache[key] = result;
                return result;
            }

            result = ComputeStringTopology(rulesDict[symbol], depth - 1);
            cache[key] = result;
            return result;
        }

        private static SubTreeData ComputeStringTopology(string str, int depth)
        {
            SubTreeData result = new SubTreeData { hasDraws = false };
            Vector2 pos = Vector2.zero;
            int dir = 0;
            float maxRad = 0f;

            foreach (char c in str)
            {
                if (c == '[')
                {
                    sharedStack.Push((pos, dir));
                    continue;
                }

                if (c == ']')
                {
                    if (sharedStack.Count > 0)
                    {
                        var s = sharedStack.Pop();
                        pos = s.Item1;
                        dir = s.Item2;
                    }
                    continue;
                }

                SubTreeData child = ComputeSubTree(c, depth);

                if (child.hasDraws) result.hasDraws = true;

                float reach = pos.magnitude + child.maxRadius;
                if (reach > maxRad) maxRad = reach;

                pos += RotateVector(child.endPos, dir);
                dir += child.endDirOffset;

                if (pos.magnitude > maxRad) maxRad = pos.magnitude;
            }

            result.endPos = pos;
            result.endDirOffset = dir;
            result.maxRadius = maxRad;
            return result;
        }

        private static void ProcessStringWithPruning(string str, int depth, ref Vector2 pos, ref int dir, List<(Vector2, Vector2)> segments, float scale, float threshold)
        {
            foreach (char c in str)
            {
                if (c == '[')
                {
                    sharedStack.Push((pos, dir));
                }
                else if (c == ']')
                {
                    if (sharedStack.Count > 0)
                    {
                        var s = sharedStack.Pop();
                        pos = s.Item1;
                        dir = s.Item2;
                    }
                }
                else
                {
                    GeneratePruned(c, depth, ref pos, ref dir, segments, scale, threshold);
                }
            }
        }

        private static void GeneratePruned(char symbol, int depth, ref Vector2 pos, ref int dir, List<(Vector2, Vector2)> segments, float scale, float threshold)
        {
            if (depth == 0 || !rulesDict.ContainsKey(symbol))
            {
                if (symbol == 'F' || symbol == 'G')
                {
                    Vector2 next = pos + RotateVector(new Vector2(1, 0), dir);
                    segments.Add((pos, next));
                    pos = next;
                }
                else if (symbol == 'f') pos += RotateVector(new Vector2(1, 0), dir);
                else if (symbol == '+') dir++;
                else if (symbol == '-') dir--;
                return;
            }

            SubTreeData data = cache[new CacheKey(symbol, depth)];
            float diameter = data.maxRadius * 2f;

            if (diameter * scale < threshold)
            {
                Vector2 endOffset = RotateVector(data.endPos, dir);
                Vector2 next = pos + endOffset;

                if (data.hasDraws && endOffset.sqrMagnitude > 0.00001f)
                {
                    segments.Add((pos, next));
                }

                pos = next;
                dir += data.endDirOffset;
                return;
            }

            ProcessStringWithPruning(rulesDict[symbol], depth - 1, ref pos, ref dir, segments, scale, threshold);
        }

        private static Vector2 RotateVector(Vector2 v, int dirIndex)
        {
            if (v.sqrMagnitude == 0) return v;
            float rad = dirIndex * angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }
    }
}