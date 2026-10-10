using System;
using Edam.Diagnostics;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Edam.WinUI.Controls.Common
{
   /// <summary>
   /// Renders a log entry's <b>kind</b> as a colour, so the kind is visible <i>before</i> the words are: a log
   /// is scanned, not read line by line.
   /// <list type="bullet">
   /// <item><b>navy</b> — information;</item>
   /// <item><b>dark goldenrod</b> — a warning (something to look at, nothing broken);</item>
   /// <item><b>brown</b> — an error or an exception (what the person has to act on);</item>
   /// <item><b>dark red</b> — critical/fatal, the strongest kind;</item>
   /// <item><b>grey</b> — debug/trace noise, deliberately quiet.</item>
   /// </list>
   /// <para>
   /// A converter rather than a brush on the entry: the entries are built on whatever thread logged them, and a
   /// <see cref="SolidColorBrush"/> is a UI object that must be created on the UI thread — this converter is
   /// instantiated and invoked by the binding, i.e. on the UI thread, so the brushes are created where they
   /// belong.
   /// </para>
   /// </summary>
   public sealed class SeverityLevelBrushConverter : IValueConverter
   {
      private static readonly Brush CriticalBrush = FromRgb(139, 0, 0);      // dark red
      private static readonly Brush ErrorBrush = FromRgb(139, 69, 19);       // brown (saddle brown)
      private static readonly Brush WarningBrush = FromRgb(184, 134, 11);    // dark goldenrod
      private static readonly Brush InfoBrush = FromRgb(0, 0, 128);          // navy
      private static readonly Brush QuietBrush = FromRgb(128, 128, 128);     // grey

      public object Convert(object value, Type targetType, object parameter, string language)
      {
         if (value is SeverityLevel level)
         {
            switch (level)
            {
               case SeverityLevel.Critical:
                  return CriticalBrush;
               case SeverityLevel.Fatal:      // the level an error/exception arrives as (see the log bridge)
                  return ErrorBrush;
               case SeverityLevel.Warning:
                  return WarningBrush;
               case SeverityLevel.Info:
                  return InfoBrush;
               default:                        // Debug / Unknown: quiet, so the eye skips it
                  return QuietBrush;
            }
         }

         return QuietBrush;
      }

      public object ConvertBack(object value, Type targetType, object parameter, string language)
         => throw new NotSupportedException("A severity is never converted back from a colour.");

      private static Brush FromRgb(byte red, byte green, byte blue)
         => new SolidColorBrush(Color.FromArgb(255, red, green, blue));
   }
}
