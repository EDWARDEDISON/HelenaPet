using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HelenaPet {
public class DecorationSpec {
 public string id;public int x,y,w,h;public double dx,dy,dw,dh;public bool front;
}
public class AccessoryArt {
 public static readonly string[] Ids={"none","cat","deer","bunny","crown","bow","star"};
 public static readonly string[] Names={"不戴装饰","小猫耳朵","小鹿角","兔耳朵","小皇冠","蝴蝶结","星星夹"};
 public string Error="";public Dictionary<string,DecorationSpec> Specs=new Dictionary<string,DecorationSpec>();
 Dictionary<string,BitmapSource> images=new Dictionary<string,BitmapSource>();
 public AccessoryArt(string folder){try{
  var atlas=new BitmapImage();atlas.BeginInit();atlas.UriSource=new Uri(Path.Combine(folder,"decorations-v1.png"));atlas.CacheOption=BitmapCacheOption.OnLoad;atlas.EndInit();atlas.Freeze();
  var specs=new JavaScriptSerializer().Deserialize<DecorationSpec[]>(File.ReadAllText(Path.Combine(folder,"decorations.json")));
  foreach(var item in specs){if(item.x<0 || item.y<0 || item.w<=0 || item.h<=0 || item.x+item.w>atlas.PixelWidth || item.y+item.h>atlas.PixelHeight || item.dw<=0 || item.dh<=0 || Specs.ContainsKey(item.id))throw new Exception("装饰图集范围无效。");var image=new CroppedBitmap(atlas,new Int32Rect(item.x,item.y,item.w,item.h));image.Freeze();Specs.Add(item.id,item);images.Add(item.id,image);}
  foreach(string id in Ids)if(id!="none" && !Specs.ContainsKey(id))throw new Exception("缺少装饰："+id);
 }catch(Exception ex){Error=ex.Message;Specs.Clear();images.Clear();}}
 public static bool CakeAnimal(Skin skin,string id){return skin!=null && (skin.id=="gold-cake" || skin.id=="dark-cake" || skin.id=="sweet-cake") && (id=="cat" || id=="deer" || id=="bunny");}
 public Rect Layout(Skin skin,string id,double scale){DecorationSpec item;if(!Specs.TryGetValue(id??"none",out item))return Rect.Empty;
  bool animal=id=="cat" || id=="deer" || id=="bunny",cake=CakeAnimal(skin,id);double x=item.dx,y=item.dy+(skin==null?0:skin.decorationOffsetY),width=item.dw,height=item.dh;
  if(cake){if(id=="cat"){x=36;y=24;width=328;height=164;}else if(id=="deer"){x=40;y=-7;width=320;height=188;}else{x=54;y=-12;width=292;height=215;}}
  double factor=Math.Max(.7,Math.Min(2,scale))*(animal?1:1.35),cx=x+width/2,bottom=y+height;width*=factor;height*=factor;
  if(animal){double fit=Math.Min(1,Math.Min(376/width,300/height));width*=fit;height*=fit;}
  x=cx-width/2;y=bottom-height;x=Math.Max(12,Math.Min(388-width,x));y=Math.Max(-70,Math.Min(420-height,y));return new Rect(x,y,width,height);
 }
 public void Draw(DrawingContext d,HeadProjection rig,string id,bool front,Skin skin=null,double scale=1){DecorationSpec item;BitmapSource image;if(!Specs.TryGetValue(id??"none",out item) || (CakeAnimal(skin,id)?true:item.front)!=front || !images.TryGetValue(id,out image))return;
  d.PushTransform(new MatrixTransform(rig.Tangent(new Point(200,45),.8,"hat")));d.DrawImage(image,Layout(skin,id,scale));d.Pop();
 }
}
}
