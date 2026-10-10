using System;
using System.ComponentModel;
using Edam.Diagnostics;

namespace Edam.WinUI.Controls.ViewModels
{
   /// <summary>
   /// <b>One line of the diagnostics view.</b> It wraps the log entry with the view's own state — whether it is
   /// selected for copying — so the panel can copy <b>any combination</b> of messages instead of "one or all".
   /// <para>
   /// A wrapper is needed because the entry itself (an <see cref="IMessageLogEntry"/>) is a platform type whose
   /// content is read-only, and because the same line is what both the panel and the clipboard render.
   /// </para>
   /// </summary>
   public sealed class DiagnosticsEntry : INotifyPropertyChanged
   {
      private Boolean m_IsSelected;

      public DiagnosticsEntry(IMessageLogEntry entry)
      {
         Entry = entry ?? throw new ArgumentNullException(nameof(entry));
      }

      /// <summary>The log entry behind this line.</summary>
      public IMessageLogEntry Entry { get; }

      /// <summary>When the entry was logged, as the log renders it.</summary>
      public String LoggedDateTimeText => Entry.LoggedDateTimeText;

      /// <summary>The entry's message, in the entry's own words (with any attached exception).</summary>
      public String ErrorMessage => Entry.ErrorMessage;

      /// <summary>Selected for copying: the panel copies exactly what is selected (or all of it).</summary>
      public Boolean IsSelected
      {
         get => m_IsSelected;
         set
         {
            if (m_IsSelected == value) return;

            m_IsSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            SelectionChanged?.Invoke(this, EventArgs.Empty);
         }
      }

      /// <summary>The line this entry contributes when it is copied — timestamp, then message.</summary>
      public String Line => LoggedDateTimeText + "  " + ErrorMessage;

      /// <summary>Raised when this entry's selection changes, so the view can re-count what will be copied.</summary>
      public event EventHandler? SelectionChanged;

      public event PropertyChangedEventHandler? PropertyChanged;
   }
}
