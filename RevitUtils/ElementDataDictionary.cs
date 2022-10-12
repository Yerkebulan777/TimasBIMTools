using Newtonsoft.Json;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class ElementDataDictionary
    {

        [JsonExtensionData]

        private static readonly string fileName = Path.Combine(SmartToolHelper.DocumentPath, @"SizeTypeData.json");
        private static readonly JsonSerializerSettings options = new() { NullValueHandling = NullValueHandling.Ignore };
        public static ConcurrentDictionary<string, ElementTypeData> ElementTypeSizeDictionary = new();

        [STAThread]
        public static void SerializeData(ConcurrentDictionary<string, ElementTypeData> sourceDict)
        {
            if (sourceDict.Count > 0)
            {
                try
                {
                    using MemoryStream stream = new(capacity: 10);
                    using StreamWriter writer = new(stream, Encoding.UTF8);
                    using JsonTextWriter jsonWriter = new(writer);
                    JsonSerializer.CreateDefault(options).Serialize(jsonWriter, sourceDict);
                    jsonWriter.Flush();
                    stream.Position = 0;
                    File.WriteAllBytes(fileName, stream.ToArray());
                }
                catch (Exception exc)
                {
                    Logger.Error(nameof(SerializeData) + ":\t" + exc.Message);
                }
            }
        }


        public void DeserialiseSizeData()
        {
            if (File.Exists(fileName))
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
