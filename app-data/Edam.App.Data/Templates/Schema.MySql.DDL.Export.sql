/*

SELECT 'mysql' dbms,'db' TABLE_CATALOG,t.TABLE_SCHEMA,t.TABLE_NAME,c.COLUMN_NAME,c.ORDINAL_POSITION,
       c.DATA_TYPE,c.CHARACTER_MAXIMUM_LENGTH,n.CONSTRAINT_TYPE,
	   k.REFERENCED_TABLE_SCHEMA,k.REFERENCED_TABLE_NAME,k.REFERENCED_COLUMN_NAME 
  FROM INFORMATION_SCHEMA.TABLES t 
  LEFT JOIN INFORMATION_SCHEMA.COLUMNS c 
    ON t.TABLE_SCHEMA=c.TABLE_SCHEMA 
   AND t.TABLE_NAME=c.TABLE_NAME 
  LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k 
    ON c.TABLE_SCHEMA=k.TABLE_SCHEMA 
   AND c.TABLE_NAME=k.TABLE_NAME 
   AND c.COLUMN_NAME=k.COLUMN_NAME 
  LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS n 
    ON k.CONSTRAINT_SCHEMA=n.CONSTRAINT_SCHEMA 
   AND k.CONSTRAINT_NAME=n.CONSTRAINT_NAME 
   AND k.TABLE_SCHEMA=n.TABLE_SCHEMA 
   AND k.TABLE_NAME=n.TABLE_NAME 
 WHERE t.TABLE_TYPE='BASE TABLE' 
   AND t.TABLE_SCHEMA NOT IN('INFORMATION_SCHEMA','mysql','performance_schema')
 ORDER BY t.TABLE_SCHEMA,t.TABLE_NAME,c.COLUMN_NAME,c.ORDINAL_POSITION;

*/

-- -----------------------------------------------------------------------------
-- SET THE DATABASE TO BE USED
USE TestDb;

-- RELEVANT TABLES
-- -----------------------------------------------------------------------------
-- DROP TABLE ObjectInclude
CREATE TABLE ObjectInclude (
   OBJECT_ID   INT PRIMARY KEY,
   SCHEMA_NAME VARCHAR(80),
   OBJECT_NAME VARCHAR(80)
);

-- Add as many tables as needed to define those as the relevant tables
INSERT INTO ObjectInclude (OBJECT_ID, SCHEMA_NAME, OBJECT_NAME) VALUES
   (1, 'TestDb', 'Person'),
   (2, 'TestDb', 'ObjectInclude');
   
-- List the tables to be included in subsequent listings...
SELECT * FROM ObjectInclude;



-- DATABASE SCHEMA (tables and routines)
-- -----------------------------------------------------------------------------
-- Fetch the schema of relevant tables only
SELECT 'mariadb' InstanceName,
       ifnull(DATABASE(), 'catalog') CatalogName,
	   SchemaName,
	   ObjectName,
	   ColumnName,
	   OrdinalPosition,
	   DataType,
	   CharacterMaxLength,
	   NumericPrecision,
	   NumericScale,
	   IsOutput,
	   IsReadOnly,
	   IsNullable,
	   IsIdentity,
       ObjectType,
	   ConstraintType,
       ReferenceTableSchema,
       ReferenceTableName,
       ReferenceColumnName
  FROM (
SELECT t.TABLE_SCHEMA SchemaName,
       t.TABLE_NAME ObjectName,
       c.COLUMN_NAME ColumnName,
       c.ORDINAL_POSITION OrdinalPosition,
       c.DATA_TYPE DataType,
       ifnull(c.CHARACTER_MAXIMUM_LENGTH, 0) CharacterMaxLength,
       ifnull(c.NUMERIC_PRECISION, 0) NumericPrecision,
  	   ifnull(c.NUMERIC_SCALE, 0) NumericScale,
       0 IsOutput,
       0 IsReadOnly,
       case when IS_NULLABLE = 'NO' 
            then 0 else 1 end IsNullable,
       0 IsIdentity,
       case when t.TABLE_TYPE = 'BASE TABLE'
            then 'TABLE' else t.TABLE_TYPE end ObjectType,
       ifnull(n.CONSTRAINT_TYPE,'') ConstraintType,
       ifnull(k2.TABLE_SCHEMA,'') ReferenceTableSchema,
       ifnull(k2.TABLE_NAME,'') ReferenceTableName,
       ifnull(k2.COLUMN_NAME,'') ReferenceColumnName
-- select *
  FROM INFORMATION_SCHEMA.TABLES t 
  LEFT JOIN INFORMATION_SCHEMA.COLUMNS c 
    ON t.TABLE_CATALOG=c.TABLE_CATALOG 
   AND t.TABLE_SCHEMA=c.TABLE_SCHEMA 
   AND t.TABLE_NAME=c.TABLE_NAME 
  LEFT JOIN(INFORMATION_SCHEMA.KEY_COLUMN_USAGE k 
  JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS n 
    ON k.CONSTRAINT_CATALOG=n.CONSTRAINT_CATALOG 
   AND k.CONSTRAINT_SCHEMA=n.CONSTRAINT_SCHEMA 
   AND k.CONSTRAINT_NAME=n.CONSTRAINT_NAME 
  LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r 
    ON k.CONSTRAINT_CATALOG=r.CONSTRAINT_CATALOG 
   AND k.CONSTRAINT_SCHEMA=r.CONSTRAINT_SCHEMA 
   AND k.CONSTRAINT_NAME=r.CONSTRAINT_NAME)
    ON c.TABLE_CATALOG=k.TABLE_CATALOG 
   AND c.TABLE_SCHEMA=k.TABLE_SCHEMA 
   AND c.TABLE_NAME=k.TABLE_NAME
   AND c.COLUMN_NAME=k.COLUMN_NAME 
  LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k2 
    ON k.ORDINAL_POSITION=k2.ORDINAL_POSITION 
   AND r.UNIQUE_CONSTRAINT_CATALOG=k2.CONSTRAINT_CATALOG
   AND r.UNIQUE_CONSTRAINT_SCHEMA=k2.CONSTRAINT_SCHEMA 
   AND r.UNIQUE_CONSTRAINT_NAME=k2.CONSTRAINT_NAME 
 UNION
SELECT r.ROUTINE_SCHEMA SchemaName,
       r.ROUTINE_NAME ObjectName,
	      case when p.PARAMETER_NAME = ''
               then 'ReturnValue' else p.PARAMETER_NAME end ColumnName,
	   p.ORDINAL_POSITION OrdinalPosition,
	   p.DATA_TYPE DataType,
	   ifnull(p.CHARACTER_MAXIMUM_LENGTH, 0) CharacterMaxLength,
	   ifnull(p.NUMERIC_PRECISION, 0) NumericPrecision,
	   ifnull(p.NUMERIC_SCALE, 0) NumericScale,
	      case when p.PARAMETER_MODE = 'INOUT'
               then 1 else 0 end IsOutput,
	   0 IsReadOnly,
	   0 IsNullable,
	   0 IsIdentity,
       r.ROUTINE_TYPE ObjectType,
	   '' ConstraintType,
       '' ReferenceTableSchema,
       '' ReferenceTableName,
       '' ReferenceColumnName
  FROM INFORMATION_SCHEMA.ROUTINES r
  JOIN INFORMATION_SCHEMA.PARAMETERS p
    ON r.ROUTINE_CATALOG = p.SPECIFIC_CATALOG
   AND r.ROUTINE_SCHEMA = p.SPECIFIC_SCHEMA
   AND r.ROUTINE_NAME = p.SPECIFIC_NAME) x
 WHERE x.ObjectName in (
    SELECT OBJECT_NAME
      FROM ObjectInclude
 )
 ORDER BY ObjectType, SchemaName, ObjectName, 
          OrdinalPosition, ColumnName


 
