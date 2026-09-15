-- DROP TABLE dbo.tmpDictionary;
CREATE TABLE dbo.tmpDictionary (
   KeyNo INT IDENTITY PRIMARY KEY,
   Category VARCHAR(40),
   Context VARCHAR(40),
   Term VARCHAR(128),
   Description VARCHAR(4000),
   TermCount INT DEFAULT 1,
   keyNo_ INT
)
GO

-- DELETE FROM dbo.tmpDictionary;
INSERT INTO dbo.tmpDictionary (Category, Context, Term, Description)
SELECT [Category]
      ,[Context]
      ,[Term]
      ,[Description]
  FROM [CACT_BMS].[dbo].[Glossary-Full Tab-Delimited]
GO

-- drop table dbo.tmpDictionaryDuplicates;
SELECT Term, Description, cnt, keyNo
  INTO dbo.tmpDictionaryDuplicates
  FROM (
SELECT Term, Description, min(KeyNo) keyNo, count(*) cnt 
  FROM dbo.tmpDictionary 
 WHERE Term = Term
 GROUP BY Term, Description) x
 WHERE cnt > 1
GO

UPDATE d
   SET d.TermCount = 1
  FROM dbo.tmpDictionary d
  JOIN dbo.tmpDictionaryDuplicates x
    ON d.Term = x.Term
   AND d.Description = x.Description
GO

UPDATE d
   SET d.keyNo_ = x.keyNo
-- SELECT *
  FROM dbo.tmpDictionary d
  JOIN dbo.tmpDictionaryDuplicates x
    ON d.Term = x.Term
   AND d.Description = x.Description
GO

-- Finally remove all duplicates...
DELETE 
--SELECT *
  FROM dbo.tmpDictionary
 WHERE TermCount > 1
   AND keyNo_ <> KeyNo
GO

-- select * from dbo.tmpDictionary where term = 'NBIS'

-- delete from [Edam.Dictionary].Dictionary.Word
DECLARE @cdt DATETIMEOFFSET(7) = sysdatetimeoffset()

INSERT INTO [Edam.Dictionary].Dictionary.Word (
   LexiconID,    Category,     Context,
   Term,         OriginalTerm, [Soundex],  
   Confidence,   Definition,   Status,
   DataOwnerID,  DataSourceID, CreatedDate,
   UpdatedDate,  KeyID)
SELECT 'common.en' LexiconID,
       lower(Category) Category,
	   Context,
	   Term,
	   Term,
	   soundex(Term) TermSoundex,
	   1.0 Confidence,
	   Description,
	   2 Status,
	   'EDAM' DataOwnerID,
	   'CA-DOT' DataSourceID,
	   @cdt CreatedDate,
	   @cdt UpdatedDate,
	   NEWID() KeyId
  FROM CACT_BMS.dbo.[Glossary-Full Tab-Delimited]
GO

-- -----------------------------------------------------------------------------
-- FREE API DICTIONARY SUPPORT
-- Check the Words Queue where you get the term being validated
-- by an external dictionary (2 = found; 3 = not-found)
SELECT [KeyID]
      ,[LexiconID]
      ,[Category]
      ,[Context]
      ,[Term]
      ,[OriginalTerm]
      ,[Soundex]
      ,[Confidence]
      ,[Definition]
      ,[Status]
      ,[DataOwnerID]
      ,[DataSourceID]
      ,[CreatedDate]
      ,[UpdatedDate]
  FROM [Edam.Dictionary].[Dictionary].[WordQueue]
 WHERE UpdatedDate >= '2024-03-17'
 ORDER BY UpdatedDate DESC
 
SELECT Status, count(*) cnt
  FROM [Edam.Dictionary].[Dictionary].[WordQueue]
 GROUP BY Status
