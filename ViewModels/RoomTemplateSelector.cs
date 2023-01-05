using System.Windows.Controls;
using System.Windows;

namespace RevitTimasBIMTools.ViewModels
{
    public class RoomTemplateSelector : DataTemplateSelector
    {
        public HierarchicalDataTemplate TopLevelTemplate { get; set; }
        public HierarchicalDataTemplate SecondLevelTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is string)
            {
                return TopLevelTemplate;
            }
            else
            {
                return SecondLevelTemplate;
            }
        }
    }
}
