namespace CyberAwarenessBot
{
    /// <summary>
    /// Core engine that orchestrates MemoryStore, ResponseManager,
    /// SentimentAnalyzer, and the delegate pipeline to generate responses.
    /// Part 3 adds NLP intent detection for tasks, reminders, quiz, and log.
    /// </summary>
    public class ChatbotEngine
    {
        // ── Dependencies ───────────────────────────────────────────────────
        private readonly MemoryStore       _memory;
        private readonly ResponseManager   _responses;
        private readonly SentimentAnalyzer _sentiment;

        // ── Part 3 additions ──────────────────────────────────────────────
        private TaskManager?    _taskManager;
        private ActivityLogger? _activityLogger;

        // ── Delegates configured at construction ───────────────────────────
        private readonly FormatBotMessage     _formatter;
        private readonly ValidateUserInput    _validator;
        private readonly LogConversationEntry _logger;

        // ── Sentiment exposed for UI display ──────────────────────────────
        public Sentiment LastSentiment { get; private set; } = Sentiment.Neutral;

        // ── Reminder state (tracks if we're awaiting reminder for a task) ─
        private string? _pendingTaskTitle = null;

        // ── Special response signals ──────────────────────────────────────
        public bool ShouldStartQuiz  { get; private set; } = false;
        public bool ShouldShowLog    { get; private set; } = false;

        // ──────────────────────────────────────────────────────────────────
        public ChatbotEngine(MemoryStore memory)
        {
            _memory    = memory;
            _responses = new ResponseManager();
            _sentiment = new SentimentAnalyzer();

            _formatter = DelegateHandlers.PersonalisedBotFormatter;
            _validator = DelegateHandlers.EmptyInputValidator;
            _logger    = DelegateHandlers.DebugLogger;
        }

        /// <summary>Injects Part 3 services after construction.</summary>
        public void SetPart3Services(TaskManager taskManager, ActivityLogger activityLogger)
        {
            _taskManager    = taskManager;
            _activityLogger = activityLogger;
        }

        // ══════════════════════════════════════════════════════════════════
        //  MAIN ENTRY POINT
        // ══════════════════════════════════════════════════════════════════

        public string GetResponse(string rawInput)
        {
            // Reset one-shot flags
            ShouldStartQuiz = false;
            ShouldShowLog   = false;

            // 1. Validate via delegate
            string? validationError = _validator(rawInput);
            if (validationError != null)
            {
                string errMsg = _memory.HasName
                    ? $"{validationError} {_memory.UserName}."
                    : validationError;
                return Format(errMsg);
            }

            // 2. Trim and log
            string input = rawInput.Trim();
            _memory.PushUserMessage(input);
            _memory.AddToHistory($"User: {input}");
            _logger("User", input);

            // 3. Detect sentiment
            LastSentiment = _sentiment.DetectSentiment(input);

            // 4. Name collection stage
            if (!_memory.HasName)
                return HandleNameCollection(input);

            // 5. Topic collection stage
            if (!_memory.TopicAsked)
                return HandleTopicCollection(input);

            // 6. Standard response flow (NLP first in Part 3)
            return HandleNormalInput(input);
        }

        // ══════════════════════════════════════════════════════════════════
        //  GREETING  (called once on startup)
        // ══════════════════════════════════════════════════════════════════
        public string GetGreetingMessage()
        {
            _memory.GreetingDone = true;
            return Format("Hey there! Welcome to ShieldBot — your personal cybersecurity guide. I'm here to help you navigate the digital world safely. What should I call you?");
        }

        // ══════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════

        private string HandleNameCollection(string input)
        {
            string name = StripNamePrefix(input);

            if (name.Length < 1 || name.Length > 40)
            {
                _memory.NameAsked = true;
                return Format("Hmm, I didn't catch that. Could you share your name with me?");
            }

            _memory.RememberName(name);
            _memory.NameAsked = true;

            string response = $"Great to meet you, {_memory.UserName}! Which cybersecurity topic would you like to explore first? "
                            + "(Choose from: passwords, phishing, safe browsing, scams, malware, privacy, 2FA, or updates)";
            _memory.AddToHistory($"Bot: {response}");
            _logger("Bot", response);
            return Format(response);
        }

