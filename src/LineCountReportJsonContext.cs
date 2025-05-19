using System.Text.Json.Serialization;
using LineCount;

[JsonSerializable(typeof(LineCountReport))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, WriteIndented = true)]
public partial class LineCountReportJsonContext : JsonSerializerContext
{
    
}