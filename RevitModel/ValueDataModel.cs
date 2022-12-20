using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitModel;

internal sealed class ValueDataModel
{
    internal int Counter { get; set; } = 0;
    internal string Content { get; set; } = string.Empty;
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