        private string HandleTopicCollection(string input)
        {
            string? topicResponse = _responses.GetKeywordResponse(input);
            string  topic         = DetectTopicLabel(input);

            _memory.TopicAsked = true;

            if (!string.IsNullOrWhiteSpace(topic))
            {
                _memory.RememberFavouriteTopic(topic);
                string response = $"Noted! I'll keep {topic} in mind throughout our chat, {_memory.UserName}. "
                               + (topicResponse ?? _responses.GetRandomTip())
                               + $"\n\nI'll circle back to {topic} as we go. What would you like to know next?";
                return Format(response);
            }

            _memory.RememberFavouriteTopic(input.Length > 30 ? input[..30] : input);
            return Format($"Thanks for sharing that, {_memory.UserName}! Feel free to ask me anything about staying safe online. "
                        + "Popular topics include passwords, phishing, scams, and safe browsing.");
        }

        private string HandleNormalInput(string input)
        {
            string lower = input.ToLower();

            // ── Step 1: Handle pending reminder for a task ─────────────────
            if (_pendingTaskTitle != null)
            {
                string reminder = input.Trim();
                string confirm  = string.Empty;

                if (_taskManager != null)
                {
                    // Add reminder to most recent task (update via re-add)
                    confirm = $"Got it! I'll remind you: {reminder} for '{_pendingTaskTitle}'.";
                    _activityLogger?.Log($"Reminder set: '{_pendingTaskTitle}' — {reminder}");
                }

                _pendingTaskTitle = null;
                return Format(confirm);
            }

            // ── Step 2: Detect task intent ─────────────────────────────────
            if (ContainsAny(lower, "add task", "add a task", "create task", "i need to", "enable two", "enable 2fa", "set up two", "set up 2fa"))
            {
                return HandleTaskIntent(input, lower);
            }

            // ── Step 3: Detect reminder intent ────────────────────────────
            if (ContainsAny(lower, "remind me", "reminder", "set a reminder", "remind me to", "don't forget"))
            {
                return HandleReminderIntent(input);
            }

            // ── Step 4: Detect quiz intent ────────────────────────────────
            if (ContainsAny(lower, "start quiz", "take quiz", "test my knowledge", "quiz me", "play the game", "quiz", "play quiz"))
            {
                _activityLogger?.Log("Quiz started");
                ShouldStartQuiz = true;
                return Format("Launching the cybersecurity quiz! Switch to the Quiz tab to play, or it will open automatically.");
            }

            // ── Step 5: Detect log intent ─────────────────────────────────
            if (ContainsAny(lower, "show activity log", "what have you done", "what did you do", "show log", "recent actions", "activity log", "show me log"))
            {
                _activityLogger?.Log("Activity log viewed by user");
                ShouldShowLog = true;
                string logContent = _activityLogger?.GetRecentLog(10) ?? "No activity recorded yet.";
                return Format($"Here's a summary of recent actions:\n\n{logContent}");
            }

            // ── Step 6: Show more log entries ─────────────────────────────
            if (ContainsAny(lower, "show more", "more log", "full log", "all log"))
            {
                string fullLog = _activityLogger?.GetFullLog() ?? "No activity recorded yet.";
                return Format($"Full activity log:\n\n{fullLog}");
            }

            // ── Step 7: Predefined conversational queries ──────────────────
            if (ContainsAny(lower, "how are you", "how r u", "how are u"))
                return Format($"All systems running and threats being monitored! 😊 How can I assist you today, {_memory.UserName}?");

            if (ContainsAny(lower, "what's your purpose", "your purpose", "what do you do", "why are you here"))
                return Format("🛡️ My job is to raise cybersecurity awareness — helping you understand threats like phishing, weak passwords, and online scams so you can stay protected!");

            if (ContainsAny(lower, "what can i ask", "what can you do", "help", "topics"))
                return Format("💬 Here's what you can ask me about:\n• Passwords & passphrases\n• Phishing & scam emails\n• Safe browsing & HTTPS\n• Online scams & fraud\n• Malware, viruses & ransomware\n• Privacy & data protection\n• Two-factor authentication (2FA)\n• Software updates & patches\n\nYou can also type 'start quiz', 'add task', 'show activity log', or use the panels on the left.");

            if (ContainsAny(lower, "tell me more", "more info", "give me another tip", "explain more", "what else"))
                return HandleFollowUp();

            // ── Step 8: Keyword + sentiment response ──────────────────────
            string? keywordResponse = _responses.GetKeywordResponse(input);

            if (keywordResponse != null)
            {
                string topic = DetectTopicLabel(input);
                if (!string.IsNullOrEmpty(topic))
                    _activityLogger?.Log($"Keyword matched: {topic} — response delivered");

                string sentimentOpener = _sentiment.GetSentimentAcknowledgement(LastSentiment, _memory.UserName);
                string favouriteSuffix = BuildFavouriteSuffix();

                bool isPhishing = ContainsAny(lower, "phish", "phishing");
                string topicContent = isPhishing ? _responses.GetRandomPhishingTip() : keywordResponse;

                string full = string.IsNullOrEmpty(sentimentOpener)
                    ? $"{topicContent}{favouriteSuffix}"
                    : $"{sentimentOpener}\n\n{topicContent}{favouriteSuffix}";

                return Format(full);
            }

            // ── Step 9: Sentiment without keyword ────────────────────────
            if (LastSentiment != Sentiment.Neutral)
            {
                string ack = _sentiment.GetSentimentAcknowledgement(LastSentiment, _memory.UserName);
                string tip = _responses.GetRandomTip();
                return Format($"{ack}\n\n{tip}");
            }

            // ── Step 10: Gibberish check ──────────────────────────────────
            if (IsGibberish(input))
                return Format("🤔 That one's got me stumped. Could you try rephrasing? Ask about passwords, phishing, or safe browsing.");

            // ── Step 11: Default fallback ─────────────────────────────────
            return Format($"I'm not quite sure what you mean, {_memory.UserName}. Try asking about passwords, phishing, scams, malware, privacy, 2FA, or type 'help' for all options!");
        }

