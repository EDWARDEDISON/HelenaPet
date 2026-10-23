using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HelenaPet {
public static partial class Program {
 static void RunInteractionQA(PetWindow w){
  var log=new List<string>();var config=w.Interaction;string previousRoot=w.Root;var bank=w.Dialogues;
  w.Interaction=new InteractionConfig();w.Pet.PreviewUntil=-1;w.Pet.Motion.Stop();w.Pet.Mood="calm";w.Pet.Sway=0;w.Pet.Time=2.2;w.Pet.X=w.Pet.Y=0;
  string[] parts={"hat","glasses","cheek","hair"};
  foreach(string skin in new[]{"original-no-bangs","original"}){w.SetSkin(skin);foreach(string action in new[]{"","read","eat","cane"}){if(action=="")w.Pet.Motion.Stop();else w.Pet.Motion.Start(action,0,0);w.Pet.Time=2.2;w.Pet.InvalidateVisual();w.Pet.UpdateLayout();w.RefreshHitMask();foreach(string part in parts){Point p=w.Pet.PartPoint(part);if(w.Pet.HitPart(p)!=part)throw new Exception("Part hit projection "+skin+"/"+action+"/"+part+" got "+w.Pet.HitPart(p));}}}
  log.Add("PASS four projected hotspots in both hairstyles and three prop actions");
  w.Pet.Motion.Stop();w.SetMood("calm");for(int i=0;i<4;i++){if(!w.TouchPart(parts[i]) || w.State!=w.Interaction.Rules[i].Mood || w.LastTrigger!="touch-"+parts[i])throw new Exception("part routing");}
  w.Interaction.Parts=false;if(w.TouchPart("cheek"))throw new Exception("disabled part interaction");w.Interaction.Parts=true;w.Interaction.Rules[0].Action="read";w.TouchPart("hat");if(w.Pet.Motion.Kind!="read")throw new Exception("configured part action");w.Interaction.Rules[0].Action="none";
  w.Interaction.ReadSeconds=31;w.Interaction.PageSeconds=2;w.StartAction("read");if(Math.Abs(w.Pet.Motion.EndAt-w.Pet.Motion.Started-31)>.05)throw new Exception("configured duration");w.Talk("click");if(w.Pet.Motion.Kind!="read")throw new Exception("keep action on chat");w.Interaction.KeepActionOnChat=false;w.Talk("click");if(w.Pet.Motion.Active)throw new Exception("configured interruption");
  var seq=new ActionSequence{AutoPage=false};seq.Start("read",0,0);seq.Advance(7);if(seq.Page!=0)throw new Exception("auto page disabled");seq.AutoPage=true;seq.PageInterval=2;seq.Advance(7);if(seq.Page!=1)throw new Exception("configured page interval");
  log.Add("PASS configurable hotspot actions/moods, disabling, action duration, automatic page interval and keep-action switch");
  string scratch=Path.GetFullPath(Path.Combine(root,"..","..","work","interaction-data-qa"));Directory.CreateDirectory(scratch);try{w.Root=scratch;File.Copy(Path.Combine(root,"dialogue.json"),Path.Combine(scratch,"dialogue.json"),true);w.Dialogues=new DialogueBank(Path.Combine(scratch,"dialogue.json"));var serializer=new JavaScriptSerializer();var lines=serializer.Deserialize<List<DialogueLine>>(serializer.Serialize(w.Dialogues.Lines));lines.Add(new DialogueLine{id="qa-custom",@event="touch-hat",text="测试台词",mood="happy",kind="fan",source="用户自定义"});w.SaveDialogue(lines);if(w.Dialogues.Lines.Last().id!="qa-custom" || !File.Exists(Path.Combine(scratch,"dialogue.json.bak")))throw new Exception("dialogue save/reload/backup");lines=serializer.Deserialize<List<DialogueLine>>(serializer.Serialize(w.Dialogues.Lines));lines.First(l=>l.kind=="official").text="changed";bool rejected=false;try{w.SaveDialogue(lines);}catch{rejected=true;}if(!rejected)throw new Exception("official mutation accepted");lines=serializer.Deserialize<List<DialogueLine>>(serializer.Serialize(w.Dialogues.Lines));lines.Add(new DialogueLine{id="invalid",@event="click",text=new string('a',181),mood="happy",kind="fan"});rejected=false;try{w.SaveDialogue(lines);}catch{rejected=true;}if(!rejected || new DialogueBank(Path.Combine(scratch,"dialogue.json")).Error!="")throw new Exception("invalid dialogue corrupted file");}finally{w.Root=previousRoot;w.Dialogues=bank;}
  log.Add("PASS custom dialogue saves/reloads atomically with previous-version backup; official provenance and invalid edits protected (isolated files)");
  w.Pet.Motion.Stop();w.Pet.Mood="calm";w.Pet.X=w.Pet.Y=0;w.Pet.Time=2.2;w.Pet.InvalidateVisual();w.Pet.UpdateLayout();w.RefreshHitMask();Point blank=new Point(4,4),solid=w.Pet.PartPoint("glasses");if(w.OpaqueAt(blank) || !w.OpaqueAt(solid))throw new Exception("alpha hit mask");
  w.Interaction.PixelThrough=true;w.UpdateClickThrough(blank);if(!w.IsPassing)throw new Exception("transparent native style");Point screen=w.Pet.PointToScreen(blank);IntPtr blankWindow=w.NativeWindowAt(screen);if(blankWindow==w.NativeHandle)throw new Exception("OS hit still intercepted at transparent pixel");w.UpdateClickThrough(solid);if(w.IsPassing)throw new Exception("solid native style not restored");screen=w.Pet.PointToScreen(solid);IntPtr solidWindow=w.NativeWindowAt(screen);if(solidWindow!=w.NativeHandle)throw new Exception("OS hit not restored at opaque pixel: "+solidWindow+" expected "+w.NativeHandle);
  w.Interaction.PixelThrough=false;w.UpdateClickThrough(blank);if(w.IsPassing)throw new Exception("clickthrough toggle off");
  log.Add("PASS real Win32 WindowFromPoint excludes pet on transparent pixels and restores pet on opaque pixels; off switch restores rectangular input");
  w.Interaction=new InteractionConfig();w.OpenInteractionSettings();var tabs=(TabControl)w.InteractionPanel.Content;w.InteractionPanel.Content=null;var frame=new Border{Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5"),Child=tabs};Capture(frame,Path.Combine(root,"preview","部位与动作设置.png"),660,760);tabs.SelectedIndex=1;Capture(frame,Path.Combine(root,"preview","台词编辑设置.png"),660,760);frame.Child=null;w.InteractionPanel.Content=tabs;w.InteractionPanel.Close();
  log.Add("PASS both settings tabs render with large controls");w.Interaction=config;File.WriteAllLines(Path.Combine(root,"interaction-qa-results.txt"),log);
 }
}
}
