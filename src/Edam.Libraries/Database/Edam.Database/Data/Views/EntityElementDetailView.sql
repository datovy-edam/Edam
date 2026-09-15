-- DROP VIEW [Data].[EntityElementDetailView]
CREATE VIEW [Data].[EntityElementDetailView]
   AS
SELECT ElementID,
       ElementUri,
       ParentNo ElementParentNo,
       Guid,
       DomainNo,
       NamespacePrefix = (SELECT value FROM string_split ((
                          SELECT value FROM string_split(ElementPath, '/', 1)
                           WHERE ordinal = 1), ':', 1)
                           WHERE ordinal = 1),
       NamespaceName = Domain,
       d.ElementParentID,
	   d.ElementParentName,
       ElementName EntityQualifiedName,
       ElementName = (SELECT value FROM string_split(ElementName, ':', 1)
                       WHERE ordinal = 2),
       ElementPath,
       OriginalName,
       Tags,
       ExpiredDate
  FROM Data.DataElement e
 CROSS APPLY (
SELECT ElementID ElementParentID,
       case when right(OriginalName, 1) = '_'
       then substring(OriginalName,1,len(OriginalName) - 1)
       else OriginalName end ElementParentName
  FROM Data.DataElement
 WHERE ParentNo = e.ParentNo
   AND ElementTypeNo = 5
   AND DomainNo = e.DomainNo
   AND Domain = e.Domain) d
 WHERE ElementTypeNo = 1   -- an element
   AND DomainNo = 1

 -- SELECT * FROM Data.EntityElementDetailView WHERE DomainNo = 1

 /*
 SELECT ElementParentID 
  FROM Data.EntityElementDetailView
  WHERE DomainNo = 1
    AND NamespaceName = 'Management'
    AND ElementParentName = 'Referral'
    AND OriginalName = 'Referral_ID'
    AND ElementParentID is not null
  */
