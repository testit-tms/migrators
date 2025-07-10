using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Importer.Utils;

public static class HtmlEscapeUtils
{
    private const string NoEscapeHtmlEnvVar = "NO_ESCAPE_HTML";

    // Regex pattern to detect HTML tags
    private static readonly Regex HtmlTagPattern = new(@"<\S.*?(?:>|\/>)", RegexOptions.Compiled);
    
    // Regex patterns to escape only non-escaped characters
    private static readonly Regex LessThanPattern = new(@"(?<!\\)<", RegexOptions.Compiled);
    private static readonly Regex GreaterThanPattern = new(@"(?<!\\)>", RegexOptions.Compiled);

    /// <summary>
    /// Escapes HTML tags to prevent XSS attacks.
    /// First checks if the string contains HTML tags using regex pattern.
    /// Only performs escaping if HTML tags are detected.
    /// Escapes all &lt; as \\&lt; and &gt; as \\&gt; only if they are not already escaped.
    /// Uses regex with negative lookbehind to avoid double escaping.
    /// </summary>
    /// <param name="text">The text to escape</param>
    /// <returns>Escaped text or original text if escaping is disabled or no HTML tags found</returns>
    public static string? EscapeHtmlTags(string? text)
    {
        if (text == null)
        {
            return null;
        }

        // First check if the string contains HTML tags
        if (!HtmlTagPattern.IsMatch(text))
        {
            return text; // No HTML tags found, return original string
        }

        // Use regex with negative lookbehind to escape only non-escaped characters
        var result = LessThanPattern.Replace(text, "\\<");
        result = GreaterThanPattern.Replace(result, "\\>");
        
        return result;
    }

    /// <summary>
    /// Escapes HTML tags in all String properties of an object using reflection
    /// Also processes List properties: if List of objects - calls EscapeHtmlInObjectList,
    /// Can be disabled by setting NO_ESCAPE_HTML environment variable to "true"
    /// if List of Strings - escapes each string
    /// </summary>
    /// <param name="obj">The object to process</param>
    /// <returns>The processed object with escaped strings</returns>
    public static T? EscapeHtmlInObject<T>(T? obj)
    {
        if (obj == null)
        {
            return default;
        }

        // Check if escaping is disabled via environment variable
        var noEscapeHtml = Environment.GetEnvironmentVariable(NoEscapeHtmlEnvVar);
        if ("true".Equals(noEscapeHtml, StringComparison.OrdinalIgnoreCase))
        {
            return obj;
        }

        try
        {
            var type = obj.GetType();

            // Process only properties (modern C# models use properties, not fields)
            ProcessProperties(obj, type);
        }
        catch (Exception)
        {
            // Silently ignore reflection errors
        }

        return obj;
    }

    /// <summary>
    /// Escapes HTML tags in all String properties of objects in a list using reflection
    /// Can be disabled by setting NO_ESCAPE_HTML environment variable to "true"
    /// </summary>
    /// <param name="list">The list of objects to process</param>
    /// <returns>The processed list with escaped strings in all objects</returns>
    public static List<T>? EscapeHtmlInObjectList<T>(List<T>? list)
    {
        if (list == null)
        {
            return null;
        }

        // Check if escaping is disabled via environment variable
        var noEscapeHtml = Environment.GetEnvironmentVariable(NoEscapeHtmlEnvVar);
        if ("true".Equals(noEscapeHtml, StringComparison.OrdinalIgnoreCase))
        {
            return list;
        }

        foreach (var obj in list)
        {
            EscapeHtmlInObject(obj);
        }

        return list;
    }

    private static void ProcessProperties(object obj, Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            try
            {
                // Only process writable properties
                if (!property.CanRead || !property.CanWrite)
                    continue;

                var value = property.GetValue(obj);
                
                if (value is string stringValue)
                {
                    // Escape String properties
                    property.SetValue(obj, EscapeHtmlTags(stringValue));
                }
                else if (value is IList list && list.Count > 0)
                {
                    ProcessList(list);
                }
                else if (value != null && !IsSimpleType(value.GetType()))
                {
                    // Process nested objects (but not simple types like int, DateTime, etc.)
                    EscapeHtmlInObject(value);
                }
            }
            catch (Exception)
            {
                // Silently ignore reflection errors for individual properties
            }
        }
    }

    private static void ProcessList(IList list)
    {
        if (list.Count == 0)
            return;

        var firstElement = list[0];
        
        if (firstElement is string)
        {
            // List of Strings - escape each string
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is string stringElement)
                {
                    list[i] = EscapeHtmlTags(stringElement);
                }
            }
        }
        else if (firstElement != null)
        {
            // List of objects - process each object
            foreach (var item in list)
            {
                EscapeHtmlInObject(item);
            }
        }
    }

    /// <summary>
    /// Checks if a type is a simple type that doesn't need HTML escaping
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <returns>True if it's a simple type</returns>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type == typeof(Uri) ||
               // Nullable wrappers
               type == typeof(bool?) ||
               type == typeof(byte?) ||
               type == typeof(char?) ||
               type == typeof(short?) ||
               type == typeof(int?) ||
               type == typeof(long?) ||
               type == typeof(float?) ||
               type == typeof(double?) ||
               type == typeof(decimal?) ||
               type == typeof(DateTime?) ||
               type == typeof(DateTimeOffset?) ||
               type == typeof(TimeSpan?) ||
               type == typeof(Guid?) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }
} 
