// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace MapGen.Helpers
{
    public enum LogLevel
    {
        Trace,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILogger
    {
        LogLevel Verbosity { get; set; }
        void Log(LogLevel logLevel, string message);
        void LogCritical(string message);
        void LogError(string message);
        void LogInformation(string message);
        void LogTrace(string message);
        void LogWarning(string message);
    }

    public class Logger : ILogger {

        public static readonly ILogger GlobalLog = new Logger();
        public LogLevel Verbosity { get; set; }

        private string origin;
        private string diagnosticDataPath;
        private static readonly string[] verbosityFormatString;

        static Logger() {
            verbosityFormatString = new string[Enum.GetValues(typeof(LogLevel)).Length];

            // Available colours: black, red, green, lime, yellow, blue, magenta, pink, purple, cyan, white, orange, gray
            // https://github.com/godotengine/godot/pull/60675/files#diff-dac8c9fa12d043216f1be80cbcc56d82ea88845b5eebb7214bbb1935eb4ddef9L82
            verbosityFormatString[(int)LogLevel.Trace]    = "[color=white]{0}[/color]"; // using white rather than grey as grey is the default that other output uses
            verbosityFormatString[(int)LogLevel.Info]     = "{0}";
            verbosityFormatString[(int)LogLevel.Warning]  = "[color=yellow]{0}[/color]";
            verbosityFormatString[(int)LogLevel.Error]    = "[color=red]{0}[/color]";
            verbosityFormatString[(int)LogLevel.Critical] = "[color=red][b]{0}[/b][/color]";
        }

        public Logger(LogLevel verbosity = LogLevel.Trace) {
            origin = "";
            Verbosity = verbosity;
        }

        public Logger(object godotNode_or_ClrObj, LogLevel verbosity = LogLevel.Trace) {
            if (godotNode_or_ClrObj is Node godotNode) {
                origin = $"{godotNode.Name}@{godotNode.GetType().Name}";
            } else if (godotNode_or_ClrObj is string stringValue) {
                origin = stringValue;
            } else {
                origin = godotNode_or_ClrObj.GetType().Name;
            }
            origin = $" [{origin}]";
            Verbosity = verbosity;
        }

        public void Log(LogLevel logLevel, string message) {
            if (logLevel >= Verbosity) {
                string formattedMessage = $"{System.Threading.Thread.CurrentThread.ManagedThreadId:X3} {DateTime.Now:HH:mm:ss.ff} {logLevel}{origin}: {message}";

                try {
                    if (!UnitTestDetector.IsRunningAsUnitTest) { // Godot functions crash when running under the test-runner instead of under Godot.
                        GD.PrintRich(string.Format(verbosityFormatString[(int)logLevel], formattedMessage));
                    }
                } catch { /* Is there a way to know when this is being run in a unit test and not in Godot? */ }
                Console.WriteLine(formattedMessage);

                if (logLevel >= LogLevel.Warning) {
                    string formattedMessageSansLoglevel = $"[My C#]{origin}: {message}";
                    if (logLevel >= LogLevel.Error) {
                        GD.PushError(formattedMessageSansLoglevel);
                    } else {
                        GD.PushWarning(formattedMessageSansLoglevel);
                    }
                }
            }
        }

        void ILogger.LogCritical(string message) {
            Log(LogLevel.Critical, message);
        }

        void ILogger.LogError(string message) {
            Log(LogLevel.Error, message);
        }

        void ILogger.LogInformation(string message) {
            Log(LogLevel.Info, message);
        }

        void ILogger.LogTrace(string message) {
            Log(LogLevel.Trace, message);
        }

        void ILogger.LogWarning(string message) {
            Log(LogLevel.Warning, message);
        }
    }
}
