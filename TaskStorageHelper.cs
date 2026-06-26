using Microsoft.EntityFrameworkCore;

namespace CyberAwarenessBot
{
    /// <summary>
    /// Handles all database read and write operations for tasks.
    /// All file/DB operations for tasks live here and nowhere else.
    /// </summary>
    public class TaskStorageHelper
    {
        private readonly ApplicationDbContext _db;

        public TaskStorageHelper()
        {
            _db = new ApplicationDbContext();
            _db.Database.EnsureCreated();
        }

        /// <summary>Returns all tasks from the database.</summary>
        public List<CyberTask> LoadTasks()
        {
            return _db.Tasks.ToList();
        }

        /// <summary>Adds a new task with given details and saves to the database.</summary>
        public void AddTask(string title, string description, string reminder)
        {
            var task = new CyberTask
            {
                Title       = title,
                Description = description,
                Reminder    = reminder,
                IsComplete  = false,
                CreatedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };
            _db.Tasks.Add(task);
            _db.SaveChanges();
        }

        /// <summary>Marks the task with the given ID as complete in the database.</summary>
        public void MarkAsComplete(int id)
        {
            var task = _db.Tasks.Where(t => t.Id == id).FirstOrDefault();
            if (task == null) return;
            task.IsComplete = true;
            _db.Tasks.Update(task);
            _db.SaveChanges();
        }

        /// <summary>Deletes the task with the given ID from the database.</summary>
        public void DeleteTask(int id)
        {
            var task = _db.Tasks.Where(t => t.Id == id).FirstOrDefault();
            if (task == null) return;
            _db.Tasks.Remove(task);
            _db.SaveChanges();
        }
    }
}
