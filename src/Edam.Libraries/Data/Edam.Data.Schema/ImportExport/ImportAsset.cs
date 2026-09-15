using DocumentFormat.OpenXml.Spreadsheet;
using Edam.Data.Asset;
using Edam.Data.AssetSchema;
using Edam.Diagnostics;
using Edam.Xml.OpenXml;
using Edam.Data.AssetManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Reader = Edam.Text.StringReader;

namespace Edam.Data.Schema.ImportExport;

// -----------------------------------------------------------------------------

public class ItemColumnInfo
{
   public string Type { get; set; }
   public int Length { get; set; } = 0;      // For INT, VARCHAR, etc.
   public int Precision { get; set; } = 0;   // For DECIMAL(p,s)
   public int Scale { get; set; } = 0;       // For DECIMAL(p,s)
   public bool IsUnsigned { get; set; } = false;

   public static bool TryParseMySqlType(
      string definition, out ItemColumnInfo info)
   {
      info = null;

      if (string.IsNullOrWhiteSpace(definition))
         return false;

      var def = definition.Trim();
      var upper = def.ToUpperInvariant();

      // Basic validation
      var isValid = System.Text.RegularExpressions.Regex.IsMatch(
          upper,
          @"^[A-Z]+(\s*\([0-9,\s]+\))?(\s+UNSIGNED)?$"
      );

      if (!isValid)
         return false;

      info = new ItemColumnInfo();

      // Detect UNSIGNED
      info.IsUnsigned = upper.Contains("UNSIGNED");
      upper = upper.Replace("UNSIGNED", "").Trim();

      // Extract type and parentheses
      int parenIndex = upper.IndexOf('(');
      string type;
      string inside = null;

      if (parenIndex > 0)
      {
         type = upper.Substring(0, parenIndex).Trim();

         int endParen = upper.IndexOf(')', parenIndex + 1);
         if (endParen < 0)
            return false;

         inside = upper.Substring(parenIndex + 1, endParen - parenIndex - 1).Trim();
      }
      else
      {
         type = upper.Trim();
      }

      info.Type = type;

      // Detect numeric type:
      // 1. If inside contains a comma → DECIMAL(p,s)
      // 2. If type is an integer type → INT, BIGINT, etc.
      bool isIntegerType =
          type.StartsWith("INT") ||
          type.StartsWith("TINYINT") ||
          type.StartsWith("SMALLINT") ||
          type.StartsWith("MEDIUMINT") ||
          type.StartsWith("BIGINT");

      bool isNumeric = (inside != null && inside.Contains(",")) || isIntegerType;

      if (isNumeric)
      {
         info.Length = 0; // enforce rule

         if (inside != null)
         {
            if (inside.Contains(","))
            {
               // DECIMAL(p,s)
               var parts = inside.Split(',');
               if (parts.Length != 2)
                  return false;

               if (!int.TryParse(parts[0], out int p)) return false;
               if (!int.TryParse(parts[1], out int s)) return false;

               info.Precision = p;
               info.Scale = s;
            }
            else
            {
               // INT(11) → treat as precision
               if (!int.TryParse(inside, out int p)) return false;

               info.Precision = p;
               info.Scale = 0;
            }
         }
         else
         {
            // No parentheses → numeric type with no explicit precision
            info.Precision = 0;
            info.Scale = 0;
         }
      }
      else
      {
         // Non-numeric types → use Length
         if (inside != null)
         {
            if (!int.TryParse(inside, out int len))
               return false;

            info.Length = len;
         }
      }

      return true;
   }

}

// -----------------------------------------------------------------------------

public class ImportAsset
{
   private const string CLASS_NAME = "ImportAsset";

   private DataTextMap _Mapper = null;
   private ImportValues _Values;
   private ResultLog _Results = new ResultLog();

   public ResultLog Results
   {
      get { return _Results; }
   }

