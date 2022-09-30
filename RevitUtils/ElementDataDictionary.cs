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
using System.Windows.Shapes;
using Path = System.IO.Path;

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


        public void OnSerializeData(ConcurrentDictionary<string, ElementTypeData> dictionary)
        {
            Dictionary<string, ElementTypeData> data = dictionary.ToDictionary(k => k.Key, v => v.Value);
            if (data.Count > 0)
            {
                SerializeSizeData(data, dataPath);
            }
        }


        private void SerializeSizeData(Dictionary<string, ElementTypeData> data, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!File.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
            string json = JsonSerializer.Serialize(data, options);
            try
            {
                File.WriteAllText(path, json);
                //using FileStream fs = File.OpenWrite(path);
                //using JsonDocument jdoc = JsonDocument.Parse(json, docOptions);
                //using Utf8JsonWriter writer = new(fs, options: writerOptions);
                //jdoc.WriteTo(writer);
            }
            catch (Exception exc)
            {
                Logger.Error(nameof(SerializeSizeData) + exc.Message);
            }
            finally
            {
                Logger.Info(json);
            }
        }


        public void OnDeserialiseSizeData()
        {
            if (File.Exists(dataPath))
            {
                try
                {

                }
                catch (Exception exc)
                {
                    Logger.Error(nameof(OnDeserialiseSizeData) + exc.Message);
                }
                finally { Task.Delay(100).Wait(); }
            }
        }

    }
}
