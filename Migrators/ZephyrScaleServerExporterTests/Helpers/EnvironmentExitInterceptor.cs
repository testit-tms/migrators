using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ZephyrScaleServerExporterTests.Helpers;

/// <summary>
/// Helper class to intercept Environment.Exit calls in tests
/// </summary>
public static class EnvironmentExitInterceptor
{
    private static Harmony? _harmony;
    private static readonly List<int> ExitCalls = new();
    private static bool _isIntercepted;

    /// <summary>
    /// Start intercepting Environment.Exit calls
    /// </summary>
    public static void StartIntercepting()
    {
        if (_isIntercepted) return;

        _harmony = new Harmony("EnvironmentExitInterceptor");
        _harmony.Patch(
            typeof(Environment).GetMethod(nameof(Environment.Exit), new[] { typeof(int) })!,
            new HarmonyMethod(typeof(EnvironmentExitInterceptor), nameof(ExitPrefix))
        );
        _isIntercepted = true;
        ExitCalls.Clear();
    }

    /// <summary>
    /// Stop intercepting Environment.Exit calls
    /// </summary>
    public static void StopIntercepting()
    {
        if (!_isIntercepted) return;

        _harmony?.UnpatchAll("EnvironmentExitInterceptor");
        _harmony = null;
        _isIntercepted = false;
        ExitCalls.Clear();
    }

    /// <summary>
    /// Get the list of exit codes that were intercepted
    /// </summary>
    public static IReadOnlyList<int> GetExitCalls() => ExitCalls;

    /// <summary>
    /// Clear the list of exit calls
    /// </summary>
    public static void ClearExitCalls() => ExitCalls.Clear();

    /// <summary>
    /// Harmony prefix method that intercepts Environment.Exit
    /// </summary>
    private static bool ExitPrefix(int exitCode)
    {
        ExitCalls.Add(exitCode);
        // Return false to prevent the original method from executing
        return false;
    }
}
