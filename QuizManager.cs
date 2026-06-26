namespace CyberAwarenessBot
{
    /// <summary>
    /// Manages all quiz logic: questions, answer checking, scoring, and feedback.
    /// Questions are stored in a List and presented one at a time.
    /// </summary>
    public class QuizManager
    {
        private readonly List<QuizQuestion> _questions;
        private int _currentIndex = 0;
        private int _score        = 0;

        public QuizManager()
        {
            _questions = new List<QuizQuestion>
            {
                // ── Phishing ───────────────────────────────────────────────
                new QuizQuestion
                {
                    Question      = "What should you do if you receive an email asking for your password?",
                    Options       = new List<string> { "A) Reply with your password", "B) Delete the email", "C) Report the email as phishing", "D) Ignore it" },
                    CorrectAnswer = "C",
                    Explanation   = "Reporting phishing emails helps prevent scams and alerts your email provider to block the sender."
                },
                new QuizQuestion
                {
                    Question      = "A phishing email always comes from an unknown sender.",
                    Options       = new List<string> { "True", "False" },
                    CorrectAnswer = "False",
                    IsTrueFalse   = true,
                    Explanation   = "Phishing emails can spoof trusted contacts or companies. Always check the actual sender email address, not just the display name."
                },

                // ── Password Safety ────────────────────────────────────────
                new QuizQuestion
                {
                    Question      = "Which of the following is the strongest password?",
                    Options       = new List<string> { "A) password123", "B) MyDog2020", "C) Tr0ub4dor&3", "D) qwerty" },
                    CorrectAnswer = "C",
                    Explanation   = "A strong password mixes uppercase, lowercase, numbers, and symbols. 'Tr0ub4dor&3' is long and complex."
                },
                new QuizQuestion
                {
                    Question      = "You should use the same password across multiple accounts to make them easier to remember.",
                    Options       = new List<string> { "True", "False" },
                    CorrectAnswer = "False",
                    IsTrueFalse   = true,
                    Explanation   = "Reusing passwords means one breach can expose all your accounts. Use a password manager to track unique passwords."
                },

                // ── Safe Browsing ──────────────────────────────────────────
                new QuizQuestion
                {
                    Question      = "What does the padlock icon in your browser's address bar indicate?",
                    Options       = new List<string> { "A) The site is government-approved", "B) Your connection to the site is encrypted (HTTPS)", "C) The site has no ads", "D) The site is free to use" },
                    CorrectAnswer = "B",
                    Explanation   = "The padlock means the connection uses HTTPS, encrypting data between your browser and the server."
                },
                new QuizQuestion
                {
                    Question      = "It is safe to enter your banking credentials on public Wi-Fi without a VPN.",
                    Options       = new List<string> { "True", "False" },
                    CorrectAnswer = "False",
                    IsTrueFalse   = true,
                    Explanation   = "Public Wi-Fi can be monitored by attackers. Always use a VPN or mobile data when accessing sensitive accounts."
                },

                // ── Social Engineering ─────────────────────────────────────
                new QuizQuestion
                {
                    Question      = "A caller claims to be IT support and urgently needs your login details to fix an issue. What do you do?",
                    Options       = new List<string> { "A) Provide them immediately to be helpful", "B) Hang up and call the official IT number to verify", "C) Give them a fake password", "D) Ask them to send an email" },
                    CorrectAnswer = "B",
                    Explanation   = "This is a social engineering attack. Legitimate IT support will never ask for your password. Verify via official channels."
                },
                new QuizQuestion
                {
                    Question      = "Social engineering attacks rely on technology vulnerabilities rather than human trust.",
                    Options       = new List<string> { "True", "False" },
                    CorrectAnswer = "False",
                    IsTrueFalse   = true,
                    Explanation   = "Social engineering exploits human psychology — trust, urgency, and authority — rather than software vulnerabilities."
                },

                // ── Two-Factor Authentication ──────────────────────────────
                new QuizQuestion
                {
                    Question      = "What does two-factor authentication (2FA) add to your login process?",
                    Options       = new List<string> { "A) A second password", "B) A verification step using a separate device or code", "C) A CAPTCHA challenge", "D) A security question" },
                    CorrectAnswer = "B",
                    Explanation   = "2FA requires a second proof of identity (e.g. an OTP on your phone), stopping attackers even if they have your password."
                },

                // ── Malware / Ransomware ───────────────────────────────────
                new QuizQuestion
                {
                    Question      = "What is ransomware?",
                    Options       = new List<string> { "A) Software that speeds up your computer", "B) A type of spam filter", "C) Malware that encrypts your files and demands payment", "D) An antivirus program" },
                    CorrectAnswer = "C",
                    Explanation   = "Ransomware locks or encrypts your files and demands a ransom for the decryption key. Regular backups are the best defence."
                },
                new QuizQuestion
                {
                    Question      = "Keeping your antivirus software up-to-date helps protect against the latest malware threats.",
                    Options       = new List<string> { "True", "False" },
                    CorrectAnswer = "True",
                    IsTrueFalse   = true,
                    Explanation   = "Antivirus updates include new threat definitions. Outdated antivirus cannot detect the newest malware strains."
                },

                // ── Privacy ────────────────────────────────────────────────
                new QuizQuestion
                {
                    Question      = "Which action best protects your online privacy?",
                    Options       = new List<string> { "A) Sharing your location publicly on social media", "B) Reviewing and restricting app permissions regularly", "C) Using the same email for all sign-ups", "D) Leaving your accounts set to public by default" },
                    CorrectAnswer = "B",
                    Explanation   = "Regularly auditing app permissions ensures apps only access what they truly need, reducing your exposure."
                },

                // ── Data Backup ────────────────────────────────────────────
                new QuizQuestion
                {
                    Question      = "How often should you back up your important data?",
                    Options       = new List<string> { "A) Only when your device is full", "B) Once a year", "C) At least weekly, or continuously with cloud backup", "D) Backups are unnecessary if you have antivirus" },
                    CorrectAnswer = "C",
                    Explanation   = "Regular backups protect you from ransomware, hardware failure, and accidental deletion. Follow the 3-2-1 backup rule."
                },
            };
        }

        // ──────────────────────────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────────────────────────

        public QuizQuestion GetCurrentQuestion()  => _questions[_currentIndex];
        public bool         IsFinished()          => _currentIndex >= _questions.Count;
        public int          TotalQuestions        => _questions.Count;
        public int          CurrentNumber         => _currentIndex + 1;

        /// <summary>
        /// Checks the submitted answer. Returns true if correct and advances the index.
        /// </summary>
        public bool SubmitAnswer(string answer)
        {
            bool correct = string.Equals(
                answer.Trim(),
                _questions[_currentIndex].CorrectAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase);

            if (correct) _score++;
            _currentIndex++;
            return correct;
        }

        public string GetFinalScore()   => $"{_score} / {_questions.Count}";
        public string GetFinalMessage() => _score >= (int)(_questions.Count * 0.7)
            ? "🎉 Great job! You have solid cybersecurity knowledge."
            : "📚 Keep learning — review the topics you missed to strengthen your defences.";

        public void ResetQuiz()
        {
            _currentIndex = 0;
            _score        = 0;
        }
    }
}
