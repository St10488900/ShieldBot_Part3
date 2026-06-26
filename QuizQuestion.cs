namespace CyberAwarenessBot
{
    /// <summary>
    /// Represents a single quiz question with options, correct answer, and explanation.
    /// </summary>
    public class QuizQuestion
    {
        public string       Question      { get; set; } = string.Empty;
        public List<string> Options       { get; set; } = new();   // e.g. "A) ...", "B) ..."
        public string       CorrectAnswer { get; set; } = string.Empty; // "A", "B", "True", "False"
        public string       Explanation   { get; set; } = string.Empty;
        public bool         IsTrueFalse   { get; set; } = false;
    }
}
