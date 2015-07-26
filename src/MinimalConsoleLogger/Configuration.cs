using System;
using System.Xml.Serialization;

namespace CodehulkNET.MSBuild.Loggers.MinimalConsoleLogger
{
    /// <summary>
    /// Contains the configuration of the <see cref="MinimalConsoleLogger"/>.
    /// </summary>
    [Serializable]
    [XmlRoot("Categories")]
    public class Configuration
    {
        #region Properties

        /// <summary>
        /// Gets or sets the categories.
        /// </summary>
        /// <value>
        /// The categories.
        /// </value>
        [XmlElement("Category")]
        public Category[] Categories
        {
            get;
            set;
        }

        #endregion Properties
    }
}