using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HelenaPet {
public class PropSpec { public string id;public int x,y,w,h; }
public class PropAtlas {
 public Dictionary<string,BitmapSource> Images=new Dictionary<string,BitmapSource>();public string Error="";
 public static readonly string[] FoodIds={"sugar","cookie","cake","soup"};
 public static readonly string[] FoodNames={"胶牙饧","曲奇","小蛋糕？","酥皮奶油浓汤"};
 public PropAtlas(string folder){try{
  var atlas=new BitmapImage();atlas.BeginInit();atlas.UriSource=new Uri(Path.Combine(folder,"props.png"));atlas.CacheOption=BitmapCacheOption.OnLoad;atlas.EndInit();atlas.Freeze();
  var parts=new JavaScriptSerializer().Deserialize<PropSpec[]>(File.ReadAllText(Path.Combine(folder,"props.json")));
  foreach(var p in parts){if(p.x<0 || p.y<0 || p.w<=0 || p.h<=0 || p.x+p.w>atlas.PixelWidth || p.y+p.h>atlas.PixelHeight || Images.ContainsKey(p.id))throw new Exception("道具图集范围无效。");var image=new CroppedBitmap(atlas,new Int32Rect(p.x,p.y,p.w,p.h));image.Freeze();Images.Add(p.id,image);}
  foreach(var id in new[]{"book","closed","cane","sugar","cookie","cake","soup","piece","spoon","cookie-piece"})if(!Images.ContainsKey(id))throw new Exception("缺少道具："+id);
 }catch(Exception ex){Error="道具未载入："+ex.Message;Images.Clear();}}
 public void Draw(DrawingContext d,string id,Rect rect){BitmapSource b;if(Images.TryGetValue(id,out b))d.DrawImage(b,rect);}
}
public class ActionSequence {
 public bool AutoPage=true;public double PageInterval=5.8;
 public string Kind="";public int Food,Page;public double Started,EndAt,CycleStarted,PageTurned=-99,TapAt=-99,LastInput=-99;
 public bool Active {get{return Kind!="";}}
 public void Start(string kind,int food,double now){if(kind!="eat" && kind!="read" && kind!="cane")throw new ArgumentException("Unknown action");Kind=kind;Food=Math.Max(0,Math.Min(3,food));Page=0;Started=now;EndAt=now+(kind=="read"?20:kind=="eat"?12:10);CycleStarted=now+0.8;PageTurned=now;TapAt=-99;LastInput=-99;}
 public void Stop(){Kind="";}
 public string Advance(double now){if(!Active)return "";if(now>=EndAt){string ended=Kind;Stop();return ended;}if(Kind=="read" && AutoPage && now-PageTurned>PageInterval){Page++;PageTurned=now;}return "";}
 public bool Feed(double now){if(Kind!="eat" || now-LastInput<0.65)return false;LastInput=now;CycleStarted=now;EndAt=Math.Max(EndAt,now+6);return true;}
 public bool TurnPage(double now){if(Kind!="read" || now-LastInput<0.65)return false;LastInput=now;Page++;PageTurned=now;EndAt=Math.Max(EndAt,now+8);return true;}
 public bool Tap(double now){if(Kind!="cane" || now-LastInput<0.85)return false;LastInput=now;TapAt=now;EndAt=Math.Max(EndAt,now+3);return true;}
 public double Age(double now){return Math.Max(0,now-Started);}
 public double Envelope(double now){return Active?Smooth(Math.Min(1,Age(now)/0.7))*Smooth(Math.Min(1,Math.Max(0,EndAt-now)/0.7)):0;}
 public double Cycle(double now){return Math.Max(0,now-CycleStarted)%4;}
 public static double Smooth(double v){v=Math.Max(0,Math.Min(1,v));return v*v*(3-2*v);}
}
public class ActionArt {
 public PropAtlas Atlas;public BitmapSource CaneImage;public double CaneGrip=.286;public double HandScale=1.4;
 public Rect CaneRect(Point pivot){double width=CaneImage==null?50.8:Math.Min(108,276.0*CaneImage.PixelWidth/CaneImage.PixelHeight);return new Rect(pivot.X-4.6-width/2,pivot.Y-276,width,276);}
 public ActionArt(PropAtlas atlas){Atlas=atlas;}
 static Brush B(string value){return (Brush)new BrushConverter().ConvertFromString(value);}
 static double Mix(double a,double b,double t){return a+(b-a)*t;}
 public string Hit(ActionSequence seq,Point p,double now){if(!seq.Active)return "";p.Y-=(1-seq.Envelope(now))*90;if(seq.Kind=="read" && new Rect(112,380,220,125).Contains(p))return "page";if(seq.Kind=="eat" && new Rect(55,375,305,130).Contains(p))return "feed";
  if(seq.Kind=="cane"){double angle,tap;Point pivot;CanePose(seq,now,out angle,out tap,out pivot);Point local=new RotateTransform(angle,pivot.X,pivot.Y).Inverse.Transform(p);
   var caneRect=CaneRect(pivot);if(new Rect(caneRect.X,caneRect.Y,caneRect.Width,84).Contains(local) || new Rect(pivot.X-10,pivot.Y-213,19,215).Contains(local) || (local-new Point(pivot.X-1,pivot.Y-276+276*CaneGrip)).Length<16*HandScale)return "tap";
  }return "";
 }
 void CanePose(ActionSequence seq,double now,out double angle,out double tap,out Point pivot){double age=seq.Age(now);angle=age<2.9?-13+Math.Sin(age*2)*10:Math.Sin(age*1.7)*4;tap=seq.TapAt>-90?now-seq.TapAt:age>=6.6?age-6.6:age-3.5;double lift=tap>-.42 && tap<0?-Math.Sin((tap+.42)/.42*Math.PI)*13:0;if(tap>=-.42 && tap<.4)angle*=.2;pivot=new Point(351,487+lift);}
 void Mitten(DrawingContext d,Point p,double angle,double size,bool enabled){if(!enabled)return;d.PushTransform(new TranslateTransform(p.X,p.Y));d.PushTransform(new RotateTransform(angle));d.PushTransform(new ScaleTransform(size*HandScale,size*HandScale));
  var fill=new RadialGradientBrush(Color.FromRgb(255,253,246),Color.FromRgb(233,218,201)){GradientOrigin=new Point(.33,.24),RadiusX=.78,RadiusY=.78};
  d.DrawEllipse(fill,new Pen(B("#795B60"),1.5),new Point(0,0),15,15);
  d.DrawEllipse(B("#55FFFFFF"),null,new Point(-4,-6),5,3);d.Pop();d.Pop();d.Pop();
 }
 public void Draw(DrawingContext d,ActionSequence seq,double now,bool hands){if(!seq.Active || !string.IsNullOrEmpty(Atlas.Error))return;double e=seq.Envelope(now),age=seq.Age(now);d.PushOpacity(e);d.PushTransform(new TranslateTransform(0,(1-e)*90));
  if(seq.Kind=="eat")Eat(d,seq,now,hands);else if(seq.Kind=="read")Read(d,seq,now,hands);else Cane(d,seq,now,hands);
  d.Pop();d.Pop();
 }
 public bool Chewing(ActionSequence seq,double now){double c=seq.Cycle(now);return seq.Kind=="eat" && now>=seq.CycleStarted && c>1.05 && c<3.0;}
 void Eat(DrawingContext d,ActionSequence seq,double now,bool hands){
  string food=PropAtlas.FoodIds[seq.Food];double c=seq.Cycle(now);bool chewing=Chewing(seq,now);
  Rect plate=food=="cake"?new Rect(77,388,103,111):food=="soup"?new Rect(62,406,129,98):new Rect(65,410,130,93);
  Atlas.Draw(d,food,plate);Mitten(d,new Point(plate.Left+10,plate.Bottom-18),-20,.72,hands);
  double approach=c<1?ActionSequence.Smooth(c):c<1.5?1:c<2.4?1-ActionSequence.Smooth((c-1.5)/.9):0;
  Point bite=new Point(Mix(298,221,approach),Mix(464,392,approach)-Math.Sin(approach*Math.PI)*15);
  if(food=="sugar" || food=="cookie"){
   double size=food=="sugar"?23:24;Rect r=new Rect(bite.X-size/2,bite.Y-size/2,size,size);
   if(food=="cookie")d.PushClip(new EllipseGeometry(r));
   if(chewing){d.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(0,0,440,510)),new EllipseGeometry(new Point(r.Left+size*.4,r.Top+2),size*.27,size*.22)));}
   Atlas.Draw(d,food=="sugar"?"piece":"cookie-piece",r);if(chewing)d.Pop();if(food=="cookie")d.Pop();
   Mitten(d,new Point(bite.X+8,bite.Y+17),-25,.8,hands);
  } else {
   Rect spoon=new Rect(bite.X-9,bite.Y-10,70,52);Atlas.Draw(d,"spoon",spoon);
   if(food=="cake"){d.DrawEllipse(B("#68416B"),null,new Point(bite.X+7,bite.Y+6),7,4);d.DrawEllipse(B("#F4DCD1"),null,new Point(bite.X+6,bite.Y+3),5,2);}
   Mitten(d,new Point(bite.X+42,bite.Y+22),-35,.78,hands);
  }
  if(chewing){for(int i=0;i<3;i++){double a=(now*.7+i*.32)%1;d.PushOpacity(1-a);d.DrawEllipse(B("#DBB179"),null,new Point(215+Math.Sin(i*2)*18,391+a*23),1.2,1.2);d.Pop();}}
 }
 void Read(DrawingContext d,ActionSequence seq,double now,bool hands){
  double age=seq.Age(now);Rect r=new Rect(119,382,198,120);double opening=ActionSequence.Smooth(Math.Min(1,age/.85));
  if(opening<1){d.PushOpacity(1-opening);Atlas.Draw(d,"closed",new Rect(174,399,88,96));d.Pop();}
  d.PushOpacity(opening);Atlas.Draw(d,"book",r);
  Mitten(d,new Point(r.Left+6,r.Top+88),-16,.78,hands);
  // A rounded fingertip/mitten traces raised dots, and a hinged leaf crosses the spine on each turn.
  double trace=(Math.Sin(age*1.4)*.5+.5);double row=(int)(age/2.7)%3;Point finger=new Point(151+trace*117,418+row*12);
  if(hands){d.DrawEllipse(B("#6BB7C3A5"),null,new Point(finger.X,finger.Y-3),10,4);Mitten(d,finger,-45,.55,true);}
  double turn=now-seq.PageTurned;
  if(seq.Page>0 && turn>=0 && turn<.85){double p=turn/.85;double span=Math.Cos(p*Math.PI)*78;var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(218,408),true,true);c.LineTo(new Point(218+span,401-14*Math.Sin(p*Math.PI)),true,false);c.LineTo(new Point(218+span,468-14*Math.Sin(p*Math.PI)),true,false);c.LineTo(new Point(218,477),true,false);}d.DrawGeometry(B("#FFF4DA"),new Pen(B("#B59B7A"),1),g);
   for(int i=0;i<5;i++)for(int j=0;j<2;j++)d.DrawEllipse(B("#BAA184"),null,new Point(218+span*(.25+i*.1),423+j*10),1.1,1.1);
  }
  d.Pop();
 }
 void Cane(DrawingContext d,ActionSequence seq,double now,bool hands){
  double angle,tap;Point pivot;CanePose(seq,now,out angle,out tap,out pivot);d.PushTransform(new RotateTransform(angle,pivot.X,pivot.Y));
  if(CaneImage==null)Atlas.Draw(d,"cane",CaneRect(pivot));else d.DrawImage(CaneImage,CaneRect(pivot));Mitten(d,new Point(pivot.X-1,pivot.Y-276+276*CaneGrip),-15,.84,hands);d.Pop();
  if(tap>=0 && tap<1.6)for(int i=0;i<3;i++){double a=tap/1.6-i*.15;if(a<0 || a>1)continue;d.PushOpacity((1-a)*.75);d.DrawEllipse(null,new Pen(B("#79AFC0"),1.5),new Point(pivot.X,487),8+a*72,3+a*16);d.Pop();}
 }
}
}
