namespace AncientMountain.Managed.Services
{
    public static class NotificationService
    {
        public static event Action<Notification> NewNotification = delegate { };
        public static List<Notification> Notifications { get; set; } = [];

        public static void PushNotification(Notification notification)
        {
            NewNotification(notification);
            Notifications.Add(notification);
        }

        public static void RemoveNotification(Notification n)
        {
            Notifications.Remove(n);
        }
    }

    public struct Notification
    {
        public Guid Id { get; set; }
        public DateTime CreateTime { get; set; }
        public NotificationLevel Level { get; set; }
        public string Message { get; set; }
    }

    public enum NotificationLevel
    {
        Info = 0,
        Alert = 1,
        Error = 2
    }
}