using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSystem
{
    /// <summary>
    /// Represents a single production rule for the L-System rewriting process.
    /// </summary>
    [Serializable]
    public class LSystemRule
    {
        public char symbol;
        public string replacement;
    }

    /// <summary>
    /// ScriptableObject containing the core topological and geometric parameters 
    /// required to generate and render an L-System fractal.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLSystem", menuName = "L-System/Data")]
    public class LSystemData : ScriptableObject
    {
        [Tooltip("The display name of the fractal.")]
        public string fractalName = "NewFractal";

        [Tooltip("The initial state (axiom) of the system.")]
        public string axiom = "F";

        [Tooltip("The turning angle in degrees for the turtle graphics.")]
        public float angle = 60f;

        [Tooltip("The length of a single forward segment.")]
        public float segmentLength = 1f;

        [Tooltip("Number of recursive iterations (depth) to perform.")]
        public int iterations = 5;

        [Tooltip("The initial rotation offset in degrees.")]
        public float startAngle = 0f;

        public List<LSystemRule> rules = new List<LSystemRule>();

        /// <summary>
        /// Converts the list of rules into a dictionary for O(1) lookups during the generation phase.
        /// </summary>
        /// <returns>A dictionary mapping characters to their string replacements.</returns>
        public Dictionary<char, string> GetRuleDictionary()
        {
            var dict = new Dictionary<char, string>();
            foreach (var rule in rules)
            {
                if (!string.IsNullOrEmpty(rule.replacement) && !dict.ContainsKey(rule.symbol))
                    dict[rule.symbol] = rule.replacement;
            }
            return dict;
        }
    }
}