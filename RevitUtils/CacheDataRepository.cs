using Newtonsoft.Json;
using SmartBIMTools.Core;
using SmartBIMTools.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace SmartBIMTools.RevitUtils
{
    internal static class CacheDataRepository
    {

        [JsonExtensionData]

        private static readonly string fileName = Path.Combine(SmartToolHelper.DocumentPath, @"TypeData.json");
        private static readonly JsonSerializerSettings options = new() { NullValueHandling = NullValueHandling.Ignore };

        [STAThread]
        public static void SerializeData(IDictionary<string, object> sourceDict)
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
                    SBTLogger.Error(nameof(SerializeData) + ":\t" + exc.Message);
                }
            }
        }


        public static void DeserialiseSizeData()
        {
            if (File.Exists(fileName))
            {
                try
                {

                }
                catch (Exception exc)
                {
                    SBTLogger.Error(nameof(DeserialiseSizeData) + exc.Message);
                }
                finally { Task.Delay(100).Wait(); }
            }
        }

    }
}
