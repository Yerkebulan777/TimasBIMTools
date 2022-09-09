using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.CustomControls
{
    internal sealed class SliderControl : Control
    {
        private Label label;
        private Slider slider;
        private TextBlock text;
        private Border border;


        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(string), typeof(SliderControl), new PropertyMetadata(""));


        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SliderControl), new PropertyMetadata(0));


        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(SliderControl), new PropertyMetadata(0));


        public double Maximum
        {
            get => (double)GetValue(MaximumProperty); 
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(SliderControl), new PropertyMetadata(100));


        public double TickFrequency
        {
            get => (double)GetValue(TickFrequencyProperty);
            set => SetValue(TickFrequencyProperty, value);
        }

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register("TickFrequency", typeof(double), typeof(SliderControl), new PropertyMetadata(5));


        public SliderControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderControl), new FrameworkPropertyMetadata(typeof(SliderControl)));
        }


        public override void OnApplyTemplate()
        {
            label = Template.FindName("SliderLabel", this) as Label;
            text = Template.FindName("SliderText", this) as TextBlock;
            border = Template.FindName("SliderBorder", this) as Border;
            slider = Template.FindName("SliderControl", this) as Slider;

            base.OnApplyTemplate();
        }


    }
}
