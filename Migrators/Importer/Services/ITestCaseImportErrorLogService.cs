using System;
using System.Collections.Generic;
using TestIT.ApiClient.Model;

namespace Importer.Services;


/// <summary>
/// Defines a contract for a service that logs errors encountered during test case processing
/// to a dedicated file, including context and details of the problematic test cases.
/// </summary>
public interface ITestCaseImportErrorLogService
{
    /// <summary>
    /// Logs an error related to test case processing.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="contextMessage">A message describing the context of the error.</param>
    /// <param name="problematicTestCase">The specific test case that might have directly caused the error (optional).</param>
    /// <param name="associatedTestCases">A collection of test cases that were being processed or are related to the error context (optional).</param>
    void LogError(Exception ex, string contextMessage, Object? problematicTestCase = null, IEnumerable<Object>? associatedTestCases = null);
} 