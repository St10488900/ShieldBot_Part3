namespace CyberAwarenessBot
{
    /// <summary>
    /// Represents a cybersecurity task created by the user.
    /// Named CyberTask to avoid conflict with System.Threading.Tasks.Task.
    /// </summary>
    public class CyberTask
    {
        public int    Id          { get; set; }
        public string Title       { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reminder    { get; set; } = string.Empty;
        public bool   IsComplete  { get; set; } = false;
        public string CreatedAt   { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }
}
