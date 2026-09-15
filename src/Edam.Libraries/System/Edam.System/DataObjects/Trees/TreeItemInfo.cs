using System;
using Newtonsoft.Json;

namespace Edam.DataObjects.Trees
{

   /// <summary>
   /// Type of a tree item (branch = container/folder, leaf = item/file).
   /// </summary>
   public enum TreeItemType
   {
      Unknown = 0,
      Branch = 1,
      Leaf = 2
   }

   /// <summary>
   /// Marker interface for tree items.
   /// </summary>
   public interface ITreeItem
   {
   }

   /// <summary>
   /// Marker interface for tree containers.
   /// </summary>
   public interface ITreeContainer
   {
   }

   /// <summary>
   /// Non-generic tree item base used by consumers that build a directory
   /// tree (e.g. the Catalog solution). Provides the common item fields and
   /// JSON (de)serialization helpers for a directory tree.
   /// </summary>
   public class TreeItem
   {
      public string Name { get; set; }
      public string Title { get; set; }
      public object Tag { get; set; }
      public TreeItemType Type { get; set; } = TreeItemType.Unknown;
      public int Number { get; set; }
      public string Icon { get; set; }
      public short[] Level { get; set; }

      /// <summary>
      /// Serialize a directory tree (root item) to JSON text.
      /// </summary>
      /// <param name="root">root tree item</param>
      /// <returns>JSON text</returns>
      public static string ToDirectoryJsonText(object root)
      {
         var settings = new JsonSerializerSettings
         {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
         };
         return JsonConvert.SerializeObject(root, settings);
      }

      /// <summary>
      /// Deserialize a directory tree (root item) from JSON text.
      /// </summary>
      /// <typeparam name="T">root item type</typeparam>
      /// <param name="jsonText">JSON text</param>
      /// <returns>root item instance</returns>
      public static T FromDirectoryJsonText<T>(string jsonText)
      {
         return JsonConvert.DeserializeObject<T>(jsonText);
      }
   }

}
