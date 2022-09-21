using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.CustomControls
{
    [TemplatePart(Name = SliderPart, Type = typeof(Slider))]
    [TemplatePart(Name = SliderBorder, Type = typeof(Border))]
    internal sealed class SliderBar : Control
    {
        public const string SliderPart = "PART_Slider";
        public const string SliderBorder = "PART_Border";
        
        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(string), typeof(SliderBar), new PropertyMetadata("Text"));


        public SliderBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderBar), new FrameworkPropertyMetadata(typeof(SliderBar)));
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


        public Slider sliderControl;
        public Border sliderBorder;

        public override void OnApplyTemplate()
        {
            sliderControl = FindName("PART_Slider") as Slider;
            sliderBorder = GetTemplateChild(SliderBorder) as Border;
            sliderBorder.CornerRadius = CornerRadius;
            base.OnApplyTemplate();
        }
    }

}
