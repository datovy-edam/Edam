
/*
SELECT * 
  FROM Data.DataDomain
 */

-- DROP FUNCTION Data.EntityDetailFunc
CREATE FUNCTION Data.EntityDetailFunc (
   @DomainNo INT
)
RETURNS @tbl TABLE (
   ElementID           VARCHAR(20),
   ElementUri          VARCHAR(256),
   ElementParentNo     INT,
   ElementParentID     VARCHAR(20),
   Guid                VARCHAR(40),
   DomainNo            INT,
   NamespacePrefix     VARCHAR(120),
   NamespaceName       VARCHAR(120),
   EntityQualifiedName VARCHAR(120),
   EntityName          VARCHAR(120),
   EntityPath          VARCHAR(256),
   OriginalName        VARCHAR(120),
   Tags                VARCHAR(256),
   ExpiredDate         DATETIME,
   ElementCount        INT,
   ElementKeyCount     INT
)
AS
BEGIN
   INSERT INTO @tbl (ElementID,     ElementUri,   ElementParentNo,
                     ElementParentID, Guid,          
                     DomainNo,      NamespacePrefix,
                     NamespaceName, EntityQualifiedName, 
                     EntityName,    EntityPath,   OriginalName, Tags,
					 ExpiredDate,   ElementCount, ElementKeyCount)
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
		  NamespaceName = (SELECT value FROM string_split(ElementPath, '/', 1)
                            WHERE ordinal = 1),
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
		  KeyCount     = (SELECT count(*) FROM Data.DataElement p
                           WHERE p.DomainNo = e.DomainNo
                             AND p.ElementTypeNo = 1
							 AND p.ElementKeyTypeNo = 2
							 AND p.ElementPath like e.ElementPath + '%')
     FROM Data.DataElement e
    WHERE DomainNo = @DomainNo
      AND ElementTypeNo = 5   -- a type
   RETURN
END
GO