        // ── NLP Intent Handlers ───────────────────────────────────────────

        private string HandleTaskIntent(string input, string lower)
        {
            // Extract task title from input
            string title = ExtractTaskTitle(input, lower);
            string description = $"Review and complete: {title}";

            string confirm = _taskManager != null
                ? _taskManager.AddTask(title, description, string.Empty)
                : $"Task noted: '{title}'. Would you like a reminder?";

            _activityLogger?.Log($"NLP recognised task intent from: '{input}'");
            _pendingTaskTitle = title;

            return Format(confirm);
        }

        private string HandleReminderIntent(string input)
        {
            // Extract what to be reminded about
            string lower = input.ToLower();
            string subject = input.Trim();

            // Strip common prefixes
            foreach (var prefix in new[] { "remind me to ", "remind me ", "set a reminder to ", "set a reminder for ", "don't forget to " })
            {
                if (lower.StartsWith(prefix))
                {
                    subject = input[prefix.Length..].Trim();
                    break;
                }
            }

            // Check if it contains "in X days/tomorrow"
            bool hasTime = ContainsAny(lower, "tomorrow", "in 1 day", "in 2 day", "in 3 day", "in 4 day", "in 5 day", "in 6 day", "in 7 day", "next week", "days");
            string reminder = hasTime ? ExtractTimePhrase(lower) : "as soon as possible";
            string taskTitle = subject.Length > 50 ? subject[..50] : subject;

            string confirm = _taskManager != null
                ? _taskManager.AddTask(taskTitle, $"Reminder: {taskTitle}", reminder)
                : $"Reminder set for '{taskTitle}' — {reminder}.";

            _activityLogger?.Log($"Reminder set: '{taskTitle}' — {reminder}");

            return Format($"Reminder set for '{taskTitle}' on {reminder}.");
        }

