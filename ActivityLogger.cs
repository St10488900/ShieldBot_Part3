using Microsoft.EntityFrameworkCore;

namespace CyberAwarenessBot
{
    /// <summary>
    /// Records every significant chatbot action with a description and timestamp.
    /// Stores entries in the database via ApplicationDbContext.
    /// </summary>
    public class ActivityLogger
    {
        private readonly ApplicationDbContext _db;

        public ActivityLogger()
        {
            _db = new ApplicationDbContext();
            _db.Database.EnsureCreated();
        }

        /// <summary>Writes a new log entry to the database.</summary>
        public void Log(string action)
        {
            var entry = new CyberLog
            {
                Description = action,
                CreatedAt   = DateTime.Now.ToString("[HH:mm] ")
            };
            _db.Logs.Add(entry);
            _db.SaveChanges();
        }

        /// <summary>
        /// Returns the last <paramref name="count"/> log entries as a numbered list string.
        /// </summary>
        public string GetRecentLog(int count = 10)
        {
            var entries = _db.Logs.ToList().TakeLast(count).ToList();
            if (entries.Count == 0)
                return "No activity recorded yet.";

            var lines = entries
                .Select((e, i) => $"{i + 1}. {e.CreatedAt}{e.Description}")
                .ToList();
            return string.Join("\n", lines);
        }

        /// <summary>Returns ALL log entries as a numbered list string.</summary>
        public string GetFullLog()
        {
            var entries = _db.Logs.ToList();
            if (entries.Count == 0)
                return "No activity recorded yet.";

            var lines = entries
                .Select((e, i) => $"{i + 1}. {e.CreatedAt}{e.Description}")
                .ToList();
            return string.Join("\n", lines);
        }

        /// <summary>Returns total number of log entries.</summary>
        public int GetCount()
        {
            return _db.Logs.Count();
        }
    }
}
