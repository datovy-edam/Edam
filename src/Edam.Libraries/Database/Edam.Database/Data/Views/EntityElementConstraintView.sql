-- DROP VIEW [Data].[EntityElementConstraintView]
CREATE VIEW [Data].[EntityElementConstraintView]
   AS
SELECT e.ElementID,
       e.ParentNo ElementParentNo,
       ElementParentID = (SELECT TOP 1 ElementID
                            FROM Data.DataElement
                           WHERE ParentNo = e.ParentNo
                             AND ElementTypeNo = 5
                             AND DomainNo = e.DomainNo
                             AND e.ElementPath like ElementPath + '%'),
       e.ElementPath,
       c.ConstraintName,
       c.ConstraintType,
       c.ConstraintDescription,
	   ReferenceID = (SELECT ElementParentID FROM Data.EntityElementDetailView
                       WHERE DomainNo = e.DomainNo
                         AND NamespaceName = c.ReferenceSchemaName
                         AND ElementParentName = c.ReferenceEntityName
                         AND ElementName = c.ReferenceElementName
                         AND ElementParentID is not null),
       c.ReferenceSchemaName,
       c.ReferenceEntityName,
       c.ReferenceElementName
  FROM Data.DataElement e
 CROSS APPLY (
SELECT *
  FROM openjson(cast((
SELECT ConstraintsText
  FROM Data.DataElement
 WHERE DomainNo = 1 and ElementID = e.ElementID
   AND ConstraintsText is not null
   AND ConstraintsText <> ''
   AND ConstraintsText <> '[]') as nvarchar(max)),N'$') WITH (
       ConstraintName VARCHAR(128) N'$.ContraintName',
       ConstraintType INT N'$.ContraintType',
       ConstraintDescription VARCHAR(1024) N'$.ContraintDescription',
       ReferenceSchemaName VARCHAR(128) N'$.ReferenceSchemaName',
       ReferenceEntityName VARCHAR(128) N'$.ReferenceEntityName',
       ReferenceElementName VARCHAR(128) N'$.ReferenceElementName'
    )) c
 WHERE e.ElementTypeNo = 1  -- an element
 GO

 -- SELECT * FROM Data.EntityElementConstraintView
 /*
 
 SELECT ReferenceID, ReferenceSchemaName, ReferenceEntityName, ReferenceElementName, count(*) cnt
   FROM Data.EntityElementConstraintView
  GROUP BY ReferenceID, ReferenceSchemaName, ReferenceEntityName, ReferenceElementName
  ORDER BY ReferenceID, ReferenceSchemaName, ReferenceEntityName, ReferenceElementName

 SELECT ReferenceID, ReferenceSchemaName, ReferenceEntityName, ReferenceElementName, count(*) cnt
   FROM Data.EntityElementConstraintView
  GROUP BY ReferenceID, ReferenceSchemaName, ReferenceEntityName, ReferenceElementName
  ORDER BY ReferenceID, ReferenceSchemaName, ReferenceEntityName, ReferenceElementName
  */


