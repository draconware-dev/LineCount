using System.Text.Json.Serialization;

namespace Linecount.Serialization.Json;

[JsonSerializable(typeof(LineCountReport))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization, PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
public partial class LineCountReportJsonContext : JsonSerializerContext;