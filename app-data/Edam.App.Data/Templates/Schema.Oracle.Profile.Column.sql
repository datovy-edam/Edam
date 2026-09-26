
/*
   DELETE FROM ora$out;
   TRUNCATE TABLE ora$out;
   DROP TABLE ora$out;
 */

-- INSTRUCTIONS:
-- 1) run this script
-- 2) to output results use this query:  SELECT * FROM ora$out;
-- 3) store the results in a csv file and sent it to me.
-- DONE.

CREATE GLOBAL TEMPORARY TABLE ora$out (
   schema_Name VARCHAR2(128),
   table_Name  VARCHAR2(128),
   column_Name VARCHAR2(128),
   data_Type   VARCHAR2(128),
   distinct_Count NUMBER,
   null_Count  NUMBER,
   min_Value   VARCHAR2(1024),
   max_Value   VARCHAR2(1024),
   min_Length  NUMBER,
   max_Length  NUMBER
)  ON COMMIT PRESERVE ROWS;

TRUNCATE TABLE ora$out;

--SET SERVEROUTPUT ON;
DECLARE
   
   TYPE sqlCols IS RECORD (
      schemaName VARCHAR2(128),
      tableName  VARCHAR2(128),
      columnName VARCHAR2(128),
      dataType   VARCHAR2(128),
      distinctCount NUMBER,
      nullCount  NUMBER,
      minVal     VARCHAR2(1024),
      maxVal     VARCHAR2(1024),
      minLeng    NUMBER,
      maxLeng    NUMBER);

   --TYPE collectionType IS TABLE OF sqlCols;
   collection sqlCols;
   
   CURSOR sqlStmt_cur IS
   SELECT 'SELECT '''||tbc.owner||''' SchemaName, '''
          ||tbc.table_name||''' TableName, '''
          ||tbc.column_name||''' ColumnName, '''
          ||tbc.data_type||''' DataType, '
          ||'count(distinct '||tbc.column_name||') DistinctCount, '
          ||'sum(case when '||tbc.column_name||' is null then 1 else 0 end) NullCount, '
          ||'min('||tbc.column_name||') MinVal, '
          ||'max('||tbc.column_name||') MaxVal, '
          ||'min(length('||tbc.column_name||')) MinLeng, '
          ||'max(length('||tbc.column_name||')) MaxLeng '
          ||'FROM '||tbc.owner||'.'||tbc.table_name||'' stmtQuery
     FROM all_tab_columns tbc
     JOIN dba_objects dbo 
       ON dbo.object_name = tbc.table_name
      AND dbo.object_type = 'TABLE'
    WHERE data_type in ('DATE','DATETIME','NUMBER','VARCHAR2','NVARCHAR2','CHAR','TIMESTAMP(2)')
      AND owner='SYSTEM';
      
   stmt sqlStmt_cur%ROWTYPE;
   results_cur sys_refcursor;
BEGIN

OPEN sqlStmt_cur;
LOOP
   FETCH sqlStmt_cur INTO stmt;
   EXIT WHEN sqlStmt_cur%NOTFOUND;
   
   DBMS_OUTPUT.PUT_LINE(stmt.stmtQuery);
   
   EXECUTE IMMEDIATE stmt.stmtQuery
      INTO collection;

   --DBMS_OUTPUT.PUT_LINE(collection.schemaName);
   --DBMS_OUTPUT.PUT_LINE(collection.tableName);
   
   INSERT INTO ora$out (
      schema_Name, table_Name,     column_Name, 
      data_Type,   distinct_Count, null_Count, 
      min_Value,   max_Value,      min_Length, 
      max_Length)
   VALUES (
      collection.schemaName, collection.tableName,     collection.columnName, 
      collection.dataType,   collection.distinctCount, collection.nullCount, 
      collection.minVal,     collection.maxVal,        collection.minLeng, 
      collection.maxLeng);
END LOOP;

CLOSE sqlStmt_cur;

--OPEN results_cur FOR SELECT * FROM ora$out;
--DBMS_SQL.RETURN_RESULT(results_cur);  -- ORA 12g or above
--CLOSE results_cur;

END;
--SELECT * FROM ora$out;