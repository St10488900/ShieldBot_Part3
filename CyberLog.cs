namespace CyberAwarenessBot
{
    /// <summary>
    /// Represents a single activity log entry stored in the database.
    /// </summary>
    public class CyberLog
    {
        public int    Id          { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CreatedAt   { get; set; } = DateTime.Now.ToString("HH:mm dd MMM");
    }
}
