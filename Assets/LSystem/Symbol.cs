using System;

namespace LSystem
{
    [Serializable]
    public class Symbol : Object
    {
        public enum SymbolType
        {
            Variable,
            Constant
        }

        public SymbolType type;
        public char symbol;
        public Interpreter interpreter;
        public string interpreterMethod;

        public Symbol()
        {
        }

        public Symbol(char _symbol, SymbolType _type, Interpreter _interpreter, string _interpreterMethod)
        {
            symbol = _symbol;
            type = _type;
            interpreter = _interpreter;
            interpreterMethod = _interpreterMethod;
        }
    }
}