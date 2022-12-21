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
            int number = count + 1;
            data[value] = number;
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


