using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.CustomControls
{
    [TemplatePart(Name = SliderBorder, Type = typeof(Border))]
    internal sealed class SliderBar : Slider
    {
        public const string SliderBorder = "PART_Border";

        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(string), typeof(SliderBar), new PropertyMetadata(""));


        public SliderBar()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderBar), new FrameworkPropertyMetadata(typeof(SliderBar)));
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
            = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(SliderBar), new FrameworkPropertyMetadata(default(CornerRadius)));


        public Border sliderBorder { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            sliderBorder = GetTemplateChild(SliderBorder) as Border;
        }
    }

}
