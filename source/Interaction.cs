using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HelenaPet {
public class TouchRule { public string Part,Mood,Action; public bool Enabled=true; }
public class InteractionConfig {
 public bool Parts=true,PixelThrough=true,KeepActionOnChat=true,AutoPage=true;
 public double EatSeconds=12,ReadSeconds=20,CaneSeconds=10,PageSeconds=5.8,IdleSeconds=180,MoodSeconds=6,IdleMaxSeconds=300,WakeMinSeconds=1200,WakeMaxSeconds=1800;public int RestSchema=1;
 public TouchRule[] Rules={new TouchRule{Part="hat",Mood="shy",Action="none"},new TouchRule{Part="glasses",Mood="curious",Action="none"},new TouchRule{Part="cheek",Mood="happy",Action="none"},new TouchRule{Part="hair",Mood="peace",Action="none"}};
 public void Validate(){
  double[] values={EatSeconds,ReadSeconds,CaneSeconds,PageSeconds,IdleSeconds,MoodSeconds};double[] min={4,4,4,1,10,1},max={120,180,120,30,1800,60};
  for(int i=0;i<values.Length;i++)if(double.IsNaN(values[i]) || double.IsInfinity(values[i]) || values[i]<min[i] || values[i]>max[i])throw new Exception("时间参数超出范围。");
  if(double.IsNaN(IdleMaxSeconds) || double.IsInfinity(IdleMaxSeconds) || IdleMaxSeconds<IdleSeconds || IdleMaxSeconds>3600 || double.IsNaN(WakeMinSeconds) || double.IsInfinity(WakeMinSeconds) || WakeMinSeconds<60 || WakeMinSeconds>86400 || double.IsNaN(WakeMaxSeconds) || double.IsInfinity(WakeMaxSeconds) || WakeMaxSeconds<WakeMinSeconds || WakeMaxSeconds>86400)throw new Exception("作息时间范围无效。");
  if(Rules==null || Rules.Length!=4 || Rules.Select(v=>v.Part).Distinct().Count()!=4)throw new Exception("互动部位配置不完整。");
  foreach(var r in Rules)if(!new[]{"hat","glasses","cheek","hair"}.Contains(r.Part) || !PetWindow.States.Contains(r.Mood) || !new[]{"none","eat","read","cane"}.Contains(r.Action))throw new Exception("互动规则无效。");
 }
 public static InteractionConfig Read(string path){try{var c=new JavaScriptSerializer().Deserialize<InteractionConfig>(File.ReadAllText(path));if(!File.ReadAllText(path).Contains("RestSchema")){c.IdleSeconds=180;c.IdleMaxSeconds=300;c.WakeMinSeconds=1200;c.WakeMaxSeconds=1800;c.RestSchema=1;}if(c.Rules!=null)foreach(var rule in c.Rules)if(rule!=null && rule.Mood=="pout")rule.Mood="happy";c.Validate();return c;}catch{return new InteractionConfig();}}
}
public static class SafeJson {
 public static void Write(string path,object value){string temp=path+".new";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(value),new System.Text.UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
}
public partial class PetVisual {
 Matrix headToCanvas=Matrix.Identity;HeadProjection hitRig=new HeadProjection();
 public Point PartPoint(string part){Point p=part=="hat"?new Point(200,150):part=="glasses"?new Point(200,285):part=="cheek"?new Point(260,350):new Point(68,361);return headToCanvas.Transform(hitRig.Project(p,part=="hat"?.8:part=="glasses"?1:.65,part=="hat"?"hat":part=="glasses"?"glasses":"face"));}
 public string HitPart(Point canvas){if(!headToCanvas.HasInverse)return "";var inverse=headToCanvas;inverse.Invert();Point projected=inverse.Transform(canvas);
  var g=hitRig.Tangent(new Point(200,285),1,"glasses");g.Invert();Point p=g.Transform(projected);if(new Rect(79,241,244,91).Contains(p))return "glasses";
  var f=hitRig.Tangent(new Point(200,340),.65,"face");f.Invert();p=f.Transform(projected);if((Math.Pow((p.X-128)/38,2)+Math.Pow((p.Y-350)/26,2)<1) || (Math.Pow((p.X-272)/38,2)+Math.Pow((p.Y-350)/26,2)<1))return "cheek";
  var h=hitRig.Tangent(new Point(200,150),.8,"hat");h.Invert();p=h.Transform(projected);if(new Rect(29,45,345,188).Contains(p))return Skin!=null && !Skin.hasHeadwear?"hair":"hat";
  p=f.Transform(projected);if(new Rect(36,210,47,180).Contains(p) || new Rect(322,210,49,180).Contains(p) || new Rect(70,384,260,29).Contains(p))return "hair";return "";
 }
 void StoreHitPose(HeadProjection rig,double tilt,double fit,double t,double bob,double envelope){hitRig=rig;var transforms=new TransformGroup();transforms.Children.Add(new ScaleTransform((1+Math.Sin(t*1.8)*.006)*fit,(1-Math.Sin(t*1.8)*.004)*fit,200,235));transforms.Children.Add(new RotateTransform(tilt,200,235));transforms.Children.Add(new TranslateTransform(20,93+bob));transforms.Children.Add(new ScaleTransform(1-.16*envelope,1-.16*envelope,220,270));transforms.Children.Add(new TranslateTransform(0,-24*envelope));if(IsHopping)transforms.Children.Add(new TranslateTransform(0,-HopHeight));else if(IsEchoRelease)transforms.Children.Add(new TranslateTransform(0,-ChargedJumpHeight));headToCanvas=transforms.Value;}
}
public partial class PetWindow {
 public InteractionConfig Interaction;public Window InteractionPanel;public string PendingPart="";
 IntPtr petHandle;byte[] hitPixels;RenderTargetBitmap hitBitmap;double hitMaskAt=-99;bool isPassing;public bool IsPassing{get{return isPassing;}}
 [DllImport("user32.dll",EntryPoint="GetWindowLong")]static extern int GetStyle(IntPtr h,int n);
 [DllImport("user32.dll",EntryPoint="SetWindowLong")]static extern int SetStyle(IntPtr h,int n,int value);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(POINT p);
 public IntPtr NativeHandle{get{return petHandle;}}
 public IntPtr NativeWindowAt(Point screen){return WindowFromPoint(new POINT{X=(int)screen.X,Y=(int)screen.Y});}
 void InitInteraction(){Interaction=InteractionConfig.Read(Path.Combine(Root,"interaction.json"));if(!Testing && File.Exists(Path.Combine(Root,"interaction.json")) && !File.ReadAllText(Path.Combine(Root,"interaction.json")).Contains("RestSchema"))SafeJson.Write(Path.Combine(Root,"interaction.json"),Interaction);SourceInitialized+=(s,e)=>petHandle=new WindowInteropHelper(this).Handle;Closed+=(s,e)=>{if(InteractionPanel!=null)InteractionPanel.Close();};}
 public void RefreshHitMask(){if(!Pet.IsArrangeValid)Pet.UpdateLayout();if(hitBitmap==null)hitBitmap=new RenderTargetBitmap(440,510,96,96,PixelFormats.Pbgra32);var bitmap=hitBitmap;bitmap.Clear();bitmap.Render(Pet);if(hitPixels==null)hitPixels=new byte[440*510*4];bitmap.CopyPixels(hitPixels,440*4,0);hitMaskAt=Clock.Elapsed.TotalSeconds;}
 public bool OpaqueAt(Point p){if(p.X<0 || p.Y<0 || p.X>=440 || p.Y>=510 || hitPixels==null)return false;int x=(int)p.X,y=(int)p.Y;return hitPixels[(y*440+x)*4+3]>=20;}
 public void UpdateClickThrough(Point? testPoint){if(petHandle==IntPtr.Zero)return;bool passing=false;if(Interaction.PixelThrough && !down && !rightDown){if(hitPixels==null || Clock.Elapsed.TotalSeconds-hitMaskAt>.1)RefreshHitMask();POINT cursor;if(testPoint.HasValue)passing=!OpaqueAt(testPoint.Value);else if(GetCursorPos(out cursor))passing=!OpaqueAt(Pet.PointFromScreen(new Point(cursor.X,cursor.Y)));}
  if(passing!=isPassing){int style=GetStyle(petHandle,-20);SetStyle(petHandle,-20,passing?style|0x20:style&~0x20);isPassing=passing;}
 }
 public bool TouchPart(string part){NoteEngagement();if(!Interaction.Parts)return false;var rule=Interaction.Rules.FirstOrDefault(v=>v.Part==part);if(rule==null || !rule.Enabled)return false;
  if(State=="sleep"){Talk("wake");return true;}if(rule.Action!="none")StartAction(rule.Action);LastTrigger="touch-"+part;var line=Dialogues.Pick(LastTrigger);SetMood(rule.Mood);ShowLine(line,5);return true;
 }
 public void SaveDialogue(List<DialogueLine> lines){
  var json=new JavaScriptSerializer();foreach(var official in Dialogues.Lines.Where(v=>v.kind=="official")){var match=lines.FirstOrDefault(v=>v.id==official.id);if(match==null || json.Serialize(match)!=json.Serialize(official))throw new Exception("活动原文与出处保留不变，请添加日常台词。");}
  string path=Path.Combine(Root,"dialogue.json"),temp=Path.Combine(Root,"dialogue-check.json");try{File.WriteAllText(temp,json.Serialize(lines));var bank=new DialogueBank(temp);if(bank.Error!="")throw new Exception(bank.Error);SafeJson.Write(path,lines);Dialogues=new DialogueBank(path);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 TextBox Entry(string text){return new TextBox{Text=text,FontSize=18,Padding=new Thickness(8),Margin=new Thickness(0,3,0,12)};}
 CheckBox Toggle(StackPanel panel,string title,bool value){var b=new CheckBox{Content=title,IsChecked=value,Margin=new Thickness(0,4,0,14)};panel.Children.Add(b);return b;}
 public void OpenInteractionSettings(){if(InteractionPanel!=null){InteractionPanel.Activate();return;}var page=new Window{Title="海伦娜 · 互动设置",Width=700,Height=820,MinWidth=600,MinHeight=600,FontSize=18,FontFamily=new FontFamily("Microsoft YaHei UI"),Background=B("#EDF2F5"),WindowStartupLocation=WindowStartupLocation.CenterScreen};InteractionPanel=page;AttachEngagement(page);page.Closed+=(s,e)=>InteractionPanel=null;
  var tabs=new TabControl{Margin=new Thickness(18)};page.Content=tabs;var config=new StackPanel{Margin=new Thickness(18)};tabs.Items.Add(new TabItem{Header="部位与动作",Content=new ScrollViewer{Content=config,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});
  config.Children.Add(Txt("点点她的帽子、眼镜、脸颊和卷发",23,"#36556D"));config.Children.Add(Txt("单击触发；拖动仍可搬家，双击仍可轮换心情。",17,"#7F7268"));
  var parts=Toggle(config,"启用部位互动",Interaction.Parts);var through=Toggle(config,"透明空白处允许点击桌面",Interaction.PixelThrough);var keep=Toggle(config,"聊天和摸摸时继续当前道具动作",Interaction.KeepActionOnChat);var autoPage=Toggle(config,"读书时自动翻页",Interaction.AutoPage);
  string[] ids={"hat","glasses","cheek","hair"},names={"帽子","眼镜","脸颊","卷发"};var ruleChecks=new List<CheckBox>();var moodBoxes=new List<ComboBox>();var actionBoxes=new List<ComboBox>();
  for(int i=0;i<4;i++){var rule=Interaction.Rules.First(v=>v.Part==ids[i]);config.Children.Add(Txt(names[i],19,"#36556D"));ruleChecks.Add(Toggle(config,"启用这个部位",rule.Enabled));var mood=new ComboBox{ItemsSource=Names,SelectedIndex=Array.IndexOf(States,rule.Mood),Padding=new Thickness(7),Margin=new Thickness(0,0,0,10)};var action=new ComboBox{ItemsSource=new[]{"只做表情","吃当前选中的甜点","一起读书","使用盲杖"},SelectedIndex=Array.IndexOf(new[]{"none","eat","read","cane"},rule.Action),Padding=new Thickness(7),Margin=new Thickness(0,0,0,18)};config.Children.Add(mood);config.Children.Add(action);moodBoxes.Add(mood);actionBoxes.Add(action);}
  string[] labels={"吃甜点持续秒数（4–120）","读书持续秒数（4–180）","盲杖持续秒数（4–120）","自动翻页间隔（1–30 秒）","最早入睡（秒，默认 180）","表情持续秒数（1–60）","最晚入睡（秒，默认 300）","睡后最早醒来（秒，默认 1200）","睡后最晚醒来（秒，默认 1800）"};double[] values={Interaction.EatSeconds,Interaction.ReadSeconds,Interaction.CaneSeconds,Interaction.PageSeconds,Interaction.IdleSeconds,Interaction.MoodSeconds,Interaction.IdleMaxSeconds,Interaction.WakeMinSeconds,Interaction.WakeMaxSeconds};var fields=new List<TextBox>();for(int i=0;i<labels.Length;i++){config.Children.Add(Txt(labels[i],17,"#36556D"));var box=Entry(values[i].ToString(CultureInfo.InvariantCulture));config.Children.Add(box);fields.Add(box);}
  var status=Txt("设置保存后生效；新的时长从下次动作开始应用。",17,"#7F7268");config.Children.Add(status);config.Children.Add(Btn("保存互动设置",()=>{try{var v=fields.Select(f=>double.Parse(f.Text,CultureInfo.InvariantCulture)).ToArray();var c=new InteractionConfig{Parts=parts.IsChecked==true,PixelThrough=through.IsChecked==true,KeepActionOnChat=keep.IsChecked==true,AutoPage=autoPage.IsChecked==true,EatSeconds=v[0],ReadSeconds=v[1],CaneSeconds=v[2],PageSeconds=v[3],IdleSeconds=v[4],MoodSeconds=v[5],IdleMaxSeconds=v[6],WakeMinSeconds=v[7],WakeMaxSeconds=v[8]};for(int i=0;i<4;i++)c.Rules[i]=new TouchRule{Part=ids[i],Enabled=ruleChecks[i].IsChecked==true,Mood=States[moodBoxes[i].SelectedIndex],Action=new[]{"none","eat","read","cane"}[actionBoxes[i].SelectedIndex]};c.Validate();SafeJson.Write(Path.Combine(Root,"interaction.json"),c);Interaction=c;ResetRestSchedule();Pet.Motion.AutoPage=c.AutoPage;Pet.Motion.PageInterval=c.PageSeconds;UpdateClickThrough(null);status.Text="已保存。上一次设置保留在 interaction.json.bak。";}catch(Exception ex){status.Text="未保存："+ex.Message;}}));
  var dialogue=new StackPanel{Margin=new Thickness(18)};tabs.Items.Add(new TabItem{Header="台词编辑",Content=new ScrollViewer{Content=dialogue,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});dialogue.Children.Add(Txt("为不同互动添加自己的句子",23,"#36556D"));dialogue.Children.Add(Txt("活动原文可查看；日常台词可编辑、添加或删除。每句最多 180 字。保存前会保留上一版备份。",17,"#7F7268"));
  var list=new ListBox{Height=210,FontSize=16,Margin=new Thickness(0,0,0,12)};dialogue.Children.Add(list);var trigger=Entry("click");var text=Entry("");text.AcceptsReturn=true;text.TextWrapping=TextWrapping.Wrap;text.Height=115;var moodEdit=new ComboBox{ItemsSource=Names,SelectedIndex=0,Padding=new Thickness(7),Margin=new Thickness(0,0,0,12)};var source=Txt("",16,"#7F7268");
  dialogue.Children.Add(Txt("触发事件（例如 click、touch-hat、touch-cheek、page）",16,"#36556D"));dialogue.Children.Add(trigger);dialogue.Children.Add(text);dialogue.Children.Add(moodEdit);dialogue.Children.Add(source);var message=Txt("",17,"#7F7268");DialogueLine selected=null;
  Action refresh=()=>{list.Items.Clear();foreach(var line in Dialogues.Lines)list.Items.Add(new ListBoxItem{Content=(line.kind=="official"?"原文 · ":"")+line.text,Tag=line});};refresh();
  list.SelectionChanged+=(s,e)=>{var item=list.SelectedItem as ListBoxItem;selected=item==null?null:item.Tag as DialogueLine;if(selected==null)return;text.Text=selected.text;trigger.Text=selected.@event;moodEdit.SelectedIndex=Array.IndexOf(States,selected.mood);bool locked=selected.kind=="official";text.IsReadOnly=trigger.IsReadOnly=locked;moodEdit.IsEnabled=!locked;source.Text=selected.Label+" · "+selected.source;};
  dialogue.Children.Add(Btn("新增一句",()=>{list.SelectedItem=null;selected=null;text.Text="";trigger.Text="click";moodEdit.SelectedIndex=0;text.IsReadOnly=trigger.IsReadOnly=false;moodEdit.IsEnabled=true;source.Text="自己的日常台词";}));
  var buttons=new WrapPanel();buttons.Children.Add(Btn("保存这句",()=>{try{if(selected!=null && selected.kind=="official")throw new Exception("活动原文只供查看。");var lines=new JavaScriptSerializer().Deserialize<List<DialogueLine>>(new JavaScriptSerializer().Serialize(Dialogues.Lines));string id=selected==null?"custom-"+Guid.NewGuid().ToString("N"):selected.id;lines.RemoveAll(l=>l.id==id);lines.Add(new DialogueLine{id=id,@event=trigger.Text.Trim(),text=text.Text.Trim(),mood=States[moodEdit.SelectedIndex],kind="fan",source="用户自定义"});SaveDialogue(lines);refresh();message.Text="已保存并立即载入；上一版在 dialogue.json.bak。";}catch(Exception ex){message.Text="未保存："+ex.Message;}}));buttons.Children.Add(Btn("试读",()=>{if(string.IsNullOrWhiteSpace(text.Text) || text.Text.Length>180){message.Text="请填写 1–180 字。";return;}SetMood(States[moodEdit.SelectedIndex]);ShowLine(new DialogueLine{id="preview",text=text.Text,mood=State,kind=selected==null?"fan":selected.kind,source="设置试读"},8);}));buttons.Children.Add(Btn("删除这句",()=>{try{if(selected==null || selected.kind=="official")throw new Exception("请选择一条日常台词。");var lines=Dialogues.Lines.Where(l=>l.id!=selected.id).ToList();SaveDialogue(lines);refresh();message.Text="已删除；上一版保留在备份中。";}catch(Exception ex){message.Text=ex.Message;}}));dialogue.Children.Add(buttons);dialogue.Children.Add(message);page.Show();
 }
}
}
