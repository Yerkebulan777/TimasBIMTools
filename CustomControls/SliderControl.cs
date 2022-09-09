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
