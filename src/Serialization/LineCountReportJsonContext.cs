using System.Text.Json.Serialization;

namespace Linecount.Serialization.Json;

[JsonSerializable(typeof(ILineCountReport))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization, PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
public partial class LineCountReportJsonContext : JsonSerializerContext;