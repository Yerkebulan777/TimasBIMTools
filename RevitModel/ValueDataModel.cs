using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.RevitModel;

internal sealed class ValueDataModel
{
    public int Counter { get; private set; } = 0;
    public string Content { get; private set; } = null;
    private IDictionary<string, int> data { get; set; } = null;

    public ValueDataModel(string value)
    {

        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(nameof(value), "Can't be null.");
        }

        data = new Dictionary<string, int> { { value, 0 } };

    }

    public void SetNewValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(nameof(value), "Can't be null.");
        }
        else if (data.TryGetValue(value, out int count))
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
            data?.Add(value, 0);
        }
    }
}
