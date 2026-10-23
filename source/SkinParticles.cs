using System;
using System.Windows;
using System.Windows.Media;
namespace HelenaPet {
public static class SkinParticles {
 static readonly Geometry Star=Geometry.Parse("M0,-1 L.25,-.25 1,0 .25,.25 0,1 -.25,.25 -1,0 -.25,-.25 Z");
 static readonly Geometry Leaf=Geometry.Parse("M-1,0 Q0,-1.3 1,0 Q0,1.3 -1,0 Z");
 static readonly Geometry Diamond=Geometry.Parse("M0,-1 L.7,0 0,1 -.7,0 Z");
 public static string Kind(string skin){
  if((skin??"").EndsWith("cake"))return "糖屑";
  switch(skin){case "seaside":case "sea-witch":return "泡泡";case "star-girl":case "redeem-star":case "clerk":return "星光";case "architect":return "冰晶";case "maria":return "晶石";case "gilda":return "叶片";case "sound-wave":return "音符";case "haixi":return "金纹";case "sleeping-girl":return "梦花";default:return "回声星点";}
 }
 static Brush Paint(string color){var brush=(Brush)new BrushConverter().ConvertFromString(color);brush.Freeze();return brush;}
 static readonly Brush Pink=Paint("#DC779F"),Mint=Paint("#68B9A2"),Gold=Paint("#DEAC49"),Blue=Paint("#4D9DC6"),Purple=Paint("#8D78BD"),LeafGreen=Paint("#5D9275"),Ice=Paint("#72BACA"),White=Paint("#FFF8DA");
 public static void Draw(DrawingContext d,string skin,double age,double ground,double strength=1.3,double power=1){
  if(age<0 || age>1.2)return;string kind=Kind(skin);int count=kind=="糖屑"?16:12;
  for(int i=0;i<count;i++){
   double life=(age-i*.012)/1.0;if(life<=0 || life>=1)continue;
   double side=i%2==0?-1:1,start=76+(i%4)*13,drift=28+(i%3)*13;
   double x=220+side*(start+drift*life),y=ground-13-(57+(i%4)*13)*life+27*life*life;
   double size=(4+(i%3))*Math.Min(1.35,.8+strength*.2)*(.85+power*.15),alpha=Math.Min(1,life/.08)*Math.Pow(1-life,.65)*.96;
   Brush primary=skin=="sweet-cake"?Pink:skin=="dark-cake"?Blue:Gold,paint=i%3==0?White:i%3==1?primary:kind=="叶片"?LeafGreen:kind=="梦花"?Purple:Mint;
   d.PushOpacity(alpha);d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new RotateTransform(i*31+life*(i%2==0?90:-90)));d.PushTransform(new ScaleTransform(size,size));
   if(kind=="泡泡"){d.DrawEllipse(null,new Pen(Ice,.35),new Point(0,0),1,1);d.DrawEllipse(White,null,new Point(-.3,-.3),.22,.22);}
   else if(kind=="糖屑")d.DrawRoundedRectangle(paint,new Pen(primary,.1),new Rect(-.8,-.35,1.6,.7),.2,.2);
   else if(kind=="叶片")d.DrawGeometry(i%3==0?Mint:LeafGreen,new Pen(White,.12),Leaf);
   else if(kind=="音符"){d.DrawEllipse(i%2==0?Gold:Blue,null,new Point(-.2,.6),.55,.35);d.DrawLine(new Pen(i%2==0?Gold:Blue,.28),new Point(.25,.6),new Point(.25,-1));d.DrawLine(new Pen(i%2==0?Gold:Blue,.28),new Point(.25,-1),new Point(.9,-.7));}
   else if(kind=="冰晶" || kind=="晶石" || kind=="金纹"){d.DrawGeometry(kind=="金纹"?Gold:Ice,new Pen(kind=="晶石"?Gold:White,.18),Diamond);d.DrawLine(new Pen(White,.12),new Point(0,-.8),new Point(0,.8));}
   else if(kind=="梦花"){for(int k=0;k<4;k++){d.PushTransform(new RotateTransform(k*90));d.DrawEllipse(i%2==0?Pink:Purple,null,new Point(0,-.5),.35,.55);d.Pop();}d.DrawEllipse(White,null,new Point(0,0),.18,.18);}
   else d.DrawGeometry(kind=="回声星点"?(i%2==0?Blue:Pink):i%2==0?Gold:Blue,null,Star);
   d.Pop();d.Pop();d.Pop();d.Pop();
  }
 }
}
public partial class PetVisual {
 public bool ShowSkinParticles=true;public double EchoStrength=1.3;
 void DrawHopParticles(DrawingContext d,double age,double ground,int count,string skin=null){if(!ShowSkinParticles)return;for(int hop=1;hop<=count;hop++)SkinParticles.Draw(d,skin??(Skin==null?"":Skin.id),age-hop*EchoHop.Interval,ground,EchoStrength);}
}
}
