using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class ZephyrCyclesDictionary
{
    public Dictionary<string, ZephyrCycle> CyclesDictionary { get; set; }
}
