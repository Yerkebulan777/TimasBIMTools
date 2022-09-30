using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class ElementDataDictionary
    {
        public static ConcurrentDictionary<string, ElementTypeData> ElementTypeSizeDictionary = new();

        private static readonly string dataPath = Path.Combine(SmartToolGeneralHelper.DocumentPath, "DataBase", @"TypeSizeData.json");

        private static readonly JsonDocumentOptions docOptions = new() { CommentHandling = JsonCommentHandling.Skip };
        private static readonly JsonWriterOptions writerOptions = new() { Indented = true };
        private static readonly JsonSerializerOptions options = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };


        public static void OnSerializeData(ConcurrentDictionary<string, ElementTypeData> dictionary)
        {
            if (!string.IsNullOrEmpty(dataPath) && dictionary.Count > 0)
            {
                SerializeSizeData(dictionary.ToDictionary(k => k.Key, v => v.Value), dataPath);
            }
        }


        public static void OnDeserialiseSizeData()
        {
            if (!string.IsNullOrEmpty(dataPath))
            {
                DeserialiseSizeData(dataPath);
            }
        }


        private static void SerializeSizeData(Dictionary<string, ElementTypeData> data, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!File.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
            try
            {
                using FileStream fs = File.OpenWrite(path);
                string json = JsonSerializer.Serialize(data, options);
                using JsonDocument jdoc = JsonDocument.Parse(json, docOptions);
                using Utf8JsonWriter writer = new(fs, options: writerOptions);

                JsonElement root = jdoc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    writer.WriteStartObject();
                }
                else
                {
                    return;
                }

                foreach (JsonProperty property in root.EnumerateObject())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();

                writer.Flush();

            }
            catch (Exception exc)
            {
                Logger.Error(nameof(SerializeSizeData) + exc.Message);
            }
            finally
            {
                Task.Delay(100).Wait();
            }
        }


        private static void DeserialiseSizeData(string path)
        {
            if (File.Exists(path))
            {
                try
                {

                }
                catch (Exception exc)
                {
                    Logger.Error(nameof(DeserialiseSizeData) + exc.Message);
                }
                finally { Task.Delay(100).Wait(); }
            }
        }
    }
}
