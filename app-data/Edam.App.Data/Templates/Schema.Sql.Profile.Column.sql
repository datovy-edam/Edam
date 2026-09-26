-- generate SQL for getting column stats

-- DROP PROCEDURE dbo.TableColumnProfile
CREATE PROCEDURE dbo.TableColumnProfile
   @SchemaName VARCHAR(128),
   @TableName  VARCHAR(128)
AS
BEGIN
   SET NOCOUNT ON
   DECLARE @tableCursor CURSOR;
   DECLARE @schName VARCHAR(128),
           @tblName nvarchar(max),
           @ColumnName nvarchar(max),
           @DataType nvarchar(max),
           @SqlText nvarchar(max);

   IF EXISTS (SELECT * FROM tempdb.sys.tables WHERE name like '#colstats%')
      DROP TABLE #colstats
   
   CREATE TABLE #colstats(
      SchemaName VARCHAR(128),
      TableName NVARCHAR(MAX), 
      ColumnName NVARCHAR(MAX), 
      DataType NVARCHAR(MAX), 
      DistinctCount int, 
      NullCount int, 
      MinValue NVARCHAR(MAX), 
      MaxValue NVARCHAR(MAX),
      MinLength INT,
      MaxLength INT);

   SET @tableCursor = CURSOR FOR
   WITH T AS
   (
      SELECT s.Name AS Schema_Name,
             t.Name AS Table_Name,
             t.object_id,
             t.type_desc,
             p.rows AS Row_Count,
             CAST(ROUND((SUM(a.used_pages) / 128.00), 2) AS NUMERIC(36, 2)) AS Used_MB,
             CAST(ROUND((SUM(a.total_pages) - SUM(a.used_pages)) / 128.00, 2) AS NUMERIC(36, 2)) AS Unused_MB,
             CAST(ROUND((SUM(a.total_pages) / 128.00), 2) AS NUMERIC(36, 2)) AS Total_MB
        FROM sys.tables t
       INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
       INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
       INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
       INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
       WHERE p.index_id IN ( 0, 1 )
       GROUP BY t.Name, t.object_id,t.type_desc,s.Name, p.Rows
   )
   SELECT --top 10 
          T.schema_name,
          T.Table_name ,
          c.name ,
          tt.name  
     FROM sys.all_columns c
     JOIN T on  (c.object_id=T.object_id and T.type_desc='USER_TABLE')
     JOIN sys.types tt on (c.system_type_id=tt.user_type_id)
    WHERE T.row_count>0
      AND T.table_name  = @TableName
    ORDER BY 1,2,3
   ;

   open @tableCursor;
   fetch next from @tableCursor into @schName, @tblName, @ColumnName, @DataType;
   WHILE @@FETCH_STATUS = 0 
   BEGIN
      set @SqlText = 'insert into #colstats select ' +
          '''' + @schName + ''' SchemaName,' +
          '''' + @tblName + ''' TableName,' +
          '''' + @ColumnName + ''' ColumnName,' +
          '''' + @DataType + ''' DataType,' + 
          'count(distinct ' + @ColumnName + ') DistinctCount, ' +
          'sum(case when ' +  @ColumnName + ' is null then 1 else 0 end) NullCount, ' +
          'isnull(cast(min(' + @ColumnName + ') as varchar(255)),'''') MinVal, ' +
          'isnull(cast(max(' + @ColumnName + ') as varchar(255)),'''') MaxVal, ' +
          'isnull(min(len(' + @ColumnName + ')),-1) MinLen, ' +
          'isnull(max(len(' + @ColumnName + ')),-1) MaxLen ' +
          'from ' + @schName + '.' + @tblName + ';'

      --SELECT @SqlText
      EXECUTE (@SqlText);
      FETCH NEXT FROM @tableCursor INTO @schName, @tblName, @ColumnName, @DataType;
   END
   CLOSE @tableCursor;
   DEALLOCATE @tableCursor;

   SELECT * FROM #colstats;
END
GO

-- EXEC dbo.TableColumnProfile 'ActivityLocation'
