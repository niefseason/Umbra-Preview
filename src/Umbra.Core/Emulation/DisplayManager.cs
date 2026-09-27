namespace Umbra.Core.Emulation;

// Presentation policy shared by all adapters. Output size is separate from emulated speed.
public static class DisplayManager
{
    public static (int Width,int Height) StartupSize(int workWidth,int workHeight,double dpiScale)
    {
        if(workWidth<=0 || workHeight<=0 || !double.IsFinite(dpiScale) || dpiScale<=0)
            throw new ArgumentOutOfRangeException(nameof(workWidth));
        var margin=(int)Math.Ceiling(16*dpiScale);
        return (Math.Max(1,Math.Min((int)(1320*dpiScale),workWidth-2*margin)),
            Math.Max(1,Math.Min((int)(880*dpiScale),workHeight-2*margin)));
    }
    public static bool UsesManagedFullscreen(ConsoleKind platform, bool fullscreen, string renderer)
        => platform==ConsoleKind.N64 && fullscreen && renderer!="glide64mk2";
    public static int InternalScale(DisplaySettings display) => display.Profile switch
    { GraphicsProfile.Enhanced => 3, GraphicsProfile.Custom => display.Scale, _ => 1 };
    public static (int X, int Y, int Width, int Height) Fit(int width, int height, int aspectWidth=4, int aspectHeight=3)
    {
        if(width<=0 || height<=0 || aspectWidth<=0 || aspectHeight<=0) throw new ArgumentOutOfRangeException(nameof(width));
        var fitWidth=Math.Min(width,(int)((long)height*aspectWidth/aspectHeight));
        var fitHeight=Math.Min(height,(int)((long)width*aspectHeight/aspectWidth));
        return ((width-fitWidth)/2,(height-fitHeight)/2,fitWidth,fitHeight);
    }
}