   public ImportAsset(ImportValues values, string mapperfilePath = null)
   {
      _Values = values;
      _Mapper = (String.IsNullOrWhiteSpace(mapperfilePath)) ?
         null : DataTextMap.FromFile(mapperfilePath);
   }

   /// <summary>
   /// Import an Item (Table, Function, View, or Stored-Procedure)
   /// </summary>
   /// <param name="values">list of values</param>
   /// <returns>instance of ImportItemInfo is returned with info</returns>
   /// <exception cref="ArgumentException"></exception>
   private static ImportItemInfo ImportItem(ImportValues values)
   {
      String func = "SetValues";
      if (values.Values.Count > 21)
      {
         throw new ArgumentException(CLASS_NAME + "::" + func +
            ": Expected no more than 13, 15 or 19/21 columns got (" +
            values.Values.Count.ToString() + ")");
      }

      ImportItemInfo item = new ImportItemInfo();

      if (values.Values.Count <= 15 && values.Header[11] != null &&
          values.Header[11].ToUpper() == "CONSTRAINT_TYPE")
      {
         item.Dbms = Reader.GetString(values.Values[0]);
         item.TableCatalog = Reader.GetString(values.Values[1]);
         item.TableSchema = Reader.GetString(values.Values[2]);
         item.TableName = Reader.GetString(values.Values[3]);
         item.ColumnName = Reader.GetString(values.Values[4]);
         item.OrdinalPosition = Reader.GetLong(values.Values[5]);
         item.DataType = Reader.GetString(values.Values[6]);
         item.CharacterMaximumLength = Reader.GetInteger(values.Values[7]);

         item.Precision = Reader.GetInteger(values.Values[8]);
         item.Scale = Reader.GetInteger(values.Values[9]);
         item.IsNullable = Reader.GetBool(values.Values[10]);

         item.ConstraintType = Reader.GetString(values.Values[11]);
         item.ConstraintTableSchema = Reader.GetString(values.Values[12]);
         item.ConstraintTableName = Reader.GetString(values.Values[13]);
         item.ConstraintColumnName = Reader.GetString(values.Values[14]);
      }
      else if (values.Values.Count <= 15)
      {
         item.Dbms = Reader.GetString(values.Values[0]);
         item.TableCatalog = Reader.GetString(values.Values[1]);
         item.TableSchema = Reader.GetString(values.Values[2]);
         item.TableName = Reader.GetString(values.Values[3]);
         item.ColumnName = Reader.GetString(values.Values[4]);
         item.OrdinalPosition = Reader.GetLong(values.Values[5]);
         item.DataType = Reader.GetString(values.Values[6]);
         item.CharacterMaximumLength = Reader.GetInteger(values.Values[7]);
         item.ConstraintType = Reader.GetString(values.Values[8]);
         item.ConstraintTableSchema = Reader.GetString(values.Values[9]);
         item.ConstraintTableName = Reader.GetString(values.Values[10]);
         item.ConstraintColumnName = Reader.GetString(values.Values[11]);

         item.IsIdentity = (values.Values.Count > 12) ?
            Reader.GetBool(values.Values[12]) : false;
         item.Tags = (values.Values.Count > 13) ?
            Reader.GetString(values.Values[13]) : String.Empty;
         item.ColumnDescription = (values.Values.Count > 14) ?
            Reader.GetString(values.Values[14]) : String.Empty;
      }

      // the following support an enhace schema with all object types
      else if (values.Values.Count >= 19)
      {
         item.Dbms = Reader.GetString(values.Values[0]);
         item.TableCatalog = Reader.GetString(values.Values[1]);
         item.TableSchema = Reader.GetString(values.Values[2]);
         item.ObjectName = Reader.GetString(values.Values[3]);
         item.ColumnName = Reader.GetString(values.Values[4]);
         item.OrdinalPosition = Reader.GetLong(values.Values[5]);
         item.DataType = Reader.GetString(values.Values[6]);
         item.CharacterMaximumLength = Reader.GetInteger(values.Values[7]);

         item.Precision = Reader.GetInteger(values.Values[8]);
         item.Scale = Reader.GetInteger(values.Values[9]);

         item.IsOutput = Reader.GetBool(values.Values[10]);
         item.IsReadOnly = Reader.GetBool(values.Values[11]);
         item.IsNullable = Reader.GetBool(values.Values[12]);
         item.IsIdentity = Reader.GetBool(values.Values[13]);

         // if DataType is null assume it is an entity record and therefore "object"
         if (item.DataType == null)
         {
            item.DataType = "object";
         }

         string otype = Reader.GetString(values.Values[14]).ToUpper();
         switch (otype)
         {
            case "PROCEDURE":
               item.ObjectType = ElementType.procedure;
               break;
            case "FUNCTION":
               item.ObjectType = ElementType.function;
               break;
            case "VIEW":
               item.ObjectType = ElementType.view;
               break;
            case "ELEMENT":
               item.ObjectType = ElementType.element;
               break;
            case "TABLE":
            default:
               item.ObjectType = ElementType.type;
               break;
         }

         item.ConstraintType = Reader.GetString(values.Values[15]);
         item.ConstraintTableSchema = Reader.GetString(values.Values[16]);
         item.ConstraintTableName = Reader.GetString(values.Values[17]);
         item.ConstraintColumnName = Reader.GetString(values.Values[18]);

         if (item.ObjectType == ElementType.procedure ||
             item.ObjectType == ElementType.function)
         {
            item.ColumnName = item.ColumnName.Replace("@", "");
         }

         if (values.Values.Count == 21)
         {
            item.Tags = Reader.GetString(values.Values[19]);
            item.ColumnDescription = Reader.GetString(values.Values[20]);
         }
      }

      if (item.ConstraintType == "P")
         item.ConstraintType = AssetElementConstraintInfo.KEY;
      else if (item.ConstraintType == "F")
         item.ConstraintType = AssetElementConstraintInfo.FOREIGN_KEY;
      else if (item.ConstraintType == "R")
         item.ConstraintType = AssetElementConstraintInfo.FOREIGN_KEY;

      return item;
   }

