using System;
using System.Text;

namespace UNOversal.Services.Logging
{
    public static class SafeLogRecord
    {
        // Exception messages, Data, Source, paths and user text are never serialized.
        public static string Format(Exception error)
        {
            var record = new StringBuilder();
            for (var depth = 0; error != null && depth < 8; depth++, error = error.InnerException)
                record.Append(error.GetType().FullName).Append(" HResult=").Append(error.HResult).AppendLine();
            return record.ToString();
        }
    }
}
