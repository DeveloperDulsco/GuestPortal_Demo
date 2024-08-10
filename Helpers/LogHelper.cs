using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Helpers
{
    public class LogHelper
    {
       private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<LogHelper>
          lazy = new Lazy<LogHelper>(() => new LogHelper()
          {
              
          });

        public static LogHelper Instance
        {
            get
            {
                return lazy.Value;
            }
        }



        public void Log(string message,string reservationNameID,string actionName, string actionGroup)
        {
            _logger.Factory.ThrowExceptions = true;

            LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, "WebCheckin", message);
            logEvent.Properties["actionName"] = actionName;
            logEvent.Properties["reservationNameID"] = reservationNameID;
            logEvent.Properties["applicationName"] = "WebCheckin";
            logEvent.Properties["actionGroup"] = actionGroup;
            _logger.Log(logEvent);
        }

        public void Warn(string message, string reservationNameID, string actionName, string actionGroup)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Warn, "WebCheckin", message);
            logEvent.Properties["actionName"] = actionName;
            logEvent.Properties["reservationNameID"] = reservationNameID;
            logEvent.Properties["applicationName"] = "WebCheckin";
            logEvent.Properties["actionGroup"] = actionGroup;
            _logger.Log(logEvent);
        }

        public void Debug(string message, string reservationNameID, string actionName, string actionGroup)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Debug, "WebCheckin", message);
            logEvent.Properties["actionName"] = actionName;
            logEvent.Properties["reservationNameID"] = reservationNameID;
            logEvent.Properties["applicationName"] = "WebCheckin";
            logEvent.Properties["actionGroup"] = actionGroup;
            _logger.Log(logEvent);
        }

        public void Error(Exception  message, string reservationNameID, string actionName, string actionGroup)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, "WebCheckin", message.ToString());
            logEvent.Properties["actionName"] = actionName;
            logEvent.Properties["reservationNameID"] = reservationNameID;
            logEvent.Properties["applicationName"] = "WebCheckin";
            logEvent.Properties["actionGroup"] = actionGroup;
            _logger.Error(logEvent);
        }

    }
}