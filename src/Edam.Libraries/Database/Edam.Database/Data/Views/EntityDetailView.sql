-- DROP VIEW [Data].[EntityDetailView]
CREATE VIEW [Data].[EntityDetailView]
   AS
SELECT ElementID,
       ElementUri,
       ParentNo ElementParentNo,
       ElementParentID = (SELECT TOP 1 ElementID
                            FROM Data.DataElement
                           WHERE ParentNo = e.ParentNo
                             AND ElementTypeNo = 5
                             AND DomainNo = e.DomainNo
                             AND ElementPath like e.ElementPath + '%'),
       Guid,
       DomainNo,
       NamespacePrefix = (SELECT value FROM string_split ((
                          SELECT value FROM string_split(ElementPath, '/', 1)
                           WHERE ordinal = 1), ':', 1)
                           WHERE ordinal = 1),
       NamespaceName = Domain,
       ElementName EntityQualifiedName,
       EntityName = (SELECT value FROM string_split(ElementName, ':', 1)
                      WHERE ordinal = 2),
       ElementPath,
       OriginalName,
       Tags,
       ExpiredDate,
       ElementCount = (SELECT count(*) FROM Data.DataElement p
                        WHERE p.DomainNo = e.DomainNo
                          AND p.ElementTypeNo = 1
							 AND p.ElementPath like e.ElementPath + '%'),
       KeyCount = (SELECT count(*) FROM Data.DataElement p
                    WHERE p.DomainNo = e.DomainNo
                      AND p.ElementTypeNo = 1
                      AND p.ElementKeyTypeNo = 2
                      AND p.ElementPath like e.ElementPath + '%'),
       RelationshipCount = (SELECT count(*) FROM Data.EntityElementConstraintView
                             WHERE ElementID in (
                            SELECT ElementID FROM Data.EntityElementDetailView
                             WHERE ElementParentID = e.ElementID)
                               AND ConstraintType = 1),  -- a foreign key
       ReferencedCount = (SELECT count(*) FROM Data.EntityElementConstraintView
                           WHERE ReferenceID = e.ElementID
                             AND ConstraintType = 1)  -- a foreign key
  FROM Data.DataElement e
 WHERE ElementTypeNo = 5   -- a type

 -- SELECT * FROM Data.EntityDetailView WHERE DomainNo = 1
 -- SELECT * FROM Data.EntityElementConstraintView
