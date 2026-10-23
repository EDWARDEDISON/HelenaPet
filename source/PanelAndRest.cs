using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace HelenaPet {
public class RestScheduler {
 readonly Random random;
 public double SleepAt=double.PositiveInfinity,WakeAt=double.PositiveInfinity;
 public RestScheduler():this(Environment.TickCount){}
 public RestScheduler(int seed){random=new Random(seed);}
 double Between(double min,double max){return min+(max-min)*random.NextDouble();}
 public void NoteActivity(double now,InteractionConfig c){SleepAt=now+Between(c.IdleSeconds,c.IdleMaxSeconds);}
 public void EnterSleep(double now,InteractionConfig c){SleepAt=double.PositiveInfinity;WakeAt=now+Between(c.WakeMinSeconds,c.WakeMaxSeconds);}
 public void LeaveSleep(double now,InteractionConfig c){WakeAt=double.PositiveInfinity;NoteActivity(now,c);}
 public string Advance(double now,bool sleeping,bool enabled,bool busy,InteractionConfig c){
  if(!enabled)return "";
  if(sleeping){if(now<WakeAt)return "";LeaveSleep(now,c);return "wake";}
  if(busy || now<SleepAt)return "";EnterSleep(now,c);return "sleep";
 }
}
public partial class PetWindow {
 public RestScheduler Rest=new RestScheduler();
 public List<Expander> PanelSections=new List<Expander>();
 public TextBlock PanelStatus;public string BubbleTrigger="";
 bool changingSections;
 public static string StateName(string mood){int index=Array.IndexOf(States,mood??"calm");return index<0?"安静陪伴":Names[index];}
 public static string StatusCaption(string trigger,string mood){
  if(States.Contains(trigger??""))return StateName(mood);
  string topic="";
  switch(trigger??""){
   case "greet":case "click":topic="打个招呼";break;
   case "poetry":topic="聊聊诗歌";break;case "sweet":topic="聊聊甜食";break;
   case "writing":topic="关于写作";break;case "company":topic="陪我一会儿";break;
   case "official":topic="活动原句";break;case "wake":topic="醒来啦";break;
   case "periodic-wake":topic="醒来看看你";break;case "pet":topic="摸摸头";break;
   case "touch-hat":topic="摸摸帽子";break;case "touch-hair":topic="摸摸头发";break;
   case "touch-glasses":topic="点点眼镜";break;case "touch-cheek":topic="戳戳脸颊";break;
   case "action-read":case "page":topic="一起读书";break;
   case "action-cane":case "tap":topic="探杖听回声";break;
   case "skin":topic="换好新衣服";break;case "drag":topic="换个位置陪你";break;
  }
  if((trigger??"").StartsWith("action-eat-") || (trigger??"").StartsWith("feed-"))topic="一起吃甜点";
  return topic==""?StateName(mood):topic+" · "+StateName(mood);
 }
 public void NoteEngagement(){double now=Clock.Elapsed.TotalSeconds;if(Interaction!=null)Rest.NoteActivity(now,Interaction);Mango.NoteActivity(now);DailyHop.NoteActivity(now);Pet.MangoUntil=0;Pet.HopUntil=0;CancelEchoCharge();}
 public void ResetRestSchedule(){if(State=="sleep")Rest.EnterSleep(Clock.Elapsed.TotalSeconds,Interaction);else Rest.LeaveSleep(Clock.Elapsed.TotalSeconds,Interaction);}
 public void AdvanceRest(double now){string action=Rest.Advance(now,State=="sleep",Pref.Auto,down || rightDown || Pet.Motion.Active || Pet.IsHopping || Pet.EchoCharging || Pet.IsEchoRelease || (State!="calm" && State!="sleep"),Interaction);if(action=="sleep")Talk("sleep",true);else if(action=="wake"){Talk("wake",true);BubbleTrigger="periodic-wake";Mango.NoteActivity(Clock.Elapsed.TotalSeconds);DailyHop.NoteActivity(Clock.Elapsed.TotalSeconds);RefreshStatusCaption();}}
 public void RefreshStatusCaption(){
  if(PanelStatus!=null)PanelStatus.Text=Pet.IsMango?"此刻 · 芒果海伦娜":Pet.EchoCharging?"此刻 · 蓄力回声":Pet.IsEchoRelease?(Pet.ChargedJumpHeight>0?"此刻 · 蓄力跳起":"此刻 · 落地回声"):Pet.IsHopping?"此刻 · 回声小跳":"此刻 · "+StateName(State);
  if(Bubble!=null && Bubble.View.Line!=null){string title=StatusCaption(BubbleTrigger,State);if(Bubble.View.StatusTitle!=title){Bubble.View.StatusTitle=title;Bubble.View.InvalidateVisual();}}
 }
 void RememberSections(){if(changingSections)return;Pref.ExpandedSections=PanelSections.Where(s=>s.IsExpanded).Select(s=>(string)s.Tag).ToArray();Save();}
 public void SetAllSections(bool expanded){changingSections=true;try{foreach(var section in PanelSections)section.IsExpanded=expanded;}finally{changingSections=false;}RememberSections();}
 Border Section(string key,string title,string summary,StackPanel content){
  if(content.Children.Count>0 && content.Children[0] is TextBlock)content.Children.RemoveAt(0);
  var heading=new StackPanel();var row=new DockPanel();var action=Txt("",16,"#7F7268");action.Margin=new Thickness(12,3,0,0);DockPanel.SetDock(action,Dock.Right);row.Children.Add(action);var name=Txt(title,21,"#36556D");name.Margin=new Thickness(0,0,0,4);row.Children.Add(name);heading.Children.Add(row);var subtitle=Txt(summary,16,"#7F7268");subtitle.MaxWidth=350;subtitle.Margin=new Thickness(0,0,0,8);heading.Children.Add(subtitle);
  var section=new Expander{Tag=key,Header=heading,Content=content,IsExpanded=(Pref.ExpandedSections??new[]{"chat"}).Contains(key),Foreground=B("#36556D"),HorizontalContentAlignment=HorizontalAlignment.Stretch};
  action.Text=section.IsExpanded?"收起":"展开";section.Expanded+=(s,e)=>{if(!ReferenceEquals(e.Source,section))return;action.Text="收起";RememberSections();};section.Collapsed+=(s,e)=>{if(!ReferenceEquals(e.Source,section))return;action.Text="展开";RememberSections();};content.Margin=new Thickness(0,12,0,0);PanelSections.Add(section);return Card(section);
 }
 void DecorationControls(StackPanel panel){
  var label=Txt("装饰大小："+Pref.DecorationSize.ToString("0.0")+"×",17,"#7F7268");panel.Children.Add(label);var slider=new Slider{Minimum=.7,Maximum=2,Value=Pref.DecorationSize,TickFrequency=.1,IsSnapToTickEnabled=true,Margin=new Thickness(0,0,0,12)};
  slider.ValueChanged+=(s,e)=>{Pet.DecorationSize=Pref.DecorationSize=slider.Value;label.Text="装饰大小："+Pref.DecorationSize.ToString("0.0")+"×";Pet.InvalidateVisual();Save();};panel.Children.Add(slider);panel.Children.Add(Txt("小皇冠、蝴蝶结和星星夹默认更大；蛋糕高帽会自动适配动物耳朵的位置与边界。",16,"#7F7268"));
 }
}
}

