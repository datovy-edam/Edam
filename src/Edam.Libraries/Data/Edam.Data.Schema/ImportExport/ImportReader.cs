using DocumentFormat.OpenXml.Spreadsheet;
using Edam.Application;
using Edam.Data.Asset;
using Edam.Data.AssetConsole;
using Edam.Data.AssetManagement;
using Edam.Data.Assets.AssetSchema;
using Edam.Data.AssetSchema;
using Edam.Data.Schema.DataDefinitionLanguage;
using Edam.DataObjects.Models;
using Edam.Diagnostics;
using Edam.Xml.OpenXml;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjAssets = Edam.DataObjects.Assets;

// -----------------------------------------------------------------------------

namespace Edam.Data.Schema.ImportExport;

public class ImportValues
{
   public List<string> Values { get; set; }
   public List<string> Header { get; set; }
   public List<List<string>> Rows { get; set; }
   public List<ImportItemInfo> Items = new List<ImportItemInfo>();
}

public class ImportReader : IDataAssets
{
   private const string CLASS_NAME = "ImportReader";

   private DataTextMap m_Mapper;
   private AssetConsoleArgumentsInfo m_Arguments;

   private string m_VersionId
   {
      get { return m_Arguments.Project.VersionId; }
   }

   public List<string> GetFileList(AssetConsoleArgumentsInfo arguments)
   {
      var uriList = UriResourceInfo.GetUriList(
        arguments.UriList, UriResourceType.xsd);
      return null;
   }

