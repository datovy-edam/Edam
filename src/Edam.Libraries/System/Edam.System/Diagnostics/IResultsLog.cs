using System;
using System.Collections.Generic;

// BL-4.2 (name-collision safety): alias the Microsoft.Extensions.Logging types so they
// never collide with the custom Edam.Diagnostics.ILogger.
using MsLogger = Microsoft.Extensions.Logging;
using MsLoggerAbstractions = Microsoft.Extensions.Logging.Abstractions;

// -----------------------------------------------------------------------------

namespace Edam.Diagnostics
{

   public interface IResultsLog
   {

      Verbosity Verbosity { get; set; }
      Object DataObject { get; }
      Object ResultValueObject { get; }
      Boolean Success { get; }
      List<IMessageLogEntry> Messages { get; }
      String MessageText { get; }
      Exception LastException { get; }
      Int32 ReturnValue { get; set; }
      string ReturnText { get; }

      /// <summary>
      /// BL-4.1: additive Microsoft.Extensions.Logging (MEL) surface on the result log,
      /// exposed via a default interface method (DIM) so existing implementers keep
      /// compiling unchanged. Returns a null logger until a concrete logger is injected.
      /// </summary>
      MsLogger.ILogger Logger => MsLoggerAbstractions.NullLogger.Instance;

      IMessageLogEntry CreateMessageLogEntry();

      void Add(Exception exception);
      void Add(String message);
      void Add(String source, Exception exception);
      void Add(String source, String message);
      void Add(EventCode code, String details);
      //void Add(DataObjects.CodeProcess.EventCode code);

      void Succeeded();

      void Failed(Exception exception);
      void Failed(String message);
      void Failed(String source, Exception exception);
      void Failed(String source, String message);
      //void Failed(DataObjects.CodeProcess.EventCode code, String details);
      //void Failed(DataObjects.CodeProcess.EventCode code);

      void Copy(ResultLog log);
      void Copy(IResultsLog log);
      void Copy(List<Diagnostics.IMessageLogEntry> messages);
      void Copy(List<String> messages);

      void Write(IMessageLogEntry entry);

      void Clear();

   }

}
