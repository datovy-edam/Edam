
   UPDATE x
      SET x.Confidence = j.Confidence
-- SELECT *
     FROM [Edam.Lexicon].Vocabulary.Term x
	 JOIN (
   SELECT t.KeyID,
          t.Term,
		  Confidence = case when w.Term is null
                       then t.Confidence else 1.0 end
     FROM [Edam.Lexicon].Vocabulary.Term t
     LEFT JOIN [Edam.Dictionary].Dictionary.WordQueue w
       ON t.Term = w.Term) j
       ON j.KeyID = x.KeyID

WITH trms AS (
   SELECT KeyId, Term FROM [Edam.Lexicon].Vocabulary.Term
)
SELECT *
  FROM trms

DECLARE @tot INT = (SELECT count(*) FROM [Edam.Lexicon].Vocabulary.Term)
SELECT @tot

SELECT t.Confidence,
       count(*) cnt,
	   cast((count(*) / (@tot * 1.0)) * 100.00 as decimal(10,2)) pcnt
  FROM [Edam.Lexicon].Vocabulary.Term t
 GROUP BY t.Confidence
 ORDER BY t.Confidence desc


SELECT count(*) FROM [Edam.Dictionary].Dictionary.WordQueue

SELECT ((16.0 / 1605.0) * 100.0) - 1.0




