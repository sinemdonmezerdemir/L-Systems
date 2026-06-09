using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSystem
{
    [Serializable]
    public class LSystemRule
    {
        public char symbol;
        public string replacement;
    }

    [CreateAssetMenu(fileName = "NewLSystem", menuName = "L-System/Data")]
    public class LSystemData : ScriptableObject
    {
        public string fractalName = "NewFractal";

        public string axiom = "F";

        public float angle = 60f;

        public int thickness = 1;

        public int iterations = 5;

        public float startAngle = 0f;

        public List<LSystemRule> rules = new List<LSystemRule>();

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