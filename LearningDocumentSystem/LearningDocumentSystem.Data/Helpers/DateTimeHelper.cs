namespace LearningDocumentSystem.Common.Helpers
{
    /// <summary>
    /// Helper for datetime operations and timezone conversions.
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Vietnam timezone (UTC+7), no DST.
        /// </summary>
        private static readonly TimeZoneInfo VietnamTimeZone = 
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>
        /// Converts a UTC DateTime to Vietnam time (UTC+7).
        /// </summary>
        /// <param name="utcDateTime">The UTC datetime value. If null, returns null.</param>
        /// <returns>DateTime converted to Vietnam timezone, or null if input is null.</returns>
        public static DateTime? ConvertToVietnamTime(DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
                return null;

            return ConvertToVietnamTime(utcDateTime.Value);
        }

        /// <summary>
        /// Converts a UTC DateTime to Vietnam time (UTC+7).
        /// </summary>
        /// <param name="utcDateTime">The UTC datetime value.</param>
        /// <returns>DateTime converted to Vietnam timezone.</returns>
        public static DateTime ConvertToVietnamTime(DateTime utcDateTime)
        {
            // Ensure the input is treated as UTC
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            // Convert to Vietnam timezone
            return TimeZoneInfo.ConvertTime(utcDateTime, VietnamTimeZone);
        }
    }
}
