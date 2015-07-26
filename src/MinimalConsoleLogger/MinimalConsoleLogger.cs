using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CodehulkNET.MSBuild.Loggers.MinimalConsoleLogger
{
    /// <summary>
    /// A minimal configurable console logger.
    /// </summary>
    public class MinimalConsoleLogger : Logger
    {
        #region Fields

        private readonly IDictionary<Category, List<BuildWarningEventArgs>> _categories = new Dictionary<Category, List<BuildWarningEventArgs>>();
        private readonly List<BuildErrorEventArgs> _errors = new List<BuildErrorEventArgs>();
        private readonly List<string> _projects = new List<string>();
        private readonly List<BuildWarningEventArgs> _warnings = new List<BuildWarningEventArgs>();

        private DateTime _buildStartedTimeStamp;
        private Configuration _configuration = new Configuration();

        #endregion Fields

        #region Methods

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, subscribes the logger to specific events.
        /// </summary>
        /// <param name="eventSource">The available events that a logger can subscribe to.</param>
        public override void Initialize(IEventSource eventSource)
        {
            if (Parameters != null)
            {
                var configFilename = Parameters.Split(';').FirstOrDefault();
                if (string.IsNullOrWhiteSpace(configFilename))
                    throw new LoggerException("Configuration file was not set.");

                LoadConfiguration(configFilename);
            }

            eventSource.ProjectStarted += ProjectStartedEventHandler;
            eventSource.WarningRaised += WarningRaisedEventHandler;
            eventSource.ErrorRaised += ErrorRaisedEventHandler;
            eventSource.BuildStarted += BuildStartedEventHandler;
            eventSource.BuildFinished += BuildFinishedEventHandler;
            eventSource.TargetStarted += TargetStartedEventHandler;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Handles the BuildFinished event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="BuildFinishedEventArgs"/> instance containing the event data.</param>
        private void BuildFinishedEventHandler(object sender, BuildFinishedEventArgs e)
        {
            foreach (var category in _categories)
            {
                foreach (var warning in category.Value.OrderBy(w => w.Code))
                {
                    Console.ForegroundColor = category.Key.Color;
                    Console.WriteLine("\n[{4}] {0} ({1},{2}): {3}", Path.Combine(Path.GetDirectoryName(warning.ProjectFile), warning.File ?? string.Empty), warning.LineNumber, warning.ColumnNumber, warning.Message, warning.Code);
                    Console.ResetColor();
                }
            }

            foreach (var warning in _warnings.OrderBy(w => w.Code))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[{4}] {0} ({1},{2}): {3}", Path.Combine(Path.GetDirectoryName(warning.ProjectFile), warning.File ?? string.Empty), warning.LineNumber, warning.ColumnNumber, warning.Message, warning.Code);
                Console.ResetColor();
            }

            foreach (var error in _errors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[{4}] {0} ({1},{2}): {3}", Path.Combine(Path.GetDirectoryName(error.ProjectFile), error.File ?? string.Empty), error.LineNumber, error.ColumnNumber, error.Message, error.Code);
                Console.ResetColor();
            }

            Console.WriteLine();

            if (_categories.Count > 0)
            {
                foreach (var category in _categories)
                {
                    Console.ForegroundColor = category.Key.Color;
                    Console.Write("   {0} categorized warnings", category.Value.Count);
                    Console.WriteLine(!string.IsNullOrWhiteSpace(category.Key.Description) ? string.Format(" ({0})", category.Key.Description) : string.Empty);
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("   {0} warnings", _warnings.Count);
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("   {0} errors\n", _errors.Count);
            Console.ResetColor();

            var buildFinishedTimeStamp = DateTime.Now;
            var timeSpan = buildFinishedTimeStamp - _buildStartedTimeStamp;
            Console.WriteLine("   Build finished at {0} in {1}.", buildFinishedTimeStamp, string.Format("{0:m\\:ss} minutes", timeSpan));
        }

        /// <summary>
        /// Handles the BuildStarted event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="BuildStartedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void BuildStartedEventHandler(object sender, BuildStartedEventArgs e)
        {
            _buildStartedTimeStamp = DateTime.Now;
        }

        /// <summary>
        /// Handles the ErrorRaised event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="BuildErrorEventArgs"/> instance containing the event data.</param>
        private void ErrorRaisedEventHandler(object sender, BuildErrorEventArgs e)
        {
            _errors.Add(e);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [{0}] {1} ({2},{3})", e.Code, e.File, e.LineNumber, e.ColumnNumber);
            Console.ResetColor();
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="configFilename">The configuration filename.</param>
        private bool LoadConfiguration(string configFilename)
        {
            if (string.IsNullOrWhiteSpace(configFilename) || !File.Exists(configFilename))
                return false;

            var serializer = new XmlSerializer(typeof(Configuration));
            using (var reader = new StreamReader(configFilename))
            {
                _configuration = (Configuration)serializer.Deserialize(reader);

                foreach (var category in _configuration.Categories)
                {
                    _categories.Add(category, new List<BuildWarningEventArgs>());
                }

                return true;
            }
        }

        /// <summary>
        /// Handles the ProjectStarted event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProjectStartedEventArgs"/> instance containing the event data.</param>
        private void ProjectStartedEventHandler(object sender, ProjectStartedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.TargetNames) && !_projects.Contains(e.ProjectFile))
            {
                _projects.Add(e.ProjectFile);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Project: {0}", e.ProjectFile);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Handles the TargetStarted event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TargetStartedEventArgs"/> instance containing the event data.</param>
        private void TargetStartedEventHandler(object sender, TargetStartedEventArgs e)
        {
            if (e.TargetName.StartsWith("__", StringComparison.Ordinal))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Target: {0}", e.TargetName);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Handles the WarningRaised event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="BuildWarningEventArgs"/> instance containing the event data.</param>
        private void WarningRaisedEventHandler(object sender, BuildWarningEventArgs e)
        {
            var category = _configuration.Categories.FirstOrDefault(c => c.Warnings.Contains(e.Code));
            if (category != null)
            {
                _categories[category].Add(e);
            }
            else
            {
                _warnings.Add(e);
            }
        }

        #endregion Private Methods

        #endregion Methods
    }
}