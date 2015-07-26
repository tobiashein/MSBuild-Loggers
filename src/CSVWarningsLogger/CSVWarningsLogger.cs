using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodehulkNET.MSBuild.Loggers.CSVWarningsLogger
{
    /// <summary>
    /// Logs warnings to a CSV file.
    /// </summary>
    public class CSVWarningsLogger : Logger, IDisposable
    {
        #region Constants

        private const string LOG_FORMAT_STRING = "{0};{1};{2};{3};{4};{5}";

        #endregion Constants

        #region Fields

        private readonly List<BuildWarningEventArgs> _warnings = new List<BuildWarningEventArgs>();

        private StreamWriter _streamWriter;

        #endregion Fields

        #region Methods

        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// When overridden in a derived class, subscribes the logger to specific events.
        /// </summary>
        /// <param name="eventSource">The available events that a logger can subscribe to.</param>
        /// <exception cref="Microsoft.Build.Framework.LoggerException">
        /// Log file was not set.
        /// or
        /// Log file was not set.
        /// or
        /// Failed to create log file:  + ex.Message
        /// </exception>
        public override void Initialize(IEventSource eventSource)
        {
            // The name of the log file should be passed as the first item in the
            // "parameters" specification in the /logger switch.  It is required
            // to pass a log file to this logger. Other loggers may have zero or more than
            // one parameters.
            if (Parameters == null)
                throw new LoggerException("Log file was not set.");

            var logFilename = Parameters.Split(';').FirstOrDefault();
            if (string.IsNullOrEmpty(logFilename))
                throw new LoggerException("Log file was not set.");

            try
            {
                _streamWriter = new StreamWriter(logFilename, false, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new LoggerException("Failed to create log file: " + ex.Message);
            }

            eventSource.WarningRaised += WarningRaisedEventHandler;
            eventSource.BuildFinished += BuildFinishedEventHandler;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _streamWriter.Close();
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Handles the BuildFinished event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="BuildFinishedEventArgs" /> instance containing the event data.</param>
        private void BuildFinishedEventHandler(object sender, BuildFinishedEventArgs e)
        {
            Log("Warning", "Project", "File", "Line", "Column", "Message");

            foreach (var warning in _warnings.OrderBy(w => w.Code))
            {
                Log(warning.Code, warning.ProjectFile, warning.File, warning.LineNumber.ToString(), warning.ColumnNumber.ToString(), warning.Message);
            }
        }

        /// <summary>
        /// Writes a new message to the log file.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <param name="project">The project.</param>
        /// <param name="file">The file.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="columnNumber">The column number.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="Microsoft.Build.Framework.LoggerException">Failed to write to the log file.</exception>
        private void Log(string warning, string project, string file, string lineNumber, string columnNumber, string message)
        {
            try
            {
                _streamWriter.WriteLine(string.Format(LOG_FORMAT_STRING, warning, project, file, lineNumber, columnNumber, message));
            }
            catch (Exception)
            {
                throw new LoggerException("Failed to write to the log file.");
            }
        }

        /// <summary>
        /// Handles the WarningRaised event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="BuildWarningEventArgs" /> instance containing the event data.</param>
        private void WarningRaisedEventHandler(object sender, BuildWarningEventArgs e)
        {
            _warnings.Add(e);
        }

        #endregion Private Methods

        #endregion Methods
    }
}