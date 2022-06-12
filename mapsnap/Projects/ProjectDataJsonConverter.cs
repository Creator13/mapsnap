using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mapsnap.Projects;

internal class ProjectDataJsonConverter : JsonConverter<ProjectSaveData>
{
    public override ProjectSaveData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return null;
    }

    public override void Write(Utf8JsonWriter writer, ProjectSaveData value, JsonSerializerOptions options) { }
}