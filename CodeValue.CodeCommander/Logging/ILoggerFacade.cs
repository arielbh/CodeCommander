﻿namespace CodeValue.CodeCommander.Logging
{
    /// <summary>
    /// Defines a simple logger façade to be used by the Composite Application Library.
    /// </summary>
    public interface ILoggerFacade
    {
        /// <summary>
        /// When True write the log message else ignore the writing
        /// </summary>
        bool EnableLog { get; set; }

        /// <summary>
        /// Write a new log entry with the specified category and priority.
        /// </summary>
        /// <param name="message">Message body to log.</param>
        /// <param name="category">Category of the entry.</param>
        /// <param name="priority">The priority of the entry.</param>
        void Log(string message, Category category, Priority priority);
    }
}