//#r "nuget: System.Text.Json, 6.0.0"
#r "System.Memory"

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

var m = new SomeData();
Console.WriteLine(m);
var opts = new JsonSerializerOptions
{
    // PropertyNamingPolicy = new UpperCaseNamingPolicy(),
    Converters =
    {
        new SomeDataConverter()
    },
    WriteIndented = true
};

var ser = JsonSerializer.Serialize<SomeData>(m, opts);
Console.WriteLine(ser);
SomeData deser = JsonSerializer.Deserialize<SomeData>(ser, opts);
Console.WriteLine(deser);

public class UpperCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) {
        switch (name)
        {
            case "Id" : return "id";
            default:
                return name;
        }
    }
}

public class SomeDataConverter : JsonConverter<SomeData>
{
    public override SomeData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        Console.WriteLine("R");
        string serialized = reader.GetString();
        Console.WriteLine($"R-> {serialized}");
        Dictionary<string,object> obj = JsonSerializer.Deserialize<Dictionary<string,object>>(serialized);
        JsonElement o = (JsonElement)obj["id"];
        Console.WriteLine($"R-> {o.GetString()}");
        return new SomeData {
            Id = o.GetString()
        };
    }

    public override void Write(Utf8JsonWriter writer, SomeData data, JsonSerializerOptions options) {
        Console.WriteLine("W");
        string serialized = JsonSerializer.Serialize(
            new Dictionary<string,object> {
                {"id", data.Id}
            }
        );
        Console.WriteLine($"W-> {serialized}");
        writer.WriteStringValue(serialized);
    }
}

public class SomeData {
    public string Id { get; set; }

    public SomeData() {
        Id = Guid.NewGuid().ToString();
    }

    public override string ToString() => $"<SomeData id: {Id}>";
}
