namespace AgyTui.UI.Core.Layouts;

public static class LayoutCalculator
{
    public static (int width, int height) CalculateDynamicBounds(int containerWidth, int containerHeight, double scaleRatio = 0.8)
    {
        var targetWidth = Math.Max(40, (int)(containerWidth * scaleRatio));
        var targetHeight = Math.Max(10, (int)(containerHeight * scaleRatio));
        return (targetWidth, targetHeight);
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
