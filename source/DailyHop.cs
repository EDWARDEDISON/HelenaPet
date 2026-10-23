using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Web.Script.Serialization;
namespace HelenaPet {
public class DailyHopScheduler {
 readonly Random random;public double NextAt=double.PositiveInfinity;
 public DailyHopScheduler():this(Environment.TickCount+3409){}
 public DailyHopScheduler(int seed){random=new Random(seed);}
 public void NoteActivity(double now){NextAt=now+45+random.NextDouble()*45;}
 public bool Advance(double now,bool enabled,bool eligible){
  if(!enabled || now<NextAt)return false;
  if(!eligible){NextAt=now+12+random.NextDouble()*8;return false;}
  NoteActivity(now);return true;
 }
}
public partial class PetWindow {
 public DailyHopScheduler DailyHop=new DailyHopScheduler();
 public void AdvanceDailyHop(double now){
  bool eligible=State=="calm" && !down && !rightDown && !Pet.Motion.Active && !Pet.IsMango && !Pet.IsHopping && !Pet.EchoCharging && !Pet.IsEchoRelease && (Bubble==null || !Bubble.IsVisible) && (!Pref.Auto || Rest.SleepAt-now>2);
  if(!DailyHop.Advance(now,Pref.DailyHop,eligible))return;
  Pet.HopCount=1;Pet.HopAmplitude=30;Pet.HopStarted=now;Pet.HopUntil=now+EchoHop.Interval+.9;Pet.Time=now;Pet.InvalidateVisual();RefreshStatusCaption();
 }
 void DailyHopControls(StackPanel panel){
  var enabled=new CheckBox{Content="日常小跳 · 清醒空闲时偶尔跳一下",IsChecked=Pref.DailyHop,Margin=new Thickness(0,10,0,12)};
  enabled.Click+=(s,e)=>{Pref.DailyHop=enabled.IsChecked==true;if(!Pref.DailyHop && Pet.HopCount==1)Pet.HopUntil=0;Save();Pet.InvalidateVisual();};panel.Children.Add(enabled);
  panel.Children.Add(Txt("空闲约 45–90 秒后轻跳一次，落地散开回声。睡觉、聊天或做其他动作时会等一等；不会重置入睡时间。",16,"#7F7268"));
 }
}
public static partial class Program {
 static void RunDailyHopQA(PetWindow w){
  var log=new List<string>();var schedule=new DailyHopScheduler(4321);double now=0;
  for(int i=0;i<2000;i++){
   schedule.NoteActivity(now);double due=schedule.NextAt;if(due<now+45 || due>now+90 || schedule.Advance(due-.001,true,true))throw new Exception("Daily hop interval");
   if(schedule.Advance(due,false,true) || schedule.Advance(due,true,false))throw new Exception("Daily hop disabled/busy");if(schedule.NextAt<due+12 || schedule.NextAt>due+20)throw new Exception("Daily hop retry spacing");
   now=schedule.NextAt;if(!schedule.Advance(now,true,true) || schedule.NextAt<now+45 || schedule.NextAt>now+90)throw new Exception("Daily hop recurrence");
  }log.Add("PASS 2000 simulated idle episodes: 45–90s spacing, 12–20s defer while busy, disabled mode stays quiet");
  var pref=w.Pref;var previousSchedule=w.DailyHop;var rest=w.Rest;var mango=w.Mango;string skin=w.CurrentSkin;double oldTime=w.Pet.Time;int oldCount=w.Pet.HopCount;double oldAmplitude=w.Pet.HopAmplitude;
  try{
   w.Pref=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(pref));w.Pref.DailyHop=true;w.Pref.Auto=true;w.DailyHop=new DailyHopScheduler(7);w.Rest=new RestScheduler(9);w.Mango=new MangoChance(10);w.SetSkin("original-no-bangs");w.SetMood("calm");w.Pet.Motion.Stop();w.NoteEngagement();w.Bubble.Hide();
   w.Rest.NoteActivity(0,w.Interaction);w.Mango.NoteActivity(0);double restAt=w.Rest.SleepAt,mangoAt=w.Mango.CheckAt;string selected=w.CurrentSkin;w.DailyHop.NextAt=10;w.Pet.Time=10;w.AdvanceDailyHop(10);if(!w.Pet.IsHopping || w.Pet.HopCount!=1 || w.Pet.HopAmplitude!=30 || Math.Abs(w.Pet.HopUntil-11.7)>.0001)throw new Exception("Daily single hop routing");if(w.Rest.SleepAt!=restAt || w.Mango.CheckAt!=mangoAt || w.CurrentSkin!=selected)throw new Exception("Automatic hop changed idle or skin");
   w.Pet.Time=10.4;RenderPixels(w.Pet);if(w.Pet.HopHeight<29.9)throw new Exception("Daily hop not visibly raised");w.NoteEngagement();if(w.Pet.IsHopping)throw new Exception("Daily hop interaction cancel");
   foreach(string blocked in new[]{"sleep","talk","prop","charge","mango","echo","hop","bedtime"}){
    w.SetMood("calm");w.Pet.Motion.Stop();w.CancelEchoCharge();w.Pet.HopUntil=w.Pet.MangoUntil=0;w.Pet.Time=20;w.Rest.SleepAt=1000;w.Bubble.Hide();
    if(blocked=="sleep")w.SetMood("sleep");if(blocked=="talk")w.Bubble.Show();if(blocked=="prop")w.Pet.Motion.Start("read",0,20);if(blocked=="charge")w.Pet.EchoCharging=true;if(blocked=="mango"){w.Pet.MangoStarted=20;w.Pet.MangoUntil=26;}if(blocked=="echo"){w.Pet.EchoReleasedAt=20;w.Pet.EchoUntil=22;}if(blocked=="hop"){w.Pet.HopStarted=20;w.Pet.HopUntil=23;}if(blocked=="bedtime")w.Rest.SleepAt=21;
    w.DailyHop.NextAt=20;w.AdvanceDailyHop(20);if(w.DailyHop.NextAt<32 || w.DailyHop.NextAt>40)throw new Exception("Daily hop interrupted "+blocked);
   }
   w.Bubble.Hide();w.CancelEchoCharge();w.Pet.HopUntil=w.Pet.MangoUntil=0;w.Pet.Motion.Stop();w.SetMood("calm");w.Pref.DailyHop=false;w.DailyHop.NextAt=20;w.AdvanceDailyHop(20);if(w.Pet.IsHopping)throw new Exception("Daily hop toggle off");w.Pref.DailyHop=true;w.StartEchoHop();if(w.Pet.HopCount!=3 || w.Pet.HopAmplitude!=42)throw new Exception("Manual triple hop changed");w.NoteEngagement();
   w.Pref.DailyHop=true;w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="actions").IsExpanded=true;w.PanelSections.Single(s=>(string)s.Tag=="echo-effects").IsExpanded=true;w.UpdateEchoControls();w.RefreshStatusCaption();CapturePanelSheet(w,Path.Combine(root,"preview","听见你-日常小跳开关.png"));w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="chat").IsExpanded=true;
   var roundtrip=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(new Preferences{DailyHop=false}));if(roundtrip.DailyHop)throw new Exception("Daily toggle preference round trip");log.Add("PASS native single hop, manual triple hop, all eight busy states, cancellation and persisted switch; automatic hop preserves sleep and mango clocks and selected skin");
   string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","daily-hop-preview-frames"));Directory.CreateDirectory(frames);var demo=new PetVisual{Skin=w.Pet.Skin,HopStarted=.5,HopUntil=2.2,HopCount=1,HopAmplitude=30,Sway=.1};for(int i=0;i<72;i++){demo.Time=i/24.0;demo.InvalidateVisual();Capture(demo,Path.Combine(frames,i.ToString("D4")+".png"),440,510);}log.Add("PASS 72 native daily single-hop preview frames at 24fps");
  }finally{w.Pref=pref;w.DailyHop=previousSchedule;w.Rest=rest;w.Mango=mango;w.CancelEchoCharge();w.Pet.HopUntil=w.Pet.MangoUntil=0;w.Pet.HopCount=oldCount;w.Pet.HopAmplitude=oldAmplitude;w.Pet.Time=oldTime;w.SetSkin(skin);w.SetMood("calm");}
  File.WriteAllLines(Path.Combine(root,"daily-hop-qa-results.txt"),log);
 }
}
}
