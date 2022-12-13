using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Testing.Utilities
{
    public static class LoggingEventExtensions
    {
        public static void AssertNoErrorsInLogs(this MemoryAppender memoryAppender)
        {
            LoggingEvent[] logEvents = memoryAppender.GetEvents();
            foreach (LoggingEvent logEvent in logEvents)
            {
                Assert.That(logEvent.Level == Level.Info || logEvent.Level == Level.Debug, $"There was an error in the logs. \"{logEvent.RenderedMessage}\"");
            }
        }

        public static bool LogExists(this MemoryAppender memoryAppender, Level logLevel, string logMessage)
        {
            LoggingEvent logEvent = memoryAppender.GetEvents().First(logEvent => logEvent.Level == logLevel && logEvent.RenderedMessage.Equals(logMessage));
            if (logEvent != null)
            {
                return true;
            }

            return false;
        }
    }
}
