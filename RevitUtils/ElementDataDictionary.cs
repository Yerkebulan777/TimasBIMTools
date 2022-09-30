using Newtonsoft.Json;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class ElementDataDictionary
    {

        [JsonExtensionData]
        private Dictionary<string, ElementTypeData> dataDict { get; set; }

        public static ConcurrentDictionary<string, ElementTypeData> ElementTypeSizeDictionary = new();

        private static readonly string dataPath = Path.Combine(SmartToolGeneralHelper.DocumentPath, @"TypeSizeData.json");



        [STAThread]
        public void SerializeData(ConcurrentDictionary<string, ElementTypeData> sourceDict)
        {

            dataDict = sourceDict.ToDictionary(k => k.Key, v => v.Value);
            if (dataDict.Count > 0)
            {
                string json = JsonConvert.SerializeObject(dataDict, Formatting.Indented);
                //string json = JsonSerializer.Serialize(dataDict, options);
                try
                {
                    //File.WriteAllText(dataPath, json);
                }
                catch (Exception exc)
                {
                    Logger.Error(nameof(SerializeData) + exc.Message);
                }
                finally
                {
                    //Logger.Info(json);
                }
            }
        }


        public void DeserialiseSizeData()
        {
            if (File.Exists(dataPath))
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
