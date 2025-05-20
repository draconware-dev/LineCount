using System.Text.Json.Serialization;

namespace LineCount.Serialization.Json;

[JsonSerializable(typeof(LineCountReport))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, WriteIndented = true)]
public partial class LineCountReportJsonContext : JsonSerializerContext
{
    
}