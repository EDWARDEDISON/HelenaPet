using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace HelenaPet {
public static class Theme {
 static Brush B(string value){var brush=(Brush)new BrushConverter().ConvertFromString(value);brush.Freeze();return brush;}
 public static void Apply(Application app){foreach(Type type in new[]{typeof(CheckBox),typeof(ComboBox),typeof(TextBox),typeof(ListBox),typeof(ListBoxItem),typeof(TabItem),typeof(TabControl)}){var style=new Style(type);style.Setters.Add(new Setter(Control.ForegroundProperty,B("#36556D")));style.Setters.Add(new Setter(Control.FontFamilyProperty,new FontFamily("Microsoft YaHei UI")));if(type!=typeof(CheckBox) && type!=typeof(ListBoxItem)){style.Setters.Add(new Setter(Control.BackgroundProperty,B(type==typeof(TabControl)?"#EDF2F5":type==typeof(TabItem)?"#DFE9EF":"#FFFCF6")));style.Setters.Add(new Setter(Control.BorderBrushProperty,B("#A8BFCE")));}if(type==typeof(TabItem)){style.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(12,8,12,8)));style.Setters.Add(new Setter(Control.FontSizeProperty,18.0));}app.Resources[type]=style;}}
}
}
