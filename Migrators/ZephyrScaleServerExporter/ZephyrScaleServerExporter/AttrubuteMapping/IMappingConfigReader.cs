namespace ZephyrScaleServerExporter.AttrubuteMapping;

public interface IMappingConfigReader
{
        void InitOnce(string configFilePath, string mappingFilesDirectory);
        
        string? GetMappingForValue(
                string targetValue,
                string attributeName);
}