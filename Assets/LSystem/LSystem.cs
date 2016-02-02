using UnityEngine;
using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace LSystem
{
    [ExecuteInEditMode]
    public class LSystem : MonoBehaviour
    {
        public enum ProcessingState
        {
            None,
            ConstructingString,
            CallingInterpreter
        }

        public ProcessingState currentState = ProcessingState.None;

        public List<Symbol> symbols;
        public string axiom;
        public List<Rule> rules;

        public string current = "";
        public int iterationCount = 0;

        public bool debugLogging = false;

        //Progress variables for UI
        private int currentStep = 0;
        private int currentChar = 0;
        private int stringLength = 0;

        private Thread processingThread;

        public int CurrentStep { get { return currentStep; } }
        public int CurrentChar { get { return currentChar; } }
        public int StringLength { get { return stringLength; } }

        Symbol CharToSymbol(char c)
        {
            Symbol retval = null;
            symbols.ForEach((Symbol s) =>
            {
                if (s.symbol == c)
                    retval = s;
            });

            return retval;
        }

        void Update()
        {
            if (currentState == ProcessingState.CallingInterpreter)
            {
                CallInterpreter();
            }
        }

        public void Process()
        {
            if (processingThread != null && processingThread.IsAlive)
            {
                PrintDebugLog("LSystem::Process() - Old thread is still alive, Aborting...");
                processingThread.Abort();
            }

            PrintDebugLog("LSystem::Process() - Starting new thread...");
            processingThread = new Thread(() =>
            {
                Step(iterationCount);
            });
            processingThread.Start();
            //Step(steps);
        }

        public void StopProcess()
        {
            if (processingThread != null && processingThread.IsAlive)
            {
                PrintDebugLog("LSystem::StopProcess() - Aborting the thread...");
                processingThread.Abort();
                processingThread.Join(100);
            }

            currentStep = 0;
            stringLength = 0;
            currentChar = char.MinValue;
        }

        void Step(int n)
        {
            PrintDebugLog("LSystem::Step() - Starting string construction.");

            currentState = ProcessingState.ConstructingString;

            for (int j = 0; j < n; j++)
            {
                currentStep = j + 1;

                Interpreter interp = (Interpreter)symbols[0].interpreter;
                if (interp != null)
                    interp.EmptyValues();

                if (current == "") current = axiom;

                string newCurrent = "";
                for (int i = 0; i < current.Length; i++)
                {
                    currentChar = i;
                    try
                    {
                        newCurrent += ApplyRules(current, i);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        PrintDebugLog("LSystem::Step() - Error on applying rules: " + e);
                    }
                }

                current = newCurrent;
                stringLength = current.Length;
            }

            PrintDebugLog("LSystem::Step() - String construction finished.");

            currentState = ProcessingState.CallingInterpreter;

            CallInterpreter();
        }

        void CallInterpreter()
        {
            currentState = ProcessingState.None;

            PrintDebugLog("LSystem::Step() - Starting the interpreter calls.");

            for (int i = 0; i < current.Length; i++)
            {
                currentChar = i;
                stringLength = current.Length;

                Symbol s = CharToSymbol(current[i]);
                if (s != null && s.interpreterMethod != "Null")
                {
                    typeof(Interpreter)
                        .GetMethod(s.interpreterMethod, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Invoke(s.interpreter, new object[0]);

                    //sInterp.SendMessage(s.interpreterMethod, SendMessageOptions.DontRequireReceiver);
                }
            }

            PrintDebugLog("LSystem::Step() - The interpreter calls finished.");
        }

        string ApplyRules(string _str, int _index)
        {
            char c = _str[_index];

            List<Rule> relevantRules = new List<Rule>();
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].predecessor == c)
                    relevantRules.Add(rules[i]);
            }

            if (relevantRules == null || relevantRules.Count <= 0) return c.ToString();

            PrintDebugLog("LSystem::ApplyRules() - index: " + _index + ", Relevant rules found: " + relevantRules.Count + ", Predecessor: " + c);
            for (int i = 0; i < relevantRules.Count; i++)
            {
                PrintDebugLog("i = " + i + ": " + relevantRules[i].ToString());
            }

            //Context check
            for (int i = relevantRules.Count - 1; i >= 0; i--)
            {
                //Without contexts the rule passes
                if (relevantRules[i].contextCount == 0)
                    continue;

                PrintDebugLog("LSystem::ApplyRules() - Checking rule " + relevantRules[i].ToString());

                //One context is given
                if (relevantRules[i].contextCount == 1)
                {
                    bool givenContextIsLeft;
                    if (relevantRules[i].leftContext != "")
                        givenContextIsLeft = true;
                    else
                        givenContextIsLeft = false;

                    PrintDebugLog("LSystem::ApplyRules() - One context given, it is " + (givenContextIsLeft ? "left" : "right"));

                    if (givenContextIsLeft)
                    {
                        if (CheckLeftContext(relevantRules[i], _str, _index))
                        {
                            PrintDebugLog("LSystem::ApplyRules() - Left context matched.");
                            continue;
                        }
                    }
                    else
                    {
                        if (CheckRightContext(relevantRules[i], _str, _index))
                        {
                            PrintDebugLog("LSystem::ApplyRules() - Right context matched.");
                            continue;
                        }
                    }
                }

                //Both contexts are given
                if (relevantRules[i].contextCount == 2)
                {
                    bool leftMatches = CheckLeftContext(relevantRules[i], _str, _index);
                    bool rightMatches = CheckRightContext(relevantRules[i], _str, _index);

                    if (relevantRules[i].contextSensitivity == Rule.ContextSensitivity.OR)
                    {
                        if (leftMatches || rightMatches)
                        {
                            PrintDebugLog("LSystem::ApplyRules() - OR sensitivity: One context matched, " + " Left: " + leftMatches + ", Right: " + rightMatches);
                            continue;
                        }
                    }

                    if (relevantRules[i].contextSensitivity == Rule.ContextSensitivity.AND)
                    {
                        if (leftMatches && rightMatches)
                        {
                            PrintDebugLog("LSystem::ApplyRules() - AND sensitivity: One context matched, " + " Left: " + leftMatches + ", Right: " + rightMatches);
                            continue;
                        }
                    }
                }

                PrintDebugLog("LSystem::ApplyRules() - Removing rule " + relevantRules[i].ToString());
                relevantRules.Remove(relevantRules[i]);
            }

            PrintDebugLog("LSystem::ApplyRules() - index: " + _index + ", Relevant rules left: " + relevantRules.Count + ", Predecessor: " + c);

            //If rules with context passed, use them only for probability checks
            List<Rule> relevantRulesWithContext = new List<Rule>();
            for (int i = 0; i < relevantRules.Count; i++)
            {
                if (relevantRules[i].contextCount > 0)
                    relevantRulesWithContext.Add(relevantRules[i]);
            }

            if (relevantRulesWithContext.Count > 0)
            {
                PrintDebugLog("LSystem::ApplyRules() - Rules with context passed, using them only for probability.");
                relevantRules = relevantRulesWithContext;
            }

            Rule chosen = null;

            if (relevantRules.Count > 0)
            {
                if (relevantRules.Count == 1)
                {
                    chosen = relevantRules[0];
                }
                else
                {
                    //Probability
                    float sumWeights = 0f;
                    relevantRules.ForEach(r => sumWeights += r.probability);

                    System.Random rObj = new System.Random(Guid.NewGuid().GetHashCode());
                    float rand = (float)rObj.NextDouble() * sumWeights;
                    PrintDebugLog("LSystem::ApplyRules() - Probability random value: " + rand);

                    for (int i = 0; i < relevantRules.Count; i++)
                    {
                        if (rand < relevantRules[i].probability)
                        {
                            chosen = relevantRules[i];
                            break;
                        }
                        rand -= relevantRules[i].probability;
                    }

                    if (chosen == null)
                    {
                        Debug.LogError("LSystem::ApplyRules() - Chosen rule null after probability! Something is wrong! Random was " + rand);
                    }
                }
            }
            else
            {
                PrintDebugLog("LSystem::ApplyRules() - None of the relevant rules applied.");
                return c.ToString();
            }

            PrintDebugLog("LSystem::ApplyRules() - index: " + _index + ", Chosen rule: " + chosen.ToString());
            return chosen.successsor;
        }

        bool CheckLeftContext(Rule _rule, string _str, int _index)
        {
            if (_index - _rule.leftContext.Length > 0 && _str.Length > _rule.leftContext.Length)
            {
                string leftContextInStr = _str.Substring(_index - _rule.leftContext.Length, _rule.leftContext.Length);
                bool leftContextMatch = leftContextInStr.CompareTo(_rule.leftContext) == 0;
                if (leftContextInStr != "")
                {
                    PrintDebugLog("LSystem::CheckLeftContext() - Left context: " + _rule.leftContext);
                    PrintDebugLog("LSystem::CheckLeftContext() - Left context in current: " + leftContextInStr);
                }

                return leftContextMatch;
            }

            return false;
        }

        bool CheckRightContext(Rule _rule, string _str, int _index)
        {
            if (_index + _rule.rightContext.Length + 1 < _str.Length)
            {
                string rightContextInStr = _str.Substring(_index + 1, _rule.rightContext.Length);
                bool rightContextMatch = rightContextInStr.CompareTo(_rule.rightContext) == 0;
                if (rightContextInStr != "")
                {
                    PrintDebugLog("LSystem::CheckRightContext() - Right context: " + _rule.rightContext);
                    PrintDebugLog("LSystem::CheckRightContext() - Right context in current: " + rightContextInStr);
                }

                return rightContextMatch;
            }

            return false;
        }

        void PrintDebugLog(string msg)
        {
            if (debugLogging)
                Debug.Log(msg);
        }
    }
}