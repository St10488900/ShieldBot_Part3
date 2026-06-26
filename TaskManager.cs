namespace CyberAwarenessBot
{
    /// <summary>
    /// Sits between the GUI and TaskStorageHelper.
    /// Contains business logic and delegates DB operations to storage.
    /// Also logs every action via ActivityLogger.
    /// </summary>
    public class TaskManager
    {
        private readonly TaskStorageHelper _storage;
        private readonly ActivityLogger    _logger;

        public TaskManager(ActivityLogger logger)
        {
            _storage = new TaskStorageHelper();
            _logger  = logger;
        }

        /// <summary>
        /// Adds a new task, logs the action, and returns a confirmation message.
        /// </summary>
        public string AddTask(string title, string description, string reminder)
        {
            _storage.AddTask(title, description, reminder);

            string reminderNote = string.IsNullOrWhiteSpace(reminder)
                ? "no reminder set"
                : $"Reminder: {reminder}";

            _logger.Log($"Task added: '{title}' ({reminderNote})");

            return string.IsNullOrWhiteSpace(reminder)
                ? $"Task added: '{title}'. Would you like to set a reminder for this task?"
                : $"Task added: '{title}' with reminder — {reminder}.";
        }

        /// <summary>Returns all tasks from the database.</summary>
        public List<CyberTask> GetAllTasks()
        {
            return _storage.LoadTasks();
        }

        /// <summary>Marks the specified task as complete and logs the action.</summary>
        public void MarkAsComplete(int id, string title)
        {
            _storage.MarkAsComplete(id);
            _logger.Log($"Task marked complete: '{title}'");
        }

        /// <summary>Deletes the specified task and logs the action.</summary>
        public void DeleteTask(int id, string title)
        {
            _storage.DeleteTask(id);
            _logger.Log($"Task deleted: '{title}'");
        }
    }
}
