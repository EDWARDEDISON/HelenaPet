using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace HelenaPet {
public static partial class Program {
 static void CaptureMangoEffectGallery(PetWindow w){
  var gallery=new Canvas{Width=1020,Height=900,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};
  var heading=new TextBlock{Text="芒果海伦娜 · 六秒变身",FontSize=28,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D"),FontFamily=new FontFamily("Microsoft YaHei UI")};Canvas.SetLeft(heading,24);Canvas.SetTop(heading,20);gallery.Children.Add(heading);
  double[] ages={0,.35,1,2.12,5.65,6};string[] labels={"开始 · 原来的海伦娜","柔软烟雾 · 变身中","烟雾散开 · 变身完成","轻轻跳起 · 落地回声","烟雾浮起 · 恢复中","结束 · 回到原皮肤"};
  for(int i=0;i<ages.Length;i++){
   var card=new Canvas{Width=316,Height=394,Background=(Brush)new BrushConverter().ConvertFromString("#FFFCF6")};Canvas.SetLeft(card,24+i%3*332);Canvas.SetTop(card,78+i/3*406);gallery.Children.Add(card);
   var label=new TextBlock{Text=labels[i],FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=17,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,14);Canvas.SetTop(label,14);card.Children.Add(label);
   var pet=new PetVisual{Skin=w.Pet.Skin,MangoImage=w.Pet.MangoImage,MangoStarted=1,MangoUntil=1+MangoChance.Duration,Time=1+ages[i],Mood="calm",Sway=.3,Decoration="none",Accessories=w.Pet.Accessories};
   var view=new Viewbox{Child=pet,Width=316,Height=347};Canvas.SetTop(view,42);card.Children.Add(view);
  }Capture(gallery,Path.Combine(root,"preview","芒果六秒变身分镜.png"),1020,900);
 }
}
}