        private static string ExtractTaskTitle(string input, string lower)
        {
            string[] stripPhrases =
            {
                "add task to ", "add a task to ", "add task ", "add a task ",
                "create task to ", "create task ", "i need to ", "please add task "
            };

            foreach (var phrase in stripPhrases)
            {
                if (lower.StartsWith(phrase))
                    return input[phrase.Length..].Trim();
            }

            // Try to find "to" keyword after "add"
            int toIdx = lower.IndexOf(" to ");
            if (toIdx > 0 && toIdx < lower.Length - 4)
                return input[(toIdx + 4)..].Trim();

            return input.Trim();
        }

        private static string ExtractTimePhrase(string lower)
        {
            if (lower.Contains("tomorrow"))    return "tomorrow";
            if (lower.Contains("next week"))   return "next week";

            // Look for "in X days"
            var parts = lower.Split(' ');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "in" && int.TryParse(parts[i + 1], out _))
                {
                    string unit = i + 2 < parts.Length ? parts[i + 2] : "days";
                    return $"in {parts[i + 1]} {unit}";
                }
            }

            return "as requested";
        }

        private string HandleFollowUp()
        {
            if (_memory.HasFavouriteTopic)
                return Format($"Since you're keen on {_memory.FavouriteTopic}, {_memory.UserName}, here's an extra tip: "
                            + _responses.GetRandomTip());

            return Format($"Here's a bonus security tip for you, {_memory.UserName}:\n\n{_responses.GetRandomTip()}");
        }

        // ──────────────────────────────────────────────────────────────────
        private string Format(string message)
        {
            string formatted = _formatter(message, _memory.UserName);
            _memory.AddToHistory($"Bot: {message}");
            _logger("Bot", message);
            return formatted;
        }

        private string BuildFavouriteSuffix()
        {
            if (!_memory.HasFavouriteTopic) return string.Empty;
            return $"\n\n📌 Keeping your interest in {_memory.FavouriteTopic} in mind — I'll flag related tips!";
        }

        private static bool ContainsAny(string input, params string[] terms)
            => terms.Any(t => input.Contains(t, StringComparison.OrdinalIgnoreCase));

        private static string StripNamePrefix(string input)
        {
            string[] prefixes = { "i'm ", "i am ", "my name is ", "it's ", "its ", "call me ", "name's " };
            string lower = input.ToLower().Trim();
            foreach (var p in prefixes)
                if (lower.StartsWith(p)) return input[p.Length..].Trim();
            return input.Trim();
        }

        private static string DetectTopicLabel(string input)
        {
            string lower = input.ToLower();
            if (lower.Contains("password"))                                 return "passwords";
            if (lower.Contains("phish"))                                    return "phishing";
            if (lower.Contains("scam") || lower.Contains("fraud"))          return "scams";
            if (lower.Contains("malware") || lower.Contains("virus"))       return "malware";
            if (lower.Contains("privacy"))                                  return "privacy";
            if (lower.Contains("2fa") || lower.Contains("two factor") || lower.Contains("mfa")) return "2FA";
            if (lower.Contains("update") || lower.Contains("patch"))        return "software updates";
            if (lower.Contains("brows") || lower.Contains("https"))         return "safe browsing";
            return string.Empty;
        }

        private static bool IsGibberish(string input)
        {
            if (input.Length < 3) return true;
            int vowels  = input.Count(c => "aeiouAEIOU".Contains(c));
            int letters = input.Count(char.IsLetter);
            if (letters < 3) return true;
            double ratio = letters > 0 ? (double)vowels / letters : 0;
            return ratio < 0.10 && letters > 4;
        }
    }
}
