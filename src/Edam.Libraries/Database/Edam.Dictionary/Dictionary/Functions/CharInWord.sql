CREATE FUNCTION Dictionary.CharInWord(
   @word VARCHAR(128), @chr VARCHAR(128))
RETURNS bit
AS
BEGIN
   DECLARE @hasChar BIT
   SET @hasChar = case when @word like '%'+@chr+'%' then 1 else 0 end
   RETURN @hasChar
END
GO