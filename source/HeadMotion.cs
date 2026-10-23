using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace HelenaPet {
// A small, bounded 2D deformation rig. It does not produce or load a Cubism model.
public class HeadProjection {
 public double X,Y;
 public Point Project(Point p,double depth,string part) {
  double u=(p.X-200)/195,curve=Math.Max(0,1-u*u),dx=p.X-200,dy=p.Y-260;
  double scaleX=1-.085*Math.Abs(X),scaleY=1-.045*Math.Abs(Y);
  if(part=="hat")scaleY+=Y*.015;
  return new Point(200+dx*scaleX+X*(depth*18+curve*17),260+dy*scaleY+Y*(depth*15+curve*10)+X*dx*.022);
 }
 public Matrix Tangent(Point p,double depth,string part) {
  Point q=Project(p,depth,part),a=Project(new Point(p.X+1,p.Y),depth,part),b=Project(new Point(p.X,p.Y+1),depth,part);
  double xx=a.X-q.X,xy=a.Y-q.Y,yx=b.X-q.X,yy=b.Y-q.Y;
  return new Matrix(xx,xy,yx,yy,q.X-xx*p.X-yx*p.Y,q.Y-xy*p.X-yy*p.Y);
 }
}
public class HeadSurface {
 class Patch {public Point A,B,C;public Geometry Shape;}
 class Part {public Layer Layer;public bool IsPout;public Brush Brush;public List<Patch> Patches=new List<Patch>();}
 Skin current;Layer neutralFace;List<Part> parts=new List<Part>();
 static Geometry Triangle(Point a,Point b,Point c) {var g=new StreamGeometry();using(var s=g.Open()){s.BeginFigure(a,true,true);s.LineTo(b,true,false);s.LineTo(c,true,false);}g.Freeze();return g;}
 static Geometry PartTriangle(Layer layer,Point a,Point b,Point c){var triangle=Triangle(a,b,c);if(layer.ClipShape==null)return triangle;var shape=Geometry.Combine(triangle,layer.ClipShape,GeometryCombineMode.Intersect,null);shape.Freeze();return shape;}
 void Load(Skin skin) {
  current=skin;neutralFace=Array.Find(skin.layers,l=>l.name=="face");parts.Clear();var layers=new List<Layer>();foreach(var normal in skin.layers){layers.Add(normal);if(normal.name=="face" && skin.poutFace!=null)layers.Add(skin.poutFace);}foreach(var l in layers) {
   var part=new Part{Layer=l,IsPout=l==skin.poutFace,Brush=Skin.LayerBrush(l)};
   const int cols=10,rows=5;
   for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){
    Point a=new Point(l.x+l.w*x/cols,l.y+l.h*y/rows),b=new Point(l.x+l.w*(x+1)/cols,a.Y),c=new Point(b.X,l.y+l.h*(y+1)/rows),e=new Point(a.X,c.Y);
    part.Patches.Add(new Patch{A=a,B=b,C=c,Shape=PartTriangle(l,a,b,c)});part.Patches.Add(new Patch{A=a,B=c,C=e,Shape=PartTriangle(l,a,c,e)});
   }parts.Add(part);
  }
 }
 Point Vertex(Part part,Point point,HeadProjection projection,double pout){
  var l=part.Layer;if(part.IsPout && neutralFace!=null){double u=(point.X-l.x)/l.w,v=(point.Y-l.y)/l.h;
   double x=neutralFace.x+(l.x-neutralFace.x)*pout,y=neutralFace.y+(l.y-neutralFace.y)*pout,w=neutralFace.w+(l.w-neutralFace.w)*pout,h=neutralFace.h+(l.h-neutralFace.h)*pout;
   point=new Point(x+u*w,y+v*h);double lower=ActionSequence.Smooth((v-.48)/.4);point.X=200+(point.X-200)*(1-.10*(1-pout)*lower);
  }return projection.Project(point,l.depth,l.name);
 }
 public void Draw(DrawingContext target,Skin skin,HeadProjection projection,double time,double sway,double lagX,double lagY,double pout,AccessoryArt accessories,string decoration,bool hatOnly=false,double decorationScale=1) {
  if(current!=skin)Load(skin);
  // Aliased triangle edges avoid transparent hairline seams between adjacent patches;
  // the source artwork keeps its own antialiased alpha silhouette.
  var drawing=new DrawingGroup();RenderOptions.SetEdgeMode(drawing,EdgeMode.Aliased);
  using(var d=drawing.Open())foreach(var p in parts){
   var l=p.Layer;if((l.name=="hat")!=hatOnly)continue;
   bool face=skin.poutFace!=null && l.name=="face";if(face && (p.IsPout!=(pout>.001)))continue;
   if(l.name=="hair")d.PushTransform(new TranslateTransform(lagX*9,lagY*7));
   if(l.name=="hair")d.PushTransform(new RotateTransform(Math.Sin(time*1.7-projection.X)*1.25*sway,200,145));
   if(l.name=="hat")d.PushTransform(new RotateTransform(Math.Sin(time*1.5-.5)*.2*sway,200,255));
   
   foreach(var patch in p.Patches){Point a=Vertex(p,patch.A,projection,pout),b=Vertex(p,patch.B,projection,pout),c=Vertex(p,patch.C,projection,pout);
    Vector ab=patch.B-patch.A,ac=patch.C-patch.A;double det=ab.X*ac.Y-ab.Y*ac.X;
    Vector qa=b-a,qc=c-a;double xx=(qa.X*ac.Y-qc.X*ab.Y)/det,yx=(qc.X*ab.X-qa.X*ac.X)/det,xy=(qa.Y*ac.Y-qc.Y*ab.Y)/det,yy=(qc.Y*ab.X-qa.Y*ac.X)/det;
    d.PushTransform(new MatrixTransform(new Matrix(xx,xy,yx,yy,a.X-xx*patch.A.X-yx*patch.A.Y,a.Y-xy*patch.A.X-yy*patch.A.Y)));
    d.DrawGeometry(p.Brush,null,patch.Shape);d.Pop();
   }
   if(l.name=="hat" && accessories!=null)accessories.Draw(d,projection,decoration,true,skin,decorationScale);
   if(l.name=="hair" || l.name=="hat")d.Pop();
   if(l.name=="hair")d.Pop();
  }
  target.DrawDrawing(drawing);
 }
}
public partial class PetVisual {
 internal static void DrawHeadsetMic(DrawingContext d,HeadProjection rig){
  d.PushTransform(new MatrixTransform(rig.Tangent(new Point(145,340),.65,"headwear")));
  var shape=Geometry.Parse("M110,358 C128,362 153,352 174,350");
  var outline=new Pen(new SolidColorBrush(Color.FromRgb(80,53,37)),6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
  var gold=new Pen(new SolidColorBrush(Color.FromRgb(222,181,99)),3.3){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
  d.DrawGeometry(null,outline,shape);d.DrawGeometry(null,gold,shape);
  d.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(48,44,42)),new Pen(new SolidColorBrush(Color.FromRgb(79,60,44)),1.4),new Rect(169,344,24,11),5.5,5.5);
  d.DrawEllipse(new SolidColorBrush(Color.FromRgb(135,130,121)),null,new Point(176,347),3.5,1.2);d.Pop();
 }
}
public class ButtonEyes {
 static Dictionary<string,Brush> brushes=new Dictionary<string,Brush>();
 static Brush B(string s){Brush b;if(!brushes.TryGetValue(s,out b)){b=(Brush)new BrushConverter().ConvertFromString(s);b.Freeze();brushes[s]=b;}return b;}
 static Pen P(string s,double width){var p=new Pen(B(s),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};p.Freeze();return p;}
 static void Path(DrawingContext d,string path,string color,double width){d.DrawGeometry(null,P(color,width),Geometry.Parse(path));}
 public void Draw(DrawingContext d,Skin skin,HeadProjection rig,double eyeX,double eyeY,string mood,double time) {
  if(!skin.expressiveEyes)return;
  Layer l=Array.Find(skin.layers,v=>v.name=="glasses");if(l==null)return;
  for(int side=0;side<2;side++){
   Point center=new Point(l.x+l.w*(side==0?.24:.76),l.y+l.h*.50);double size=l.w/246.0;
   d.PushTransform(new MatrixTransform(rig.Tangent(center,l.depth,"glasses")));d.PushTransform(new TranslateTransform(center.X,center.Y));d.PushTransform(new ScaleTransform(size,size));
   var clip=new EllipseGeometry(new Point(0,0),34.2,35.0);d.PushClip(clip);
   var lens=new RadialGradientBrush((Color)ColorConverter.ConvertFromString(skin.lensLight),(Color)ColorConverter.ConvertFromString(skin.lensDark)){GradientOrigin=new Point(.36,.22),RadiusX=.9,RadiusY=.9};
   d.DrawEllipse(lens,null,new Point(0,0),35,36);
   double ex=eyeX*8.8,ey=eyeY*6.8;
   d.PushTransform(new TranslateTransform(ex,ey));
   d.DrawEllipse(null,P(skin.eyeOutline,1.4),new Point(0,0),27.5,28.2);d.DrawEllipse(null,P(skin.lensLight,1.2),new Point(-.4,-.6),25.3,26);
   bool closed=mood=="sleep" || mood=="peace" || (mood=="smug" && side==1) || (time%6.7>6.52 && mood!="surprise" && mood!="sad" && mood!="expect");
   if(closed)Path(d,"M-13,1 Q0,10 13,1","#F8E9D7",4.5);
   else if(mood=="drowsy")Path(d,"M-13,3 Q0,5 13,3","#F8E9D7",4.5);
   else if(mood=="expect")d.DrawGeometry(B("#FFF0D5"),P("#856058",1.2),Geometry.Parse("M0,-14 L4,-5 14,-4 6,3 9,14 0,8 -9,14 -6,3 -14,-4 -4,-5 Z"));
   else if(mood=="happy")Path(d,"M-14,6 Q0,-13 14,6","#FFF1DF",5.5);
   else if(skin.eyeStyle=="flower"){for(int petal=0;petal<4;petal++){d.PushTransform(new RotateTransform(petal*90));d.DrawEllipse(B(skin.eyeMark),P(skin.eyeOutline,1.3),new Point(0,-9),6,9);d.Pop();}d.DrawEllipse(B("#FFF9E8"),P(skin.eyeOutline,1),new Point(0,0),4,4);}
   else if(skin.eyeStyle=="star")d.DrawGeometry(B(skin.eyeMark),P(skin.eyeOutline,1.3),Geometry.Parse("M0,-15 L5,-5 15,-4 7,4 10,15 0,9 -10,15 -7,4 -15,-4 -5,-5 Z"));
   else if(skin.eyeStyle=="spiral")Path(d,"M-13,1 C-16,-20 14,-23 19,-3 C25,20 -13,24 -16,8 C-20,-8 4,-15 9,-3 C15,10 -5,12 -4,2 C-4,-3 2,-3 3,0",skin.eyeMark,3.2);
   else if(skin.eyeStyle=="plus"){d.DrawLine(P(skin.eyeOutline,8),new Point(-13,0),new Point(13,0));d.DrawLine(P(skin.eyeOutline,8),new Point(0,-13),new Point(0,13));d.DrawLine(P(skin.eyeMark,4.6),new Point(-13,0),new Point(13,0));d.DrawLine(P(skin.eyeMark,4.6),new Point(0,-13),new Point(0,13));}
   else if(skin.eyeStyle=="gear"){d.DrawEllipse(B("#30363E"),P(skin.eyeMark,1.7),new Point(0,0),16,16);d.DrawEllipse(null,P("#727780",1.8),new Point(0,0),11,11);d.DrawEllipse(B("#171E24"),null,new Point(0,0),5,5);}
   else {
    double crossScale=mood=="confused"?(side==0?.7:1.2):mood=="pout"?.85:1;d.PushTransform(new ScaleTransform(crossScale,crossScale));
    d.PushTransform(new RotateTransform(mood=="angry"?(side==0?-14:14):mood=="sad"?(side==0?12:-12):0));
    d.DrawLine(P(skin.eyeOutline,8),new Point(-8,-8),new Point(8,8));d.DrawLine(P(skin.eyeOutline,8),new Point(-8,8),new Point(8,-8));
    d.DrawLine(P(skin.eyeMark,5.6),new Point(-8,-8),new Point(8,8));d.DrawLine(P(skin.eyeMark,5.6),new Point(-8,8),new Point(8,-8));d.Pop();d.Pop();
   }
   d.Pop();
   if(mood=="angry"){
    string shape=side==0?"M-39,-39 L39,-39 L39,-6 Q0,-15 -39,-29 Z":"M-39,-39 L39,-39 L39,-29 Q0,-15 -39,-6 Z";
    d.DrawGeometry(B("#AA6F80"),P("#754854",1.8),Geometry.Parse(shape));
   }
   if(mood=="sad")Path(d,side==0?"M-25,-12 Q-7,-30 20,-29":"M-20,-29 Q7,-30 25,-12","#704956",3.6);
   if(mood=="curious" && side==1){d.PushTransform(new TranslateTransform(0,-8));Path(d,"M-20,-14 Q0,-29 20,-14","#F7E2CC",2.5);d.Pop();}
   if(mood=="confused")Path(d,side==0?"M-20,-21 Q0,-15 20,-24":"M-20,-22 Q0,-35 20,-21","#704956",2.8);
   if(mood=="drowsy")Path(d,"M-24,-11 Q0,-7 24,-11","#8D5768",3);
   // Reflections travel less than the button centers and never leave the lens mask.
   d.PushOpacity(.38);d.DrawEllipse(B("#FFF7EC"),null,new Point(-17+eyeX*2,-21+eyeY*2),6,2.6);d.Pop();
   d.Pop();
   if(mood=="sad"){
    double drop=(time*.6+side*.35)%1;d.PushOpacity(.95-drop*.35);d.PushTransform(new TranslateTransform(side==0?-27:27,46+drop*16));d.PushTransform(new ScaleTransform(1.5,1.5));
    d.DrawGeometry(B("#B4E1EC"),P("#6D98AE",1.2),Geometry.Parse("M0,-7 Q-9,4 0,8 Q9,4 0,-7 Z"));d.Pop();d.Pop();d.Pop();
   }
   d.Pop();d.Pop();d.Pop();
  }
 }
}
}