-- ROW-COUNT
-- -----------------------------------------------------------------------------
-- Tables Row Count (rough estimate)
SELECT table_name, TABLE_ROWS 
  FROM INFORMATION_SCHEMA.TABLES 
 WHERE TABLE_SCHEMA = 'TestDb';

-- -----------------------------------------------------------------------------
-- For an exact ROW COUNT do the following STEPs...
-- STEP 1. ROW-COUNT: Tables Row Count exact count (from the output goto STEP 2)
SELECT CONCAT(
    'SELECT "', 
    table_name, 
    '" table_name, COUNT(*) RowCount FROM `', 
    table_schema,
    '`.`',
    table_name, 
    '` UNION '
) row_count_select
  FROM INFORMATION_SCHEMA.TABLES 
 WHERE table_schema = 'TestDb'
   AND table_name in (
SELECT OBJECT_NAME
  FROM ObjectInclude
 );

-- STEP 2. ROW-COUNT: copy+paste without the last UNION and execute UNIONs...
SELECT "Person" table_name, COUNT(*) row_count FROM `TestDb`.`Person` UNION 
SELECT "ObjectInclude" AS table_name, COUNT(*) AS exact_row_count FROM `TestDb`.`ObjectInclude` -- UNION


 
-- DATA/VALUE-PROFILING
-- -----------------------------------------------------------------------------
-- STEP 1. Gather all info table - column data/value profile info SQL Statements
SELECT concat('SELECT ''', t.TABLE_SCHEMA, ''' TABLE_SCHEMA, ''',
       t.TABLE_NAME, ''' TABLE_NAME, ''',
       c.COLUMN_NAME,''' COLUMN_NAME, ',
       'count(distinct ', c.COLUMN_NAME, ') DISTINCT_COUNT, ',
       'sum(case when ', c.COLUMN_NAME, ' is null then 1 else 0 end) NullCount,'
       'min(', c.COLUMN_NAME, '), ',
       'max(', c.COLUMN_NAME, '), ',
       'min(length(', c.COLUMN_NAME, ')), ',
       'max(length(', c.COLUMN_NAME, ')) '
       'FROM ', t.TABLE_NAME, ' UNION') sqlStatement
  FROM INFORMATION_SCHEMA.TABLES t 
  LEFT JOIN INFORMATION_SCHEMA.COLUMNS c 
    ON t.TABLE_SCHEMA=c.TABLE_SCHEMA 
   AND t.TABLE_NAME=c.TABLE_NAME 
 WHERE t.TABLE_NAME in (
SELECT Object_Name
  FROM ObjectInclude)
 GROUP BY t.TABLE_SCHEMA,t.TABLE_NAME,c.COLUMN_NAME
 ORDER BY t.TABLE_SCHEMA,t.TABLE_NAME,c.COLUMN_NAME,c.ORDINAL_POSITION;

-- STEP 2. Using the output of STEP 1, execute the statements (remove last UNION)
-- put statements here ...



-- -----------------------------------------------------------------------------
-- DONE.





