using RevitTimasBIMTools.Services;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitModel;

internal sealed class ValueDataModel
{
    internal int Counter { get; private set; } = 0;
    internal string Content { get; private set; } = null;
    private IDictionary<string, int> data { get; set; } = new Dictionary<string, int>();

    public ValueDataModel(string value)
    {
        data.Add(value, 0);
    }


    internal void SetNewValue(string value)
    {
        if (data.TryGetValue(value, out int count))
        {
            int number = count++;
            data[value] = number;
            Logger.Log("Set New value...");
            Logger.Log("Value: " + value.ToString());
            Logger.Log("Count: " + number.ToString());
            if (Counter < number)
            {
                Counter = number;
                Content = value;
            }
        }
        else
        {
            data.Add(value, 0);
        }
    }
}

