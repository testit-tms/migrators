using System.Text.Json.Serialization;

namespace Models;

public class Section
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("preconditionSteps")]
    public List<Step> PreconditionSteps { get; set; } = new();

    [JsonPropertyName("postconditionSteps")]
    public List<Step> PostconditionSteps { get; set; } = new();

    [JsonPropertyName("sections")]
    public List<Section> Sections { get; set; } = new();

    public static Section CreateSection(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        PreconditionSteps = new List<Step>(),
        PostconditionSteps = new List<Step>(),
        Sections = new List<Section>()
    };


    /// <summary>
    /// BFS
    /// </summary>
    public static Section? FindSection(Func<Section, bool> predicate, Section sectionRoot)
    {
        var queue = new Queue<Section>();
        queue.Enqueue(sectionRoot);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (predicate(current))
                return current;

            foreach (var section in current.Sections)
            {
                queue.Enqueue(section);
            }
        }

        return null;
    }

    /// <summary>
    /// parallel handling
    /// </summary>
    public static Section? FindSectionParallel(Func<Section, bool> predicate, Section sectionRoot)
    {
        Section? result = null;
        var sectionsToCheck = new List<Section> { sectionRoot };

        Parallel.ForEach(sectionsToCheck, (section, state) =>
        {
            if (result != null) state.Stop();
            if (predicate(section))
            {
                result = section;
                state.Stop();
            }

            lock (sectionsToCheck)
            {
                sectionsToCheck.AddRange(section.Sections);
            }
        });

        return result;
    }
}
