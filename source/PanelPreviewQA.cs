using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace HelenaPet {
public static partial class Program {
 static void CapturePanelSheet(PetWindow w,string file){
  Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Render,new Action(()=>{}));
  var scroll=(ScrollViewer)w.Panel.Content;var stack=(FrameworkElement)scroll.Content;scroll.Content=null;stack.InvalidateMeasure();stack.InvalidateArrange();stack.Measure(new Size(520,double.PositiveInfinity));
  int height=(int)Math.Ceiling(stack.DesiredSize.Height);var sheet=new Border{Background=(Brush)new BrushConverter().ConvertFromString("#EDF2F5"),Child=stack};Capture(sheet,file,520,height);sheet.Child=null;scroll.Content=stack;
 }
}
}

