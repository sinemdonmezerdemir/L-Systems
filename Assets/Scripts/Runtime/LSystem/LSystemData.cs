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
        [SerializeField] private string fractalName = "NewFractal";
        public string FractalName => fractalName;

        [SerializeField] private string axiom = "F";
        public string Axiom => axiom;

        [SerializeField] private float angle = 60f;
        public float Angle => angle;

        [SerializeField] private int thickness = 1;
        public int Thickness => thickness;

        [SerializeField] private int iterations = 5;
        public int Iterations
        {
            get => iterations;
            set => iterations = value;
        }

        [SerializeField] private float startAngle = 0f;
        public float StartAngle => startAngle;

        [SerializeField] private List<LSystemRule> rules = new List<LSystemRule>();
        public IReadOnlyList<LSystemRule> Rules => rules;

        public Dictionary<char, string> BuildRuleDictionary()
        {
            var dict = new Dictionary<char, string>();
            foreach (var rule in rules)
            {
                if (!string.IsNullOrEmpty(rule.replacement) && !dict.ContainsKey(rule.symbol))
                {
                    dict[rule.symbol] = rule.replacement;
                }
            }
            return dict;
        }

        public void UpdateData(string newName, string newAxiom, float newAngle, int newThickness, float newStartAngle, int newIterations, List<LSystemRule> newRules)
        {
            fractalName = newName;
            axiom = newAxiom;
            angle = newAngle;
            thickness = newThickness;
            startAngle = newStartAngle;
            iterations = newIterations;

            rules.Clear();
            if (newRules != null)
            {
                rules.AddRange(newRules);
            }
        }
    }
}