   public void ProcessItem(ImportItemInfo item)
   {
      if (ItemColumnInfo.TryParseMySqlType(
         item.DataType, out ItemColumnInfo colInfo))
      {
         item.DataType = colInfo.Type;
         item.CharacterMaximumLength = colInfo.Length;
         item.Precision = colInfo.Precision;
         item.Scale = colInfo.Scale;
         item.DataTypeIsUnsigned = colInfo.IsUnsigned;
      }

      if (_Mapper != null)
      {
         var element = _Mapper.MapText(
            item.DataType.ToLowerInvariant(), DataTextMapDirection.From);
         if (!String.IsNullOrWhiteSpace(element))
         {
            var indx = element.IndexOf(
               "(MAX)", StringComparison.OrdinalIgnoreCase);
            if (indx > 0)
            {
               element = element.Replace(
                  "(MAX)", String.Empty, StringComparison.OrdinalIgnoreCase);
               item.CharacterMaximumLength = int.MaxValue;
            }
            item.DataType = element;
         }
      }

      return;
   }

   /// <summary>
   /// Import Asset Data from provided values.
   /// </summary>
   /// <param name="values">import values information</param>
   /// <returns>results log is returned</returns>
   public static ResultLog ImportAssetData(
      ImportValues values, string mapperFilePath)
   {
      ImportAsset importer = new ImportAsset(values, mapperFilePath);

      values.Header = values.Rows[0];
      foreach (var list in values.Rows)
      {
         // skip empty rows
         if (ExcelDocumentReader.IsEmptyList(list))
         {
            continue;
         }
         values.Values = list;

         var item = ImportItem(values);
         importer.ProcessItem(item);

         values.Items.Add(item);
      }

      // remove header row
      if (values.Items.Count > 0)
      {
         values.Items.RemoveAt(0);
      }

      return importer.Results;
   }

}
