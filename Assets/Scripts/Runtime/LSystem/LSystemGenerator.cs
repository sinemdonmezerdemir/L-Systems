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

        private struct SubTreeMetrics
        {
            public Vector2 endPos;
            public int endDirOffset;
            public float maxRadius;
            public bool containsVisibleSegments;
        }

        private class GenerationContext
        {
            public readonly Dictionary<CacheKey, SubTreeMetrics> Cache = new Dictionary<CacheKey, SubTreeMetrics>(1024);
            public readonly Stack<(Vector2, int)> TransformStack = new Stack<(Vector2, int)>(512);
            public Dictionary<char, string> RulesDict;
            public float AngleDeg;

            public void Clear()
            {
                Cache.Clear();
                TransformStack.Clear();
                RulesDict = null;
                AngleDeg = 0f;
            }
        }

        [ThreadStatic]
        private static GenerationContext _sharedContext;

        private static GenerationContext GetContext()
        {
            if (_sharedContext == null) _sharedContext = new GenerationContext();
            _sharedContext.Clear();
            return _sharedContext;
        }

        public static List<(Vector2 from, Vector2 to)> GenerateWithLOD(
            LSystemData data, float viewWidth, float viewHeight, float prunePixelThreshold)
        {
            var segments = new List<(Vector2, Vector2)>(16384);

            if (data == null || string.IsNullOrEmpty(data.Axiom)) return segments;

            var ctx = GetContext();
            ctx.RulesDict = data.BuildRuleDictionary();
            ctx.AngleDeg = data.Angle;

            SubTreeMetrics axiomMetrics = ComputeStringMetrics(data.Axiom, data.Iterations, ctx);

            float safeDiameter = axiomMetrics.maxRadius * 2f;
            float preScale = Mathf.Min(viewWidth, viewHeight) / Mathf.Max(safeDiameter, 0.0001f);

            ctx.TransformStack.Clear();
            Vector2 currentPos = Vector2.zero;
            int currentDir = 0;
            ProcessStringWithPruning(data.Axiom, data.Iterations, ref currentPos, ref currentDir, segments, preScale, prunePixelThreshold, ctx);

            return segments;
        }

        private static SubTreeMetrics EvaluateSymbolMetrics(char symbol, int depth, GenerationContext ctx)
        {
            var key = new CacheKey(symbol, depth);
            if (ctx.Cache.TryGetValue(key, out var cached)) return cached;

            SubTreeMetrics result = new SubTreeMetrics { containsVisibleSegments = false, maxRadius = 0f };

            if (depth == 0 || !ctx.RulesDict.ContainsKey(symbol))
            {
                switch (symbol)
                {
                    case 'F' or 'G' or 'f':
                        result.endPos = new Vector2(1, 0);
                        result.endDirOffset = 0;
                        result.maxRadius = 1f;
                        result.containsVisibleSegments = (symbol != 'f');
                        break;
                    case '+':
                        result.endDirOffset = 1;
                        break;
                    case '-':
                        result.endDirOffset = -1;
                        break;
                }

                ctx.Cache[key] = result;
                return result;
            }

            result = ComputeStringMetrics(ctx.RulesDict[symbol], depth - 1, ctx);
            ctx.Cache[key] = result;
            return result;
        }

        private static SubTreeMetrics ComputeStringMetrics(string str, int depth, GenerationContext ctx)
        {
            SubTreeMetrics result = new SubTreeMetrics { containsVisibleSegments = false };
            Vector2 pos = Vector2.zero;
            int dir = 0;
            float maxRad = 0f;

            foreach (char c in str)
            {
                if (c == '[')
                {
                    ctx.TransformStack.Push((pos, dir));
                    continue;
                }

                if (c == ']')
                {
                    if (ctx.TransformStack.Count > 0)
                    {
                        var s = ctx.TransformStack.Pop();
                        pos = s.Item1;
                        dir = s.Item2;
                    }
                    continue;
                }

                SubTreeMetrics child = EvaluateSymbolMetrics(c, depth, ctx);

                if (child.containsVisibleSegments) result.containsVisibleSegments = true;

                float reach = pos.magnitude + child.maxRadius;
                if (reach > maxRad) maxRad = reach;

                pos += RotateVector(child.endPos, dir, ctx.AngleDeg);
                dir += child.endDirOffset;

                if (pos.magnitude > maxRad) maxRad = pos.magnitude;
            }

            result.endPos = pos;
            result.endDirOffset = dir;
            result.maxRadius = maxRad;
            return result;
        }

        private static void ProcessStringWithPruning(string str, int depth, ref Vector2 pos, ref int dir, List<(Vector2, Vector2)> segments, float scale, float threshold, GenerationContext ctx)
        {
            foreach (char c in str)
            {
                if (c == '[')
                {
                    ctx.TransformStack.Push((pos, dir));
                }
                else if (c == ']')
                {
                    if (ctx.TransformStack.Count > 0)
                    {
                        var s = ctx.TransformStack.Pop();
                        pos = s.Item1;
                        dir = s.Item2;
                    }
                }
                else
                {
                    GeneratePruned(c, depth, ref pos, ref dir, segments, scale, threshold, ctx);
                }
            }
        }

        private static void GeneratePruned(char symbol, int depth, ref Vector2 pos, ref int dir, List<(Vector2, Vector2)> segments, float scale, float threshold, GenerationContext ctx)
        {
            if (depth == 0 || !ctx.RulesDict.ContainsKey(symbol))
            {
                if (symbol == 'F' || symbol == 'G')
                {
                    Vector2 next = pos + RotateVector(new Vector2(1, 0), dir, ctx.AngleDeg);
                    segments.Add((pos, next));
                    pos = next;
                }
                else if (symbol == 'f') pos += RotateVector(new Vector2(1, 0), dir, ctx.AngleDeg);
                else if (symbol == '+') dir++;
                else if (symbol == '-') dir--;
                return;
            }

            SubTreeMetrics metrics = ctx.Cache[new CacheKey(symbol, depth)];
            float diameter = metrics.maxRadius * 2f;

            if (diameter * scale < threshold)
            {
                Vector2 endOffset = RotateVector(metrics.endPos, dir, ctx.AngleDeg);
                Vector2 next = pos + endOffset;

                if (metrics.containsVisibleSegments && endOffset.sqrMagnitude > 0.00001f)
                {
                    segments.Add((pos, next));
                }

                pos = next;
                dir += metrics.endDirOffset;
                return;
            }

            ProcessStringWithPruning(ctx.RulesDict[symbol], depth - 1, ref pos, ref dir, segments, scale, threshold, ctx);
        }

        private static Vector2 RotateVector(Vector2 v, int dirIndex, float angleDeg)
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