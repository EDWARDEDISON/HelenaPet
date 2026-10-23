using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace HelenaPet {
public partial class PetVisual {
 public int EchoJumpCount=3;
 public int PendingEchoCount{get{return EchoHop.ChargeCount(Time-EchoChargeStarted);}}
 public bool EchoCharging;public double EchoChargeStarted,EchoReleasedAt,EchoUntil,EchoPower,ChargeJumpLimit=80;
 public bool IsEchoRelease{get{return Time>=EchoReleasedAt && Time<EchoUntil;}}
 public double ChargeFlight{get{return EchoHop.Interval;}}
 public double ChargedJumpHeight{get{return IsEchoRelease?EchoHop.Height(Time-EchoReleasedAt,EchoJumpCount,Math.Min(42,ChargeJumpLimit)):0;}}
 public double ChargeLevel{get{return EchoCharging?Math.Max(0,Math.Min(1,(Time-EchoChargeStarted)/1.6)):0;}}
 bool DrawChargedEcho(DrawingContext d){
  if(!EchoCharging && !IsEchoRelease)return false;
  double height=ChargedJumpHeight;d.PushTransform(new TranslateTransform(0,-height));DrawNormal(d);d.Pop();if(!PendantBounds.IsEmpty){var bounds=PendantBounds;bounds.Offset(0,-height);PendantBounds=bounds;}
  if(EchoCharging){
   d.DrawRoundedRectangle(B("#DDE8EF"),new Pen(B("#668FA3"),1),new Rect(150,500,140,6),3,3);
   if(ChargeLevel>0)d.DrawRoundedRectangle(B("#459FC3"),null,new Rect(150,500,140*ChargeLevel,6),3,3);
   foreach(double mark in new[]{.9/1.6,1.0})d.DrawLine(new Pen(B("#FFFFFF"),1.5),new Point(150+140*mark,499),new Point(150+140*mark,507));
   d.DrawRoundedRectangle(B("#EFFAFF"),new Pen(B("#668FA3"),1),new Rect(292,483,48,24),6,6);
   d.DrawText(new FormattedText(PendingEchoCount+" 下",System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),14,B("#36556D")),new Point(297,484));
  }else {
   double age=Time-EchoReleasedAt;
   EchoHop.DrawGround(d,age,489,EchoJumpCount,EchoStrength*(.85+.3*EchoPower),false,Skin==null?null:Skin.id);
   DrawHopParticles(d,age,480,EchoJumpCount);
  }return true;
 }
}
public partial class PetWindow {
 public Button EchoChargeButton;public ProgressBar EchoChargeBar;public TextBlock EchoChargeLabel;
 public void CancelEchoCharge(){Pet.EchoCharging=false;Pet.EchoUntil=0;if(EchoChargeButton!=null && EchoChargeButton.IsMouseCaptured)EchoChargeButton.ReleaseMouseCapture();}
 public void BeginEchoCharge(double startedAt=double.NaN){NoteEngagement();Pet.Motion.Stop();SetMood("calm");if(Bubble!=null)Bubble.Hide();double now=Clock.Elapsed.TotalSeconds;Pet.EchoChargeStarted=double.IsNaN(startedAt)?now:Math.Min(now,startedAt);Pet.Time=now;Pet.EchoCharging=true;Pet.InvalidateVisual();UpdateEchoControls();RefreshStatusCaption();}
 public void ReleaseEchoCharge(){if(!Pet.EchoCharging)return;double now=Clock.Elapsed.TotalSeconds;Pet.EchoJumpCount=EchoHop.ChargeCount(now-Pet.EchoChargeStarted);Pet.EchoPower=.25+.75*ActionSequence.Smooth((now-Pet.EchoChargeStarted)/1.6);Pet.EchoCharging=false;Pet.EchoUntil=0;Pet.Time=now;Pet.InvalidateVisual();RefreshHitMask();int top=510;for(int y=0;y<510 && top==510;y++)for(int x=0;x<440;x++)if(hitPixels[(y*440+x)*4+3]>=20){top=y;break;}Pet.ChargeJumpLimit=Math.Max(0,top-4);Pet.EchoReleasedAt=now;Pet.EchoUntil=now+Pet.EchoJumpCount*EchoHop.Interval+.9;Rest.NoteActivity(now,Interaction);DailyHop.NoteActivity(now);Mango.NoteActivity(now);if(EchoChargeButton!=null && EchoChargeButton.IsMouseCaptured)EchoChargeButton.ReleaseMouseCapture();Pet.InvalidateVisual();UpdateEchoControls();RefreshStatusCaption();}
 public void UpdateEchoControls(){
  if(EchoChargeBar!=null)EchoChargeBar.Value=Pet.ChargeLevel*100;
  if(EchoChargeLabel!=null)EchoChargeLabel.Text=Pet.EchoCharging?"蓄力 "+(Pet.ChargeLevel*100).ToString("0")+"% · 松开跳 "+Pet.PendingEchoCount+" 下":Pet.IsEchoRelease?(Pet.ChargedJumpHeight>0?"回声连跳中 · 共 "+Pet.EchoJumpCount+" 下":"落地回声扩散中"):"右键按住 0.45–0.9 秒跳 1 下；0.9–1.6 秒跳 2 下；满 1.6 秒跳 3 下。每次落地的波纹随皮肤自动配色（"+EchoHop.PaletteForSkin(CurrentSkin).Name+"）。";
 }
 void EchoControls(StackPanel panel){
  panel.Children.Add(Txt("蓄力回声",19,"#36556D"));
  var button=new Button{Content="备用按钮：按住选次数 · 松开连跳",FontSize=18,Padding=new Thickness(13,10,13,10),Margin=new Thickness(0,0,9,9),Background=B("#F4EBDD"),Foreground=B("#36556D"),BorderBrush=B("#D5C6B2"),BorderThickness=new Thickness(1),Cursor=Cursors.Hand};EchoChargeButton=button;
  button.PreviewMouseLeftButtonDown+=(s,e)=>{BeginEchoCharge();button.CaptureMouse();e.Handled=true;};button.PreviewMouseLeftButtonUp+=(s,e)=>{ReleaseEchoCharge();e.Handled=true;};
  button.LostMouseCapture+=(s,e)=>{if(Pet.EchoCharging){CancelEchoCharge();Pet.InvalidateVisual();UpdateEchoControls();}};
  button.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Space || e.Key==Key.Return){if(!e.IsRepeat)BeginEchoCharge();e.Handled=true;}else if(e.Key==Key.Escape){CancelEchoCharge();e.Handled=true;}};
  button.PreviewKeyUp+=(s,e)=>{if(e.Key==Key.Space || e.Key==Key.Return){ReleaseEchoCharge();e.Handled=true;}};
  button.LostKeyboardFocus+=(s,e)=>{if(Pet.EchoCharging && !button.IsMouseCaptured)CancelEchoCharge();};button.Click+=(s,e)=>{BeginEchoCharge();ReleaseEchoCharge();};panel.Children.Add(button);
  EchoChargeBar=new ProgressBar{Minimum=0,Maximum=100,Height=8,Foreground=B("#4B9EBF"),Background=B("#DFEAF0"),BorderThickness=new Thickness(0),Margin=new Thickness(0,0,9,9)};panel.Children.Add(EchoChargeBar);EchoChargeLabel=Txt("",16,"#7F7268");panel.Children.Add(EchoChargeLabel);
  var particles=new CheckBox{Content="显示皮肤专属粒子",IsChecked=Pref.SkinParticles,Margin=new Thickness(0,10,0,12)};particles.Click+=(s,e)=>{Pet.ShowSkinParticles=Pref.SkinParticles=particles.IsChecked==true;Save();Pet.InvalidateVisual();};panel.Children.Add(particles);
  var strengthLabel=Txt("音波强度："+Pref.EchoStrength.ToString("0.0")+"×",16,"#7F7268");panel.Children.Add(strengthLabel);var strength=new Slider{Minimum=.7,Maximum=2,Value=Pref.EchoStrength,TickFrequency=.1,IsSnapToTickEnabled=true,Margin=new Thickness(0,0,9,12)};strength.ValueChanged+=(s,e)=>{Pet.EchoStrength=Pref.EchoStrength=strength.Value;strengthLabel.Text="音波强度："+Pref.EchoStrength.ToString("0.0")+"×";Save();Pet.InvalidateVisual();};panel.Children.Add(strength);
  panel.Children.Add(Txt("左键短按聊天，左键长按摸头；右键短按打开面板，右键长按蓄力。备用按钮可用左键或获得焦点后的空格／回车。",16,"#7F7268"));DailyHopControls(panel);UpdateEchoControls();
 }
}
}

