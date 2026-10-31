using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// -----------------------------------------------------------------------------
namespace Edam.WinUI.Helpers
{
   public class FileHelper : Edam.InOut.IFileHelper
   {
      /// <summary>
      /// Get the local file full path using the application's configured writable data root.
      /// </summary>
      /// <param name="filename"></param>
      /// <returns></returns>
      public string GetLocalFilePath(string filename)
      {
         if (String.IsNullOrWhiteSpace(filename))
         {
            throw new ArgumentException("A filename is required.", nameof(filename));
         }

         var appDataRoot = Edam.Application.AppData.GetApplicationDataLocation();
         var path = Path.Combine(appDataRoot, filename);
         return path;
      }
   }
}
