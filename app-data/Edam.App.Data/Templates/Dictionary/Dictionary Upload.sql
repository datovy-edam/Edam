CREATE FUNCTION dbo.CharInWord(
   @word VARCHAR(128), @chr VARCHAR(128))
RETURNS bit
AS
BEGIN
   DECLARE @hasChar BIT
   SET @hasChar = case when @word like '%'+@chr+'%' then 1 else 0 end
   RETURN @hasChar
END
GO

-- drop function dbo.HasNumberOrSymbol
CREATE FUNCTION dbo.HasNumberOrSymbol(
   @term VARCHAR(128)
)
RETURNS bit
BEGIN
DECLARE @has BIT
SET @has = case when @term LIKE '%0%'
   OR  @term LIKE '%1%'
   OR  @term LIKE '%2%'
   OR  @term LIKE '%3%'
   OR  @term LIKE '%4%'
   OR  @term LIKE '%5%'
   OR  @term LIKE '%6%'
   OR  @term LIKE '%7%'
   OR  @term LIKE '%8%'
   OR  @term LIKE '%9%'
   OR  @term LIKE '%$%'
   OR  @term LIKE '%$%'
   OR  @term LIKE '%#%' then 1 else 0 end
RETURN @has
END
GO

/*
SELECT dbo.HasNumberOrSymbol('#');

SELECT [KeyID]
      ,[LexiconID]
      ,[BusinessDomainID]
      ,[Category]
      ,[Tag]
      ,[TermInclude]
      ,[Term]
      ,[OriginalTerm]
      ,[Synonyms]
      ,[Confidence]
      ,[Description]
      ,[Count]
  FROM [Edam.Dictionary].[dbo].[TermsView]
 WHERE Confidence <= 0.0
 ORDER BY Term

 */

INSERT INTO [Edam.Dictionary].[Dictionary].[WordQueue] (
       KeyID,
       LexiconID,
	   Category,
	   Term,
	   OriginalTerm,
	   Soundex,
	   Confidence,
	   Definition,
	   Status,
	   CreatedDate,
	   UpdatedDate
)
SELECT KeyID,
       LexiconID,
	   Category,
	   Term,
	   OriginalTerm,
	   Soundex,
	   Confidence,
	   Definition,
	   0,
	   SYSDATETIMEOFFSET(),
	   SYSDATETIMEOFFSET()
  FROM [DbTemp].[dbo].[word]
GO

-- drop view dbo.WordView
CREATE VIEW dbo.WordView AS
SELECT cast(newid() as varchar(40)) KeyID,
       cast('common.en' as varchar(128)) LexiconID,
	   Category = case when substring([Column 0],1,1) = upper(substring([Column 0],1,1)) collate SQL_Latin1_General_CP1_CS_AS
	              then 'noun' else 'word' end,
       lower([Column 0]) Term, 
       [Column 0] OriginalTerm, 
       Soundex([Column 0]) [Soundex],
       cast(1.0 as decimal(20,2)) Confidence,
	   cast('' as varchar(max)) Definition
	   -- , dbo.HasNumberOrSymbol([Column 0]) HasNumberOrSymbol
  FROM [DbTemp].[dbo].[words]
 WHERE charindex('&', [Column 0]) = 0
   AND charindex('''', [Column 0]) = 0
   AND charindex('.', [Column 0]) = 0
   AND charindex('/', [Column 0]) = 0
   AND charindex('-', [Column 0]) = 0
   AND charindex(',', [Column 0]) = 0
   AND charindex('#', [Column 0]) = 0
   AND charindex('$', [Column 0]) = 0
   AND [Column 0] NOT IN (
          'a','aa','aaa','aaaa','aaal','aaas','aaaaaa',
		  'aae','aaee','aar','aao','aau')
   AND dbo.HasNumberOrSymbol([Column 0]) = 0
   --AND ([Column 0] NOT LIKE '%0%'
   --AND  [Column 0] NOT LIKE '%1%'
   --AND  [Column 0] NOT LIKE '%2%'
   --AND  [Column 0] NOT LIKE '%3%'
   --AND  [Column 0] NOT LIKE '%4%'
   --AND  [Column 0] NOT LIKE '%5%'
   --AND  [Column 0] NOT LIKE '%6%'
   --AND  [Column 0] NOT LIKE '%7%'
   --AND  [Column 0] NOT LIKE '%8%'
   --AND  [Column 0] NOT LIKE '%9%'
   --AND  [Column 0] NOT LIKE '%$%'
   --AND  [Column 0] NOT LIKE '$%'
   --AND  [Column 0] NOT LIKE '#%')
   AND [Column 0] <> upper([Column 0]) collate SQL_Latin1_General_CP1_CS_AS
   AND len(trim([Column 0])) > 3
GO

-- drop view dbo.TermsView
CREATE VIEW dbo.TermsView AS
SELECT t.[KeyID]
      ,t.[LexiconID]
      ,t.[BusinessDomainID]
      ,t.[Category]
      ,isnull(t.[Tag], '') Tag
      ,TermInclude = case when dbo.HasNumberOrSymbol(t.Term) = 1
                     then 0 else t.[TermInclude] end
      ,t.[Term]
      ,t.[OriginalTerm]
      ,t.[Synonyms]
      ,[Confidence] = case when dbo.HasNumberOrSymbol(t.Term) = 1
                      then -1.0 else
                      case when t.Confidence >= 0.4
	                  then
					  case when w.Term is not null
					       then 1.00 else t.Confidence end
					  else t.Confidence
					  end
					  end
      ,t.[Description]
      ,t.[Count]
  FROM [Edam.Lexicon].[Vocabulary].[Term] t
  LEFT JOIN [Edam.Dictionary].[dbo].WordsView w
    ON t.Term = w.Term
	
SELECT KeyID, BusinessDomainID, Category, Tag, TermInclude, Term, OriginalTerm, Synonyms, Confidence, Description, Count
  FROM dbo.TermsView
  
SELECT Confidence, count(*) TermsCount
  FROM dbo.TermsView
 GROUP BY Confidence

/*
UPDATE [Edam.Lexicon].[Vocabulary].[Term] 
   SET Confidence = 0.4
 WHERE Confidence = 0.8
 
UPDATE [Edam.Lexicon].[Vocabulary].[Term] 
   SET Confidence = 0.1
 WHERE Confidence = 0.3

SELECT substring('$MDZAE',1,1) = '$' IN ('$')
SELECT case when '$$YW' like '%$%' then 1 else 0 end

 */

SELECT case when '$MDZAE' like '%$%' then 1 else 0 end

SELECT [KeyID]
      ,[LexiconID]
      ,[Category]
      ,[Term]
      ,[OriginalTerm]
      ,[Soundex]
      ,[Confidence]
      ,[Definition]
      ,[Status]
      ,[CreatedDate]
      ,[UpdatedDate]
  FROM [Edam.Dictionary].[Dictionary].[WordQueue]

/*
UPDATE [Edam.Dictionary].[Dictionary].[WordQueue]
   SET Status = 0
 WHERE Status = 3
 */
