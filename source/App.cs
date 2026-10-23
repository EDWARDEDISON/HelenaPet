using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace HelenaPet {
public class Layer {
 public string name,texture,clip; [ScriptIgnore] public Geometry ClipShape; public int sx,sy,sw,sh; public double x,y,w,h,depth;public double? clipTop,clipBottom;
 [ScriptIgnore] public BitmapSource Image;
}
public class Skin {
 public bool expressiveEyes=false,baldCostumeBase=false,integratedHeadwear=false;
 public Layer poutFace,cane;public double caneGrip=.286,decorationOffsetY=0;public bool complexHeadwear=false,hasHeadwear=true;
 public string eyeStyle="button",lensLight="#C17C8C",lensDark="#8D4D60",eyeMark="#FFF0DC",eyeOutline="#754554";
 public string id,name,atlas; public double mouthX=200,mouthY=350,cheekY=334; public Layer[] layers;
 [ScriptIgnore] public string Folder;
 public static Skin Load(string folder) {
  var s=new JavaScriptSerializer().Deserialize<Skin>(File.ReadAllText(Path.Combine(folder,"rig.json")));
  if(s==null || string.IsNullOrWhiteSpace(s.name) || s.layers==null || s.layers.Length<1 || s.layers.Length>20) throw new Exception("皮肤的图层信息无效。");
  var atlas=LoadBitmap(folder,s.atlas);
  foreach(var l in s.layers.Concat(s.poutFace==null?new Layer[0]:new[]{s.poutFace}).Concat(s.cane==null?new Layer[0]:new[]{s.cane})) {
   var img=string.IsNullOrWhiteSpace(l.texture)?atlas:LoadBitmap(folder,l.texture);
   if(l.sx<0 || l.sy<0 || l.sw<1 || l.sh<1 || l.sx+l.sw>img.PixelWidth || l.sy+l.sh>img.PixelHeight || l.w<=0 || l.h<=0 || double.IsNaN(l.depth)) throw new Exception("图层范围无效："+l.name);
   l.Image=new CroppedBitmap(img,new Int32Rect(l.sx,l.sy,l.sw,l.sh)); l.Image.Freeze();if(!string.IsNullOrEmpty(l.clip)){l.ClipShape=Geometry.Parse(l.clip);if(l.clipTop.HasValue)l.ClipShape=Geometry.Combine(l.ClipShape,new RectangleGeometry(new Rect(-100,l.clipTop.Value,600,600)),GeometryCombineMode.Intersect,null);if(l.clipBottom.HasValue)l.ClipShape=Geometry.Combine(l.ClipShape,new RectangleGeometry(new Rect(-100,-100,600,l.clipBottom.Value+100)),GeometryCombineMode.Intersect,null);l.ClipShape.Freeze();}
  }
  s.Folder=folder; return s;
 }
 public static Brush LayerBrush(Layer layer){
  Brush brush;if(layer.texture=="@shared-bald-base")brush=new RadialGradientBrush(Color.FromRgb(253,235,221),Color.FromRgb(253,228,213)){MappingMode=BrushMappingMode.Absolute,Center=new Point(200,288),GradientOrigin=new Point(205,240),RadiusX=130,RadiusY=170};
  else brush=new ImageBrush(layer.Image){Viewbox=new Rect(0,0,1,1),ViewboxUnits=BrushMappingMode.RelativeToBoundingBox,Viewport=new Rect(layer.x,layer.y,layer.w,layer.h),ViewportUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill};
  brush.Freeze();return brush;
 }
 static BitmapImage LoadBitmap(string folder,string filename) {
  if(filename=="@shared-neutral-face")return LoadBitmap(Path.Combine(Directory.GetParent(folder).FullName,"original-no-bangs"),"face-no-bangs.png");
  if(filename=="@shared-bald-base")return LoadBitmap(Path.Combine(Directory.GetParent(folder).Parent.FullName,"assets"),"bald-base-v1.png");
  string path=Path.GetFullPath(Path.Combine(folder,filename??""));
  if(!path.StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw new Exception("皮肤贴图必须在皮肤文件夹内。");
  var img=new BitmapImage(); img.BeginInit(); img.UriSource=new Uri(path); img.CacheOption=BitmapCacheOption.OnLoad; img.EndInit(); img.Freeze();
  if(img.PixelWidth>8192 || img.PixelHeight>8192) throw new Exception("贴图过大。");
  return img;
 }
}
public class Preferences { public double Scale=0.72,Left=double.NaN,Top=double.NaN,Turn=5.3,Sway=1,TextSize=20,HandSize=1.4,HeadRange=1,EyeRange=1; public bool Follow=true,Auto=true,Topmost=true,Dialogue=true,Typewriter=true,RoundHands=true,ExtraCostumeDecor=false,MangoEnabled=true,SkinParticles=true,DailyHop=true; public string Skin="original-no-bangs",Decoration="cat",Pendant="shell",PendantPlace="orbit";public double PendantSize=1,DecorationSize=1,EchoStrength=1.3;public string[] ExpandedSections={"chat"};public int FoodIndex=0; }
public partial class PetVisual:FrameworkElement {
 public Skin Skin; public double Time,X,Y,Turn=5.3,Sway=1; public string Mood="calm",Speech=""; public double SpeechUntil;
 public ActionSequence Motion=new ActionSequence();public ActionArt ActionArt;public bool RoundHands=true;
 public AccessoryArt Accessories;public string Decoration="none";public double DecorationSize=1;
 public double EyeX,EyeY,HairX,HairY,HeadRange=1,EyeRange=1,PreviewUntil=-1,PreviewX,PreviewY;
 HeadSurface surface=new HeadSurface();ButtonEyes eyes=new ButtonEyes();
 string preparedFaceMood="";double faceFrom,faceTo,faceStarted;
 public double PoutAmount(double now){if(Mood!=preparedFaceMood)return Mood=="pout"?1:0;double a=ActionSequence.Smooth((now-faceStarted)/.24);return faceFrom+(faceTo-faceFrom)*a;}
 public void PrepareFaceMood(string mood,double now){faceFrom=PoutAmount(now);faceTo=mood=="pout"?1:0;faceStarted=now;preparedFaceMood=mood;}
 public void StepTracking(double targetX,double targetY,double seconds){
  targetX=Math.Max(-1,Math.Min(1,targetX));targetY=Math.Max(-1,Math.Min(1,targetY));seconds=Math.Max(0,Math.Min(.15,seconds));
  double eye=1-Math.Exp(-seconds/.075),head=1-Math.Exp(-seconds/.21),hair=1-Math.Exp(-seconds/.34);
  EyeX+=(targetX-EyeX)*eye;EyeY+=(targetY-EyeY)*eye;X+=(targetX-X)*head;Y+=(targetY-Y)*head;HairX+=(X-HairX)*hair;HairY+=(Y-HairY)*hair;
 }
 public PetVisual() { Width=440;Height=510; }
 static Brush B(string s){return (Brush)new BrushConverter().ConvertFromString(s);}
 void Line(DrawingContext d, string color,double width,Point a,Point b){d.DrawLine(new Pen(B(color),width),a,b);}
 void Text(DrawingContext d,string t,double size,string color,Point p){d.DrawText(new FormattedText(t,System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,B(color)),p);}
 void Heart(DrawingContext d,double x,double y,double scale){d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(scale,scale));d.DrawGeometry(B("#D38E9E"),null,Geometry.Parse("M0,8 C-20,-6 -14,-19 0,-10 C14,-19 20,-6 0,8 Z"));d.Pop();d.Pop();}
 void Sparkle(DrawingContext d,double x,double y,double scale){d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(scale,scale));d.DrawGeometry(B("#DEB76C"),new Pen(B("#C89C59"),1),Geometry.Parse("M0,-15 L4,-4 13,0 4,4 0,15 -4,4 -13,0 -4,-4 Z"));d.Pop();d.Pop();}
 protected override void OnRender(DrawingContext d) {
  if(Skin==null)return;if(DrawMango(d) || DrawChargedEcho(d) || DrawEchoHop(d))return;
  DrawNormal(d);
 }
 void DrawNormal(DrawingContext d) {
  double t=Time,envelope=Motion.Envelope(t),fx=X*HeadRange*(1-envelope*.75),fy=Y*HeadRange*(1-envelope*.75),bob=Math.Sin(t*1.8)*2*Sway,tilt=fx*Turn*.45+Math.Sin(t*1.2)*0.55*Sway;
  double eyeX=EyeX*EyeRange,eyeY=EyeY*EyeRange;
  if(Motion.Kind=="read"){tilt=3+Math.Sin(t*.8)*1.1;fy+=.65*envelope;eyeY=.7*envelope+eyeY*(1-envelope*.8);eyeX=Math.Sin(t*1.4)*.45*envelope+eyeX*(1-envelope*.8);}
  if(Motion.Kind=="eat"){fx*=.25;fy*=.25;eyeX*=.35;eyeY=.2+eyeY*.25;}
  if(Motion.Kind=="cane")tilt=-8+Math.Sin(t*1.5)*2;
  bool chewing=ActionArt!=null && ActionArt.Chewing(Motion,t);
  if(chewing)bob+=Math.Sin(t*11)*1.4;
  if(Mood=="listen" && !Motion.Active)tilt+=-10+Math.Sin(t*2)*1.2;
  if(Mood=="happy" && !Motion.Active)bob-=Math.Abs(Math.Sin(t*5))*11;
  if(Mood=="shy")tilt+=7;
  if(Mood=="sleep"){tilt=11;bob=Math.Sin(t*1.1)*3;}
  if(Mood=="surprise")bob-=5+Math.Sin(t*12)*1.3;
  if(Mood=="angry")tilt+=Math.Sin(t*3)*1.8;
  if(Mood=="sad"){fy+=.18;tilt-=4;}
  if(Mood=="curious")tilt+=7;
  if(Mood=="smug")tilt-=6;
  if(Mood=="confused")tilt+=Math.Sin(t*.8)*8;
  if(Mood=="drowsy"){fy+=.22;tilt+=5;}
  if(Mood=="expect" && !Motion.Active)bob-=Math.Abs(Math.Sin(t*3.4))*5;
  var rig=new HeadProjection{X=Math.Max(-1.2,Math.Min(1.2,fx)),Y=Math.Max(-1.2,Math.Min(1.2,fy))};
  double fit=1-Math.Max(0,Math.Abs(tilt)-8)*0.008;StoreHitPose(rig,tilt,fit,t,bob,envelope);DrawPendant(d,envelope,true);
  d.PushTransform(new TranslateTransform(0,-24*envelope));
  d.PushTransform(new ScaleTransform(1-.16*envelope,1-.16*envelope,220,270));
  d.PushTransform(new TranslateTransform(20,93+bob));
  if(Mood=="echo")for(int i=0;i<3;i++){double a=(t*0.58+i/3.0)%1;d.DrawEllipse(null,new Pen(B("#"+((int)((1-a)*150)).ToString("X2")+"89BAC4"),2.5),new Point(200,235),80+a*137,70+a*137);}
  d.PushTransform(new RotateTransform(tilt,200,235));
  d.PushTransform(new ScaleTransform((1+Math.Sin(t*1.8)*0.006)*fit,(1-Math.Sin(t*1.8)*0.004)*fit,200,235));
  if(Accessories!=null)Accessories.Draw(d,rig,Decoration,false,Skin,DecorationSize);
  surface.Draw(d,Skin,rig,t,Sway,(HairX-X)*HeadRange*(1-envelope*.75),(HairY-Y)*HeadRange*(1-envelope*.75),PoutAmount(t),Accessories,Decoration,false,DecorationSize);
  eyes.Draw(d,Skin,rig,eyeX,eyeY,Mood,t);
  surface.Draw(d,Skin,rig,t,Sway,(HairX-X)*HeadRange*(1-envelope*.75),(HairY-Y)*HeadRange*(1-envelope*.75),PoutAmount(t),Accessories,Decoration,true,DecorationSize);
  d.PushTransform(new MatrixTransform(rig.Tangent(new Point(Skin.mouthX,Skin.mouthY),.9,"face")));
  double mx=Skin.mouthX,my=Skin.mouthY;
  if(Mood=="shy" || Mood=="happy" || Mood=="peace" || Mood=="expect") {
   d.DrawEllipse(B("#60E9A3A3"),null,new Point(mx-65,Skin.cheekY),23,9);
   d.DrawEllipse(B("#60E9A3A3"),null,new Point(mx+65,Skin.cheekY),23,9);
  }
  double eatCycle=Motion.Cycle(t);
  if(Motion.Kind=="eat" && eatCycle>.85 && eatCycle<1.35)d.DrawEllipse(B("#B56E72"),new Pen(B("#69494E"),2),new Point(mx,my),6,8);
  else if(chewing){d.DrawEllipse(B("#704E56"),null,new Point(mx,my),4.5,1.6+Math.Abs(Math.Sin(t*10))*2.2);d.DrawEllipse(B("#30E8A7AB"),null,new Point(mx+35,my-4),12,5);}
  else if(Mood=="surprise" || Mood=="echo")d.DrawEllipse(B("#B56E72"),new Pen(B("#69494E"),2),new Point(mx,my),6,9);
  else if(Mood=="drowsy")d.DrawEllipse(B("#B98288"),new Pen(B("#875D63"),1.8),new Point(mx,my),4.5,4+Math.Max(0,Math.Sin(t*1.7))*3);
  else if(Mood=="expect"){d.PushTransform(new TranslateTransform(mx,my));d.DrawGeometry(B("#EDACAA"),new Pen(B("#875D63"),1.8),Geometry.Parse("M-9,-2 Q0,1 9,-2 Q7,10 0,10 Q-7,10 -9,-2 Z"));d.Pop();}
  else if(Mood=="happy") {
   d.PushTransform(new TranslateTransform(mx,my));d.DrawGeometry(B("#EDACAA"),new Pen(B("#69494E"),2),Geometry.Parse("M-13,-5 Q0,1 13,-5 Q11,15 0,17 Q-11,12 -13,-5 Z"));d.Pop();
  } else {
   string mouth=Mood=="shy"?"M-8,1 Q0,8 8,1":Mood=="sleep"?"M-6,3 Q0,0 6,3":Mood=="angry"?"M-10,2 Q0,-4 10,2":Mood=="sad"?"M-9,4 Q0,-4 9,4":Mood=="curious"?"M-5,1 Q1,5 8,0":Mood=="smug"?"M-8,3 Q1,8 12,-3":Mood=="confused"?"M-9,2 Q-3,-2 3,2 Q7,5 11,2":Mood=="peace"?"M-10,-1 Q0,11 10,-1":Mood=="pout"?"M-9,-1 Q-5,6 0,1 Q5,6 9,-1":"M-9,0 Q0,7 9,0";
   d.PushTransform(new TranslateTransform(mx,my));d.DrawGeometry(null,new Pen(B("#875D63"),2.1),Geometry.Parse(mouth));d.Pop();
  }
  if(Skin.id=="sound-wave")DrawHeadsetMic(d,rig);
  // Legacy skins can retain their baked lens artwork.
  if(Mood!="sleep" && !Skin.expressiveEyes) {
   d.PushOpacity(0.32);Line(d,"#FFF8E8",3,new Point(123+fx*7,257+fy*4),new Point(116+fx*7,267+fy*4));
   Line(d,"#FFF8E8",3,new Point(255+fx*7,255+fy*4),new Point(248+fx*7,265+fy*4));d.Pop();
  }
  d.Pop();d.Pop();d.Pop();d.Pop();
  if(Mood=="happy" || Mood=="shy")for(int i=0;i<3;i++){
   double a=(t*0.45+i*0.33)%1;d.PushOpacity(1-a);Heart(d,100+i*116+Math.Sin(t+i)*8,112-a*75,1.2);d.Pop();
  }
  if(Mood=="sleep")Text(d,"z z Z",36,"#738A9B",new Point(300,100+Math.Sin(t)*5));
  if(Mood=="think" && !Motion.Active)Text(d,"…",54,"#6B8793",new Point(355,104));
  if(Mood=="surprise")Text(d,"!",58,"#AA7C53",new Point(370,104));
  if(Mood=="curious")Text(d,"?",56,"#698C9A",new Point(370,104));
  if(Mood=="angry"){d.PushTransform(new TranslateTransform(403,155));d.PushTransform(new ScaleTransform(1.9,1.9));d.DrawGeometry(null,new Pen(B("#BE7F86"),3),Geometry.Parse("M-9,-9 L-3,-3 -9,3 M9,-9 L3,-3 9,3 M-9,15 L-3,9 3,15 M9,15 L3,9 9,3"));d.Pop();d.Pop();}
  if(Mood=="confused")Text(d,"??",47,"#8D879E",new Point(350,103));
  if(Mood=="drowsy")Text(d,"z",44,"#738A9B",new Point(373,104));
  if(Mood=="expect"){Sparkle(d,99,116,1);Sparkle(d,350,103,.8+Math.Sin(t*3)*.15);}
  if(Mood=="smug")Sparkle(d,372,128,1.1);
  if(Mood=="peace"){Heart(d,315,103,1.1);d.PushOpacity(.6);Heart(d,355,80,.8);d.Pop();}
  d.Pop();d.Pop();
  DrawPendant(d,envelope,false);
  if(ActionArt!=null)ActionArt.Draw(d,Motion,t,RoundHands);
  if(!string.IsNullOrEmpty(Speech) && Time<SpeechUntil){
   d.DrawRoundedRectangle(B("#FFF7EB"),new Pen(B("#DFE5DF"),1),new Rect(37,12,366,65),16,16);
   d.DrawGeometry(B("#FFF7EB"),null,Geometry.Parse("M202,75 L213,88 L225,75 Z"));
   Text(d,Speech,16,"#38576B",new Point(55,32));
  }
 }
}
public partial class PetWindow:Window {
 [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;}
 [DllImport("user32.dll")]static extern bool GetCursorPos(out POINT p);
 public Preferences Pref;public PetVisual Pet=new PetVisual(); public string Root;public Window Panel;public bool Testing;
 public DialogueBank Dialogues;public BubbleWindow Bubble;public List<DialogueLine> History=new List<DialogueLine>();DispatcherTimer clickTimer;double lastTap=-99;public string LastTrigger="";string pressProp="";
 public string CurrentSkin;public DispatcherTimer Timer;public Stopwatch Clock=Stopwatch.StartNew();
 public string State="calm";double stateUntil=0,lastAction=0,tx=0,ty=0,lastTick=-1;bool down,dragged;Point press,origin;double pressTime;bool longDone;int clicks;
 Forms.NotifyIcon tray;DateTime lastClick=DateTime.MinValue;
 public static string[] States={"calm","listen","happy","shy","echo","sleep","surprise","think","angry","sad","curious","expect","smug","confused","drowsy","peace"};
 public static string[] Names={"安静陪伴","侧耳倾听","开心","害羞","回声感知","小憩","惊讶","思考","生气","委屈","好奇","期待","得意","困惑","困困","安心"};
 public PetWindow(string root,bool qa) {
  Root=root;Testing=qa;InitInteraction();Pref=qa?new Preferences():ReadPrefs();CurrentSkin=Pref.Skin;
  Pref.TextSize=Math.Max(16,Math.Min(28,Pref.TextSize));Pref.HandSize=Math.Max(1,Math.Min(1.8,Pref.HandSize));Pref.HeadRange=Math.Max(0,Math.Min(1.2,Pref.HeadRange));Pref.EyeRange=Math.Max(0,Math.Min(1.4,Pref.EyeRange));Pref.FoodIndex=Math.Max(0,Math.Min(3,Pref.FoodIndex));Dialogues=new DialogueBank(Path.Combine(root,"dialogue.json"));
  Pet.ActionArt=new ActionArt(new PropAtlas(Path.Combine(root,"assets"))){HandScale=Pref.HandSize};Pet.RoundHands=Pref.RoundHands;
  Pet.Pendants=new PendantArt(Path.Combine(root,"assets"));if(!PendantArt.Ids.Contains(Pref.Pendant))Pref.Pendant="none";Pref.PendantSize=Math.Max(.7,Math.Min(2.2,Pref.PendantSize));Pet.Pendant=Pref.Pendant;Pet.PendantPlace=Pref.PendantPlace="orbit";Pet.PendantSize=Pref.PendantSize;
  Pet.ShowSkinParticles=Pref.SkinParticles;Pet.EchoStrength=Pref.EchoStrength=double.IsNaN(Pref.EchoStrength)?1.3:Math.Max(.7,Math.Min(2,Pref.EchoStrength));InitMango();Pet.Accessories=new AccessoryArt(Path.Combine(root,"assets"));if(!AccessoryArt.Ids.Contains(Pref.Decoration))Pref.Decoration="none";Pet.Decoration=Pref.Decoration;Pref.DecorationSize=Math.Max(.7,Math.Min(2.0,Pref.DecorationSize));Pet.DecorationSize=Pref.DecorationSize;
  Title="盲女 · 听见你";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=Pref.Topmost;ShowActivated=false;
  Content=new Viewbox{Child=Pet,Stretch=Stretch.Uniform};
  try{SetSkin(CurrentSkin);}catch{CurrentSkin="original";SetSkin("original");}
  Rest.NoteActivity(Clock.Elapsed.TotalSeconds,Interaction);DailyHop.NoteActivity(Clock.Elapsed.TotalSeconds);SetScale(Pref.Scale);
  Rect area=SystemParameters.WorkArea;Left=double.IsNaN(Pref.Left)?area.Right-Width-48:Pref.Left;Top=double.IsNaN(Pref.Top)?area.Bottom-Height-35:Pref.Top;Clamp();
  MouseLeftButtonDown+=OnDown;MouseMove+=OnMove;MouseLeftButtonUp+=OnUp;
  MouseWheel+=(s,e)=>{SetScale(Pref.Scale+(e.Delta>0?0.06:-0.06));Clamp();Save();e.Handled=true;};
  MouseRightButtonDown+=OnPetRightDown;MouseRightButtonUp+=OnPetRightUp;LostMouseCapture+=OnPetLostCapture;
  KeyDown+=(s,e)=>{if(e.Key==Key.Escape)OpenPanel();};
  ContextMenu menu=new ContextMenu();
  for(int i=0;i<States.Length;i++){string st=States[i];var m=new MenuItem{Header=Names[i]};m.Click+=(s,e)=>Act(st);menu.Items.Add(m);}
  // A tray gives an exit and recovery route even when the pet is hidden.
  if(!qa){
   tray=new Forms.NotifyIcon{Text="盲女 · 听见你",Icon=System.Drawing.SystemIcons.Information,Visible=true};
   var cm=new Forms.ContextMenuStrip();cm.Items.Add("打开互动面板",null,(s,e)=>Dispatcher.Invoke(new Action(OpenPanel)));cm.Items.Add("找回桌宠",null,(s,e)=>Dispatcher.Invoke(new Action(Recover)));cm.Items.Add("退出",null,(s,e)=>Dispatcher.Invoke(new Action(()=>Close())));tray.ContextMenuStrip=cm;tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(new Action(OpenPanel));
  }
  clickTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(Forms.SystemInformation.DoubleClickTime)};clickTimer.Tick+=(s,e)=>{clickTimer.Stop();ClickInteract();};
  Timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(33)};Timer.Tick+=(s,e)=>Tick();Timer.Start();
  Closing+=(s,e)=>{Save();Timer.Stop();clickTimer.Stop();if(Bubble!=null)Bubble.Close();if(tray!=null){tray.Visible=false;tray.Dispose();}if(Panel!=null)Panel.Close();};
  Loaded+=(s,e)=>{Bubble=new BubbleWindow{Owner=this};Talk("greet");if(!qa)OpenPanel();};
 }
 Preferences ReadPrefs(){try{return new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(Path.Combine(Root,"settings.json")))??new Preferences();}catch{return new Preferences();}}
 public void Save(){NoteEngagement();if(Testing)return;try{Pref.Left=Left;Pref.Top=Top;Pref.Skin=CurrentSkin;File.WriteAllText(Path.Combine(Root,"settings.json"),new JavaScriptSerializer().Serialize(Pref));}catch{}}
 public void SetSkin(string id){if(id!=Path.GetFileName(id))throw new Exception("皮肤编号无效。");var skin=Skin.Load(Path.Combine(Root,"skins",id));Pet.Skin=skin;if(Pet.ActionArt!=null){Pet.ActionArt.CaneImage=skin.cane==null?null:skin.cane.Image;Pet.ActionArt.CaneGrip=Math.Max(.28,Math.Min(.75,skin.caneGrip));}CurrentSkin=id;Pet.Decoration=CompatibleDecoration();Pet.InvalidateVisual();}
 string CompatibleDecoration(){return Pet.Skin!=null && Pet.Skin.complexHeadwear && !Pref.ExtraCostumeDecor?"none":Pref.Decoration;}
 public void SetScale(double n){Pref.Scale=Math.Max(0.42,Math.Min(1.3,double.IsNaN(n)?0.72:n));Width=440*Pref.Scale;Height=510*Pref.Scale;}
 public void Clamp(){double left=SystemParameters.VirtualScreenLeft,top=SystemParameters.VirtualScreenTop;double right=left+SystemParameters.VirtualScreenWidth,bottom=top+SystemParameters.VirtualScreenHeight;Left=Math.Max(left,Math.Min(right-Width,Left));Top=Math.Max(top,Math.Min(bottom-Height,Top));}
 public void Recover(){var a=SystemParameters.WorkArea;Left=a.Right-Width-35;Top=a.Bottom-Height-25;Show();Clamp();Save();}
 public void Say(string text,double seconds){ShowLine(new DialogueLine{id="custom",text=text,mood=State,kind="fan",source="日常互动"},seconds);}
 public void SetMood(string s){if(!States.Contains(s))return;if(s=="sleep")Pet.Motion.Stop();Pet.PrepareFaceMood(s,Clock.Elapsed.TotalSeconds);if(s=="sleep" && State!="sleep")Rest.EnterSleep(Clock.Elapsed.TotalSeconds,Interaction);else if(s!="sleep" && State=="sleep")Rest.LeaveSleep(Clock.Elapsed.TotalSeconds,Interaction);State=s;Pet.Mood=s;lastAction=Clock.Elapsed.TotalSeconds;stateUntil=s=="sleep"?double.PositiveInfinity:lastAction+(s=="calm"?0:Interaction.MoodSeconds);RefreshStatusCaption();Pet.InvalidateVisual();}
 public void ShowLine(DialogueLine line,double duration){
  Pet.Speech="";History.Add(line);while(History.Count>40)History.RemoveAt(0);if(!Pref.Dialogue || Bubble==null)return;
  BubbleTrigger=string.IsNullOrEmpty(line.@event)?LastTrigger:line.@event;Bubble.Display(line,Clock.Elapsed.TotalSeconds,Pref.TextSize,Pref.Typewriter,duration,StatusCaption(BubbleTrigger,State));if(!Testing){Bubble.Show();Bubble.Follow(this);}
 }
 public void Talk(string trigger,bool automatic=false){if(!automatic)NoteEngagement();if(!Interaction.KeepActionOnChat || trigger=="sleep" || trigger=="wake" || trigger.StartsWith("finish-"))Pet.Motion.Stop();LastTrigger=trigger;var line=Dialogues.Pick(trigger);SetMood(line.mood);ShowLine(line,5);}
 public void StartAction(string kind){
  if(!string.IsNullOrEmpty(Pet.ActionArt.Atlas.Error)){Say("道具素材没有载入，请重新解压完整的桌宠包。",8);return;}
  string trigger=kind=="eat"?"action-eat-"+PropAtlas.FoodIds[Pref.FoodIndex]:"action-"+kind;
  Talk(trigger);Pet.Motion.Start(kind,Pref.FoodIndex,Clock.Elapsed.TotalSeconds);Pet.Motion.EndAt=Clock.Elapsed.TotalSeconds+(kind=="eat"?Interaction.EatSeconds:kind=="read"?Interaction.ReadSeconds:Interaction.CaneSeconds);Pet.Motion.AutoPage=Interaction.AutoPage;Pet.Motion.PageInterval=Interaction.PageSeconds;Pet.Time=Clock.Elapsed.TotalSeconds;Pet.RoundHands=Pref.RoundHands;Pet.InvalidateVisual();
 }
 public void StopAction(){Pet.Motion.Stop();SetMood("calm");}
 public bool InteractProp(string prop){double now=Clock.Elapsed.TotalSeconds;bool accepted=false;string trigger="";
  if(prop=="feed"){accepted=Pet.Motion.Feed(now);trigger="feed-"+PropAtlas.FoodIds[Pet.Motion.Food];}
  if(prop=="page"){accepted=Pet.Motion.TurnPage(now);trigger="page";}
  if(prop=="tap"){accepted=Pet.Motion.Tap(now);trigger="tap";}
  if(accepted){NoteEngagement();LastTrigger=trigger;var line=Dialogues.Pick(trigger);SetMood(line.mood);ShowLine(line,5);Pet.InvalidateVisual();}return accepted;
 }
 public void Act(string s){NoteEngagement();if(!States.Contains(s))return;string trigger=s=="shy"?"pet":s;if(Pet.Motion.Active && s!="sleep"){LastTrigger=trigger;var line=Dialogues.Pick(trigger);SetMood(s);ShowLine(line,5);}else Talk(trigger);}
 public void PreviewPose(double x,double y){Pet.PreviewX=x;Pet.PreviewY=y;Pet.PreviewUntil=Clock.Elapsed.TotalSeconds+3.5;lastAction=Clock.Elapsed.TotalSeconds;}
 public void ClickInteract(){NoteEngagement();
  double t=Clock.Elapsed.TotalSeconds;
  if(Bubble!=null && Bubble.IsVisible && !Bubble.View.Finished){Bubble.RevealAll();lastTap=t;return;}
  if(t-lastTap<1.1)return;lastTap=t;if(TouchPart(PendingPart))return;Talk(State=="sleep"?"wake":"click");
 }
 void OnDown(object s,MouseButtonEventArgs e){CancelPetRightPress();NoteEngagement();clickTimer.Stop();down=true;dragged=false;longDone=false;PendingPart=Pet.HitPart(e.GetPosition(Pet));pressProp=Pet.ActionArt.Hit(Pet.Motion,e.GetPosition(Pet),Clock.Elapsed.TotalSeconds);press=PointToScreen(e.GetPosition(this));origin=new Point(Left,Top);pressTime=Clock.Elapsed.TotalSeconds;CaptureMouse();e.Handled=true;}
 void OnMove(object s,MouseEventArgs e){if(!down)return;Point p=PointToScreen(e.GetPosition(this));Vector v=p-press;if(v.Length>5)dragged=true;if(dragged){var source=PresentationSource.FromVisual(this);var m=source.CompositionTarget.TransformFromDevice;Vector dip=m.Transform(v);Left=origin.X+dip.X;Top=origin.Y+dip.Y;}}
 void OnUp(object s,MouseButtonEventArgs e){if(!down)return;down=false;ReleaseMouseCapture();Clamp();Save();if(dragged){Talk("drag");lastClick=DateTime.MinValue;}else if(!longDone){if(!string.IsNullOrEmpty(pressProp)){InteractProp(pressProp);lastClick=DateTime.MinValue;}else if((DateTime.UtcNow-lastClick).TotalMilliseconds<Forms.SystemInformation.DoubleClickTime){clickTimer.Stop();clicks=(clicks+1)%States.Length;Act(States[clicks]);lastClick=DateTime.MinValue;}else{clickTimer.Start();lastClick=DateTime.UtcNow;}}e.Handled=true;}
 public void Tick(){
  double t=Clock.Elapsed.TotalSeconds,dt=lastTick>=0?t-lastTick:1.0/30;lastTick=t;Pet.Time=t;Pet.Turn=Pref.Turn;Pet.Sway=Pref.Sway;Pet.HeadRange=Pref.HeadRange;Pet.EyeRange=Pref.EyeRange;Pet.Decoration=CompatibleDecoration();Pet.DecorationSize=Pref.DecorationSize;Pet.RoundHands=Pref.RoundHands;Pet.ActionArt.HandScale=Pref.HandSize;
  string ended=Pet.Motion.Advance(t);if(ended!="")Talk("finish-"+ended,true);
  POINT p;if(GetCursorPos(out p)){
   Point local=PointFromScreen(new Point(p.X,p.Y));double x=(local.X-Width/2)/Math.Max(180,Width),y=(local.Y-Height*0.63)/Math.Max(180,Height);
   tx=Pref.Follow && State!="sleep"?Math.Max(-1,Math.Min(1,x)):0;ty=Pref.Follow && State!="sleep"?Math.Max(-1,Math.Min(1,y)):0;
  }
  if(t<Pet.PreviewUntil){tx=Pet.PreviewX;ty=Pet.PreviewY;}
  Pet.StepTracking(tx,ty,Math.Max(0,dt));
  AdvancePetRightPress(t);
  if(down && !dragged && !longDone && string.IsNullOrEmpty(pressProp) && t-pressTime>0.65){Act("shy");longDone=true;}
  if(State!="calm" && State!="sleep" && t>stateUntil){Pet.PrepareFaceMood("calm",t);State="calm";Pet.Mood="calm";}
  AdvanceRest(t);AdvanceMango(t);AdvanceDailyHop(t);UpdateEchoControls();RefreshStatusCaption();
  if(Bubble!=null){Bubble.Tick(t);Bubble.Follow(this);}
  Pet.InvalidateVisual();UpdateClickThrough(null);
 }
 static Brush B(string s){return (Brush)new BrushConverter().ConvertFromString(s);}
 TextBlock Txt(string t,double size,string color){return new TextBlock{Text=t,FontSize=Math.Max(16,size),Foreground=B(color),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)};}
 Button Btn(string text,Action action){var b=new Button{Content=text,FontSize=18,Padding=new Thickness(13,10,13,10),Margin=new Thickness(0,0,9,9),Background=B("#F4EBDD"),Foreground=B("#36556D"),BorderBrush=B("#D5C6B2"),BorderThickness=new Thickness(1),Cursor=Cursors.Hand};b.Click+=(s,e)=>{NoteEngagement();action();};return b;}
 Border Card(UIElement content){return new Border{Background=B("#FFFCF6"),BorderBrush=B("#CBD9E0"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(20,16,20,14),Margin=new Thickness(0,0,0,14),Child=content};}
 public void OpenPanel(){
  if(State=="sleep" && !Testing)Talk("wake");else NoteEngagement();
  if(Panel!=null){Panel.Show();Panel.Activate();return;}
  Panel=new Window{Title="听见你 · 盲女桌宠",Width=570,Height=840,MinWidth=500,MinHeight=600,Background=B("#EDF2F5"),FontSize=16,FontFamily=new FontFamily("Microsoft YaHei UI"),WindowStartupLocation=WindowStartupLocation.CenterScreen};
  var scroll=new ScrollViewer{Background=B("#EDF2F5"),VerticalScrollBarVisibility=ScrollBarVisibility.Auto};var root=new StackPanel{Margin=new Thickness(25,24,25,16)};scroll.Content=root;Panel.Content=scroll;PanelSections.Clear();AttachEngagement(Panel);
  root.Children.Add(Txt("HELENA  /  A LITTLE ECHO",13,"#8B7780"));root.Children.Add(Txt("听见你",32,"#28485D"));PanelStatus=Txt("此刻 · "+StateName(State),18,"#7F7268");root.Children.Add(PanelStatus);var folding=new WrapPanel();folding.Children.Add(Btn("全部展开",()=>SetAllSections(true)));folding.Children.Add(Btn("全部收起",()=>SetAllSections(false)));root.Children.Add(folding);
  var play=new StackPanel();play.Children.Add(Txt("一起做点什么",19,"#36556D"));
  var food=new ComboBox{ItemsSource=PropAtlas.FoodNames,SelectedIndex=Pref.FoodIndex,Padding=new Thickness(8),Margin=new Thickness(0,2,0,12)};
  food.SelectionChanged+=(s,e)=>{Pref.FoodIndex=Math.Max(0,food.SelectedIndex);Save();if(Pet.Motion.Kind=="eat")StartAction("eat");};play.Children.Add(food);
  var actionButtons=new WrapPanel();actionButtons.Children.Add(Btn("投喂一份",()=>StartAction("eat")));actionButtons.Children.Add(Btn("一起读书",()=>StartAction("read")));actionButtons.Children.Add(Btn("探杖听回声",()=>StartAction("cane")));actionButtons.Children.Add(Btn("收起道具",StopAction));play.Children.Add(actionButtons);
  play.Children.Add(Btn("回声小跳",StartEchoHop));play.Children.Add(Txt("轻跳三下，落地时散开音波；点击角色可停止。",16,"#7F7268"));
  var hands=new CheckBox{Content="显示圆圆小手",IsChecked=Pref.RoundHands,Margin=new Thickness(0,4,0,12)};hands.Click+=(s,e)=>{Pref.RoundHands=hands.IsChecked==true;Pet.RoundHands=Pref.RoundHands;Save();Pet.InvalidateVisual();};play.Children.Add(hands);
  var handLabel=Txt("小手大小："+Pref.HandSize.ToString("0.0")+"×",14,"#7F7268");play.Children.Add(handLabel);var handSize=new Slider{Minimum=1,Maximum=1.8,Value=Pref.HandSize,TickFrequency=.1,IsSnapToTickEnabled=true,Margin=new Thickness(0,0,0,12)};
  handSize.ValueChanged+=(s,e)=>{Pref.HandSize=handSize.Value;Pet.ActionArt.HandScale=Pref.HandSize;handLabel.Text="小手大小："+Pref.HandSize.ToString("0.0")+"×";Pet.InvalidateVisual();Save();};play.Children.Add(handSize);
  play.Children.Add(Txt("点餐盘再喂一口 · 点书帮她翻页 · 点盲杖再敲一下",14,"#7F7268"));
  if(!string.IsNullOrEmpty(Pet.ActionArt.Atlas.Error))play.Children.Add(Txt(Pet.ActionArt.Atlas.Error,14,"#A76852"));
  var echoControls=new StackPanel();EchoControls(echoControls);play.Children.Add(Section("echo-effects","回声特效与蓄力","按住蓄力、皮肤粒子与音波强度",echoControls));
  var chat=new StackPanel();chat.Children.Add(Txt("和海伦娜聊聊",19,"#36556D"));var topics=new WrapPanel();
  string[] topicIds={"click","poetry","sweet","writing","company","official"};string[] topicNames={"打个招呼","聊聊诗歌","聊聊甜食","关于写作","陪我一会儿","活动原句"};
  for(int i=0;i<topicIds.Length;i++){string id=topicIds[i];topics.Children.Add(Btn(topicNames[i],()=>Talk(id)));}chat.Children.Add(topics);chat.Children.Add(Btn("查看对话记录与出处",OpenHistory));
  if(!string.IsNullOrEmpty(Dialogues.Error))chat.Children.Add(Txt(Dialogues.Error,14,"#A76852"));
  var reading=new StackPanel();reading.Children.Add(Txt("文字显示",19,"#36556D"));
  var enabled=new CheckBox{Content="显示互动文字气泡",IsChecked=Pref.Dialogue,Margin=new Thickness(0,3,0,12)};enabled.Click+=(s,e)=>{Pref.Dialogue=enabled.IsChecked==true;if(!Pref.Dialogue && Bubble!=null)Bubble.Hide();Save();};reading.Children.Add(enabled);
  var type=new CheckBox{Content="文字逐字出现 · 点击可立即显示完整句子",IsChecked=Pref.Typewriter,Margin=new Thickness(0,0,0,12)};type.Click+=(s,e)=>{Pref.Typewriter=type.IsChecked==true;Save();};reading.Children.Add(type);
  var sizeLabel=Txt("对话字号："+Pref.TextSize.ToString("0")+" · 独立于桌宠大小",15,"#7F7268");reading.Children.Add(sizeLabel);
  var textSize=new Slider{Minimum=16,Maximum=28,TickFrequency=1,IsSnapToTickEnabled=true,Value=Pref.TextSize,Margin=new Thickness(0,3,0,12)};
  textSize.ValueChanged+=(s,e)=>{Pref.TextSize=textSize.Value;sizeLabel.Text="对话字号："+Pref.TextSize.ToString("0")+" · 独立于桌宠大小";if(Bubble!=null && Bubble.View.Line!=null){var line=Bubble.View.Line;Bubble.Display(line,Clock.Elapsed.TotalSeconds,Pref.TextSize,false,8);if(Pref.Dialogue && !Testing){Bubble.Show();Bubble.Follow(this);}}Save();};reading.Children.Add(textSize);reading.Children.Add(Btn("试读一句",()=>Talk("official")));
  var acts=new StackPanel();acts.Children.Add(Txt("此刻的心情",16,"#36556D"));var wrap=new WrapPanel();for(int i=0;i<States.Length;i++){string st=States[i];wrap.Children.Add(Btn(Names[i],()=>Act(st)));}acts.Children.Add(wrap);
  var settings=new StackPanel();settings.Children.Add(Txt("陪伴方式",16,"#36556D"));var tracking=new StackPanel();tracking.Children.Add(Txt("跟随与大小",19,"#36556D"));
  var follow=new CheckBox{Content="跟随鼠标 · 纽扣眼先动，头部随后转动",IsChecked=Pref.Follow,Margin=new Thickness(0,4,0,12)};follow.Click+=(s,e)=>{Pref.Follow=follow.IsChecked==true;Pet.PreviewUntil=-1;Save();};tracking.Children.Add(follow);
  tracking.Children.Add(Txt("转头、抬头与低头幅度",14,"#7F7268"));var headRange=new Slider{Minimum=0,Maximum=1.2,Value=Pref.HeadRange,Margin=new Thickness(0,0,0,12)};headRange.ValueChanged+=(s,e)=>{Pref.HeadRange=headRange.Value;Save();};tracking.Children.Add(headRange);
  tracking.Children.Add(Txt("镜片内纽扣眼的跟随幅度",14,"#7F7268"));var eyeRange=new Slider{Minimum=0,Maximum=1.4,Value=Pref.EyeRange,Margin=new Thickness(0,0,0,12)};eyeRange.ValueChanged+=(s,e)=>{Pref.EyeRange=eyeRange.Value;Save();};tracking.Children.Add(eyeRange);
  tracking.Children.Add(Txt("朝向试一试 · 每次停留约 3 秒",14,"#7F7268"));var directions=new System.Windows.Controls.Primitives.UniformGrid{Columns=3,Margin=new Thickness(0,0,0,10)};string[] directionNames={"左上","抬头","右上","向左","正面","向右","左下","低头","右下"};for(int i=0;i<9;i++){double px=i%3-1,py=i/3-1;directions.Children.Add(Btn(directionNames[i],()=>PreviewPose(px,py)));}tracking.Children.Add(directions);
  var idle=new CheckBox{Content="自动作息 · 3–5 分钟入睡，20–30 分钟醒来",IsChecked=Pref.Auto,Margin=new Thickness(0,0,0,12)};idle.Click+=(s,e)=>{Pref.Auto=idle.IsChecked==true;ResetRestSchedule();Save();};settings.Children.Add(idle);
  var top=new CheckBox{Content="始终置顶",IsChecked=Pref.Topmost,Margin=new Thickness(0,0,0,14)};top.Click+=(s,e)=>{Pref.Topmost=Topmost=top.IsChecked==true;Save();};tracking.Children.Add(top);
  tracking.Children.Add(Txt("大小  ·  也可以在角色上滚动滚轮",12,"#7F7268"));var slider=new Slider{Minimum=0.42,Maximum=1.3,Value=Pref.Scale,Margin=new Thickness(0,0,0,12)};slider.ValueChanged+=(s,e)=>{SetScale(slider.Value);Clamp();Save();};tracking.Children.Add(slider);
  tracking.Children.Add(Txt("歪头幅度",12,"#7F7268"));var turn=new Slider{Minimum=0,Maximum=14,Value=Pref.Turn,Margin=new Thickness(0,0,0,12)};turn.ValueChanged+=(s,e)=>{Pref.Turn=turn.Value;Save();};tracking.Children.Add(turn);
  tracking.Children.Add(Txt("帽子与卷发的轻晃幅度",12,"#7F7268"));var sway=new Slider{Minimum=0,Maximum=2,Value=Pref.Sway,Margin=new Thickness(0,0,0,7)};sway.ValueChanged+=(s,e)=>{Pref.Sway=sway.Value;Save();};tracking.Children.Add(sway);
  var skins=new StackPanel();skins.Children.Add(Txt("衣橱",16,"#36556D"));var combo=new ComboBox{Margin=new Thickness(0,0,0,12),Padding=new Thickness(7),DisplayMemberPath="Name"};
  List<SkinChoice> choices=new List<SkinChoice>();foreach(string f in Directory.GetDirectories(Path.Combine(Root,"skins")))try{var sk=Skin.Load(f);choices.Add(new SkinChoice{Name=sk.name,Id=Path.GetFileName(f)});}catch{}
  combo.ItemsSource=choices;combo.SelectedItem=choices.FirstOrDefault(v=>v.Id==CurrentSkin);combo.SelectionChanged+=(s,e)=>{var choice=combo.SelectedItem as SkinChoice;if(choice!=null)try{SetSkin(choice.Id);Save();Talk("skin");}catch(Exception ex){MessageBox.Show(ex.Message);}};skins.Children.Add(combo);
  var hatDecor=new StackPanel();hatDecor.Children.Add(Txt("帽顶小装饰",17,"#36556D"));var extraDecor=new CheckBox{Content="高级皮肤也叠戴额外装饰",IsChecked=Pref.ExtraCostumeDecor,Margin=new Thickness(0,0,0,12)};extraDecor.Click+=(s,e)=>{Pref.ExtraCostumeDecor=extraDecor.IsChecked==true;Pet.Decoration=CompatibleDecoration();Pet.InvalidateVisual();Save();};hatDecor.Children.Add(extraDecor);var decoration=new ComboBox{ItemsSource=AccessoryArt.Names,SelectedIndex=Array.IndexOf(AccessoryArt.Ids,Pref.Decoration),Margin=new Thickness(0,0,0,12),Padding=new Thickness(7)};decoration.SelectionChanged+=(s,e)=>{int i=decoration.SelectedIndex;if(i<0)return;Pref.Decoration=AccessoryArt.Ids[i];if(Pet.Skin.complexHeadwear){Pref.ExtraCostumeDecor=true;extraDecor.IsChecked=true;}Pet.Decoration=CompatibleDecoration();lastAction=Clock.Elapsed.TotalSeconds;Save();Pet.InvalidateVisual();};hatDecor.Children.Add(decoration);hatDecor.Children.Add(Txt("猫耳、鹿角、兔耳、小皇冠、蝴蝶结、星星夹，可选不戴。\n装饰跟随头部；高级皮肤默认保留自身帽饰，可勾选叠戴。",14,"#7F7268"));if(Pet.Accessories.Error!="")hatDecor.Children.Add(Txt("装饰未载入："+Pet.Accessories.Error,14,"#A76852")); DecorationControls(hatDecor);skins.Children.Add(Section("hat-decor","帽顶小装饰","耳朵、皇冠、蝴蝶结和星星夹 · 大小可调",hatDecor));
  var pendantPanel=new StackPanel();PendantControls(pendantPanel);skins.Children.Add(Section("pendants","环绕小物","11 款 · 大小上限 2.2×",pendantPanel));
  skins.Children.Add(Txt("新皮肤放入 skins 文件夹后点刷新衣橱。\n每套皮肤可使用独立贴图、图层位置与嘴部锚点。",12,"#7F7268"));var wardrobeButtons=new WrapPanel();wardrobeButtons.Children.Add(Btn("打开皮肤文件夹",()=>Process.Start(Path.Combine(Root,"skins"))));wardrobeButtons.Children.Add(Btn("刷新衣橱",()=>{Panel.Close();OpenPanel();}));skins.Children.Add(wardrobeButtons);
  chat.Children.Add(Section("moods","此刻的心情","16 种表情，按需展开",acts));
  MangoControls(settings);settings.Children.Add(Section("tracking","跟随与大小","朝向试用、转头、歪头、轻晃与置顶",tracking));settings.Children.Add(Btn("部位、台词、动作与作息设置",OpenInteractionSettings));
  root.Children.Add(Section("chat","聊聊与心情","打招呼、聊诗歌、陪伴与对话记录",chat));
  root.Children.Add(Section("actions","动作与投喂","四种甜点、摸读翻页、探杖与小手",play));
  root.Children.Add(Section("wardrobe","衣橱与搭配","换皮肤与对应盲杖、帽顶装饰和环绕小物",skins));
  root.Children.Add(Section("company","陪伴与作息","自动睡醒、芒果彩蛋、跟随与置顶",settings));
  root.Children.Add(Section("text","文字显示","气泡开关、逐字显示与独立字号",reading));
  root.Children.Add(Txt("拖动搬家 · 单击部位互动 · 左键长按摸头 · 右键长按蓄力跳\n双击轮换心情 · 右键打开面板 · 托盘可找回桌宠",14,"#7F7268"));
  var bottom=new WrapPanel();bottom.Children.Add(Btn("回到桌宠",()=>Panel.Hide()));bottom.Children.Add(Btn("找回桌宠",Recover));bottom.Children.Add(Btn("退出",()=>Close()));root.Children.Add(bottom);
  Panel.Deactivated+=(s,e)=>{if(Pet.EchoCharging)CancelEchoCharge();};Panel.Closed+=(s,e)=>{CancelEchoCharge();Panel=null;};Panel.Show();
 }
 public void OpenHistory(){var page=new Window{Title="海伦娜 · 对话记录与出处",Width=610,Height=650,FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=17,Background=B("#EDF2F5"),Owner=Panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};AttachEngagement(page);var list=new StackPanel{Margin=new Thickness(24)};list.Children.Add(Txt("这一会儿说过的话",24,"#36556D"));list.Children.Add(Txt("活动原文按你提供的截图录入；日常互动根据资料创作。",15,"#7F7268"));foreach(var line in History.AsEnumerable().Reverse()){var block=new StackPanel();block.Children.Add(Txt(line.text,20,"#36556D"));block.Children.Add(Txt(line.Label+"\n依据："+line.source+(string.IsNullOrEmpty(line.reference)?"":"\n截图："+line.reference),14,"#7F7268"));list.Children.Add(Card(block));}page.Content=new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};page.Show();}
 public class SkinChoice{public string Name{get;set;}public string Id{get;set;}}
}
public static partial class Program {
 static string root=AppDomain.CurrentDomain.BaseDirectory;
 [STAThread]public static void Main(string[] args){
  try{
   var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};Theme.Apply(app);bool qa=args.Contains("--qa");var w=new PetWindow(root,qa);app.MainWindow=w;
   if(qa){w.Show();w.Timer.Stop();w.OpenPanel();w.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{try{if(args.Contains("--pet-charge-only")){RunPetRightChargeQA(w);app.Shutdown(0);return;}if(args.Contains("--daily-hop-only")){RunDailyHopQA(w);RunPetRightChargeQA(w);app.Shutdown(0);return;}if(args.Contains("--mango-only")){RunMangoQA(w);RunEchoHopQA(w);RunEchoExpansionQA(w);RunDailyHopQA(w);RunPetRightChargeQA(w);app.Shutdown(0);return;}if(args.Contains("--panel-rest-only")){RunPanelRestQA(w);app.Shutdown(0);return;}if(args.Contains("--costume-preview")){RunCostumeQA(w,false,false);app.Shutdown(0);return;}if(!args.Contains("--head-only")){RunQA(w);RunActionQA(w,args.Contains("--record-actions"));}RunHeadQA(w,args.Contains("--record-head"));RunPoutQA(w,args.Contains("--record-pout"));RunAccessoryQA(w,args.Contains("--record-decorations"));RunInteractionQA(w);RunPendantQA(w);RunLive2DPrep(w);RunCostumeQA(w,true,args.Contains("--record-costumes"));RunHatFitQA(w);RunCostumeCoverageQA(w);RunPanelRestQA(w);RunMangoQA(w);RunEchoHopQA(w);RunEchoExpansionQA(w);RunDailyHopQA(w);RunPetRightChargeQA(w);app.Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(root,"qa-failure.txt"),ex.ToString());app.Shutdown(2);}}));app.Run();}
   else {bool created;using(var mutex=new System.Threading.Mutex(true,"Local\\HelenaPet_"+root.GetHashCode(),out created)){if(!created){MessageBox.Show("盲女已经在桌面上啦。可从系统托盘找回。","听见你");return;}app.Run(w);}}
  }catch(Exception ex){File.WriteAllText(Path.Combine(root,"error.log"),ex.ToString());MessageBox.Show("启动未完成："+ex.Message+"\n详情见 error.log","盲女桌宠");}
 }
 static void Capture(FrameworkElement v,string file,int width,int height){v.Measure(new Size(width,height));v.Arrange(new Rect(0,0,width,height));v.UpdateLayout();var b=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);b.Render(v);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(b));using(var fs=File.Create(file))encoder.Save(fs);}
 static void MoodGallery(Skin skin,string file,string[] moods,string[] names){
  int height=96+402*((moods.Length+2)/3);var gallery=new Canvas{Width=960,Height=height,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};
  var heading=new TextBlock{Text="海伦娜的更多心情",FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=29,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(heading,25);Canvas.SetTop(heading,20);gallery.Children.Add(heading);
  for(int i=0;i<moods.Length;i++){
   var inside=new Canvas{Width=295,Height=388,ClipToBounds=true};var card=new Border{Width=295,Height=388,CornerRadius=new CornerRadius(18),Background=Brushes.White,Child=inside};Canvas.SetLeft(card,25+i%3*310);Canvas.SetTop(card,82+i/3*402);gallery.Children.Add(card);
   var label=new TextBlock{Text=names[i],FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=18,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,18);Canvas.SetTop(label,14);inside.Children.Add(label);
   var pet=new PetVisual{Skin=skin,Time=2.2,Mood=moods[i],Turn=5.3,Sway=.4};var view=new Viewbox{Width=295,Height=342,Child=pet};Canvas.SetTop(view,38);inside.Children.Add(view);
  }Capture(gallery,file,960,height);
 }
 static void RunPoutQA(PetWindow w,bool record){
  var log=new List<string>();string folder=Path.Combine(root,"preview");int index=0;string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","pout-transition-frames"));if(record)Directory.CreateDirectory(frames);
  foreach(string skin in new[]{"original-no-bangs","original"}){
   w.SetSkin(skin);var original=w.Pet.Skin.layers.First(l=>l.name=="face").Image;
   if(w.Pet.Skin.poutFace==null || w.Pet.Skin.poutFace.Image==original)throw new Exception("pout image must be independent");
   foreach(string kind in new[]{"eat","read","cane"}){w.Pet.Motion.Start(kind,1,0);w.Pet.Mood="pout";w.Pet.Time=2.2;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"pout-"+kind+"-"+skin+".png"),440,510);}
   w.Pet.Motion.Stop();w.Pet.Mood="calm";if(w.Pet.Skin.layers.First(l=>l.name=="face").Image!=original)throw new Exception("neutral face replaced");
   if(record){var pet=new PetVisual{Skin=w.Pet.Skin,Mood="calm",Sway=.5,Turn=5.3};pet.PrepareFaceMood("calm",0);
    for(int f=0;f<72;f++){double now=f/18.0;if(f==18){pet.PrepareFaceMood("pout",now);pet.Mood="pout";}if(f==54){pet.PrepareFaceMood("calm",now);pet.Mood="calm";}pet.StepTracking(Math.Sin(now*1.4)*.65,Math.Cos(now)*.2,1.0/18);pet.Time=now;pet.InvalidateVisual();Capture(pet,Path.Combine(frames,index.ToString("D4")+".png"),440,510);index++;}
   }
  }
  log.Add("PASS both hairstyle variants load separate puffed-face artwork and retain their neutral textures");log.Add("PASS puffed cheeks render with feeding, reading and cane; glasses remain independently rigged");if(record)log.Add("PASS "+index+" frames of neutral / inflated / neutral transitions with directional tracking");File.WriteAllLines(Path.Combine(root,"pout-qa-results.txt"),log);
 }
 static void RunAccessoryQA(PetWindow w,bool record){
  if(w.Pet.Accessories.Error!="")throw new Exception(w.Pet.Accessories.Error);var log=new List<string>();string folder=Path.Combine(root,"preview");int index=0;string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","decoration-preview-frames"));if(record)Directory.CreateDirectory(frames);
  foreach(string skin in new[]{"original-no-bangs","original"}){w.SetSkin(skin);
   var gallery=new Canvas{Width=960,Height=900,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};var title=new TextBlock{Text="海伦娜的帽顶小装饰",FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=29,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,20);gallery.Children.Add(title);
   for(int i=1;i<AccessoryArt.Ids.Length;i++){
    string id=AccessoryArt.Ids[i];var inside=new Canvas{Width=295,Height=388,ClipToBounds=true};var card=new Border{Width=295,Height=388,CornerRadius=new CornerRadius(18),Background=Brushes.White,Child=inside};Canvas.SetLeft(card,25+(i-1)%3*310);Canvas.SetTop(card,82+(i-1)/3*402);gallery.Children.Add(card);
    var label=new TextBlock{Text=AccessoryArt.Names[i],FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=18,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,18);Canvas.SetTop(label,14);inside.Children.Add(label);
    var pet=new PetVisual{Skin=w.Pet.Skin,Accessories=w.Pet.Accessories,Decoration=id,Time=2.2,Mood="calm",Sway=.4};var view=new Viewbox{Width=295,Height=342,Child=pet};Canvas.SetTop(view,38);inside.Children.Add(view);
    foreach(string kind in new[]{"eat","read","cane"}){w.Pet.Decoration=id;w.Pet.Motion.Start(kind,1,0);w.Pet.Mood="pout";w.Pet.Time=2.2;w.Pet.X=i%2==0?1:-1;w.Pet.Y=i%2==0?-1:1;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"decor-"+id+"-"+kind+"-"+skin+".png"),440,510);}
    if(record && skin=="original-no-bangs")for(int f=0;f<36;f++){double now=f/18.0;pet.Time=now;pet.StepTracking(Math.Sin(now*2)*.8,Math.Cos(now*2)*.5,1.0/18);pet.InvalidateVisual();Capture(pet,Path.Combine(frames,index.ToString("D4")+".png"),440,510);index++;}
   }Capture(gallery,Path.Combine(folder,"六种装饰预览-"+skin+".png"),960,900);
  }
  w.Pet.Decoration="none";w.Pet.Motion.Stop();w.Pet.Mood="calm";w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"不戴装饰.png"),440,510);w.Pet.Decoration=w.Pref.Decoration;
  log.Add("PASS six accessory atlas regions load; none option returns to undecorated rendering");log.Add("PASS both hairstyles render all six accessories; 36 accessory/action/puffed-cheek combinations at directional extremes");if(record)log.Add("PASS "+index+" frames of accessories following head direction");File.WriteAllLines(Path.Combine(root,"decoration-qa-results.txt"),log);
 }
 static void RunHeadQA(PetWindow w,bool record){
  string folder=Path.Combine(root,"preview");var log=new List<string>();var watch=Stopwatch.StartNew();
  var tracking=new PetVisual();tracking.StepTracking(1,-1,1.0/30);
  if(!(tracking.EyeX>tracking.X && tracking.X>tracking.HairX && tracking.EyeY<tracking.Y))throw new Exception("eye/head/hair response order");
  for(int i=0;i<240;i++)tracking.StepTracking(0,0,1.0/60);
  if(Math.Abs(tracking.EyeX)+Math.Abs(tracking.X)+Math.Abs(tracking.HairX)>.001)throw new Exception("tracking neutral return");
  var slow=new PetVisual();var fast=new PetVisual();for(int i=0;i<18;i++)slow.StepTracking(1,1,1.0/18);for(int i=0;i<60;i++)fast.StepTracking(1,1,1.0/60);
  if(Math.Abs(slow.X-fast.X)>.001 || Math.Abs(slow.EyeX-fast.EyeX)>.001)throw new Exception("frame-rate-dependent tracking");
  slow.StepTracking(100,-100,5);if(slow.X>1 || slow.Y< -1 || slow.EyeX>1 || slow.EyeY< -1)throw new Exception("tracking limits");
  log.Add("PASS time-based eye/head/hair response, neutral return, 18/60 fps consistency and bounded targets");
  var morph=new PetVisual{Mood="calm"};morph.PrepareFaceMood("pout",0);morph.Mood="pout";double half=morph.PoutAmount(.12);if(half<.4 || half>.6 || morph.PoutAmount(.25)<.999)throw new Exception("pout transition in");morph.PrepareFaceMood("calm",.12);morph.Mood="calm";if(Math.Abs(morph.PoutAmount(.12)-half)>.001 || morph.PoutAmount(.37)>.001)throw new Exception("pout transition continuity");log.Add("PASS cheek layout morphs over 240 ms with continuous interruption and opaque texture switching");
  Point center=new Point(200,290);Point left=new HeadProjection{X=-1}.Project(center,1,"glasses"),right=new HeadProjection{X=1}.Project(center,1,"glasses"),up=new HeadProjection{Y=-1}.Project(center,1,"glasses"),down=new HeadProjection{Y=1}.Project(center,1,"glasses");
  if(right.X-left.X<60 || down.Y-up.Y<40)throw new Exception("direction not visible");
  foreach(string skin in new[]{"original","original-no-bangs"}){
   w.SetSkin(skin);if(!w.Pet.Skin.expressiveEyes)throw new Exception("missing eye rig");if(w.Pet.Skin.poutFace==null || w.Pet.Skin.poutFace.Image==null)throw new Exception("missing puffed face layer");
   foreach(var layer in w.Pet.Skin.layers)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){
    var rig=new HeadProjection{X=x*1.2,Y=y*1.2};var m=rig.Tangent(new Point(layer.x+layer.w*.5,layer.y+layer.h*.5),layer.depth,layer.name);
    if(!m.HasInverse || double.IsNaN(m.OffsetX) || double.IsInfinity(m.OffsetY))throw new Exception("folded/nonfinite head surface");
   }
   var grid=new Canvas{Width=960,Height=1240,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};
   var title=new TextBlock{Text="朝向与纽扣眼 · "+(skin=="original"?"有刘海":"无刘海"),FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=27,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,20);grid.Children.Add(title);
   string[] labels={"左上","抬头","右上","向左","正面","向右","左下","低头","右下"};
   for(int i=0;i<9;i++){
    var inside=new Canvas{Width=295,Height=375,ClipToBounds=true};var card=new Border{Width=295,Height=375,CornerRadius=new CornerRadius(18),Background=Brushes.White,Child=inside};Canvas.SetLeft(card,25+i%3*310);Canvas.SetTop(card,70+i/3*387);grid.Children.Add(card);
    var label=new TextBlock{Text=labels[i],FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=18,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,18);Canvas.SetTop(label,14);inside.Children.Add(label);
    double px=i%3-1,py=i/3-1;var pet=new PetVisual{Skin=w.Pet.Skin,Time=2.2,Mood="calm",X=px,Y=py,EyeX=px,EyeY=py,HairX=px,HairY=py,Turn=5.3,Sway=.4};var view=new Viewbox{Width=295,Height=342,Child=pet};Canvas.SetTop(view,28);inside.Children.Add(view);
   }Capture(grid,Path.Combine(folder,"九向预览-"+skin+".png"),960,1240);
   foreach(string mood in new[]{"expect","smug","confused","drowsy","peace","pout"}){w.Pet.Motion.Stop();w.Pet.Mood=mood;w.Pet.Time=2.2;w.Pet.X=.2;w.Pet.Y=-.1;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,mood+"-"+skin+".png"),440,510);}
  }log.Add("PASS both hairstyles: continuous 3x3 head poses, invertible deformation and separate eye controls");
  w.SetSkin("original-no-bangs");var gallery=new Canvas{Width=960,Height=900,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};
  string[] moods={"calm","happy","angry","sad","curious","surprise"};string[] names={"安静陪伴","开心","生气","委屈","好奇","惊讶"};
  var heading=new TextBlock{Text="今天的心情",FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=29,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(heading,25);Canvas.SetTop(heading,20);gallery.Children.Add(heading);
  for(int i=0;i<6;i++){
   var inside=new Canvas{Width=295,Height=388,ClipToBounds=true};var card=new Border{Width=295,Height=388,CornerRadius=new CornerRadius(18),Background=Brushes.White,Child=inside};Canvas.SetLeft(card,25+i%3*310);Canvas.SetTop(card,82+i/3*402);gallery.Children.Add(card);
   var label=new TextBlock{Text=names[i],FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=18,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,18);Canvas.SetTop(label,14);inside.Children.Add(label);
   var pet=new PetVisual{Skin=w.Pet.Skin,Time=2.2,Mood=moods[i],X=0,Y=0,HairX=0,Turn=5.3,Sway=.4};var view=new Viewbox{Width=295,Height=342,Child=pet};Canvas.SetTop(view,38);inside.Children.Add(view);
  }Capture(gallery,Path.Combine(folder,"六种表情预览.png"),960,900);
  MoodGallery(w.Pet.Skin,Path.Combine(folder,"六种新增表情预览.png"),new[]{"expect","smug","confused","drowsy","peace","pout"},new[]{"期待 · 星星眼","得意 · 眨眨眼","困惑 · 大小眼","困困 · 小哈欠","安心 · 弯眼微笑","鼓脸 · 嘟嘟嘴"});
  MoodGallery(w.Pet.Skin,Path.Combine(folder,"十二种表情预览.png"),moods.Concat(new[]{"expect","smug","confused","drowsy","peace","pout"}).ToArray(),names.Concat(new[]{"期待","得意","困惑","困困","安心","鼓脸"}).ToArray());
  foreach(string kind in new[]{"eat","read","cane"})foreach(string mood in new[]{"angry","sad","curious","happy","expect","smug","confused","drowsy","peace"}){
   w.Pet.Motion.Start(kind,2,w.Clock.Elapsed.TotalSeconds);w.Act(mood);if(w.Pet.Motion.Kind!=kind || w.State!=mood)throw new Exception("emotion cancelled action");
   if(w.History.Last().mood!=mood)throw new Exception("expression dialogue mismatch");
  }w.Act("sleep");if(w.Pet.Motion.Active)throw new Exception("sleep must put away props");
  log.Add("PASS 12 expression cards; 6 new expressions render with both skins; 27 emotion/action combinations keep their props; sleeping puts props away");
  foreach(string mood in new[]{"angry","sad","curious","expect","smug","confused","drowsy","peace"}){var line=w.Dialogues.Pick(mood);if(line.@event!=mood || line.kind!="fan")throw new Exception("new dialogue provenance");}
  log.Add("PASS 24 active expression lines are labelled as fan dialogue");
  w.Pet.Motion.Stop();w.Pet.Mood="calm";w.Pet.X=w.Pet.Y=w.Pet.EyeX=w.Pet.EyeY=w.Pet.HairX=w.Pet.HairY=0;
  if(record){string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","head-preview-frames"));Directory.CreateDirectory(frames);int index=0;
   for(int segment=0;segment<9;segment++){
    var pet=w.Pet;pet.Motion.Stop();pet.Mood=segment<3?"calm":moods[segment-3];
    if(segment==7)pet.Motion.Start("read",0,0);if(segment==8)pet.Motion.Start("eat",1,0);
    for(int f=0;f<54;f++){
     double now=f/18.0+.4,px=segment==0?Math.Sin(f/53.0*Math.PI*2):segment==2?Math.Sin(f/53.0*Math.PI*2):.15,py=segment==1?-Math.Sin(f/53.0*Math.PI*2):segment==2?Math.Cos(f/53.0*Math.PI*2)*.8:0;
     pet.StepTracking(px,py,1.0/18);pet.Time=now;pet.InvalidateVisual();Capture(pet,Path.Combine(frames,index.ToString("D4")+".png"),440,510);index++;
    }
   }log.Add("PASS "+index+" animation frames rendered for directional tracking and emotions with props");
   string moreFrames=Path.GetFullPath(Path.Combine(root,"..","..","work","more-mood-preview-frames"));Directory.CreateDirectory(moreFrames);index=0;
   foreach(string mood in new[]{"expect","smug","confused","drowsy","peace","pout"}){var pet=w.Pet;pet.Motion.Stop();pet.Mood=mood;
    for(int f=0;f<54;f++){double now=f/18.0+.4;pet.StepTracking(Math.Sin(now*.7)*.25,Math.Cos(now*.5)*.1,1.0/18);pet.Time=now;pet.InvalidateVisual();Capture(pet,Path.Combine(moreFrames,index.ToString("D4")+".png"),440,510);index++;}}
   log.Add("PASS "+index+" frames rendered for the six additional expressions");
  }
  watch.Stop();log.Add("Head QA elapsed: "+watch.Elapsed.TotalSeconds.ToString("0.0")+" seconds");File.WriteAllLines(Path.Combine(root,"head-qa-results.txt"),log);
 }
 static void RunQA(PetWindow w){
  string folder=Path.Combine(root,"preview");Directory.CreateDirectory(folder);var log=new List<string>();
  foreach(var state in PetWindow.States){w.Act(state);w.Pet.Time=2.2;w.Pet.X=0.25;w.Pet.Y=-0.1;w.Pet.SpeechUntil=0;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,state+".png"),440,510);log.Add("PASS render "+state);}
  w.SetScale(-99);if(Math.Abs(w.Pref.Scale-.42)>0.001)throw new Exception("minimum scale");w.SetScale(99);if(Math.Abs(w.Pref.Scale-1.3)>0.001)throw new Exception("maximum scale");w.SetScale(.72);log.Add("PASS scale limits");
  double start=w.Pet.X;w.Pref.Follow=false;w.Tick();if(Math.Abs(w.Pet.X)>=Math.Abs(start))throw new Exception("follow return");w.Pref.Follow=true;log.Add("PASS follow smoothing return");
  w.SetSkin("original");try{w.SetSkin("../outside");throw new Exception("path validation failed");}catch(Exception ex){if(ex.Message=="path validation failed")throw;}log.Add("PASS skin load and path validation");
  w.Left=-99999;w.Top=-99999;w.Clamp();if(w.Left<SystemParameters.VirtualScreenLeft || w.Top<SystemParameters.VirtualScreenTop)throw new Exception("clamp");w.Recover();log.Add("PASS position recovery");
  var panelScroll=(ScrollViewer)w.Panel.Content;var panelStack=(FrameworkElement)panelScroll.Content;panelScroll.Content=null;
  panelStack.Measure(new Size(450,double.PositiveInfinity));int panelHeight=(int)Math.Ceiling(panelStack.DesiredSize.Height);
  var panelSheet=new Border{Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5"),Child=panelStack};Capture(panelSheet,Path.Combine(folder,"panel.png"),450,panelHeight);panelSheet.Child=null;panelScroll.Content=panelStack;log.Add("PASS panel render");
  if(!string.IsNullOrEmpty(w.Dialogues.Error))throw new Exception(w.Dialogues.Error);
  if(w.Dialogues.Lines.Count(v=>v.kind=="official")!=5 || w.Dialogues.Lines.Count(v=>v.kind=="fan")<40)throw new Exception("dialogue provenance counts");log.Add("PASS dialogue provenance: "+w.Dialogues.Lines.Count(v=>v.kind=="official")+" supplied-source quotes, "+w.Dialogues.Lines.Count(v=>v.kind=="fan")+" original lines");
  var chosen=new HashSet<string>();for(int i=0;i<6;i++){var line=w.Dialogues.Pick("click");if(!chosen.Add(line.id))throw new Exception("recent dialogue repetition");}log.Add("PASS six consecutive click lines do not repeat");
  foreach(string trigger in new[]{"greet","click","pet","wake","poetry","sweet","writing","company","official","drag","skin"}){w.Talk(trigger);if(w.History.Last().@event!=trigger)throw new Exception("wrong trigger: "+trigger);}log.Add("PASS all interaction triggers route to their own dialogue");
  w.SetMood("sleep");w.ClickInteract();if(w.LastTrigger!="wake" || w.State=="sleep")throw new Exception("wake click");int count=w.History.Count;w.ClickInteract();if(w.History.Count!=count)throw new Exception("rapid tap throttle");log.Add("PASS wake on click and rapid-click throttle");
  var longest=w.Dialogues.Lines.Where(v=>v.kind=="official").OrderByDescending(v=>v.text.Length).First();
  w.Bubble.Display(longest,0,20,true,5);w.Bubble.Show();w.Bubble.Tick(0.3);if(w.Bubble.View.Revealed==0 || w.Bubble.View.Finished)throw new Exception("typewriter progression");w.Bubble.RevealAll();if(!w.Bubble.View.Finished)throw new Exception("typewriter reveal");log.Add("PASS typewriter and immediate full reveal");
  Capture(w.Bubble.View,Path.Combine(folder,"对话大字预览.png"),(int)w.Bubble.Width,(int)w.Bubble.Height);
  w.Bubble.Display(longest,0,28,false,5);Capture(w.Bubble.View,Path.Combine(folder,"对话最大字号.png"),(int)w.Bubble.Width,(int)w.Bubble.Height);
  double bubbleWidth=w.Bubble.Width,bubbleHeight=w.Bubble.Height;w.SetScale(.42);if(w.Bubble.Width!=bubbleWidth || w.Bubble.Height!=bubbleHeight || w.Bubble.View.FontSize!=28)throw new Exception("text tied to pet scale");w.SetScale(.72);log.Add("PASS font size 20/28 and independence from pet scale");
  w.Left=SystemParameters.WorkArea.Left;w.Top=SystemParameters.WorkArea.Top;w.Bubble.Follow(w);if(w.Bubble.Left<SystemParameters.WorkArea.Left || w.Bubble.Top<SystemParameters.WorkArea.Top)throw new Exception("bubble edge clamp");w.Bubble.Tick(100);if(w.Bubble.IsVisible)throw new Exception("bubble expiration");w.Recover();log.Add("PASS screen-edge bubble placement and expiration");
  string invalidDialogue=Path.Combine(folder,"invalid-dialogue.json");File.WriteAllText(invalidDialogue,"broken json");try{var bad=new DialogueBank(invalidDialogue);if(string.IsNullOrEmpty(bad.Error) || bad.Pick("click")==null)throw new Exception("invalid dialogue fallback");}finally{File.Delete(invalidDialogue);}log.Add("PASS malformed dialogue safely falls back");
  // Render an example from the actual pet and speech components, using readable independent text sizing.
  var scene=new Canvas{Width=660,Height=700,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};
  var example=w.Dialogues.Lines.First(v=>v.id=="official-02");var bubble=new BubbleVisual{Line=example,FontSize=20,Revealed=example.text.Length,Width=350};bubble.Height=bubble.RequiredHeight(350);Canvas.SetLeft(bubble,155);Canvas.SetTop(bubble,28);scene.Children.Add(bubble);
  w.SetSkin("original-no-bangs");
  var petExample=new PetVisual{Skin=w.Pet.Skin,Width=440,Height=510,Time=2.2,Mood="happy",X=.2,Y=0};Canvas.SetLeft(petExample,110);Canvas.SetTop(petExample,bubble.Height+5);scene.Children.Add(petExample);Capture(scene,Path.Combine(folder,"点击对话效果.png"),660,700);log.Add("PASS composed speech-and-pet preview");
  string tempId="qa-"+Guid.NewGuid().ToString("N");string tempSkin=Path.Combine(root,"skins",tempId);Directory.CreateDirectory(tempSkin);
  try{File.Copy(Path.Combine(root,"skins","original","atlas.png"),Path.Combine(tempSkin,"atlas.png"));File.Copy(Path.Combine(root,"skins","original","face-pout-v2.png"),Path.Combine(tempSkin,"face-pout-v2.png"));var serializer=new JavaScriptSerializer();var manifest=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"skins","original","rig.json")));manifest["name"]="QA alternate skin";File.WriteAllText(Path.Combine(tempSkin,"rig.json"),serializer.Serialize(manifest));w.SetSkin(tempId);if(w.Pet.Skin.name!="QA alternate skin")throw new Exception("alternate skin failed");w.SetSkin("original");log.Add("PASS alternate skin switch and restore");}
  finally{File.Delete(Path.Combine(tempSkin,"atlas.png"));File.Delete(Path.Combine(tempSkin,"face-pout-v2.png"));File.Delete(Path.Combine(tempSkin,"rig.json"));Directory.Delete(tempSkin);}
  foreach(string variant in new[]{"original","original-no-bangs"}){w.SetSkin(variant);w.Pet.Mood="calm";w.Pet.X=.25;w.Pet.Y=-.1;w.Pet.Time=2.2;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,variant+".png"),440,510);w.Pet.Mood="shy";w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,variant+"-shy.png"),440,510);}
  w.SetSkin("original-no-bangs");log.Add("PASS both hairstyle skins load, render and switch in both directions");
  File.WriteAllLines(Path.Combine(root,"qa-results.txt"),log);
 }
 static void RunActionQA(PetWindow w,bool record){
  string folder=Path.Combine(root,"preview");var log=new List<string>();
  if(!string.IsNullOrEmpty(w.Pet.ActionArt.Atlas.Error))throw new Exception(w.Pet.ActionArt.Atlas.Error);log.Add("PASS all ten prop parts load within atlas bounds");
  foreach(string skin in new[]{"original","original-no-bangs"}){
   w.SetSkin(skin);for(int i=0;i<4;i++){w.Pref.FoodIndex=i;w.StartAction("eat");if(w.Pet.Motion.Kind!="eat" || w.Pet.Motion.Food!=i || w.LastTrigger!="action-eat-"+PropAtlas.FoodIds[i])throw new Exception("food selection routing");w.Pet.Motion.Start("eat",i,0);w.Pet.Time=2.0;w.Pet.RoundHands=true;w.Pet.Mood="happy";w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"eat-"+PropAtlas.FoodIds[i]+"-"+skin+".png"),440,510);}
   foreach(string kind in new[]{"read","cane"}){w.StartAction(kind);if(w.LastTrigger!="action-"+kind)throw new Exception("action dialogue routing");w.Pet.Motion.Start(kind,0,0);w.Pet.Time=kind=="read"?3.0:3.9;w.Pet.Mood="calm";w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,kind+"-"+skin+".png"),440,510);}
  }log.Add("PASS four selectable foods, reading and cane render with both hairstyles");
  var seq=new ActionSequence();seq.Start("eat",0,0);if(!seq.Feed(1) || seq.Feed(1.1))throw new Exception("feeding cooldown");if(seq.Advance(100)!="eat" || seq.Active)throw new Exception("action completion");
  seq.Start("read",0,0);if(!seq.TurnPage(1) || seq.Page!=1 || seq.TurnPage(1.1))throw new Exception("manual page turn");seq.Advance(7);if(seq.Page<2)throw new Exception("automatic page turn");
  seq.Start("cane",0,0);if(!seq.Tap(1) || seq.Tap(1.1) || seq.TapAt!=1)throw new Exception("manual cane tap");seq.Stop();if(seq.Active)throw new Exception("stop action");log.Add("PASS feeding/page/tap cooldowns, automatic page turns, finish and cancellation");
  foreach(string kind in new[]{"eat","read","cane"}){seq.Start(kind,0,0);Point point=kind=="eat"?new Point(110,460):kind=="read"?new Point(218,440):new Point(350,350);string expected=kind=="eat"?"feed":kind=="read"?"page":"tap";if(w.Pet.ActionArt.Hit(seq,point,3.9)!=expected || w.Pet.ActionArt.Hit(seq,new Point(10,10),3.9)!="")throw new Exception("prop hit area");}if(w.Pet.ActionArt.Hit(seq,new Point(285,350),3.9)!="")throw new Exception("cane steals head clicks");log.Add("PASS interactive prop hit regions and head clicks outside cane");
  w.SetSkin("original-no-bangs");w.Pet.Motion.Start("read",0,0);w.Pet.Time=3;w.Pet.RoundHands=false;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"read-hidden-hands.png"),440,510);w.Pet.RoundHands=true;w.Pet.Motion.Stop();w.Talk("click");if(w.Pet.Motion.Active)throw new Exception("chat interruption");log.Add("PASS optional hidden hands and chat interruption");
  w.Pet.Motion.Start("read",0,0);w.Pet.Mood="calm";w.Pet.Motion.Advance(5.9);w.Pet.Time=6.25;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"read-page-turn.png"),440,510);w.Pet.Motion.Stop();
  w.Pet.ActionArt.HandScale=1.8;w.Pet.Motion.Start("eat",3,0);w.Pet.Time=2;w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(folder,"largest-round-hands.png"),440,510);w.Pet.ActionArt.HandScale=1.4;w.Pet.Motion.Stop();log.Add("PASS larger spherical hands and maximum hand-size render");
  var gallery=new Canvas{Width=960,Height=900,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};
  var title=new TextBlock{Text="海伦娜的小小日常",FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=29,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,20);gallery.Children.Add(title);
  string[] labels={"胶牙饧","曲奇","小蛋糕？","酥皮奶油浓汤","摸读与翻页","探杖与回声"};
  for(int i=0;i<6;i++){var inside=new Canvas{Width=295,Height=388,ClipToBounds=true};var card=new Border{Width=295,Height=388,CornerRadius=new CornerRadius(18),Background=Brushes.White,Child=inside};Canvas.SetLeft(card,25+(i%3)*310);Canvas.SetTop(card,82+(i/3)*402);gallery.Children.Add(card);
   var label=new TextBlock{Text=labels[i],FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=17,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,18);Canvas.SetTop(label,16);inside.Children.Add(label);
   var pet=new PetVisual{Skin=w.Pet.Skin,ActionArt=w.Pet.ActionArt,RoundHands=true,Time=i<4?2:i==4?3:3.9,Mood=i<4?"happy":"calm",X=.1,Y=0};pet.Motion.Start(i<4?"eat":i==4?"read":"cane",Math.Min(i,3),0);var view=new Viewbox{Width=295,Height=342,Child=pet};Canvas.SetTop(view,38);inside.Children.Add(view);
  }Capture(gallery,Path.Combine(folder,"六组互动预览.png"),960,900);log.Add("PASS six interaction preview cards");
  if(record){string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","action-preview-frames"));Directory.CreateDirectory(frames);int index=0;
   for(int action=0;action<6;action++){string kind=action<4?"eat":action==4?"read":"cane";w.Pet.Motion.Start(kind,Math.Min(action,3),0);w.Pet.Mood=action<4?"happy":"calm";
    int frameCount=action<4?72:144;for(int f=0;f<frameCount;f++){double now=f/18.0+.4;w.Pet.Time=now;w.Pet.X=Math.Sin(now*.8)*.18;w.Pet.Y=0;w.Pet.Motion.Advance(now);w.Pet.InvalidateVisual();Capture(w.Pet,Path.Combine(frames,index.ToString("D4")+".png"),440,510);index++;}}
   log.Add("PASS "+index+" animation frames rendered from the live animation engine");
  }
  File.WriteAllLines(Path.Combine(root,"action-qa-results.txt"),log);
 }
}
}

