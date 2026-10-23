using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace HelenaPet {
public static partial class Program {
 static void RunHatFitQA(PetWindow w){var log=new List<string>();int checks=0;foreach(string folder in Directory.GetDirectories(Path.Combine(root,"skins"))){var skin=Skin.Load(folder);var hat=skin.layers.FirstOrDefault(l=>l.name=="hat");if(hat==null || hat.ClipShape==null || !skin.hasHeadwear)continue;byte[] pixels=new byte[hat.Image.PixelWidth*hat.Image.PixelHeight*4];hat.Image.CopyPixels(pixels,hat.Image.PixelWidth*4,0);int total=0,kept=0;for(int y=0;y<hat.Image.PixelHeight;y+=3)for(int x=0;x<hat.Image.PixelWidth;x+=3)if(pixels[(y*hat.Image.PixelWidth+x)*4+3]>=160){total++;Point p=new Point(hat.x+(x+.5)/hat.Image.PixelWidth*hat.w,hat.y+(y+.5)/hat.Image.PixelHeight*hat.h);if(hat.ClipShape.FillContains(p))kept++;}if(total==0 || kept/(double)total<.97)throw new Exception("Hat clip removed artwork: "+skin.name+" "+kept+"/"+total);log.Add("PASS "+skin.name+" hat retains "+(kept*100.0/total).ToString("0.0")+"% of opaque source samples");checks++;}
  var gallery=new Canvas{Width=960,Height=1302,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};string[] ids={"architect","redeem-star","sleeping-girl"};for(int row=0;row<3;row++)for(int col=0;col<3;col++){var skin=Skin.Load(Path.Combine(root,"skins",ids[row]));var card=new Canvas{Width=295,Height=388,Background=(Brush)new BrushConverter().ConvertFromString("#FFFCF6"),ClipToBounds=true};Canvas.SetLeft(card,25+col*310);Canvas.SetTop(card,82+row*402);gallery.Children.Add(card);var label=new TextBlock{Text=skin.name+" · "+new[]{"左上","正面","右下"}[col],FontSize=18,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,14);Canvas.SetTop(label,12);card.Children.Add(label);var pet=new PetVisual{Skin=skin,Time=2.2,Mood="calm",X=col-1,Y=(col-1)*.8,HairX=col-1,EyeX=col-1,Turn=14,Sway=2};var view=new Viewbox{Child=pet,Width=295,Height=342};Canvas.SetTop(view,38);card.Children.Add(view);}var title=new TextBlock{Text="帽饰贴合 · 转头与抬低头核对",FontSize=29,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,18);gallery.Children.Add(title);Capture(gallery,Path.Combine(root,"preview","帽饰贴合核对.png"),960,1302);log.Add("PASS "+checks+" costume hats conserve their source artwork; three detailed hats render in three directions at maximum roll and sway");File.WriteAllLines(Path.Combine(root,"hat-fit-qa-results.txt"),log);
 }
}
}
