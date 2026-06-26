using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace CyberAwarenessBot
{
    /// <summary>
    /// Code-behind for MainWindow.xaml.
    /// Part 3: adds Task Assistant, Quiz, Activity Log, and NLP interaction.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ── Part 1 / 2 ────────────────────────────────────────────────────
        private readonly MemoryStore   _memory = new();
        private readonly ChatbotEngine _engine;
        private          SoundPlayer?  _player;

        // ── Typing animation ──────────────────────────────────────────────
        private readonly DispatcherTimer _typingTimer  = new();
        private string     _pendingBotText = string.Empty;
        private int        _typingIndex    = 0;
        private TextBlock? _typingBlock    = null;

        // ── Part 3 services ───────────────────────────────────────────────
        private readonly ActivityLogger _activityLogger = new();
        private readonly TaskManager    _taskManager;
        private readonly QuizManager    _quizManager    = new();

        // ── Colour constants ──────────────────────────────────────────────
        private static readonly SolidColorBrush BrushUserBg    = new(Color.FromRgb(0xF4, 0xF4, 0xF4));
        private static readonly SolidColorBrush BrushBotBg     = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
        private static readonly SolidColorBrush BrushText      = new(Color.FromRgb(0x0D, 0x0D, 0x0D));
        private static readonly SolidColorBrush BrushMuted     = new(Color.FromRgb(0x6B, 0x6B, 0x6B));
        private static readonly SolidColorBrush BrushGreen     = new(Color.FromRgb(0x10, 0xA3, 0x7F));
        private static readonly SolidColorBrush BrushRed       = new(Color.FromRgb(0xEF, 0x44, 0x44));
        private static readonly SolidColorBrush BrushDivider   = new(Color.FromRgb(0xE5, 0xE5, 0xE5));

        // ── Quiz state ────────────────────────────────────────────────────
        private int  _quizCurrentScore  = 0;
        private bool _quizAnswerSubmitted = false;

        // ──────────────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            _taskManager = new TaskManager(_activityLogger);
            _engine = new ChatbotEngine(_memory);
            _engine.SetPart3Services(_taskManager, _activityLogger);

            SetupTypingTimer();
            Loaded += MainWindow_Loaded;
        }

        // ══════════════════════════════════════════════════════════════════
        //  STARTUP
        // ══════════════════════════════════════════════════════════════════
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PlayGreeting();
            ShowBotMessage(_engine.GetGreetingMessage());
            SetStatus("Awaiting your name…");
            LoadTaskList(); // Read all tasks from DB on startup (Part 3 - CRUD Read)
        }

        // ══════════════════════════════════════════════════════════════════
        //  NAVIGATION
        // ══════════════════════════════════════════════════════════════════
        private void ShowPanel(string panel)
        {
            ChatPanel.Visibility  = Visibility.Collapsed;
            TaskPanel.Visibility  = Visibility.Collapsed;
            QuizPanel.Visibility  = Visibility.Collapsed;
            LogPanel.Visibility   = Visibility.Collapsed;
            InputBar.Visibility   = Visibility.Collapsed;

            // Reset nav button styles
            BtnNavChat.Style  = (Style)FindResource("NavButton");
            BtnNavTasks.Style = (Style)FindResource("NavButton");
            BtnNavQuiz.Style  = (Style)FindResource("NavButton");
            BtnNavLog.Style   = (Style)FindResource("NavButton");

            switch (panel)
            {
                case "chat":
                    ChatPanel.Visibility = Visibility.Visible;
                    InputBar.Visibility  = Visibility.Visible;
                    BtnNavChat.Style     = (Style)FindResource("NavButtonActive");
                    SetStatus("Chat — type a message or pick a quick topic");
                    break;
                case "tasks":
                    TaskPanel.Visibility = Visibility.Visible;
                    BtnNavTasks.Style    = (Style)FindResource("NavButtonActive");
                    LoadTaskList();
                    SetStatus("Task Assistant — add, complete, or delete tasks");
                    break;
                case "quiz":
                    QuizPanel.Visibility = Visibility.Visible;
                    BtnNavQuiz.Style     = (Style)FindResource("NavButtonActive");
                    SetStatus("Quiz — test your cybersecurity knowledge");
                    break;
                case "log":
                    LogPanel.Visibility = Visibility.Visible;
                    BtnNavLog.Style     = (Style)FindResource("NavButtonActive");
                    RefreshLogPanel(fullLog: false);
                    SetStatus("Activity Log — last 10 actions recorded");
                    break;
            }
        }

        private void BtnNavChat_Click(object s, RoutedEventArgs e)  => ShowPanel("chat");
        private void BtnNavTasks_Click(object s, RoutedEventArgs e) => ShowPanel("tasks");
        private void BtnNavQuiz_Click(object s, RoutedEventArgs e)  => ShowPanel("quiz");
        private void BtnNavLog_Click(object s, RoutedEventArgs e)   => ShowPanel("log");

        // ══════════════════════════════════════════════════════════════════
        //  CHAT EVENT HANDLERS
        // ══════════════════════════════════════════════════════════════════
        private void BtnSend_Click(object s, RoutedEventArgs e)         => SendMessage();
        private void BtnPlayGreeting_Click(object s, RoutedEventArgs e) => PlayGreeting();
        private void BtnClearChat_Click(object s, RoutedEventArgs e)    => ClearChat();

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int len = TxtInput.Text.Length;
            SetStatus(len > 0 ? $"Typing… ({len} chars)" : "Ready");
        }

        private void QuickTopic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                ShowPanel("chat");
                TxtInput.Text = tag;
                SendMessage();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  MESSAGE DISPATCH
        // ══════════════════════════════════════════════════════════════════
        private void SendMessage()
        {
            string input = TxtInput.Text;
            TxtInput.Clear();
            if (string.IsNullOrWhiteSpace(input)) return;

            ShowUserMessage(input);
            string response = _engine.GetResponse(input);
            ShowBotMessage(response);
            UpdateSidePanel();
            SetStatus("Ready");

            // Handle engine signals
            if (_engine.ShouldStartQuiz)
            {
                ShowPanel("quiz");
                ShowQuizQuestion();
            }
            else if (_engine.ShouldShowLog)
            {
                // Log already shown in chat; also switch to log panel
                // (log content is in the chat message already)
            }

            // Refresh task list if a task was added via chat
            string lower = input.ToLower();
            if (lower.Contains("add task") || lower.Contains("add a task") ||
                lower.Contains("create task") || lower.Contains("remind me") ||
                lower.Contains("set a reminder") || lower.Contains("i need to"))
            {
                LoadTaskList();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  TASK PANEL HANDLERS
        // ══════════════════════════════════════════════════════════════════
        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string title    = TxtTaskTitle.Text.Trim();
            string desc     = TxtTaskDesc.Text.Trim();
            string reminder = TxtTaskReminder.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                SetStatus("⚠ Please enter a task title.");
                return;
            }

            if (string.IsNullOrWhiteSpace(desc))
                desc = $"Complete: {title}";

            _taskManager.AddTask(title, desc, reminder);
            TxtTaskTitle.Clear();
            TxtTaskDesc.Clear();
            TxtTaskReminder.Clear();
            LoadTaskList();
            SetStatus($"Task '{title}' added successfully.");
        }

        private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection tracked; action buttons are in each item template
        }

        private void LoadTaskList()
        {
            TaskListBox.Items.Clear();
            var tasks = _taskManager.GetAllTasks();

            foreach (var task in tasks)
            {
                var item = BuildTaskItem(task);
                TaskListBox.Items.Add(item);
            }

            if (tasks.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text       = "No tasks yet. Add one above or type in chat: 'Add task to enable 2FA'",
                    Foreground = BrushMuted,
                    FontSize   = 13,
                    Margin     = new Thickness(0, 12, 0, 0)
                };
                TaskListBox.Items.Add(empty);
            }
        }

        private FrameworkElement BuildTaskItem(CyberTask task)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: task details
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            if (task.IsComplete)
            {
                var checkMark = new TextBlock { Text = "✓ ", Foreground = BrushGreen, FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 13 };
                titleRow.Children.Add(checkMark);
            }
            var titleBlock = new TextBlock
            {
                Text            = task.Title,
                FontFamily      = new FontFamily("Segoe UI Semibold"),
                FontSize        = 13.5,
                Foreground      = task.IsComplete ? BrushMuted : BrushText,
                TextDecorations = task.IsComplete ? TextDecorations.Strikethrough : null
            };
            titleRow.Children.Add(titleBlock);
            info.Children.Add(titleRow);

            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                info.Children.Add(new TextBlock
                {
                    Text       = task.Description,
                    FontSize   = 12,
                    Foreground = BrushMuted,
                    TextWrapping = TextWrapping.Wrap,
                    Margin     = new Thickness(0, 3, 0, 0)
                });
            }

            var meta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            if (!string.IsNullOrWhiteSpace(task.Reminder))
            {
                meta.Children.Add(new TextBlock
                {
                    Text       = $"⏰ {task.Reminder}",
                    FontSize   = 11.5,
                    Foreground = BrushGreen,
                    Margin     = new Thickness(0, 0, 12, 0)
                });
            }
            meta.Children.Add(new TextBlock
            {
                Text       = task.CreatedAt,
                FontSize   = 11.5,
                Foreground = BrushMuted
            });
            info.Children.Add(meta);

            Grid.SetColumn(info, 0);
            grid.Children.Add(info);

            // Right: action buttons
            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            if (!task.IsComplete)
            {
                var completeBtn = new Button
                {
                    Content = "Complete",
                    Style   = (Style)FindResource("SecondaryButton"),
                    Tag     = task,
                    Margin  = new Thickness(0, 0, 8, 0),
                    Height  = 34
                };
                completeBtn.Click += (s, e) =>
                {
                    var t = (CyberTask)((Button)s).Tag;
                    _taskManager.MarkAsComplete(t.Id, t.Title);
                    LoadTaskList();
                    SetStatus($"Task '{t.Title}' marked complete.");
                };
                actions.Children.Add(completeBtn);
            }

            var deleteBtn = new Button
            {
                Content = "Delete",
                Style   = (Style)FindResource("DangerButton"),
                Tag     = task,
                Height  = 34
            };
            deleteBtn.Click += (s, e) =>
            {
                var t = (CyberTask)((Button)s).Tag;
                _taskManager.DeleteTask(t.Id, t.Title);
                LoadTaskList();
                SetStatus($"Task '{t.Title}' deleted.");
            };
            actions.Children.Add(deleteBtn);

            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);

            return grid;
        }

        // ══════════════════════════════════════════════════════════════════
        //  QUIZ PANEL HANDLERS
        // ══════════════════════════════════════════════════════════════════
        private void BtnStartQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quizManager.ResetQuiz();
            _quizCurrentScore = 0;
            _activityLogger.Log("Quiz started");
            ShowQuizQuestion();
        }

        private void ShowQuizQuestion()
        {
            if (_quizManager.IsFinished())
            {
                ShowQuizResults();
                return;
            }

            QuizStartScreen.Visibility   = Visibility.Collapsed;
            QuizQuestionScreen.Visibility = Visibility.Visible;
            QuizResultsScreen.Visibility  = Visibility.Collapsed;

            var q = _quizManager.GetCurrentQuestion();
            _quizAnswerSubmitted = false;

            TxtQuestion.Text        = q.Question;
            TxtQuizQuestionNum.Text = $"Question {_quizManager.CurrentNumber} of {_quizManager.TotalQuestions}";
            TxtQuizScore.Text       = $"Score: {_quizCurrentScore}";
            TxtQuizProgress.Text    = $"Question {_quizManager.CurrentNumber} of {_quizManager.TotalQuestions}";

            // Hide feedback and next button
            FeedbackArea.Visibility    = Visibility.Collapsed;
            BtnNextQuestion.Visibility = Visibility.Collapsed;
            BtnSubmitAnswer.Visibility = Visibility.Visible;

            if (q.IsTrueFalse)
            {
                McOptions.Visibility = Visibility.Collapsed;
                TfOptions.Visibility = Visibility.Visible;
                RbTrue.IsChecked  = false;
                RbFalse.IsChecked = false;
            }
            else
            {
                McOptions.Visibility = Visibility.Visible;
                TfOptions.Visibility = Visibility.Collapsed;
                RbA.IsChecked = RbB.IsChecked = RbC.IsChecked = RbD.IsChecked = false;

                // Set option text
                var opts = q.Options;
                RbA.Content = opts.Count > 0 ? opts[0] : "";
                RbB.Content = opts.Count > 1 ? opts[1] : "";
                RbC.Content = opts.Count > 2 ? opts[2] : "";
                RbD.Content = opts.Count > 3 ? opts[3] : "";

                RbC.Visibility = opts.Count > 2 ? Visibility.Visible : Visibility.Collapsed;
                RbD.Visibility = opts.Count > 3 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BtnSubmitAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_quizAnswerSubmitted) return;

            var q = _quizManager.GetCurrentQuestion();
            string? selected = GetSelectedAnswer(q.IsTrueFalse);

            if (selected == null)
            {
                SetStatus("⚠ Please select an answer before submitting.");
                return;
            }

            bool correct = _quizManager.SubmitAnswer(selected);
            if (correct) _quizCurrentScore++;

            // Show feedback
            FeedbackArea.Visibility = Visibility.Visible;
            if (correct)
            {
                FeedbackArea.Background       = new SolidColorBrush(Color.FromRgb(0xF0, 0xFB, 0xF8));
                TxtFeedbackResult.Text        = "✓ Correct!";
                TxtFeedbackResult.Foreground  = BrushGreen;
            }
            else
            {
                FeedbackArea.Background       = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
                TxtFeedbackResult.Text        = $"✗ Incorrect. The answer is: {q.CorrectAnswer}";
                TxtFeedbackResult.Foreground  = BrushRed;
            }
            TxtFeedbackExplanation.Text  = q.Explanation;
            TxtQuizScore.Text            = $"Score: {_quizCurrentScore}";

            BtnSubmitAnswer.Visibility   = Visibility.Collapsed;
            BtnNextQuestion.Visibility   = Visibility.Visible;
            _quizAnswerSubmitted         = true;

            SetStatus(correct ? "Correct! Click Next to continue." : "Incorrect. Click Next to continue.");
        }

        private void BtnNextQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (_quizManager.IsFinished())
                ShowQuizResults();
            else
                ShowQuizQuestion();
        }

        private void ShowQuizResults()
        {
            string scoreStr = _quizManager.GetFinalScore();
            string message  = _quizManager.GetFinalMessage();
            _activityLogger.Log($"Quiz completed — score: {scoreStr}");

            QuizQuestionScreen.Visibility = Visibility.Collapsed;
            QuizResultsScreen.Visibility  = Visibility.Visible;
            TxtFinalScore.Text            = scoreStr;
            TxtFinalMessage.Text          = message;
            TxtQuizProgress.Text          = "Quiz complete";
            SetStatus($"Quiz finished — you scored {scoreStr}");
        }

        private void BtnRetakeQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quizManager.ResetQuiz();
            _quizCurrentScore = 0;
            QuizResultsScreen.Visibility  = Visibility.Collapsed;
            QuizStartScreen.Visibility    = Visibility.Visible;
            TxtQuizProgress.Text          = "Test your knowledge";
            SetStatus("Quiz reset — ready to start again.");
        }

        private string? GetSelectedAnswer(bool isTrueFalse)
        {
            if (isTrueFalse)
            {
                if (RbTrue.IsChecked  == true) return "True";
                if (RbFalse.IsChecked == true) return "False";
                return null;
            }

            if (RbA.IsChecked == true) return "A";
            if (RbB.IsChecked == true) return "B";
            if (RbC.IsChecked == true) return "C";
            if (RbD.IsChecked == true) return "D";
            return null;
        }

        // ══════════════════════════════════════════════════════════════════
        //  LOG PANEL HANDLERS
        // ══════════════════════════════════════════════════════════════════
        private void RefreshLogPanel(bool fullLog)
        {
            int count = _activityLogger.GetCount();
            TxtLogCount.Text = $"({count} total entries)";

            string content = fullLog
                ? _activityLogger.GetFullLog()
                : _activityLogger.GetRecentLog(10);

            TxtLogEntries.Text   = content;
            BtnShowMore.Visibility = (count > 10 && !fullLog)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnShowMoreLog_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogPanel(fullLog: true);
            BtnShowMore.Visibility = Visibility.Collapsed;
        }

        private void BtnRefreshLog_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogPanel(fullLog: false);
            SetStatus("Activity log refreshed.");
        }

        // ══════════════════════════════════════════════════════════════════
        //  CHAT CARD RENDERING
        // ══════════════════════════════════════════════════════════════════
        private void ShowUserMessage(string text)
        {
            var container = new Border
            {
                Background          = BrushUserBg,
                CornerRadius        = new CornerRadius(12),
                Padding             = new Thickness(18, 12, 18, 12),
                Margin              = new Thickness(60, 6, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth            = 560
            };

            var stack = new StackPanel();

            var label = new TextBlock
            {
                Text       = $"You  ·  {DateTime.Now:HH:mm}",
                Foreground = BrushMuted,
                FontSize   = 10.5,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                Margin     = new Thickness(0, 0, 0, 5)
            };
            var msg = new TextBlock
            {
                Text         = text,
                Foreground   = BrushText,
                FontSize     = 13.5,
                TextWrapping = TextWrapping.Wrap
            };

            stack.Children.Add(label);
            stack.Children.Add(msg);
            container.Child = stack;
            ChatMessages.Children.Add(container);
            ScrollToBottom();
        }

        private void ShowBotMessage(string text, bool isError = false)
        {
            // Strip delegate timestamp prefix added by formatter
            string displayText = text;
            if (text.Length > 10 && text[0] == '[')
            {
                int closeIdx = text.IndexOf("] 🤖");
                if (closeIdx > 0)
                {
                    int colonIdx = text.IndexOf(": ", closeIdx);
                    if (colonIdx > 0) displayText = text[(colonIdx + 2)..];
                }
            }

            var container = new Border
            {
                Background          = BrushBotBg,
                CornerRadius        = new CornerRadius(12),
                Padding             = new Thickness(18, 12, 18, 12),
                Margin              = new Thickness(0, 6, 60, 6),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth            = 640
            };

            var stack = new StackPanel();

            // Bot avatar row
            var avatarRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            var dot = new System.Windows.Shapes.Ellipse
            {
                Width = 8, Height = 8,
                Fill  = isError ? BrushRed : BrushGreen,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            avatarRow.Children.Add(dot);
            avatarRow.Children.Add(new TextBlock
            {
                Text       = $"ShieldBot  ·  {DateTime.Now:HH:mm}",
                Foreground = BrushMuted,
                FontSize   = 10.5,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(avatarRow);

            var msgBlock = new TextBlock
            {
                Foreground   = BrushText,
                FontSize     = 13.5,
                TextWrapping = TextWrapping.Wrap
            };

            stack.Children.Add(msgBlock);
            container.Child = stack;
            ChatMessages.Children.Add(container);

            // Add a light separator line
            ChatMessages.Children.Add(new Border
            {
                Height     = 1,
                Background = BrushDivider,
                Margin     = new Thickness(0, 4, 0, 4),
                Opacity    = 0.5
            });

            StartTypingEffect(displayText, msgBlock);
            ScrollToBottom();
        }

        // ══════════════════════════════════════════════════════════════════
        //  TYPING ANIMATION
        // ══════════════════════════════════════════════════════════════════
        private void SetupTypingTimer()
        {
            _typingTimer.Interval = TimeSpan.FromMilliseconds(12);
            _typingTimer.Tick    += TypingTimer_Tick;
        }

        private void StartTypingEffect(string text, TextBlock block)
        {
            _typingTimer.Stop();
            if (_typingBlock != null)
                _typingBlock.Text = _pendingBotText;

            _pendingBotText = text;
            _typingIndex    = 0;
            _typingBlock    = block;
            _typingTimer.Start();
        }

        private void TypingTimer_Tick(object? sender, EventArgs e)
        {
            if (_typingBlock == null || _typingIndex >= _pendingBotText.Length)
            {
                _typingTimer.Stop();
                return;
            }
            int charsToAdd = Math.Min(3, _pendingBotText.Length - _typingIndex);
            _typingBlock.Text += _pendingBotText.Substring(_typingIndex, charsToAdd);
            _typingIndex      += charsToAdd;
            ScrollToBottom();
        }

        // ══════════════════════════════════════════════════════════════════
        //  SIDE PANEL UPDATE
        // ══════════════════════════════════════════════════════════════════
        private void UpdateSidePanel()
        {
            TxtUserName.Text  = _memory.HasName           ? $"User: {_memory.UserName}"         : "User: —";
            TxtUserTopic.Text = _memory.HasFavouriteTopic ? $"Topic: {_memory.FavouriteTopic}"  : "Topic: —";

            var sa = new SentimentAnalyzer();
            TxtSentiment.Text = $"Mood: {sa.GetSentimentLabel(_engine.LastSentiment)}";
        }

        // ══════════════════════════════════════════════════════════════════
        //  VOICE GREETING
        // ══════════════════════════════════════════════════════════════════
        private void PlayGreeting()
        {
            try
            {
                string[] candidates =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "greeting.wav"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Audio", "greeting.wav"),
                };

                string? found = candidates.FirstOrDefault(File.Exists);
                if (found != null)
                {
                    _player?.Dispose();
                    _player = new SoundPlayer(found);
                    _player.Play();
                    SetStatus("🔊 Playing voice greeting…");
                }
                else
                {
                    SetStatus("⚠ Audio file not found — place greeting.wav in the Audio folder.");
                    AppendSystemNote("⚠ No audio file detected. Place greeting.wav inside the Audio/ folder.");
                }
            }
            catch (Exception ex)
            {
                SetStatus("⚠ Audio playback failed — continuing normally.");
                AppendSystemNote($"⚠ Audio error: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  UTILITIES
        // ══════════════════════════════════════════════════════════════════
        private void ClearChat()
        {
            ChatMessages.Children.Clear();
            _memory.ClearAll();
            UpdateSidePanel();
            ShowBotMessage(_engine.GetGreetingMessage());
            SetStatus("Chat cleared — ready to start again.");
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.UpdateLayout();
            ChatScrollViewer.ScrollToEnd();
        }

        private void SetStatus(string msg) => TxtStatus.Text = msg;

        private void AppendSystemNote(string note)
        {
            ChatMessages.Children.Add(new TextBlock
            {
                Text         = note,
                Foreground   = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),
                FontSize     = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(8, 4, 8, 4),
                FontStyle    = FontStyles.Italic
            });
            ScrollToBottom();
        }
    }
}
