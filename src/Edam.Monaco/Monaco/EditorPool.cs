using Edam.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monaco;

/// <summary>
/// Manage the Editor Pool allowing to pre-instantiate editor controls and 
/// initialize and setup as needed before being used/fetched.
/// </summary>
public class EditorPool
{

   /// <summary>
   /// Maximum number of editors to keep warm in the pool. Editors are created
   /// lazily on demand (see <see cref="GetEditorInstance"/>) rather than all at
   /// once, because each editor hosts a WebView2 control which is expensive to
   /// create. The pool therefore acts as a small reuse cache, not an eager
   /// pre-instantiation buffer.
   /// </summary>
   public static int POOL_SIZE = 5;
   private static List<Monaco.MonacoEditor> Pool { get; set; } =
       new List<Monaco.MonacoEditor>();

   #region -- 4.00 - Manage Editor Pool

   /// <summary>
   /// Get an Editor instance, creating one on demand if the pool is empty.
   /// Editors are created lazily to avoid the heavy startup cost of
   /// pre-instantiating many WebView2 controls at once.
   /// </summary>
   /// <returns>return an Editor instance</returns>
   public static async Task<Monaco.MonacoEditor> GetEditorInstance()
   {
      Monaco.MonacoEditor editor;
      if (Pool.Count > 0)
      {
         editor = Pool[0];
         Pool.RemoveAt(0);
      }
      else
      {
         editor = new Monaco.MonacoEditor();
         await editor.InitializeControlAsync();
      }
      return editor;
   }

   public static int AvailablePools()
   {
      return Pool.Count;
   }

   #endregion

}
