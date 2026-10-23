using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace HelenaPet {
public static class EchoHop {
 public const double Interval=.8,Duration=3.3;
 public static int ChargeCount(double seconds){return seconds>=1.6?3:seconds>=.9?2:1;}
 public sealed class WavePalette {
  public readonly string Name;public readonly SolidColorBrush Main,Accent,Core;
  public WavePalette(string name,string main,string accent,string core){Name=name;Main=Paint(main);Accent=Paint(accent);Core=Paint(core);}
  static SolidColorBrush Paint(string hex){var brush=(SolidColorBrush)new BrushConverter().ConvertFromString(hex);brush.Freeze();return brush;}
 }
 static readonly WavePalette DefaultPalette=new WavePalette("雾蓝与白","#3994BE","#EFFAFF","#CFF6FF");
 static readonly Dictionary<string,WavePalette> Palettes=new Dictionary<string,WavePalette>{
  {"gilda",new WavePalette("森林深绿与浅绿","#315C47","#A6D4AD","#EDF9E7")},
  {"architect",new WavePalette("冰青与霜白","#4A9EA9","#DFF9FA","#F6FFFF")},
  {"clerk",new WavePalette("酒红与象牙白","#884752","#F2DFC0","#FFF7E7")},
  {"dark-cake",new WavePalette("靛紫与淡紫","#6558A5","#C6BAEC","#F6F0FF")},
  {"gold-cake",new WavePalette("蜂蜜琥珀与奶金","#B67D2D","#F3D68D","#FFF7DF")},
  {"haixi",new WavePalette("绛红与浅金","#A44654","#E7B99A","#FFF3DE")},
  {"maria",new WavePalette("宝石蓝与香槟金","#527EC4","#E6CE8C","#F1F6FF")},
  {"redeem-star",new WavePalette("星河青与珍珠白","#66ADBE","#E8D4AE","#F5FBFF")},
  {"sea-witch",new WavePalette("深海青与水绿","#287D87","#9ADDD4","#E5FFFF")},
  {"seaside",new WavePalette("海玻璃绿与浅碧","#62A99D","#D8F0E8","#FAFFF5")},
  {"sleeping-girl",new WavePalette("蔷薇粉与淡紫","#C5789C","#D8CCF2","#FFF3F9")},
  {"sound-wave",new WavePalette("暖铜棕与奶杏","#BA7545","#D7B184","#FFF2DC")},
  {"star-girl",new WavePalette("星夜蓝与月白","#416AAC","#C5D9FA","#F1F7FF")},
  {"sweet-cake",new WavePalette("糖霜粉与薄荷绿","#C86E9C","#B7DDA6","#FFF3F8")},
  {"mango",new WavePalette("芒果黄与嫩叶绿","#DDA13D","#B8D98A","#FFF8DE")}
 };
 public static WavePalette PaletteForSkin(string skin){WavePalette palette;return skin!=null && Palettes.TryGetValue(skin,out palette)?palette:DefaultPalette;}
 static readonly Brush Shadow=new RadialGradientBrush(Color.FromArgb(34,111,153,174),Colors.Transparent);
 public static double Height(double age,int count=3,double amplitude=42){
  if(age<0 || age>=count*Interval)return 0;int hop=(int)(age/Interval);double phase=(age-hop*Interval)/Interval;
  return Math.Sin(phase*Math.PI)*amplitude*Math.Pow(.82,hop);
 }
 public static void DrawWave(DrawingContext d,Point center,double radius,double depth,double life,double strength,int ring,string skin=null){
  var palette=PaletteForSkin(skin);
  if(life<=0 || life>=1)return;strength=Math.Max(.7,Math.Min(2,strength));double opacity=ActionSequence.Smooth(life/.08)*(1-ActionSequence.Smooth((life-.52)/.48));
  double stroke=(ring==0?5:3.6)*strength,halo=stroke+4;depth=Math.Min(depth,508-center.Y-halo/2);
  d.PushOpacity(opacity*.18);d.DrawEllipse(null,new Pen(palette.Main,halo),center,radius,Math.Max(2,depth));d.Pop();
  d.PushOpacity(opacity*.95);d.DrawEllipse(null,new Pen(ring==1?palette.Accent:palette.Main,stroke),center,radius,Math.Max(2,depth));d.Pop();
  if(ring==0){d.PushOpacity(opacity*.85);d.DrawEllipse(null,new Pen(palette.Core,1.2*strength),center,radius,Math.Max(2,depth));d.Pop();}
 }
 public static void DrawGround(DrawingContext d,double age,double ground,int count=3,double strength=1.3,bool shadow=true,string skin=null){
  if(age<0 || age>count*Interval+.9)return;
  double fade=ActionSequence.Smooth(Math.Min(age/.12,(count*Interval+.9-age)/.3));
  if(shadow){d.PushOpacity(fade);d.DrawEllipse(Shadow,null,new Point(220,ground),64,10);d.Pop();}
  for(int hop=1;hop<=count;hop++)for(int ring=0;ring<3;ring++){
   double life=(age-hop*Interval-ring*.1)/.7;if(life<=0 || life>=1)continue;
   DrawWave(d,new Point(220,ground),25+145*ActionSequence.Smooth(life),3+10*life,life,strength,ring,skin);
  }
 }
}
public partial class PetVisual {
 public double HopStarted,HopUntil;public int HopCount=3;public double HopAmplitude=42;
 public bool IsHopping{get{return Time>=HopStarted && Time<HopUntil;}}
 public double HopHeight{get{return IsHopping?EchoHop.Height(Time-HopStarted,HopCount,HopAmplitude):0;}}
 bool DrawEchoHop(DrawingContext d){
  if(!IsHopping)return false;double height=HopHeight;
  d.PushTransform(new TranslateTransform(0,-height));DrawNormal(d);d.Pop();
  DrawHopParticles(d,Time-HopStarted,482,HopCount);EchoHop.DrawGround(d,Time-HopStarted,491,HopCount,EchoStrength,true,Skin==null?null:Skin.id);
  if(!PendantBounds.IsEmpty){var bounds=PendantBounds;bounds.Offset(0,-height);PendantBounds=bounds;}return true;
 }
}
public partial class PetWindow {
 public void StartEchoHop(){
  NoteEngagement();Pet.Motion.Stop();SetMood("calm");if(Bubble!=null)Bubble.Hide();
  Pet.HopCount=3;Pet.HopAmplitude=42;Pet.HopStarted=Clock.Elapsed.TotalSeconds;Pet.HopUntil=Pet.HopStarted+EchoHop.Duration;Pet.Time=Pet.HopStarted;Pet.InvalidateVisual();RefreshStatusCaption();
 }
}
public static partial class Program {
 static void RunEchoHopQA(PetWindow w){
  var log=new List<string>();string skin=w.CurrentSkin;var pref=w.Pref;double oldTime=w.Pet.Time;
  try{
   w.Pet.Motion.Stop();w.Pet.MangoUntil=0;w.Pet.X=w.Pet.Y=w.Pet.EyeX=w.Pet.EyeY=0;w.SetMood("calm");int poses=0;
   foreach(string folder in Directory.GetDirectories(Path.Combine(root,"skins"))){
    w.SetSkin(Path.GetFileName(folder));w.Pet.HopUntil=0;w.Pet.Time=2;var before=RenderPixels(w.Pet);
    w.Pet.HopStarted=2;w.Pet.HopUntil=2+EchoHop.Duration;if(!before.SequenceEqual(RenderPixels(w.Pet)))throw new Exception("Hop changed initial pose");
    foreach(double age in new[]{.2,.4,.8,1.05,1.2,1.6,1.85,2.0,2.4,2.7,3.29}){w.Pet.Time=2+age;var pixels=RenderPixels(w.Pet);int visible=0;for(int y=0;y<510;y++)for(int x=0;x<440;x++){byte a=pixels[(y*440+x)*4+3];if(a>=20)visible++;if((x==0 || x==439 || y==0 || y==509) && a>=20)throw new Exception("Hop clipped: "+w.CurrentSkin);}if(visible<5000)throw new Exception("Hop became invisible");poses++;}
    w.Pet.Time=2.4;RenderPixels(w.Pet);foreach(string part in new[]{"glasses","cheek","hair"})if(w.Pet.HitPart(w.Pet.PartPoint(part))!=part)throw new Exception("Hop hotspot projection: "+part);
    w.Pet.HopUntil=0;w.Pet.Time=2;if(!before.SequenceEqual(RenderPixels(w.Pet)))throw new Exception("Hop did not restore exact skin");
   }log.Add("PASS "+poses+" native hop/landing/ring phases across 16 skins: visible, unclipped, projected hotspots retained, exact original rendering restored");
   w.StartEchoHop();if(!w.Pet.IsHopping || w.Pet.Motion.Active || w.Pet.IsMango)throw new Exception("Hop action routing");w.NoteEngagement();if(w.Pet.IsHopping)throw new Exception("Hop input cancellation");
   w.SetSkin("original-no-bangs");w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="actions").IsExpanded=true;CapturePanelSheet(w,Path.Combine(root,"preview","听见你-动作与回声小跳.png"));w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="chat").IsExpanded=true;
   string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","echo-hop-preview-frames"));Directory.CreateDirectory(frames);var demo=new PetVisual{Skin=w.Pet.Skin,Time=0,Sway=.3,Mood="calm",HopStarted=.5,HopUntil=.5+EchoHop.Duration};
   for(int i=0;i<108;i++){demo.Time=i/24.0;demo.InvalidateVisual();Capture(demo,Path.Combine(frames,i.ToString("D4")+".png"),440,510);}
   if(File.ReadAllBytes(Path.Combine(frames,"0000.png")).SequenceEqual(File.ReadAllBytes(Path.Combine(frames,"0020.png"))))throw new Exception("Hop preview frames did not animate");
   log.Add("PASS standalone action starts three diminishing hops with landing echo rings; interaction cancels immediately; 108 native preview frames at 24fps");
  }finally{w.Pref=pref;w.Pet.HopUntil=0;w.Pet.Time=oldTime;w.SetSkin(skin);w.SetMood("calm");}
  File.WriteAllLines(Path.Combine(root,"echo-hop-qa-results.txt"),log);
 }
}
}
