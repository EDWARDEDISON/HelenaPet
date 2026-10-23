using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
namespace HelenaPet {
public static partial class Program {
 static void RunMangoQA(PetWindow w){
  var log=new List<string>();if(w.MangoError!="" || w.Pet.MangoImage==null)throw new Exception("Mango art: "+w.MangoError);
  var chance=new MangoChance(9137);int count=0;for(int i=0;i<10000;i++){double now=i*1500.0;chance.NoteActivity(now);if(chance.CheckAt<now+75 || chance.CheckAt>now+120 || chance.Advance(chance.CheckAt-.001,true,true))throw new Exception("early mango");double at=chance.CheckAt;bool started=chance.Advance(at,true,true);if(!chance.Checked)throw new Exception("idle opportunity not consumed");if(started){count++;if(!chance.Active(at+MangoChance.Duration-.01) || chance.Active(at+MangoChance.Duration) || chance.NextAllowed<at+1200)throw new Exception("mango duration/cooldown");}else if(chance.Advance(at+1,true,true))throw new Exception("rerolled failed chance");chance.Advance(at+MangoChance.Duration+.01,true,true);if(chance.Active(at+MangoChance.Duration+.01))throw new Exception("mango did not expire");}
  if(count<250 || count>550)throw new Exception("mango rate "+count);log.Add("PASS 10000 simulated idle episodes; "+count+" rare transformations at configured 4% chance, one roll per episode, check after 75–120s, 6s total duration and 20min cooldown");
  chance.Preview(20000);chance.NoteActivity(20001);if(chance.Active(20001) || chance.Advance(chance.CheckAt,true,true))throw new Exception("interaction cancel/cooldown");
  foreach(bool enabled in new[]{false,true}){chance.Preview(30000);if(chance.Advance(30000,enabled,false))throw new Exception("busy transformation");}
  chance.Preview(40000);if(chance.Advance(40000,false,true))throw new Exception("disabled transformation");log.Add("PASS interaction cancels immediately; disabled/asleep/busy eligibility blocks or cancels transformation");
  var pref=w.Pref;bool wasEnabled=pref.MangoEnabled;var rest=w.Rest;var mango=w.Mango;string skin=w.CurrentSkin;double previousTime=w.Pet.Time;
  try{
   w.Pref.MangoEnabled=true;w.SetSkin("original-no-bangs");w.SetMood("calm");w.Pet.Motion.Stop();w.Bubble.Hide();w.Pet.X=w.Pet.Y=w.Pet.EyeX=w.Pet.EyeY=0;w.Pet.Time=2;w.Pet.MangoUntil=0;string current=w.CurrentSkin,prefSkin=w.Pref.Skin;var before=RenderPixels(w.Pet);
   w.Rest.NoteActivity(0,w.Interaction);double idleDeadline=w.Rest.SleepAt;w.Mango=new MangoChance(44);w.Mango.Preview(2);w.AdvanceMango(2);w.Pet.Time=2;RenderPixels(w.Pet);if(!w.Pet.IsMango || w.CurrentSkin!=current || w.Pref.Skin!=prefSkin || w.Rest.SleepAt!=idleDeadline)throw new Exception("mango changed skin or idle schedule");
   if(!before.SequenceEqual(RenderPixels(w.Pet)))throw new Exception("entry does not begin at original skin");
   foreach(double age in new[]{.01,.15,.35,.69,1.0,3.0,5.31,5.65,5.9,5.99}){w.Pet.Time=2+age;var pixels=RenderPixels(w.Pet);int visible=0;for(int y=0;y<510;y++)for(int x=0;x<440;x++){byte alpha=pixels[(y*440+x)*4+3];if(alpha>=20)visible++;if((x==0 || x==439 || y==0 || y==509) && alpha>=20)throw new Exception("transformation clipped at canvas boundary");}if(visible<5000)throw new Exception("blank transformation phase");}
   w.Pet.Time=3;RenderPixels(w.Pet);log.Add("PASS entry starts at exact original rendering; 10 transition/hold/return phases remain visible and unclipped; 0.7s entry + 4.6s mango + 0.7s return");
   w.RefreshHitMask();if(w.OpaqueAt(new Point(4,4)) || !w.OpaqueAt(new Point(220,300)))throw new Exception("mango alpha hit mask");Capture(w.Pet,Path.Combine(root,"preview","芒果海伦娜.png"),440,510);
   w.AdvanceMango(8.01);w.Pet.Time=2;var after=RenderPixels(w.Pet);if(w.Pet.IsMango || !before.SequenceEqual(after))throw new Exception("normal skin did not restore exactly");
   w.Mango.Preview(2);w.AdvanceMango(2);w.Pet.Time=2;w.NoteEngagement();if(w.Pet.IsMango)throw new Exception("native input cancellation");
   w.Pref.MangoEnabled=false;w.Mango.Preview(2);w.AdvanceMango(2);if(w.Pet.MangoUntil>0)throw new Exception("preference disable");
   log.Add("PASS native fruit rendering uses alpha hit mask, preserves selected skin and rest deadline, restores identical normal rendering, and cancels on engagement or switch");
   w.Pref.MangoEnabled=wasEnabled;w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="company").IsExpanded=true;CapturePanelSheet(w,Path.Combine(root,"preview","听见你-作息与彩蛋.png"));w.SetAllSections(false);w.PanelSections.Single(s=>(string)s.Tag=="chat").IsExpanded=true;
   string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","mango-preview-frames"));Directory.CreateDirectory(frames);var demo=new PetVisual{Skin=w.Pet.Skin,MangoImage=w.Pet.MangoImage,MangoStarted=1,MangoUntil=1+MangoChance.Duration,Accessories=w.Pet.Accessories,Decoration="none",Sway=.3,Mood="calm"};
   for(int f=0;f<192;f++){demo.Time=f/24.0;demo.InvalidateVisual();Capture(demo,Path.Combine(frames,f.ToString("D4")+".png"),440,510);}log.Add("PASS 192 native preview frames at 24fps: 1s normal → 6s transformation including entry/exit effects → 1s original skin");CaptureMangoEffectGallery(w);
  }finally{w.Pref=pref;pref.MangoEnabled=wasEnabled;w.Rest=rest;w.Mango=mango;w.Pet.MangoUntil=0;w.Pet.Time=previousTime;w.SetSkin(skin);w.SetMood("calm");}
  File.WriteAllLines(Path.Combine(root,"mango-qa-results.txt"),log);
 }
}
}

