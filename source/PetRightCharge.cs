using System;
using System.Windows;
using System.Windows.Input;
namespace HelenaPet {
public class RightChargePress {
 public bool Active,LongStarted,Cancelled;public double Started;public string CancelReason="";
 public void Begin(double now){Active=true;LongStarted=Cancelled=false;CancelReason="";Started=now;}
 public bool Advance(double now){if(!Active || LongStarted || Cancelled || now-Started<.45)return false;LongStarted=true;return true;}
 public string End(){if(!Active)return "";Active=false;return Cancelled?"cancel":LongStarted?"charge":"panel";}
 public void Cancel(string reason="cancel"){Active=false;Cancelled=true;CancelReason=reason;}
}
public partial class PetWindow {
 public RightChargePress RightPress=new RightChargePress();
 bool rightDown{get{return RightPress.Active;}}
 public void BeginPetRightPress(){if(down)return;NoteEngagement();clickTimer.Stop();RightPress.Begin(Clock.Elapsed.TotalSeconds);CaptureMouse();}
 public void AdvancePetRightPress(double now){if(!RightPress.Advance(now))return;lastClick=DateTime.MinValue;BeginEchoCharge(RightPress.Started);}
 public void FinishPetRightPress(){if(!RightPress.Active)return;AdvancePetRightPress(Clock.Elapsed.TotalSeconds);string action=RightPress.End();ReleaseMouseCapture();if(action=="charge" && Pet.EchoCharging)ReleaseEchoCharge();else if(action=="panel")OpenPanel();lastClick=DateTime.MinValue;}
 public void CancelPetRightPress(){if(!RightPress.Active)return;RightPress.Cancel();CancelEchoCharge();if(IsMouseCaptured)ReleaseMouseCapture();Pet.InvalidateVisual();}
 void OnPetRightDown(object sender,MouseButtonEventArgs e){BeginPetRightPress();e.Handled=true;}
 void OnPetRightUp(object sender,MouseButtonEventArgs e){FinishPetRightPress();e.Handled=true;}
 void OnPetLostCapture(object sender,MouseEventArgs e){if(RightPress.Active && ReferenceEquals(e.OriginalSource,this) && !IsMouseCaptured){RightPress.Cancel("capture lost");CancelEchoCharge();Pet.InvalidateVisual();}}
}
}
