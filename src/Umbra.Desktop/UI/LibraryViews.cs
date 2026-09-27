using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Umbra.Core;
namespace Umbra.Desktop;
public sealed partial class MainWindow
{
    void RenderHome()
    {
        var panel=UI.Column(UI.Text("WELCOME BACK",11,"Muted"),UI.Heading("A place for your originals.",36));
        var last=games.Where(g=>g.LastPlayed.HasValue).OrderByDescending(g=>g.LastPlayed).FirstOrDefault();
        var hero=new Grid();hero.ColumnDefinitions.Add(new ColumnDefinition());hero.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(235)});
        var copy=UI.Column(UI.Text(last is null?"FOUR CONSOLES. ONE COLLECTION.":"CONTINUE PLAYING",11,"Accent"),UI.Heading(last?.Title??"Great games.\nAs you remember them.",32),UI.Text(last is null?"Your PlayStation 2, GameCube, Nintendo 64 and Xbox 360 library,\nwith original presentation at its heart.":$"{last.PlatformLabel}  ·  {last.Playtime} played",14,"Muted"),UI.Row(UI.Button(last is null?"+  Build your library":"▶  Continue",()=>{if(last is null)AddGame();else Play(last);},true),UI.Button(last is null?"Add a folder":"Game details",()=>{if(last is null)AddFolder();else ShowGame(last);})));
        copy.VerticalAlignment=VerticalAlignment.Center;hero.Children.Add(copy);var eclipse=UI.Eclipse(215);Grid.SetColumn(eclipse,1);hero.Children.Add(eclipse);panel.Children.Add(UI.Panel(hero,new Thickness(30)));
        panel.Children.Add(UI.Text("EXPLORE YOUR SYSTEMS",11,"Muted"));
        var systems=new Grid();for(int i=0;i<Enum.GetValues<ConsoleKind>().Length;i++)systems.ColumnDefinitions.Add(new ColumnDefinition());
        var platforms=Enum.GetValues<ConsoleKind>();
        for(int i=0;i<Enum.GetValues<ConsoleKind>().Length;i++)
        {
            var p=platforms[i];var name=ConsoleNames.Display(p);
            var stack=UI.Column(UI.Text($"0{i+1}     /",12,"Muted"),UI.Heading(name,21),UI.Text($"{games.Count(g=>g.Platform==p)} games",12,"Muted"));
            var b=UI.Button("",()=>Navigate(name));b.Content=stack;b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.Padding=new Thickness(20);b.Background=UI.Brush("Panel");Grid.SetColumn(b,i);systems.Children.Add(b);
        }
        panel.Children.Add(systems);
        var recents=games.Where(g=>g.LastPlayed.HasValue).OrderByDescending(g=>g.LastPlayed).Take(3).ToList();
        panel.Children.Add(UI.Heading("Recently played",21));
        if(recents.Count==0) panel.Children.Add(UI.Panel(UI.Column(UI.Text("Your next session starts here.",18),UI.Text("Add a game to begin. Your playtime and recent sessions will appear automatically.",13,"Muted"))));else panel.Children.Add(GameGrid(recents));
        var favorites=games.Where(g=>g.Favorite).Take(3).ToList();
        if(favorites.Count>0) {panel.Children.Add(UI.Heading("Your favorites",21));panel.Children.Add(GameGrid(favorites));}
        panel.Children.Add(UI.Row(UI.Button("View all games  →",()=>Navigate("Library")),UI.Button("System readiness",()=>Navigate("System Status"))));content.Content=panel;
    }
    void RenderLibrary()
    {
        var panel=UI.Column(UI.Text("COLLECTION",11,"Muted"),UI.Heading(page=="Library"?"All games":page,34));
        var search=new TextBox{Text=query,Width=330,ToolTip="Search titles, genres and developers"};System.Windows.Automation.AutomationProperties.SetName(search,"Search games");
        var order=UI.Choice(["Title A–Z","Recently played","Most played","Console"],sort);
        var cards=new WrapPanel();var summary=UI.Text("",12,"Muted");
        void Refresh()
        {
            query=search.Text;sort=order.SelectedIndex;IEnumerable<Game> selected=games;
            selected=page switch {"Favorites"=>selected.Where(g=>g.Favorite),"PlayStation 2"=>selected.Where(g=>g.Platform==ConsoleKind.PS2),"GameCube"=>selected.Where(g=>g.Platform==ConsoleKind.GameCube),"Nintendo 64"=>selected.Where(g=>g.Platform==ConsoleKind.N64),"Xbox 360"=>selected.Where(g=>g.Platform==ConsoleKind.Xbox360),_=>selected};
            if(!string.IsNullOrWhiteSpace(query))selected=selected.Where(g=>(g.Title+" "+g.Metadata.Genre+" "+g.Metadata.Developer).Contains(query,StringComparison.OrdinalIgnoreCase));
            selected=sort switch {1=>selected.OrderByDescending(g=>g.LastPlayed),2=>selected.OrderByDescending(g=>g.PlaySeconds),3=>selected.OrderBy(g=>g.Platform).ThenBy(g=>g.Title),_=>selected.OrderBy(g=>g.Title,StringComparer.OrdinalIgnoreCase)};
            var list=selected.ToList();summary.Text=$"{list.Count} game(s)  ·  {list.Count(g=>g.Missing)} unavailable";cards.Children.Clear();foreach(var game in list)cards.Children.Add(GameCard(game));
            if(list.Count==0)cards.Children.Add(UI.Panel(UI.Column(UI.Eclipse(125),UI.Heading(games.Count==0?"Make room for your favorites.":"No matching games",23),UI.Text(games.Count==0?"Import a game or choose a folder. Your originals stay right where they are.":"Try another search or console filter.",14,"Muted"),UI.Row(UI.Button("Add game",AddGame,true),UI.Button("Add folder",AddFolder)))));
        }
        search.TextChanged+=(_,_)=>Refresh();order.SelectionChanged+=(_,_)=>Refresh();panel.Children.Add(UI.Row(search,order,UI.Button("Add folder",AddFolder)));panel.Children.Add(summary);panel.Children.Add(cards);Refresh();content.Content=panel;
    }
    WrapPanel GameGrid(IEnumerable<Game> items) {var panel=new WrapPanel();foreach(var game in items)panel.Children.Add(GameCard(game));return panel;}
    Button GameCard(Game game)
    {
        var art=new Grid{Height=150,ClipToBounds=true,Background=new SolidColorBrush(game.Platform switch {ConsoleKind.PS2=>Color.FromRgb(37,42,49),ConsoleKind.GameCube=>Color.FromRgb(43,40,48),_=>Color.FromRgb(43,44,37)})};
        if(File.Exists(game.Metadata.CoverPath))
        {
            try {var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri(game.Metadata.CoverPath);image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=500;image.EndInit();art.Children.Add(new Image{Source=image,Stretch=Stretch.UniformToFill});}catch{ }
        }
        if(art.Children.Count==0)
        {
            var orbital=UI.Eclipse(175);orbital.HorizontalAlignment=HorizontalAlignment.Right;orbital.Margin=new Thickness(80,-25,-25,0);orbital.Opacity=.6;art.Children.Add(orbital);
            var number=UI.Heading(game.Platform==ConsoleKind.PS2?"02":game.Platform==ConsoleKind.GameCube?"GC":game.Platform==ConsoleKind.Xbox360?"360":"64",47);number.Margin=new Thickness(19,65,0,0);art.Children.Add(number);
        }
        var copy=UI.Column(UI.Text(game.PlatformLabel,9,"Accent"),UI.Heading((game.Favorite?"★  ":"")+game.Title,17),UI.Text(game.Missing?"FILE UNAVAILABLE":game.PlaySeconds>0?game.Playtime+" played":"Ready for a new session",11,"Muted"));copy.Margin=new Thickness(17,17,17,10);
        var card=UI.Button("",()=>ShowGame(game));card.Content=UI.Column(art,copy);card.Width=260;card.Padding=new Thickness(0);card.Margin=new Thickness(0,0,15,16);card.Background=UI.Brush("Panel");card.HorizontalContentAlignment=HorizontalAlignment.Stretch;return card;
    }
}
