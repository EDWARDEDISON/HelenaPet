using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace HelenaPet {
public partial class PetVisual {
 public Point FacePointForQA(Point p){return headToCanvas.Transform(hitRig.Project(p,.65,"face"));}
}
public static partial class Program {
 static void RunCostumeCoverageQA(PetWindow w){
  string[] ids={"dark-cake","architect","maria","seaside","sweet-cake","haixi","sleeping-girl","clerk","gilda","redeem-star","gold-cake","sea-witch","star-girl","sound-wave"};
  var log=new List<string>();int total=0,integrated=0;var bitmap=new RenderTargetBitmap(440,510,96,96,PixelFormats.Pbgra32);var pixels=new byte[440*510*4];
  foreach(string id in ids){var skin=Skin.Load(Path.Combine(root,"skins",id));var face=skin.layers.Single(l=>l.name=="face");if(face.texture!="@shared-bald-base" || skin.layers.Any(l=>l.texture=="@shared-neutral-face" || l.name=="jaw" || l.name=="scalp"))throw new Exception("Old face overlay remains: "+id);
   if(skin.integratedHeadwear){integrated++;var upper=skin.layers.Single(l=>l.name=="headwear");var lower=skin.layers.Single(l=>l.name=="headwear-lower");if(upper.texture!=lower.texture || upper.depth!=face.depth || lower.depth!=face.depth || upper.sy+upper.sh!=lower.sy || Math.Abs(upper.y+upper.h-lower.y)>.001)throw new Exception("Integrated hair/headwear registration differs: "+id);}
   var pet=new PetVisual{Skin=skin,Mood="calm",Time=2.2,Turn=14,Sway=2};pet.Measure(new Size(440,510));pet.Arrange(new Rect(0,0,440,510));int count=0,minAlpha=255;
   foreach(double x in new[]{-1.0,0,1.0})foreach(double y in new[]{-1.0,0,1.0}){pet.X=x;pet.Y=y;pet.EyeX=x;pet.EyeY=y;pet.HairX=-x;pet.HairY=-y;pet.InvalidateVisual();pet.UpdateLayout();bitmap.Clear();bitmap.Render(pet);bitmap.CopyPixels(pixels,440*4,0);
    for(int py=205;py<=375;py+=5)for(int px=110;px<=292;px+=5){Point p=new Point(px,py);if(!face.ClipShape.FillContains(p) || !face.ClipShape.FillContains(new Point(px-2,py)) || !face.ClipShape.FillContains(new Point(px+2,py)) || !face.ClipShape.FillContains(new Point(px,py-2)) || !face.ClipShape.FillContains(new Point(px,py+2)))continue;Point c=pet.FacePointForQA(p);int cx=(int)Math.Round(c.X),cy=(int)Math.Round(c.Y);if(cx<0 || cx>=440 || cy<0 || cy>=510)throw new Exception("Face left canvas: "+id);int alpha=pixels[(cy*440+cx)*4+3];minAlpha=Math.Min(minAlpha,alpha);if(alpha<245)throw new Exception("Transparent face gap: "+id+" at "+px+","+py+" pose "+x+","+y+" alpha "+alpha);count++;}
   }total+=count;log.Add("PASS "+id+": nine directional poses; "+count+" interior face samples; minimum alpha "+minAlpha+"; single bald base, no old face overlay");
  }
  if(integrated!=8)throw new Exception("Expected eight integrated hairstyles; got "+integrated);
  for(int sheet=0;sheet<2;sheet++){var gallery=new Canvas{Width=960,Height=2896,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};var title=new TextBlock{Text="全部皮肤 · 左上 / 正面 / 右下 · "+(sheet+1),FontSize=27,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,18);gallery.Children.Add(title);
   for(int row=0;row<7;row++)for(int col=0;col<3;col++){string id=ids[sheet*7+row];var skin=Skin.Load(Path.Combine(root,"skins",id));var card=new Canvas{Width=295,Height=388,Background=(Brush)new BrushConverter().ConvertFromString("#FFFCF6"),ClipToBounds=true};Canvas.SetLeft(card,25+col*310);Canvas.SetTop(card,72+row*402);gallery.Children.Add(card);var label=new TextBlock{Text=skin.name,FontSize=18,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,12);Canvas.SetTop(label,10);card.Children.Add(label);var pet=new PetVisual{Skin=skin,Time=2.2,Mood="calm",X=col-1,Y=(col-1)*.8,HairX=1-col,HairY=1-col,EyeX=col-1,Turn=14,Sway=2};var view=new Viewbox{Child=pet,Width=295,Height=342};Canvas.SetTop(view,38);card.Children.Add(view);
   }Capture(gallery,Path.Combine(root,"preview","所有皮肤转头核对-"+(sheet+1)+".png"),960,2896);
  }log.Add("PASS eight hair/headwear assemblies share one source image and one head projection; 14 costumes / 126 poses / "+total+" face alpha samples");File.WriteAllLines(Path.Combine(root,"face-coverage-qa-results.txt"),log);
 }
}
}
