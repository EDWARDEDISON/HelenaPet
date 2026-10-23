using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;

namespace HelenaPet {
// File-format export of the existing native layers. This does not produce a Cubism model.
public static class Live2DPrep {
 const int W=440,H=510;
 static void I16(BinaryWriter b,int value){b.Write((byte)(value>>8));b.Write((byte)value);}
 static void I32(BinaryWriter b,int value){b.Write((byte)(value>>24));b.Write((byte)(value>>16));b.Write((byte)(value>>8));b.Write((byte)value);}
 static void Tag(BinaryWriter b,string text){b.Write(Encoding.ASCII.GetBytes(text));}
 static BitmapSource Render(Action<DrawingContext> draw,bool place=true){var visual=new DrawingVisual();using(var d=visual.RenderOpen()){if(place)d.PushTransform(new TranslateTransform(20,50));draw(d);if(place)d.Pop();}var bitmap=new RenderTargetBitmap(W,H,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();return bitmap;}
 static byte[][] Planes(BitmapSource bitmap){var rgba=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);var pixels=new byte[W*H*4];rgba.CopyPixels(pixels,W*4,0);var planes=new[]{new byte[W*H],new byte[W*H],new byte[W*H],new byte[W*H]};for(int i=0;i<W*H;i++){planes[0][i]=pixels[i*4+2];planes[1][i]=pixels[i*4+1];planes[2][i]=pixels[i*4];planes[3][i]=pixels[i*4+3];}return planes;}
 static void Png(BitmapSource image,string path){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var f=File.Create(path))encoder.Save(f);}
 public static void Export(Skin skin,string folder){Directory.CreateDirectory(folder);var names=new List<string>();var images=new List<BitmapSource>();foreach(var l in skin.layers){var layer=l;names.Add(layer.name);images.Add(Render(d=>{if(layer.ClipShape!=null)d.PushClip(layer.ClipShape);d.DrawRectangle(Skin.LayerBrush(layer),null,new Rect(layer.x,layer.y,layer.w,layer.h));if(layer.ClipShape!=null)d.Pop();}));}
  names.Add("mouth");images.Add(Render(d=>{d.PushTransform(new TranslateTransform(skin.mouthX,skin.mouthY));d.DrawGeometry(null,new Pen(new SolidColorBrush(Color.FromRgb(135,93,99)),2.1),Geometry.Parse("M-9,0 Q0,7 9,0"));d.Pop();}));
  if(skin.id=="sound-wave"){names.Add("headset-microphone");images.Add(Render(d=>PetVisual.DrawHeadsetMic(d,new HeadProjection())));}
  for(int i=0;i<images.Count;i++)Png(images[i],Path.Combine(folder,names[i]+".png"));
  if(skin.poutFace!=null){var layer=skin.poutFace;Png(Render(d=>{if(layer.ClipShape!=null)d.PushClip(layer.ClipShape);d.DrawRectangle(Skin.LayerBrush(layer),null,new Rect(layer.x,layer.y,layer.w,layer.h));if(layer.ClipShape!=null)d.Pop();}),Path.Combine(folder,"face-pout-alternative.png"));}
  var combined=Render(d=>{foreach(var img in images)d.DrawImage(img,new Rect(0,0,W,H));},false);Png(combined,Path.Combine(folder,"layer-composite.png"));
  var planes=images.Select(Planes).ToArray();using(var records=new MemoryStream())using(var record=new BinaryWriter(records)){
   I16(record,-images.Count);for(int i=images.Count-1;i>=0;i--){I32(record,0);I32(record,0);I32(record,H);I32(record,W);I16(record,4);foreach(int channel in new[]{0,1,2,-1}){I16(record,channel);I32(record,2+W*H);}Tag(record,"8BIM");Tag(record,"norm");record.Write((byte)255);record.Write((byte)0);record.Write((byte)0);record.Write((byte)0);byte[] name=Encoding.ASCII.GetBytes(names[i]);int padded=((name.Length+1+3)/4)*4;I32(record,8+padded);I32(record,0);I32(record,0);record.Write((byte)name.Length);record.Write(name);for(int j=name.Length+1;j<padded;j++)record.Write((byte)0);}
   for(int i=images.Count-1;i>=0;i--)foreach(var plane in planes[i]){I16(record,0);record.Write(plane);}if(records.Length%2!=0)record.Write((byte)0);
   using(var f=File.Create(Path.Combine(folder,"helena-layered.psd")))using(var b=new BinaryWriter(f)){Tag(b,"8BPS");I16(b,1);b.Write(new byte[6]);I16(b,4);I32(b,H);I32(b,W);I16(b,8);I16(b,3);I32(b,0);I32(b,0);I32(b,4+(int)records.Length+4);I32(b,(int)records.Length);b.Write(records.ToArray());I32(b,0);I16(b,0);foreach(var plane in Planes(combined))b.Write(plane);}
  }
  SafeJson.Write(Path.Combine(folder,"source-manifest.json"),new{sourceSkin=skin.id,width=W,height=H,offsetX=20,offsetY=50,layers=names.ToArray(),formalCubismModel=false,missing=new[]{"Editor mesh/deformer/parameter binding","moc3/model3.json export","Cubism Core integration"}});
 }
}
public static partial class Program {
 static void RunLive2DPrep(PetWindow w){string folder=Path.Combine(root,"Live2D准备");int count=0;foreach(var directory in Directory.GetDirectories(Path.Combine(root,"skins"))){var skin=Skin.Load(directory);Live2DPrep.Export(skin,Path.Combine(folder,Path.GetFileName(directory)));count++;}File.WriteAllText(Path.Combine(root,"live2d-prep-results.txt"),"PASS "+count+" aligned layered PSD/PNG sets exported from existing native layers.\r\nNOT a Cubism model; no moc3 or formal runtime claimed.\r\n");}
}
}
