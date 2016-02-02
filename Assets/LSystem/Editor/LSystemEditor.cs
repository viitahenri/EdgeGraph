using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace LSystem
{
    [CustomEditor(typeof(LSystem))]
    public class LSystemEditor : Editor
    {
        LSystem sys;

        public static string[] interpreterMethods;
        static string[] ignoreMethods = new string[] { "Start", "Update" };

        static LSystemEditor()
        {
            string[] _interpreterMethods =
                typeof(Interpreter)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) // Instance methods, both public and private/protected
                .Where(x => x.DeclaringType == typeof(Interpreter)) // Only list methods defined in our own class
                                                                    //.Where(x => x.GetParameters().Length == 0) // Make sure we only get methods with zero argumenrts
                .Where(x => !ignoreMethods.Any(n => n == x.Name)) // Don't list methods in the ignoreMethods array (so we can exclude Unity specific methods, etc.)
                .Select(x => x.Name)
                .ToArray();

            interpreterMethods = new string[_interpreterMethods.Length + 1];
            interpreterMethods[0] = "Null";
            _interpreterMethods.CopyTo(interpreterMethods, 1);
        }

        void OnEnable()
        {
            sys = (LSystem)target;
        }

        public override void OnInspectorGUI()
        {
            #region Symbols
            if (sys.symbols == null) sys.symbols = new List<Symbol>();

            //public SymbolType type;
            //public char symbol;
            //public Interpreter interpreter;
            //public string interpreterMethod;

            EditorGUILayout.LabelField("Symbols", GUILayout.Width(100f));
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type", GUILayout.Width(100f));
            EditorGUILayout.LabelField("Symbol (1 char)", GUILayout.Width(100f));
            EditorGUILayout.LabelField("Interpreter", GUILayout.Width(200f));
            EditorGUILayout.LabelField("Method", GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < sys.symbols.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                sys.symbols[i].type = (Symbol.SymbolType)EditorGUILayout.EnumPopup(sys.symbols[i].type, GUILayout.Width(100f));

                string charText = sys.symbols[i].symbol.ToString();
                charText = EditorGUILayout.TextField(charText, GUILayout.Width(100f));
                if (charText.Length > 1)
                    charText = charText[0].ToString();

                if (charText.Length > 0)
                    sys.symbols[i].symbol = charText.ToCharArray()[0];

                Interpreter _interpreter = sys.GetComponent<Interpreter>();
                if (_interpreter == null) _interpreter = sys.GetComponentInChildren<Interpreter>();
                if (_interpreter != null) sys.symbols[i].interpreter = _interpreter;
                sys.symbols[i].interpreter = (Interpreter)EditorGUILayout.ObjectField(sys.symbols[i].interpreter, typeof(Interpreter), false, GUILayout.Width(200f));

                int index = 0;

                try
                {
                    index = interpreterMethods
                        .Select((v, j) => new { Name = v, Index = j })
                        .First(x => x.Name == sys.symbols[i].interpreterMethod)
                        .Index;
                }
                catch
                {
                    index = 0;
                }

                sys.symbols[i].interpreterMethod = interpreterMethods[EditorGUILayout.Popup(index, interpreterMethods)];

                if (GUILayout.Button("X", GUILayout.Width(20f)))
                {
                    sys.symbols.Remove(sys.symbols[i]);
                    Repaint();
                }
                //Change order

                if (GUILayout.Button("▲", GUILayout.Width(30f)))
                {
                    if (i > 0)
                    {
                        var temp = sys.symbols[i];
                        sys.symbols[i] = sys.symbols[i - 1];
                        sys.symbols[i - 1] = temp;
                    }
                }

                if (GUILayout.Button("▼", GUILayout.Width(30f)))
                {
                    if (i < sys.symbols.Count - 1)
                    {
                        var temp = sys.symbols[i];
                        sys.symbols[i] = sys.symbols[i + 1];
                        sys.symbols[i + 1] = temp;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add", GUILayout.Width(100f)))
            {
                sys.symbols.Add(new Symbol());
            }
            EditorGUILayout.EndVertical();
            #endregion

            #region Axiom
            EditorGUILayout.LabelField("Axiom", GUILayout.Width(100f));
            EditorGUILayout.BeginVertical("box");
            sys.axiom = EditorGUILayout.TextField(RemoveInvalidSymbols(sys.axiom));
            EditorGUILayout.EndVertical();
            #endregion

            #region Rules
            EditorGUILayout.LabelField("Rules", GUILayout.Width(100f));
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Left context", GUILayout.Width(100f));
            EditorGUILayout.LabelField("Predecessor", GUILayout.Width(150f));
            EditorGUILayout.LabelField("Right context", GUILayout.Width(100f));
            EditorGUILayout.LabelField("Successor", GUILayout.Width(150f));
            EditorGUILayout.LabelField("Probability", GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical("box");
            if (sys.rules == null) sys.rules = new List<Rule>();
            for (int i = 0; i < sys.rules.Count; i++)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(("Rule " + i), GUILayout.Width(50f));
                if (sys.rules[i].contextCount > 1)
                {
                    EditorGUILayout.LabelField(("Context sensitivity"), GUILayout.Width(120f));
                    sys.rules[i].contextSensitivity = (Rule.ContextSensitivity)EditorGUILayout.EnumPopup(sys.rules[i].contextSensitivity, GUILayout.Width(80f));
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal("box");

                //Left context
                sys.rules[i].leftContext = EditorGUILayout.TextField(RemoveInvalidSymbols(sys.rules[i].leftContext), GUILayout.Width(80f));
                EditorGUILayout.LabelField("<", GUILayout.Width(15f));

                //Predecessor
                string charText = sys.rules[i].predecessor.ToString();
                charText = EditorGUILayout.TextField(charText, GUILayout.Width(130f));
                if (charText.Length > 1)
                    charText = charText[0].ToString();

                if (charText.Length > 0)
                    sys.rules[i].predecessor = charText.ToCharArray()[0];
                EditorGUILayout.LabelField(">", GUILayout.Width(15f));

                //Right context
                sys.rules[i].rightContext = EditorGUILayout.TextField(RemoveInvalidSymbols(sys.rules[i].rightContext), GUILayout.Width(80f));
                EditorGUILayout.LabelField("->", GUILayout.Width(20f));

                //Context count
                if (sys.rules[i].leftContext != "" && sys.rules[i].rightContext != "")
                    sys.rules[i].contextCount = 2;
                else if (sys.rules[i].leftContext != "" || sys.rules[i].rightContext != "")
                    sys.rules[i].contextCount = 1;
                else
                    sys.rules[i].contextCount = 0;

                //Successor
                sys.rules[i].successsor = EditorGUILayout.TextField(RemoveInvalidSymbols(sys.rules[i].successsor), GUILayout.Width(130f));
                EditorGUILayout.LabelField(" :", GUILayout.Width(15f));

                //Probability
                sys.rules[i].probability = EditorGUILayout.FloatField(sys.rules[i].probability, GUILayout.Width(70f));
                if (sys.rules[i].probability <= 0f) sys.rules[i].probability = 0.01f;

                if (GUILayout.Button("X", GUILayout.Width(20f)))
                {
                    sys.rules.Remove(sys.rules[i]);
                    Repaint();
                }
                //Change order

                if (GUILayout.Button("▲", GUILayout.Width(30f)))
                {
                    if (i > 0)
                    {
                        Rule temp = sys.rules[i];
                        sys.rules[i] = sys.rules[i - 1];
                        sys.rules[i - 1] = temp;
                    }
                }

                if (GUILayout.Button("▼", GUILayout.Width(30f)))
                {
                    if (i < sys.rules.Count - 1)
                    {
                        Rule temp = sys.rules[i];
                        sys.rules[i] = sys.rules[i + 1];
                        sys.rules[i + 1] = temp;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add", GUILayout.Width(100f)))
            {
                sys.rules.Add(new Rule());

                Repaint();
            }
            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Iteration count", GUILayout.Width(100f));
            sys.iterationCount = EditorGUILayout.IntField(sys.iterationCount, GUILayout.Width(70f));
            EditorGUILayout.LabelField("Debug logging", GUILayout.Width(100f));
            sys.debugLogging = EditorGUILayout.Toggle(sys.debugLogging, GUILayout.Width(30f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Process", GUILayout.Width(100f)))
            {
                //MentalTools.MentalWorker worker = new MentalTools.MentalWorker(() => sys.Step(n));
                sys.Process();
            }
            if (GUILayout.Button("Process new", GUILayout.Width(100f)))
            {
                sys.StopProcess();
                sys.current = sys.axiom;
                if (sys.symbols[0].interpreter)
                    sys.symbols[0].interpreter.SendMessage("EmptyValues", SendMessageOptions.DontRequireReceiver);
                sys.Process();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset", GUILayout.Width(100f)))
            {
                sys.StopProcess();
                sys.current = sys.axiom;
                if (sys.symbols[0].interpreter)
                    sys.symbols[0].interpreter.SendMessage("EmptyValues", SendMessageOptions.DontRequireReceiver);
            }

            EditorGUILayout.LabelField("Current state: " + sys.currentState.ToString());
            EditorGUILayout.LabelField("Current step: " + sys.CurrentStep);
            EditorGUILayout.LabelField("Current char: " + sys.CurrentChar);
            EditorGUILayout.LabelField("Current string length: " + sys.StringLength);
            Repaint();

            string current = "";
            if (sys.current.Length > 50)
            {
                char[] carr = new char[50];
                sys.current.CopyTo(0, carr, 0, 50);
                current = new string(carr);
            }
            else
                current = sys.current;

            current = EditorGUILayout.TextField("Current string", current);

            if (GUI.changed)
                EditorUtility.SetDirty(sys);
        }

        /// <summary>
        /// Go through the string and remove symbols that aren't in use in target LSystem
        /// </summary>
        string RemoveInvalidSymbols(string input)
        {
            if (input == null) return "";

            string retval = "";
            for (int i = 0; i < input.Length; i++)
            {
                Symbol containsC = sys.symbols.Find(s => s.symbol == input[i]);
                if (containsC != null)
                {
                    retval = retval + containsC.symbol.ToString();
                }
            }

            return retval;
        }
    }
}