   public IResultsLog ToDatabase(AssetConsoleArgumentsInfo arguments)
   {
      var uriList = UriResourceInfo.GetUriList(
        arguments.UriList, UriResourceType.xsd);
      foreach (var i in uriList)
      {
         
      }

      return null;
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="assetProperties"></param>
   /// <param name="items"></param>
   /// <param name="namespaces"></param>
   /// <param name="rootName"></param>
   /// <returns></returns>
   public List<AssetData> ToAssetData(
      AssetProperties assetProperties, List<ImportItemInfo> items,
      NamespaceList namespaces, string rootName)
   {
      if (items == null || items.Count == 0)
      {
         return null;
      }

      var header = items[0];

      String guidText = Guid.NewGuid().ToString();
      NamespaceInfo ns = namespaces == null || namespaces.Count == 0 ?
         new NamespaceInfo(guidText, "http://" + guidText) : namespaces[0];

      SortedDictionary<string, DdlAsset> dassets = 
         new SortedDictionary<string, DdlAsset>();

      DdlAsset dasset = null, previousAsset = null;

      ElementPropertyInfo xproperty;
      ElementPropertyInfo entityProperty;
      ImportItemInfo entityDeclaration = null;

      int resourceCount = 0;
      int schemaCount = 1;

      foreach (var item in items)
      {
         item.OrdinalNo = ++resourceCount;

         entityProperty = assetProperties.Find(
            item.TableSchema, item.TableName, String.Empty,
            ElementPropertyType.Description);

         // is this an entity declaration? if so, remember it
         if (string.IsNullOrWhiteSpace(item.ColumnName) &&
            item.DataType.ToLower() == "object")
         {
            entityDeclaration = item;
            continue;
         }

         // is element mapped to an external property?
         xproperty = assetProperties.Find(
            item.TableSchema, item.TableName, item.ColumnName,
            ElementPropertyType.ExternalReference);

         if (!dassets.TryGetValue(item.TableSchema, out dasset))
         {
            if (previousAsset != null)
            {
               previousAsset.PrepareAdditionalColumns();
            }

            dasset = new DdlAsset(header, namespaces, ns, m_Mapper,
               item.TableSchema, m_VersionId, schemaCount);
            dasset.asset.CatalogName = item.TableCatalog;
            previousAsset = dasset;

            dassets.Add(item.TableSchema, dasset);
            schemaCount++;
         }

         // this is a new table definition
         if (dasset == null || 
            item.TableName != previousAsset.originalTableName)
         {
            // add additional columns (if any has been specified)
            previousAsset.PrepareAdditionalColumns();

            // prepare new table definition...
            var tbl = dasset.PrepareTableDefinition(item, item.TableName);
            dasset.originalTableName = item.TableName;

            // if ColumnName is null the description for the Table
            // is there an entity declaration? if so, get its description....
            if (entityDeclaration != null &&
               entityDeclaration.TableName == item.TableName)
            {
               // assign TAGS
               tbl.Tags = entityDeclaration.Tags;

               // setup annotation
               tbl.Annotation.Clear();
               tbl.AddAnnotation(entityDeclaration.ColumnDescription);
               entityDeclaration = null;
            }
            else if (item.ColumnName == null)
            {
               tbl.Annotation.Clear();
               tbl.AddAnnotation(item.ColumnDescription);
            }

            if (entityProperty != null)
            {
               dasset.UpdateEntityProperty(entityProperty);
            }
            //continue;
         }

         // prepare/add table column definition
         var celement = dasset.PrepareColumnDefinition(item);
         if (xproperty != null && celement != null)
         {
            DdlAsset.UpdateEntityProperty(xproperty, celement);
         }
      }

      if (dasset != null)
      {
         dasset.PrepareAdditionalColumns();
      }

      // add property bags of last items for each ddl asset...
      List<AssetData> assets = new List<AssetData>();
      foreach (var i in dassets)
      {
         i.Value.AddPropertyBag();
         assets.Add(i.Value.asset);
      }

      // merge assets...
      AssetData assetData = AssetData.Merge(assets, header.TableCatalog,
         header.TableCatalog, "schemaName", "description", "title", ns,
         AssetType.Schema, m_VersionId);

      // prepare catalog document...
      var documentItems = PrepareCatalogDocument(header, dassets, ns);
      foreach (var item in documentItems)
      {
         assetData.Add(item);
      }

      assets.Clear();
      assets.Add(assetData);

      return assets;
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="header"></param>
   /// <param name="asset"></param>
   /// <param name="ns"></param>
   /// <returns></returns>
   private AssetDataElementList PrepareCatalogDocument(
      ImportItemInfo header,
      SortedDictionary<string, DdlAsset> asset, NamespaceInfo ns)
   {
      var appSettings = AppAssembly.FetchInstance(
         AssetResourceHelper.ASSET_APP_SETTINGS) as IAppSettings;
      string typePostfix = appSettings == null ? 
         String.Empty : appSettings.GetTypePostfix();

      // prepare root element
      AssetDataElementList documentList =
         new AssetDataElementList(ns, AssetType.Schema, m_VersionId);

      // add document type
      string rootElementName = header.TableCatalog + typePostfix;
      var documentSchema = DdlAsset.PrepareTypeDefinition(ElementType.type,
         String.Empty, rootElementName, header.TableCatalog, ns.Prefix,
         Session.OrganizationId, ns);
      documentSchema.InclusionType = DataElementInclusionType.Exclude;
      documentList.Add(documentSchema);
      AssetDataElementList list = 
         new AssetDataElementList(ns, AssetType.Schema, m_VersionId);

      // prepare type that include all registered schemas... add child types
      foreach (var schema in asset.Values)
      {
         // add schema type
         string tName = schema.asset.SchemaName + typePostfix;
         var parentSchema = DdlAsset.PrepareTypeDefinition(
            ElementType.type,
            String.Empty, tName, schema.asset.SchemaName, schema.ns.Prefix,
            Session.OrganizationId, schema.ns);
         parentSchema.InclusionType = DataElementInclusionType.Exclude;
         list.Add(parentSchema);

         // prepare an entry per schema table... as an element
         foreach (var table in schema.Tables)
         {
            var item = schema.PrepareElementDefinition(
               parentSchema, table.Item as ImportItemInfo, true);
            item.MaxOccurrence = int.MaxValue;
            item.InclusionType = DataElementInclusionType.Exclude;
            list.Add(item);
         }

         ImportItemInfo itm = new ImportItemInfo();
         itm.TableSchema = String.Empty;
         itm.ColumnName = schema.ns.Prefix + ":" + schema.asset.SchemaName;
         itm.DataType = parentSchema.DataType;
         itm.TableName = schema.asset.SchemaName;
         itm.CharacterMaximumLength = 0;

         var ditem = schema.PrepareElementDefinition(
            documentSchema, itm, true);
         ditem.Domain = ns.Prefix;
         ditem.InclusionType = DataElementInclusionType.Exclude;
         documentList.Add(ditem);
      }

      // prepare root and document elements...
      DdlAsset rootAsset = new DdlAsset(
         header, null, ns, null, null, m_VersionId, 0);

      // add schema type
      var rootSchema = DdlAsset.PrepareTypeDefinition(ElementType.type,
         String.Empty, header.TableCatalog, header.TableCatalog, ns.Prefix,
         Session.OrganizationId, ns);
      //list.Add(rootSchema);

      // add schema as a child of document
      ImportItemInfo ritm = new ImportItemInfo();
      ritm.TableSchema = String.Empty;
      ritm.ColumnName = rootSchema.ElementName;
      ritm.DataType = rootSchema.DataType;
      ritm.TableName = rootSchema.ElementQualifiedName.OriginalName;
      ritm.CharacterMaximumLength = 0;

      var rootDocument = rootAsset.PrepareElementDefinition(
         null, ritm, true, true);
      rootDocument.Domain = ns.Prefix;
      documentList.Add(rootDocument);

      // add document element declaration
      //fitm.TableName = rootAsset.asset.CatalogName;
      //var root = rootAsset.PrepareElementDefinition(null, fitm, true, true);
      //documentList.Add(root);

      list.AddRange(documentList);

      return list;
   }

   /// <summary>
   /// Read input file as specified in arguments and convert it to a Data 
   /// Asset (collection of Data Elements).
   /// </summary>
   /// <param name="arguments">arguments</param>
   /// <returns>results log</returns>
   public IResultsLog ToAsset(AssetConsoleArgumentsInfo arguments)
   {
      string func = "DataSchema::ImportReader::ToAsset";
      m_Arguments = arguments;

      IResultsLog resultsLog = new ResultLog();

      NamespaceList namespaces = new NamespaceList();
      namespaces.Add(arguments.Namespace);

      m_Mapper = DataTextMap.FromFile(arguments);

      var uriList = UriResourceInfo.GetUriList(
        arguments.UriList, UriResourceType.xlsx);

      if (uriList.Count <= 0)
      {
         ResultLog.Trace(
            message: "No files to process found (check file extentions).", 
            source: func, SeverityLevel.Info);
      }

      foreach (var fname in uriList)
      {
         var docList = ExcelDocumentReader.ReadDocument(
            fname, "Documentation");
         AssetProperties doc = docList.Success ?
            AssetProperties.GetInstance(docList.Data) : 
               new AssetProperties(
                  new List<ElementPropertyInfo>());

         ImportValues values = new ImportValues();
         var results = ExcelDocumentReader.ReadDocument(
            fname, arguments.Domain.DomainId);
         if (results.Success)
         {
            values.Rows = results.Data;
            var result = ImportAsset.ImportAssetData(
               values, arguments.TextMapFilePath);

            // prepare Asset Data definitions (one per schema)
            var assets = ToAssetData(doc, values.Items, namespaces,
               arguments.RootElementName);

            if (assets != null)
            {
               if (arguments.AssetDataItems == null)
               {
                  arguments.AssetDataItems = new AssetDataList();
               }
               foreach (var a in assets)
               {
                  a.Namespaces.Add(NamespaceInfo.GetW3CNamespace());
               }
               arguments.AssetDataItems.AddRange(assets);
            }
         }
         else
         {
            ResultLog.Trace(
               message: "TAB (" + arguments.Domain.DomainId + ") not found.",
               source: func, SeverityLevel.Info);
         }
      }
      resultsLog.Succeeded();
      return resultsLog;
   }

}
