
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
GO

SELECT Status, count(*) cnt
  FROM [Edam.Dictionary].[Dictionary].[WordQueue]
 GROUP BY Status
GO
