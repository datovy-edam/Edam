-- drop function dbo.HasNumberOrSymbol
CREATE FUNCTION Dictionary.HasNumberOrSymbol(
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
