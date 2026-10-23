using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HelenaPet {
public class DialogueLine {
 public string id,@event,text,mood,kind,source,reference;
 public string Label { get { return kind=="official"?"演绎之星 · 原文":"日常互动 · 同人创作"; } }
}
public class DialogueBank {
 public List<DialogueLine> Lines; readonly Queue<string> recent=new Queue<string>();readonly Random random=new Random();
 public string Error="";
 public DialogueBank(string path) {
  try{
   Lines=new JavaScriptSerializer().Deserialize<List<DialogueLine>>(File.ReadAllText(path));
   if(Lines==null || Lines.Count<1 || Lines.Any(v=>v==null || string.IsNullOrWhiteSpace(v.id) || string.IsNullOrWhiteSpace(v.text) || v.text.Length>180 || string.IsNullOrWhiteSpace(v.@event) || !PetWindow.States.Contains(v.mood) || (v.kind!="official" && v.kind!="fan")) || Lines.Select(v=>v.id).Distinct().Count()!=Lines.Count)throw new Exception("条目缺少字段、重复，或超出字数限制。");
  }catch(Exception ex){Error="台词文件未加载："+ex.Message;Lines=new List<DialogueLine>{new DialogueLine{id="fallback",@event="click",text="嗯？我在听。你可以慢慢说。",mood="listen",kind="fan",source="内置备用台词"}};}
 }
 public DialogueLine Pick(string trigger) {
  var group=Lines.Where(v=>v.@event==trigger).ToList();if(group.Count==0)group=Lines.Where(v=>v.kind=="fan" && v.@event=="click").ToList();if(group.Count==0)group=Lines;
  var candidates=group.Where(v=>!recent.Contains(v.id)).ToList();
  if(candidates.Count==0){string last=recent.LastOrDefault();candidates=group.Where(v=>v.id!=last).ToList();if(candidates.Count==0)candidates=group;}
  var selected=candidates[random.Next(candidates.Count)];recent.Enqueue(selected.id);while(recent.Count>5)recent.Dequeue();return selected;
 }
}
public class BubbleVisual:FrameworkElement {
 public DialogueLine Line;public string StatusTitle="安静陪伴";public int Revealed;public double FontSize=20;public bool Typewriter=true;
 static Brush B(string hex){return (Brush)new BrushConverter().ConvertFromString(hex);}
 FormattedText Text(string text,double size,string color){return new FormattedText(text,System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,B(color));}
 public double RequiredHeight(double width){if(Line==null)return 120;var f=Text(Line.text,FontSize,"#36556D");f.MaxTextWidth=Math.Max(80,width-44);return Math.Ceiling(f.Height)+84;}
 public bool Finished{get{return Line==null || Revealed>=Line.text.Length;}}
 protected override void OnRender(DrawingContext d){if(Line==null)return;double width=ActualWidth;
  d.DrawRoundedRectangle(B("#FFF7EB"),new Pen(B("#90AABE"),1.6),new Rect(1,1,width-2,ActualHeight-11),18,18);
  d.DrawGeometry(B("#FFF7EB"),new Pen(B("#90AABE"),1),Geometry.Parse("M"+(width/2-10)+","+(ActualHeight-11)+" L"+(width/2)+","+(ActualHeight-1)+" L"+(width/2+10)+","+(ActualHeight-11)));
  d.DrawRoundedRectangle(B("#A8BFCE"),null,new Rect(7,20,4,Math.Max(12,ActualHeight-48)),2,2);
  d.DrawRoundedRectangle(B("#8AA9BD"),new Pen(B("#687F91"),1),new Rect(width-30,14,9,17),2,2);d.DrawLine(new Pen(B("#DBC9AE"),1.5),new Point(width-27,16),new Point(width-27,29));
  var label=Text(StatusTitle,14,"#8A716B");d.DrawText(label,new Point(22,13));
  var visible=Text(Line.text.Substring(0,Math.Min(Revealed,Line.text.Length)),FontSize,"#36556D");visible.MaxTextWidth=width-44;d.DrawText(visible,new Point(22,40));
 }
}
public class BubbleWindow:Window {
 public BubbleVisual View=new BubbleVisual();public double Started,Until;public bool Typing=true;
 public BubbleWindow(){WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Focusable=false;Content=View;
  MouseLeftButtonUp+=(s,e)=>{var pet=Owner as PetWindow;if(pet!=null)pet.NoteEngagement();if(!View.Finished)RevealAll();else Hide();e.Handled=true;};MouseRightButtonUp+=(s,e)=>{Hide();e.Handled=true;};
 }
 public void Display(DialogueLine line,double now,double font,bool typing,double duration,string title=null){View.StatusTitle=title??PetWindow.StatusCaption(line.@event,line.mood);View.Line=line;View.FontSize=font;View.Revealed=typing?0:line.text.Length;Started=now;Typing=typing;Until=now+Math.Max(duration,Math.Min(24,4+line.text.Length/4.2));Width=font>=25?390:350;Height=View.RequiredHeight(Width);View.Width=Width;View.Height=Height;View.InvalidateVisual();}
 public void RevealAll(){if(View.Line!=null){View.Revealed=View.Line.text.Length;View.InvalidateVisual();}}
 public void Tick(double now){if(!IsVisible)return;if(now>Until){Hide();return;}if(View.Line!=null && !View.Finished){View.Revealed=Typing?Math.Min(View.Line.text.Length,(int)((now-Started)*30)):View.Line.text.Length;View.InvalidateVisual();}}
 public void Follow(PetWindow pet){if(!IsVisible)return;Topmost=pet.Topmost;var source=PresentationSource.FromVisual(pet);if(source==null)return;
  var physical=pet.PointToScreen(new Point(pet.Width/2,pet.Height/2));var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)physical.X,(int)physical.Y));var inv=source.CompositionTarget.TransformFromDevice;
  Point a=inv.Transform(new Point(screen.WorkingArea.Left,screen.WorkingArea.Top)),b=inv.Transform(new Point(screen.WorkingArea.Right,screen.WorkingArea.Bottom));
  double x=pet.Left+pet.Width/2-Width/2,y=pet.Top+110*pet.Pref.Scale-Height-6;
  if(y<a.Y+8)y=pet.Top+pet.Height-12;
  Left=Math.Max(a.X+8,Math.Min(b.X-Width-8,x));Top=Math.Max(a.Y+8,Math.Min(b.Y-Height-8,y));
 }
}
}
