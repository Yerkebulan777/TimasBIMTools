using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.CustomControls
{
    [TemplatePart(Name = SliderBorder, Type = typeof(Border))]
    internal sealed class SliderControl : Slider
    {
        public const string SliderBorder = "PART_Border";

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


        public CornerRadius CornerRadius
        {
            get
            {
                return (CornerRadius)GetValue(CornerRadiusProperty);
            }
            set
            {
                SetValue(CornerRadiusProperty, value);
            }
        }

        public static readonly DependencyProperty CornerRadiusProperty
            = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(SliderControl), new FrameworkPropertyMetadata(default(CornerRadius)));


        public Border sliderBorder { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            sliderBorder = GetTemplateChild(SliderBorder) as Border;
        }
    }

}
