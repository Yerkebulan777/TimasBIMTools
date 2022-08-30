using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using RevitTimasBIMTools.RevitModel;

namespace RevitTimasBIMTools.Services
{
    internal class FiIeIOServise
    {
        private readonly string path;
        public FiIeIOServise(string filePaht)
        {
            this.path = filePaht;
        }

        public void SaveData(object structDataList)
        {
            using (StreamWriter streamWriter = File.CreateText(path))
            {
                string output = JsonConvert.SerializeObject(structDataList);
                streamWriter.WriteLine(output);
            }
        }

        public BindingList<ElementModel> LoadData()
        {
            if (!File.Exists(path))
            {
                File.CreateText(path).Dispose();
                return new BindingList<ElementModel>();
            }
            using (var reader = File.OpenText(path))
            {
                var fileText = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<BindingList<ElementModel>>(fileText);
            }
        }
    }
}
