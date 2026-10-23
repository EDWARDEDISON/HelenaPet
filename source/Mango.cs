using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;
namespace HelenaPet {
public class MangoChance {
 public const double Duration=6,Transition=.7;
 readonly Random random;
 public double CheckAt=double.PositiveInfinity,Until=0,Started=0,NextAllowed=0;
 public bool Checked;
 public MangoChance():this(Environment.TickCount+1537){}
 public MangoChance(int seed){random=new Random(seed);}
 public bool Active(double now){return now>=Started && now<Until;}
 public void NoteActivity(double now){Until=0;Checked=false;CheckAt=now+75+random.NextDouble()*45;}
 public void Cancel(){Until=0;}
 public void Preview(double now){Started=now;Until=now+Duration;NextAllowed=now+1200;Checked=true;}
 public bool Advance(double now,bool enabled,bool eligible){
  if(!enabled || !eligible){Until=0;return false;}
  if(Active(now))return true;Until=0;
  if(Checked || now<CheckAt || now<NextAllowed)return false;
  Checked=true;if(random.NextDouble()>=.04)return false;Preview(now);return true;
 }
}
public class MangoSpec {public string texture;public int sx,sy,sw,sh;}
public partial class PetVisual {
 public BitmapSource MangoImage;public double MangoStarted,MangoUntil;
 public bool IsMango{get{return MangoImage!=null && Time>=MangoStarted && Time<MangoUntil;}}
 static readonly Brush MangoGold=new SolidColorBrush(Color.FromRgb(239,196,112));
 static readonly Brush MangoBlue=new SolidColorBrush(Color.FromRgb(155,195,214));
 static readonly Brush MangoSmokeCream=SmokeBrush(Color.FromRgb(255,250,238));
 static readonly Brush MangoSmokeBlue=SmokeBrush(Color.FromRgb(225,237,242));
 static Brush SmokeBrush(Color center){var brush=new RadialGradientBrush();brush.GradientStops.Add(new GradientStop(center,0));brush.GradientStops.Add(new GradientStop(Color.FromArgb(244,center.R,center.G,center.B),.57));brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,center.R,center.G,center.B),1));brush.Freeze();return brush;}
 static readonly Geometry MangoStar=Geometry.Parse("M0,-1 L.23,-.23 1,0 .23,.23 0,1 -.23,.23 -1,0 -.23,-.23 Z");
 void DrawMangoStar(DrawingContext d,Point point,double radius,Brush brush,double opacity,double rotation){
  if(opacity<=0 || radius<=0)return;d.PushOpacity(opacity);d.PushTransform(new TranslateTransform(point.X,point.Y));d.PushTransform(new RotateTransform(rotation));d.PushTransform(new ScaleTransform(radius,radius));d.DrawGeometry(brush,null,MangoStar);d.Pop();d.Pop();d.Pop();d.Pop();
 }
 void DrawMangoSmoke(DrawingContext d,double progress,bool returning){
  if(progress<=0 || progress>=1)return;
  double strength=Math.Pow(Math.Sin(progress*Math.PI),.65),spread=ActionSequence.Smooth(progress),lift=progress*11;
  d.PushOpacity(strength*.98);d.DrawEllipse(MangoSmokeCream,null,new Point(220,318-lift),138,128);
  for(int i=0;i<11;i++){
   double angle=i*Math.PI*2/11+(returning?-.1:.1),radius=49+(i%3)*7+Math.Sin(progress*Math.PI)*6;
   var point=new Point(220+Math.Cos(angle)*(72+37*spread),318-lift+Math.Sin(angle)*(75+28*spread));
   d.DrawEllipse(i%3==0?MangoSmokeBlue:MangoSmokeCream,null,point,radius,radius*(.85+(i%2)*.1));
  }d.Pop();
 }
 bool DrawMango(DrawingContext d){
  if(!IsMango)return false;
  double elapsed=Math.Max(0,Time-MangoStarted),remaining=MangoUntil-Time;
  double normalAlpha=0,fruitAlpha=1,shapeScale=1;
  // One face is visible at a time; the soft smoke conceals the midpoint swap.
  if(elapsed<MangoChance.Transition){double p=elapsed/MangoChance.Transition;if(p<.5){double q=ActionSequence.Smooth(p*2);normalAlpha=1-.8*q;fruitAlpha=0;shapeScale=1-.12*q;}else{double q=ActionSequence.Smooth((p-.5)*2);fruitAlpha=.2+.8*q;shapeScale=.88+.12*q;}}
  else if(remaining<MangoChance.Transition){double p=1-remaining/MangoChance.Transition;if(p<.5){double q=ActionSequence.Smooth(p*2);fruitAlpha=1-.8*q;shapeScale=1-.12*q;}else{double q=ActionSequence.Smooth((p-.5)*2);normalAlpha=.2+.8*q;fruitAlpha=0;shapeScale=.88+.12*q;}}
  if(normalAlpha>0){d.PushOpacity(normalAlpha);d.PushTransform(new ScaleTransform(shapeScale,shapeScale,220,330));DrawNormal(d);d.Pop();d.Pop();}PendantBounds=Rect.Empty;
  double entrance=1-Math.Exp(-elapsed*7)*Math.Cos(elapsed*11)*.1,pop=entrance*shapeScale;
  double height=390,width=height*MangoImage.PixelWidth/MangoImage.PixelHeight;if(width>350){height*=350/width;width=350;}
  double hopAge=elapsed-1.1,hopHeight=EchoHop.Height(hopAge,2,28);
  var rect=new Rect(220-width/2,330-height/2,width,height);d.PushOpacity(fruitAlpha);d.PushTransform(new TranslateTransform(0,Math.Sin(elapsed*3)*2-hopHeight));d.PushTransform(new RotateTransform(Math.Sin(elapsed*2.3)*2,220,330));d.PushTransform(new ScaleTransform(pop,pop,220,330));d.DrawImage(MangoImage,rect);d.Pop();d.Pop();d.Pop();d.Pop();
  if(fruitAlpha>0){DrawHopParticles(d,hopAge,469,2);EchoHop.DrawGround(d,hopAge,476,2,EchoStrength,true,"mango");}
  for(int i=0;i<3;i++){double pulse=Math.Max(0,Math.Sin(elapsed*2.7+i*2));DrawMangoStar(d,new Point(i==0?58:i==1?382:342,i==0?300:i==1?289:454),3+pulse*2,i==1?MangoBlue:MangoGold,fruitAlpha*pulse*.6,elapsed*18);}
  DrawMangoSmoke(d,elapsed/MangoChance.Transition,false);DrawMangoSmoke(d,1-remaining/MangoChance.Transition,true);return true;
 }
}
public partial class PetWindow {
 public MangoChance Mango=new MangoChance();public string MangoError="";
 void InitMango(){try{var spec=new JavaScriptSerializer().Deserialize<MangoSpec>(File.ReadAllText(Path.Combine(Root,"assets","mango.json")));if(spec.texture!=Path.GetFileName(spec.texture))throw new Exception("彩蛋路径无效");var img=new BitmapImage();img.BeginInit();img.UriSource=new Uri(Path.Combine(Root,"assets",spec.texture));img.CacheOption=BitmapCacheOption.OnLoad;img.EndInit();img.Freeze();if(spec.sx<0 || spec.sy<0 || spec.sw<1 || spec.sh<1 || spec.sx+spec.sw>img.PixelWidth || spec.sy+spec.sh>img.PixelHeight)throw new Exception("彩蛋范围无效");Pet.MangoImage=new CroppedBitmap(img,new Int32Rect(spec.sx,spec.sy,spec.sw,spec.sh));Pet.MangoImage.Freeze();}catch(Exception ex){MangoError=ex.Message;}Mango.NoteActivity(Clock.Elapsed.TotalSeconds);}
 public void AdvanceMango(double now){Mango.Advance(now,Pref.MangoEnabled && Pet.MangoImage!=null,State=="calm" && !Pet.Motion.Active && !Pet.IsHopping && !Pet.EchoCharging && !Pet.IsEchoRelease && !down && !rightDown && (Bubble==null || !Bubble.IsVisible));Pet.MangoStarted=Mango.Started;Pet.MangoUntil=Mango.Until;}
 void PreviewMango(){if(!Pref.MangoEnabled || Pet.MangoImage==null)return;Pet.Motion.Stop();SetMood("calm");Mango.Preview(Clock.Elapsed.TotalSeconds);Pet.MangoStarted=Mango.Started;Pet.MangoUntil=Mango.Until;Pet.Time=Clock.Elapsed.TotalSeconds;if(Bubble!=null)Bubble.Hide();Pet.InvalidateVisual();RefreshStatusCaption();}
 void MangoControls(StackPanel panel){
  var enabled=new CheckBox{Content="偶尔变成芒果 · 小彩蛋",IsChecked=Pref.MangoEnabled,Margin=new Thickness(0,12,0,12)};panel.Children.Add(enabled);var preview=Btn("预览芒果彩蛋 · 6 秒",PreviewMango);preview.IsEnabled=Pref.MangoEnabled && Pet.MangoImage!=null;
  enabled.Click+=(s,e)=>{Pref.MangoEnabled=enabled.IsChecked==true;if(!Pref.MangoEnabled){Mango.Cancel();Pet.MangoUntil=0;}preview.IsEnabled=Pref.MangoEnabled && Pet.MangoImage!=null;Save();Pet.InvalidateVisual();};panel.Children.Add(Txt("醒着闲置一会儿，偶尔会在软软烟雾中变成小芒果，轻跳两下，落地散开回声。全程约 6 秒，含变身与恢复；点击可立即恢复。",16,"#7F7268"));panel.Children.Add(preview);if(MangoError!="")panel.Children.Add(Txt("彩蛋素材未载入："+MangoError,16,"#A76852"));
 }
 void AttachEngagement(Window page){page.PreviewMouseDown+=(s,e)=>EngageFromPanel();page.PreviewKeyDown+=(s,e)=>{if(Pet.EchoCharging && (e.Key==Key.Space || e.Key==Key.Return))return;EngageFromPanel();};page.PreviewMouseWheel+=(s,e)=>EngageFromPanel();}
 void EngageFromPanel(){if(State=="sleep")Talk("wake");else NoteEngagement();}
}
}

