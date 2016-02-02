namespace LSystem
{
    [System.Serializable]
    public class Rule
    {
        public enum ContextSensitivity
        {
            AND,
            OR
        }

        public string leftContext;
        public string rightContext;
        public char predecessor;
        public string successsor;
        public float probability;
        public ContextSensitivity contextSensitivity;
        public int contextCount;

        public Rule()
        {
            leftContext = "";
            rightContext = "";
            predecessor = char.MinValue;
            successsor = "";
            probability = 1f;
        }

        public override string ToString()
        {
            return leftContext + " < " + predecessor + " > " + rightContext + " -> " + successsor + " : " + probability;
        }
    }
}