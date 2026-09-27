using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace Umbra.Desktop;
public static class UI
{
    public static Brush Brush(string key)=>(Brush)Application.Current.FindResource(key);
    public static TextBlock Text(string text,double size=14,string color="Ink")=>new(){Text=text,FontSize=size,Foreground=Brush(color),Margin=new Thickness(0,0,0,10)};
    public static TextBlock Heading(string text,double size=30)=>new(){Text=text,FontSize=size,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,12)};
    public static Button Button(string text,Action action,bool primary=false)
    {
        var b=new Button{Content=text}; if(primary) { b.Background=Brush("Accent"); b.Foreground=new SolidColorBrush(Color.FromRgb(20,20,20)); }
        b.Click+=(_,_)=>action(); System.Windows.Automation.AutomationProperties.SetName(b,text); return b;
    }
    public static Border Panel(UIElement child,Thickness? padding=null)=>new(){Background=Brush("Panel"),CornerRadius=new CornerRadius(10),Padding=padding??new Thickness(24),Margin=new Thickness(0,0,0,18),Child=child};
    public static StackPanel Column(params UIElement[] children) { var p=new StackPanel(); foreach(var child in children) p.Children.Add(child); return p; }
    public static WrapPanel Row(params UIElement[] children) { var p=new WrapPanel(); foreach(var child in children) p.Children.Add(child); return p; }
    public static ComboBox Choice(IEnumerable<string> values,int selected=0)
    { var combo=new ComboBox{MinWidth=190}; foreach(var value in values) combo.Items.Add(value); combo.SelectedIndex=selected; return combo; }
    public static Canvas Eclipse(double size=200)
    {
        var canvas=new Canvas{Width=size,Height=size,ClipToBounds=true,IsHitTestVisible=false};
        for(int i=0;i<3;i++)
        {
            var ring=new Ellipse{Width=size-10-i*30,Height=size-10-i*30,Stroke=new SolidColorBrush(Color.FromRgb((byte)(60+i*12),(byte)(61+i*11),(byte)(63+i*10))),StrokeThickness=1};
            Canvas.SetLeft(ring,5+i*15); Canvas.SetTop(ring,5+i*15); canvas.Children.Add(ring);
        }
        var sun=new Ellipse{Width=size*.62,Height=size*.62,Fill=new SolidColorBrush(Color.FromRgb(194,176,139))}; Canvas.SetLeft(sun,size*.19);Canvas.SetTop(sun,size*.19);canvas.Children.Add(sun);
        var shadow=new Ellipse{Width=size*.62,Height=size*.62,Fill=Brush("Panel")};Canvas.SetLeft(shadow,size*.24);Canvas.SetTop(shadow,size*.16);canvas.Children.Add(shadow);
        return canvas;
    }
}
