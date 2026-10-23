using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;
using System.Windows.Threading;
namespace HelenaPet {
public static partial class Program {
 static IEnumerable<DependencyObject> LogicalItems(DependencyObject node){yield return node;foreach(object child in LogicalTreeHelper.GetChildren(node)){var item=child as DependencyObject;if(item!=null)foreach(var result in LogicalItems(item))yield return result;}}
 static byte[] RenderPixels(FrameworkElement v){v.Measure(new Size(440,510));v.Arrange(new Rect(0,0,440,510));v.InvalidateVisual();v.UpdateLayout();var b=new RenderTargetBitmap(440,510,96,96,PixelFormats.Pbgra32);b.Render(v);var pixels=new byte[440*510*4];b.CopyPixels(pixels,440*4,0);return pixels;}
 static void RunPanelRestQA(PetWindow w){
  var log=new List<string>();var pref=w.Pref;var config=w.Interaction;string skinId=w.CurrentSkin;var rest=w.Rest;
  try{
   var defaults=new InteractionConfig();defaults.Validate();if(defaults.IdleSeconds!=180 || defaults.IdleMaxSeconds!=300 || defaults.WakeMinSeconds!=1200 || defaults.WakeMaxSeconds!=1800)throw new Exception("rest defaults");
   var cycle=new RestScheduler(761);double now=0;for(int i=0;i<200;i++){
    cycle.NoteActivity(now,defaults);double first=cycle.SleepAt;if(first<now+180 || first>now+300 || cycle.Advance(first-.001,false,true,false,defaults)!="")throw new Exception("early sleep");
    now=first-1;cycle.NoteActivity(now,defaults);if(cycle.SleepAt<now+180 || cycle.SleepAt>now+300 || cycle.Advance(first+.001,false,true,false,defaults)!="")throw new Exception("interaction did not reset idle");
    now=cycle.SleepAt;if(cycle.Advance(now,false,false,false,defaults)!="" || cycle.Advance(now,false,true,true,defaults)!="")throw new Exception("disabled/busy rest");
    if(cycle.Advance(now,false,true,false,defaults)!="sleep")throw new Exception("sleep transition");double wake=cycle.WakeAt;if(wake<now+1200 || wake>now+1800 || cycle.Advance(wake-.001,true,true,false,defaults)!="")throw new Exception("wake range");
    now=wake;if(cycle.Advance(now,true,true,false,defaults)!="wake" || cycle.SleepAt<now+180 || cycle.SleepAt>now+300 || cycle.Advance(now,false,true,false,defaults)!="")throw new Exception("wake/sleep cycle");
   }log.Add("PASS 200 simulated sleep/wake cycles: idle 180–300s, asleep 1200–1800s, user interaction resets idle, disabled/busy states do not trigger");
   w.Pref=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(pref));w.Pref.Dialogue=true;w.Pref.Auto=true;w.Interaction=defaults;w.Rest=new RestScheduler(21);w.Pet.Motion.Stop();w.SetMood("calm");w.Rest.NoteActivity(0,defaults);w.AdvanceRest(w.Rest.SleepAt+.01);if(w.State!="sleep" || w.Pet.Motion.Active)throw new Exception("native auto sleep");int count=w.History.Count;w.AdvanceRest(w.Rest.WakeAt+.01);if(w.State=="sleep" || w.History.Count!=Math.Min(40,count+1) || w.History.Last().@event!="wake" || !w.Bubble.View.StatusTitle.StartsWith("醒来看看你"))throw new Exception("native periodic wake");
   w.Talk("poetry");if(!w.Bubble.View.StatusTitle.Contains("聊聊诗歌") || w.Bubble.View.StatusTitle.Contains("同人"))throw new Exception("topic caption");w.SetMood("calm");if(!w.Bubble.View.StatusTitle.Contains("安静陪伴"))throw new Exception("caption state update");w.Act("listen");if(w.Bubble.View.StatusTitle!="侧耳倾听")throw new Exception("mood caption");
   w.Talk("poetry");w.Bubble.RevealAll();Capture(w.Bubble.View,Path.Combine(root,"preview","对话状态标题.png"),(int)w.Bubble.Width,(int)w.Bubble.Height);log.Add("PASS native auto-sleep/wake routing, wake dialogue, topic and live mood bubble titles; provenance retained in history");
   if(w.PanelSections.Count!=10 || w.PanelSections.Select(s=>(string)s.Tag).Distinct().Count()!=10)throw new Exception("panel section inventory");
   var buttons=LogicalItems(w.Panel).OfType<Button>().Select(b=>b.Content as string).Where(v=>v!=null).ToList();string[] required={"投喂一份","一起读书","探杖听回声","收起道具","打个招呼","聊聊诗歌","聊聊甜食","关于写作","陪我一会儿","活动原句","查看对话记录与出处","部位、台词、动作与作息设置","试读一句","打开皮肤文件夹","刷新衣橱","找回桌宠","回到桌宠","退出"};
   foreach(string name in required.Concat(PetWindow.Names))if(!buttons.Contains(name))throw new Exception("Panel feature missing: "+name);
   w.SetAllSections(true);if(w.PanelSections.Any(s=>!s.IsExpanded))throw new Exception("expand all");w.SetAllSections(false);if(w.PanelSections.Any(s=>s.IsExpanded))throw new Exception("collapse all");
   w.PanelSections.Single(s=>(string)s.Tag=="chat").IsExpanded=true;Capture((FrameworkElement)w.Panel.Content,Path.Combine(root,"preview","听见你-折叠主界面.png"),554,790);
   w.PanelSections.Single(s=>(string)s.Tag=="chat").IsExpanded=false;w.PanelSections.Single(s=>(string)s.Tag=="wardrobe").IsExpanded=true;w.PanelSections.Single(s=>(string)s.Tag=="hat-decor").IsExpanded=true;w.PanelSections.Single(s=>(string)s.Tag=="pendants").IsExpanded=true;Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Render,new Action(()=>{}));
   var scroll=(ScrollViewer)w.Panel.Content;var stack=(FrameworkElement)scroll.Content;scroll.Content=null;stack.InvalidateMeasure();stack.InvalidateArrange();stack.Measure(new Size(520,double.PositiveInfinity));int height=(int)Math.Ceiling(stack.DesiredSize.Height);var sheet=new Border{Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5"),Child=stack};Capture(sheet,Path.Combine(root,"preview","听见你-衣橱展开.png"),520,height);sheet.Child=null;scroll.Content=stack;w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="chat").IsExpanded=true;
   w.Pref.DecorationSize=1.8;w.Pref.PendantSize=2.2;var roundtrip=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(w.Pref));if(roundtrip.DecorationSize!=1.8 || roundtrip.PendantSize!=2.2 || !roundtrip.ExpandedSections.SequenceEqual(w.Pref.ExpandedSections))throw new Exception("new prefs round trip");
   log.Add("PASS ten collapsible sections retain all 16 moods and existing action/chat/settings/wardrobe controls; expand/collapse and preference round trip");
   string[] cakes={"gold-cake","dark-cake","sweet-cake"},animals={"cat","deer","bunny"};int combinations=0;
   var gallery=new Canvas{Width=1200,Height=1290,Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5")};var title=new TextBlock{Text="蛋糕高帽 · 三种动物耳朵",FontSize=29,FontFamily=new FontFamily("Microsoft YaHei UI"),Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(title,25);Canvas.SetTop(title,18);gallery.Children.Add(title);
   for(int row=0;row<3;row++)for(int col=0;col<3;col++){var skin=Skin.Load(Path.Combine(root,"skins",cakes[row]));string animal=animals[col];if(!AccessoryArt.CakeAnimal(skin,animal))throw new Exception("cake compatibility");
    foreach(double size in new[]{1.0,2.0})foreach(int direction in new[]{-1,0,1}){var pet=new PetVisual{Skin=skin,Accessories=w.Pet.Accessories,Decoration="none",DecorationSize=size,X=direction,Y=direction*.8,Time=2.2,Turn=14,Sway=2};var before=RenderPixels(pet);pet.Decoration=animal;var after=RenderPixels(pet);int changed=0;for(int k=0;k<after.Length;k+=4)if(Math.Abs(after[k]-before[k])+Math.Abs(after[k+1]-before[k+1])+Math.Abs(after[k+2]-before[k+2])>30)changed++;if(changed<800)throw new Exception("Cake ears hidden: "+cakes[row]+"/"+animal+" "+changed);combinations++;}
    var card=new Canvas{Width=374,Height=391,Background=(Brush)new BrushConverter().ConvertFromString("#FFFCF6"),ClipToBounds=true};Canvas.SetLeft(card,25+col*389);Canvas.SetTop(card,78+row*402);gallery.Children.Add(card);var label=new TextBlock{Text=skin.name+" · "+AccessoryArt.Names[Array.IndexOf(AccessoryArt.Ids,animal)],FontSize=19,Foreground=(Brush)new BrushConverter().ConvertFromString("#36556D")};Canvas.SetLeft(label,14);Canvas.SetTop(label,10);card.Children.Add(label);var preview=new PetVisual{Skin=skin,Accessories=w.Pet.Accessories,Decoration=animal,DecorationSize=1,Time=2.2,Sway=.2};var view=new Viewbox{Child=preview,Width=374,Height=338};Canvas.SetTop(view,40);card.Children.Add(view);
   }Capture(gallery,Path.Combine(root,"preview","蛋糕皮动物耳朵兼容.png"),1200,1290);log.Add("PASS "+combinations+" cake/animal/size/direction combinations visibly differ from undecorated rendering; animal ears use front placement on all three cake hats");
   foreach(string id in new[]{"crown","bow","star"}){var item=w.Pet.Accessories.Specs[id];Rect normal=w.Pet.Accessories.Layout(w.Pet.Skin,id,1),large=w.Pet.Accessories.Layout(w.Pet.Skin,id,2);if(normal.Width<=item.dw || large.Width<=normal.Width)throw new Exception("front decoration scale: "+id);}
   int orbitChecks=0;foreach(string id in PendantArt.Ids.Skip(1))for(int i=0;i<16;i++){var pet=new PetVisual{Skin=w.Pet.Skin,Pendants=w.Pet.Pendants,Pendant=id,PendantSize=2.2,Time=i*1.1,Sway=0};RenderPixels(pet);Rect rect=pet.PendantBounds;if(rect.IsEmpty)throw new Exception("large pendant missing");double angle=Math.Sin(pet.Time*.8)*5;var transform=new RotateTransform(angle,rect.X+rect.Width/2,rect.Y+rect.Height/2);Rect bounds=transform.TransformBounds(rect);if(bounds.Left<-.1 || bounds.Right>440.1 || bounds.Top<0 || bounds.Bottom>510)throw new Exception("large pendant clipped");orbitChecks++;}
   log.Add("PASS crown/bow/star default growth and size slider; "+orbitChecks+" full-orbit samples at 2.2× remain within canvas");
  }finally{w.Pref=pref;w.Interaction=config;w.Rest=rest;w.SetSkin(skinId);w.Pet.DecorationSize=pref.DecorationSize;w.SetMood("calm");}
  File.WriteAllLines(Path.Combine(root,"panel-rest-qa-results.txt"),log);
 }
}
}

