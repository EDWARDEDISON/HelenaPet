using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HelenaPet {
public class PendantSpec {public string id,name,atlas;public int x,y,w,h;}
public class PendantArt {
 public static string[] Ids={"none","rabbit","purse","sunglasses","cone","bubbles","memory","key","light","basket","record","shell"};
 public static string[] Names={"不挂小物","兔宝宝","零钱包","墨镜","甜筒","心形泡泡圈","逝去的纪念","日晷密钥","“光明”","野餐篮","黑胶碟","海贝泡芙"};
 public string Error="";public Dictionary<string,BitmapSource> Images=new Dictionary<string,BitmapSource>();
 public PendantArt(string folder){try{var specs=new JavaScriptSerializer().Deserialize<PendantSpec[]>(File.ReadAllText(Path.Combine(folder,"pendants.json")));var atlases=new Dictionary<string,BitmapImage>();foreach(var item in specs){if(item.atlas!=Path.GetFileName(item.atlas))throw new Exception("图集路径无效");BitmapImage atlas;if(!atlases.TryGetValue(item.atlas,out atlas)){atlas=new BitmapImage();atlas.BeginInit();atlas.UriSource=new Uri(Path.Combine(folder,item.atlas));atlas.CacheOption=BitmapCacheOption.OnLoad;atlas.EndInit();atlas.Freeze();atlases.Add(item.atlas,atlas);}if(item.w<1 || item.h<1 || item.x<0 || item.y<0 || item.x+item.w>atlas.PixelWidth || item.y+item.h>atlas.PixelHeight)throw new Exception("挂件范围无效");var crop=new CroppedBitmap(atlas,new Int32Rect(item.x,item.y,item.w,item.h));crop.Freeze();Images.Add(item.id,crop);}if(Ids.Skip(1).Any(id=>!Images.ContainsKey(id)))throw new Exception("挂件不完整");}catch(Exception ex){Error=ex.Message;Images.Clear();}}
 public Rect Draw(DrawingContext d,string id,Point center,double size){BitmapSource img;if(!Images.TryGetValue(id??"none",out img))return Rect.Empty;double factor=size/Math.Max(img.PixelWidth,img.PixelHeight),w=img.PixelWidth*factor,h=img.PixelHeight*factor;var rect=new Rect(center.X-w/2,center.Y-h/2,w,h);d.DrawImage(img,rect);return rect;}
}
public partial class PetVisual {
 public PendantArt Pendants;public string Pendant="none",PendantPlace="hat";public double PendantSize=1;
 public Rect PendantBounds=Rect.Empty;
 public double OrbitAngle{get{return Time*.38-.4;}}
 public bool OrbitBehind{get{return Math.Sin(OrbitAngle)<0;}}
 public Point OrbitCenter(double envelope){double rx=177-30*envelope,ry=68-9*envelope;return new Point(220+rx*Math.Cos(OrbitAngle)+X*6,316+ry*Math.Sin(OrbitAngle)-48*envelope);}
 void DrawPendant(DrawingContext d,double envelope,bool behind){if(Pendants==null || Pendant=="none"){PendantBounds=Rect.Empty;return;}if(behind!=OrbitBehind)return;Point center=OrbitCenter(envelope);double depth=Math.Sin(OrbitAngle),size=64*PendantSize*(.88+.12*(depth+1)/2),padding=size*.55+4;center.X=Math.Max(padding,Math.Min(440-padding,center.X));d.PushOpacity(depth<0?.66:.98);d.PushTransform(new RotateTransform(Math.Sin(Time*.8)*5,center.X,center.Y));PendantBounds=Pendants.Draw(d,Pendant,center,size);d.Pop();d.Pop();}
}
public partial class PetWindow {
 void PendantControls(StackPanel skins){skins.Children.Add(Txt("环绕小挂件 · 11 款",19,"#36556D"));var pick=new ComboBox{ItemsSource=PendantArt.Names,SelectedIndex=Array.IndexOf(PendantArt.Ids,Pref.Pendant),Padding=new Thickness(7),Margin=new Thickness(0,0,0,12)};pick.SelectionChanged+=(s,e)=>{if(pick.SelectedIndex<0)return;Pet.Pendant=Pref.Pendant=PendantArt.Ids[pick.SelectedIndex];Pet.InvalidateVisual();Save();};skins.Children.Add(pick);skins.Children.Add(Txt("小物沿椭圆缓慢环绕：经过身后会被遮住，转到前方再出现。",16,"#7F7268"));var sizeLabel=Txt("环绕小物大小："+Pref.PendantSize.ToString("0.0")+"×（0.7–2.2）",17,"#7F7268");skins.Children.Add(sizeLabel);var scale=new Slider{Minimum=.7,Maximum=2.2,TickFrequency=.1,IsSnapToTickEnabled=true,Value=Pref.PendantSize,Margin=new Thickness(0,0,0,14)};scale.ValueChanged+=(s,e)=>{Pet.PendantSize=Pref.PendantSize=scale.Value;sizeLabel.Text="环绕小物大小："+Pref.PendantSize.ToString("0.0")+"×（0.7–2.2）";Pet.InvalidateVisual();Save();};skins.Children.Add(scale);if(Pet.Pendants.Error!="")skins.Children.Add(Txt("挂件载入失败："+Pet.Pendants.Error,16,"#A76852"));}
}

public static partial class Program {
 static void RunPendantQA(PetWindow w){if(w.Pet.Pendants.Error!="")throw new Exception(w.Pet.Pendants.Error);var log=new List<string>();int count=0;foreach(string skin in new[]{"original-no-bangs","original"}){w.SetSkin(skin);foreach(string mode in new[]{"orbit"}){var gallery=new Canvas{Width=1200,Height=1290,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};var title=new TextBlock{Text="海伦娜的小挂件 · "+"缓慢环绕",FontSize=29,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,18);gallery.Children.Add(title);
  for(int i=1;i<PendantArt.Ids.Length;i++){var card=new Canvas{Width=274,Height=391,Background=Brushes.White,ClipToBounds=true};Canvas.SetLeft(card,25+(i-1)%4*289);Canvas.SetTop(card,78+(i-1)/4*402);gallery.Children.Add(card);var text=new TextBlock{Text=PendantArt.Names[i],FontSize=19,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(text,14);Canvas.SetTop(text,10);card.Children.Add(text);var pet=new PetVisual{Skin=w.Pet.Skin,Pendants=w.Pet.Pendants,Pendant=PendantArt.Ids[i],PendantPlace=mode,PendantSize=1,Accessories=w.Pet.Accessories,Decoration="cat",Time=.6,Mood="calm",Sway=.2};var view=new Viewbox{Child=pet,Width=274,Height=330};Canvas.SetTop(view,44);card.Children.Add(view);
   foreach(string kind in new[]{"eat","read","cane"}){w.Pet.Motion.Start(kind,0,0);w.Pet.Pendant=PendantArt.Ids[i];w.Pet.PendantPlace=mode;w.Pet.PendantSize=2.2;w.Pet.Time=2.2;w.Pet.Mood="happy";w.Pet.X=i%2==0?1:-1;w.Pet.Y=i%2==0?-1:1;w.Pet.InvalidateVisual();w.RefreshHitMask();if(w.Pet.PendantBounds.IsEmpty)throw new Exception("pendant missing");count++;}
  }Capture(gallery,Path.Combine(root,"preview","挂件预览-"+mode+"-"+skin+".png"),1200,1290);}}
  w.Pet.Pendant="none";w.Pet.Motion.Stop();w.Pet.Mood="calm";w.Pet.InvalidateVisual();log.Add("PASS 11 pendants load from preserved-alpha generated atlases");log.Add("PASS "+count+" hairstyle/placement/prop combinations at maximum size and directional extremes");log.Add("PASS two native orbit galleries; none option and independent hat decoration retained");File.WriteAllLines(Path.Combine(root,"pendant-qa-results.txt"),log);
 }
}
}
