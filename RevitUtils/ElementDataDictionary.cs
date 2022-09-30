﻿using Newtonsoft.Json;
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
        private Dictionary<string, ElementTypeData> dataDict { get; set; }
        private static readonly string fileName = Path.Combine(SmartToolGeneralHelper.DocumentPath, @"TypeSizeData.json");
        private static readonly JsonSerializerSettings options = new() { NullValueHandling = NullValueHandling.Ignore };

        public static ConcurrentDictionary<string, ElementTypeData> ElementTypeSizeDictionary = new();

        [STAThread]
        public void SerializeData(ConcurrentDictionary<string, ElementTypeData> sourceDict)
        {

            dataDict = sourceDict.ToDictionary(k => k.Key, v => v.Value);
            if (dataDict.Count > 0)
            {
                try
                {
                    using MemoryStream stream = new(capacity: 100);
                    using StreamWriter writer = new(stream, Encoding.UTF8);
                    using JsonTextWriter jsonWriter = new(writer);
                    JsonSerializer.CreateDefault(options).Serialize(jsonWriter, dataDict);
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
