using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.CustomControls
{
    internal class SliderControl : Control
    {
        public SliderControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderControl), new FrameworkPropertyMetadata(typeof(SliderControl)));
        }
    }

}
