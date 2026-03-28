using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LSystem
{
    /// <summary>
    /// Core engine responsible for processing the string rewriting logic and interpreting 
    /// the resulting string into 2D geometric segments (Turtle Graphics).
    /// </summary>
    public static class LSystemGenerator
    {
        /// <summary>
        /// Expands the axiom based on the provided production rules and iteration depth.
        /// </summary>
        public static string Generate(LSystemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.axiom)) return string.Empty;

            var rules = data.GetRuleDictionary();

            // Pre-calculate capacity to minimize memory allocations during string expansion.
            // Length scales exponentially based on iteration count.
            int estimatedCapacity = data.axiom.Length * (int)Mathf.Pow(2, data.iterations);
            var current = new StringBuilder(data.axiom, estimatedCapacity);

            for (int i = 0; i < data.iterations; i++)
            {
                var next = new StringBuilder(current.Length * 2);
                for (int j = 0; j < current.Length; j++)
                {
                    char c = current[j];
                    if (rules.TryGetValue(c, out string replacement))
                        next.Append(replacement);
                    else
                        next.Append(c);
                }
                current = next;
            }
            return current.ToString();
        }

        /// <summary>
        /// Interprets the generated L-System string into physical 2D coordinates.
        /// </summary>
        public static List<(Vector2 from, Vector2 to)> Interpret(
            string lString, float segmentLength, float angleDegrees, Vector2 startPosition, float startAngle)
        {
            var segments = new List<(Vector2, Vector2)>(lString.Length);
            var stack = new Stack<(Vector2 pos, int dirIndex)>();

            Vector2 pos = startPosition;
            int dirIndex = 0;

            foreach (char c in lString)
            {
                switch (c)
                {
                    case 'F':
                    case 'G':
                        {
                            float currentAngleDeg = startAngle + (dirIndex * angleDegrees);
                            Vector2 dir = AngleToDir(currentAngleDeg);
                            Vector2 next = pos + dir * segmentLength;
                            segments.Add((pos, next));
                            pos = next;
                            break;
                        }
                    case 'f':
                        {
                            float currentAngleDeg = startAngle + (dirIndex * angleDegrees);
                            pos += AngleToDir(currentAngleDeg) * segmentLength;
                            break;
                        }
                    case '+':
                        dirIndex++;
                        break;
                    case '-':
                        dirIndex--;
                        break;
                    case '[':
                        stack.Push((pos, dirIndex));
                        break;
                    case ']':
                        if (stack.Count > 0)
                        {
                            var state = stack.Pop();
                            pos = state.pos;
                            dirIndex = state.dirIndex;
                        }
                        break;
                }
            }
            return segments;
        }

        /// <summary>
        /// Converts an angle in degrees to a normalized 2D direction vector.
        /// </summary>
        private static Vector2 AngleToDir(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}