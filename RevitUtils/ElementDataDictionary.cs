using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class ElementDataDictionary
    {
        public static ConcurrentDictionary<string, ElementTypeData> ElementTypeSizeDictionary = new ConcurrentDictionary<string, ElementTypeData>();
        private static readonly DataContractJsonSerializer formater = new DataContractJsonSerializer(typeof(Dictionary<int, ElementTypeData>));
        private static readonly string dataPath = Path.Combine(SmartToolGeneralHelper.AssemblyDirectory, nameof(ElementDataDictionary));
        

        [STAThread]
        public static void OnSerializeData(ConcurrentDictionary<string, ElementTypeData> dictionary)
        {
            if (!string.IsNullOrEmpty(dataPath) && dictionary.Count > 0)
            {
                SerializeSizeData(dictionary.ToDictionary(k => k.Key, v => v.Value), dataPath);
            }
        }


        [STAThread]
        public static void OnDeserialiseSizeData()
        {
            if (!string.IsNullOrEmpty(dataPath))
            {
                DeserialiseSizeData(dataPath);
            }
        }


        private static void SerializeSizeData(Dictionary<string, ElementTypeData> data, string path)
        {
            try
            {
                if (!File.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(path);
                }
                using (FileStream file = new FileStream(path, FileMode.OpenOrCreate))
                {
                    formater.WriteObject(file, data);
                }
            }
            catch (Exception exc)
            {
                Logger.Error(exc.Message);
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
                    using (FileStream file = File.OpenRead(path))
                    {
                        object result = formater.ReadObject(file);
                        if (result != null && result is Dictionary<string, ElementTypeData> dict)
                        {
                            foreach (KeyValuePair<string, ElementTypeData> item in dict)
                            {
                                ElementTypeSizeDictionary.TryAdd(item.Key, item.Value);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    Logger.Error(exc.Message);
                }
                finally { Task.Delay(100).Wait(); }
            }
        }


    }
}
