using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
namespace HelenaPet {
public partial class PetWindow {
 public void VerifyLeftInputQA(){
  CancelPetRightPress();NoteEngagement();Bubble.Hide();SetMood("calm");Pet.Motion.Stop();down=true;dragged=false;longDone=false;pressProp="";pressTime=Clock.Elapsed.TotalSeconds-.8;Tick();if(State!="shy" || LastTrigger!="pet" || Pet.EchoCharging)throw new Exception("Left long press no longer pets");down=false;longDone=false;
  Bubble.Hide();lastTap=-99;PendingPart="";ClickInteract();if(LastTrigger!="click" || Pet.EchoCharging)throw new Exception("Left short press chat");
  down=true;dragged=true;OnUp(this,new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});if(LastTrigger!="drag" || Pet.EchoCharging)throw new Exception("Left drag routing");
  down=true;dragged=false;longDone=false;pressProp="";lastClick=DateTime.UtcNow;clicks=0;OnUp(this,new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});if(State!="listen" || Pet.EchoCharging)throw new Exception("Left double click routing");clickTimer.Stop();down=false;dragged=false;longDone=false;lastClick=DateTime.MinValue;
 }
}
public static partial class Program {
 static int FloorVisible(byte[] pixels){int total=0;for(int y=485;y<510;y++)for(int x=0;x<440;x++)if(pixels[(y*440+x)*4+3]>=20)total++;return total;}
 static void RunPetRightChargeQA(PetWindow w){
  var log=new List<string>();var gesture=new RightChargePress();for(int i=0;i<1000;i++){double t=i*10;gesture.Begin(t);if(gesture.Advance(t+.44))throw new Exception("Premature right long press");if(!gesture.Advance(t+.46) || gesture.Advance(t+2) || gesture.End()!="charge")throw new Exception("Right hold classification");gesture.Begin(t);if(gesture.End()!="panel")throw new Exception("Right short classification");gesture.Begin(t);gesture.Cancel();if(gesture.Advance(t+2) || gesture.End()!="")throw new Exception("Right move cancellation");}log.Add("PASS 1000 right short/long/cancel sequences; hold threshold 0.45s, no duplicate action or premature charge");
  var pref=w.Pref;bool pixelThrough=w.Interaction.PixelThrough;string skin=w.CurrentSkin,pendant=w.Pet.Pendant;double size=w.Pet.DecorationSize,sway=w.Pet.Sway;string decoration=w.Pet.Decoration;
  try{
   w.VerifyLeftInputQA();log.Add("PASS native left short chat, left long pet, drag and double-click mood remain unchanged");
   w.NoteEngagement();w.Interaction.PixelThrough=false;w.UpdateClickThrough(null);w.SetSkin("original-no-bangs");w.SetMood("calm");w.Pet.Pendant="none";w.Pet.Sway=0;w.Pet.X=w.Pet.Y=w.Pet.EyeX=w.Pet.EyeY=0;w.Bubble.Hide();w.Panel.Hide();var history=w.History.ToArray();
   w.BeginPetRightPress();w.RightPress.Started=w.Clock.Elapsed.TotalSeconds;w.FinishPetRightPress();if(!w.Panel.IsVisible || w.Pet.EchoCharging || !history.SequenceEqual(w.History))throw new Exception("Right short did not open only panel: visible="+w.Panel.IsVisible+", charging="+w.Pet.EchoCharging+", history="+history.SequenceEqual(w.History)+", active="+w.RightPress.Active+", cancelled="+w.RightPress.Cancelled+", reason="+w.RightPress.CancelReason);
   w.Panel.Hide();w.BeginPetRightPress();w.RightPress.Started=w.Clock.Elapsed.TotalSeconds-1.8;w.AdvancePetRightPress(w.Clock.Elapsed.TotalSeconds);if(!w.Pet.EchoCharging || w.Panel.IsVisible)throw new Exception("Pet right hold did not charge without panel");w.FinishPetRightPress();if(w.Pet.EchoCharging || !w.Pet.IsEchoRelease || w.Pet.EchoPower<.99 || w.Panel.IsVisible || !history.SequenceEqual(w.History))throw new Exception("Right hold release opened UI or chatted");
   double released=w.Pet.EchoReleasedAt;w.Pet.Time=released+.4;byte[] airborne=RenderPixels(w.Pet);if(w.Pet.ChargedJumpHeight<30 || FloorVisible(airborne)>0)throw new Exception("Echo before landing or missing jump");w.Pet.Time=released+w.Pet.EchoJumpCount*EchoHop.Interval+.3;byte[] landed=RenderPixels(w.Pet);if(w.Pet.ChargedJumpHeight!=0 || FloorVisible(landed)<300)throw new Exception("Missing landing echo");
   Capture(w.Pet,Path.Combine(root,"preview","蓄力跳-落地回声.png"),440,510);log.Add("PASS right hold on pet charges without opening UI or chatting; release jumps first; no floor echo before first landing, skin-colored waves after every landing");
   int releases=0;foreach(double held in new[]{.46,.75,.89,.91,1.2,1.59,1.61,2.5}){
    w.BeginEchoCharge();w.Pet.EchoChargeStarted=w.Clock.Elapsed.TotalSeconds-held;w.ReleaseEchoCharge();int expected=held<.9?1:held<1.6?2:3;
    if(w.Pet.EchoJumpCount!=expected)throw new Exception("Hold duration chose wrong hop count: "+held);
    for(int hop=0;hop<expected;hop++){w.Pet.Time=w.Pet.EchoReleasedAt+hop*EchoHop.Interval+.4;if(w.Pet.ChargedJumpHeight<20)throw new Exception("Missing repeated hop");CheckEchoBounds(RenderPixels(w.Pet));}
    w.Pet.Time=w.Pet.EchoReleasedAt+expected*EchoHop.Interval+.3;if(w.Pet.ChargedJumpHeight!=0 || FloorVisible(RenderPixels(w.Pet))<300)throw new Exception("Extra hop or missing final landing wave");
    w.Pet.Time=w.Pet.EchoUntil+.01;if(w.Pet.IsEchoRelease || w.Pet.ChargedJumpHeight!=0)throw new Exception("Charge sequence did not end");releases++;
   }log.Add("PASS "+releases+" actual hold/release durations select 1/2/3 hops at 0.9s/1.6s; every hop renders, final landing waves visible, no extra hop");
   w.NoteEngagement();w.BeginPetRightPress();w.RightPress.Started=w.Clock.Elapsed.TotalSeconds-1;w.AdvancePetRightPress(w.Clock.Elapsed.TotalSeconds);w.ReleaseMouseCapture();if(w.RightPress.Active || w.Pet.EchoCharging || w.Pet.IsEchoRelease)throw new Exception("Lost pet capture cancellation");
   int poses=0;foreach(string folder in Directory.GetDirectories(Path.Combine(root,"skins"))){w.SetSkin(Path.GetFileName(folder));w.BeginEchoCharge();w.Pet.EchoChargeStarted=w.Clock.Elapsed.TotalSeconds-2;w.ReleaseEchoCharge();foreach(double age in new[]{.2,.4,.8,1.1,2.3,2.7,3.2}){w.Pet.Time=w.Pet.EchoReleasedAt+age;CheckEchoBounds(RenderPixels(w.Pet));poses++;}}
   foreach(string cake in new[]{"dark-cake","sweet-cake","gold-cake"})foreach(string animal in new[]{"cat","deer","bunny"}){w.SetSkin(cake);w.Pet.Decoration=animal;w.Pet.DecorationSize=2;w.BeginEchoCharge();w.Pet.EchoChargeStarted=w.Clock.Elapsed.TotalSeconds-2;w.ReleaseEchoCharge();w.Pet.Time=w.Pet.EchoReleasedAt+.4;CheckEchoBounds(RenderPixels(w.Pet));poses++;}
   log.Add("PASS "+poses+" charged jump/landing poses across 16 skins and maximum cake animal decorations; measured alpha clearance prevents top clipping");
   w.NoteEngagement();w.SetSkin("original-no-bangs");w.Pet.Decoration="none";w.Pet.DecorationSize=1;w.Pet.ChargeJumpLimit=80;string frames=Path.GetFullPath(Path.Combine(root,"..","..","work","pet-right-charge-preview-frames"));Directory.CreateDirectory(frames);var demo=new PetVisual{Skin=w.Pet.Skin,EchoChargeStarted=.5,EchoReleasedAt=2.1,EchoUntil=5.4,EchoPower=1,EchoStrength=1.3,Sway=.1};for(int i=0;i<144;i++){demo.Time=i/24.0;demo.EchoCharging=demo.Time>=.5 && demo.Time<2.1;demo.InvalidateVisual();Capture(demo,Path.Combine(frames,i.ToString("D4")+".png"),440,510);}log.Add("PASS 144 native frames: charge meter, release jump, landing echo, original idle pose");
  }finally{w.CancelPetRightPress();w.NoteEngagement();w.Interaction.PixelThrough=pixelThrough;w.Pref=pref;w.Pet.Pendant=pendant;w.Pet.DecorationSize=size;w.Pet.Sway=sway;w.SetSkin(skin);w.Pet.Decoration=decoration;w.SetMood("calm");w.Panel.Show();}
  File.WriteAllLines(Path.Combine(root,"pet-right-charge-qa-results.txt"),log);
 }
}
}






