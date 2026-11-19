namespace YMH219_KeyEventLogger.Models
{
    /// <summary>
    /// En sık kullanılan tuşların istatistiksel özetini temsil eden sınıf.
    /// </summary>
    public class KeyUsageStatistic
    {
        public string KeyName { get; }
        public int PressCount { get; }
        public double Percentage { get; }

        public KeyUsageStatistic(string keyName, int pressCount, double percentage)
        {
            KeyName = keyName;
            PressCount = pressCount;
            Percentage = percentage;
        }

        public override string ToString()
        {
            return $"{KeyName}: {PressCount} kez (%{Percentage:F1})";
        }
    }
}
