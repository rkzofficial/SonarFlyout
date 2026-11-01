using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Windows.UI.ViewManagement;

namespace SonarFlyout;

static class Motion
{
    static readonly UISettings s_settings = new();
    static readonly TimeSpan s_short = TimeSpan.FromMilliseconds(150);

    static bool Enabled => s_settings.AnimationsEnabled;

    static CompositionEasingFunction Decelerate(Compositor compositor) =>
        compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

    public static void PrepareEnter(UIElement element)
    {
        if (Enabled)
            ElementCompositionPreview.GetElementVisual(element).Opacity = 0;
    }

    public static void Enter(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        if (!Enabled)
        {
            visual.Opacity = 1;
            return;
        }
        var compositor = visual.Compositor;
        var ease = Decelerate(compositor);
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

        var slide = compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0, new Vector3(0, 16, 0));
        slide.InsertKeyFrame(1, Vector3.Zero, ease);
        slide.Duration = TimeSpan.FromMilliseconds(250);

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, ease);
        fade.Duration = TimeSpan.FromMilliseconds(200);

        visual.StartAnimation("Translation", slide);
        visual.StartAnimation("Opacity", fade);
    }

    public static void Pop(FrameworkElement element)
    {
        if (!Enabled)
            return;
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        visual.CenterPoint = new Vector3((float)element.ActualWidth / 2, (float)element.ActualHeight / 2, 0);

        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0.35f, new Vector3(0.8f, 0.8f, 1));
        scale.InsertKeyFrame(1, Vector3.One, Decelerate(compositor));
        scale.Duration = TimeSpan.FromMilliseconds(250);
        visual.StartAnimation("Scale", scale);
    }

    public static void Rotate(FrameworkElement element, float degrees)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        visual.CenterPoint = new Vector3((float)element.ActualWidth / 2, (float)element.ActualHeight / 2, 0);
        if (!Enabled)
        {
            visual.RotationAngleInDegrees = degrees;
            return;
        }
        var rotate = visual.Compositor.CreateScalarKeyFrameAnimation();
        rotate.InsertKeyFrame(1, degrees, Decelerate(visual.Compositor));
        rotate.Duration = s_short;
        visual.StartAnimation("RotationAngleInDegrees", rotate);
    }

    public static void FadeInWhenShown(UIElement element)
    {
        if (!Enabled)
            return;
        var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.Target = "Opacity";
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, Decelerate(compositor));
        fade.Duration = s_short;
        ElementCompositionPreview.SetImplicitShowAnimation(element, fade);
    }
}
