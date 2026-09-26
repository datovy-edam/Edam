SELECT 'mariadb' InstanceName,
       ifnull(DATABASE(), 'catalog') CatalogName,
       c.TABLE_SCHEMA SchemaName,
       c.TABLE_NAME ObjectName,
       c.COLUMN_NAME ColumnName,
       c.ORDINAL_POSITION OrdinalPosition,
       c.COLUMN_TYPE DataType,
       ifnull(c.CHARACTER_MAXIMUM_LENGTH, 0) CharacterMaxLength,
       ifnull(c.NUMERIC_PRECISION, 0) NumericPrecision,
  	   ifnull(c.NUMERIC_SCALE, 0) NumericScale,  	   
       0 IsOutput,
       0 IsReadOnly,
       case when IS_NULLABLE = 'NO' 
            then 0 else 1 end IsNullable,
       0 IsIdentity,
       'TABLE' ObjectType,
       CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PRIMARY KEY'
            WHEN fk.COLUMN_NAME IS NOT NULL THEN 'FOREIGN_KEY' END AS ConstraintType,
       fk.TABLE_SCHEMA ReferenceTableSchema,
       fk.REFERENCED_TABLE_NAME ReferenceTableName,
       fk.REFERENCED_COLUMN_NAME ReferenceColumnName
  FROM INFORMATION_SCHEMA.COLUMNS c
  LEFT JOIN (
SELECT kcu.TABLE_SCHEMA,
       kcu.TABLE_NAME,
       kcu.COLUMN_NAME,
       tc.CONSTRAINT_NAME
  FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
  JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
   AND tc.TABLE_SCHEMA     = kcu.TABLE_SCHEMA
   AND tc.TABLE_NAME       = kcu.TABLE_NAME
 WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY') pk
    ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
   AND pk.TABLE_NAME   = c.TABLE_NAME
   AND pk.COLUMN_NAME  = c.COLUMN_NAME
  LEFT JOIN (
SELECT kcu.TABLE_SCHEMA,
       kcu.TABLE_NAME,
       kcu.COLUMN_NAME,
       kcu.CONSTRAINT_NAME,
       kcu.REFERENCED_TABLE_NAME,
       kcu.REFERENCED_COLUMN_NAME
  FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
 WHERE kcu.REFERENCED_TABLE_NAME IS NOT NULL) fk
    ON fk.TABLE_SCHEMA = c.TABLE_SCHEMA
   AND fk.TABLE_NAME   = c.TABLE_NAME
   AND fk.COLUMN_NAME  = c.COLUMN_NAME
  LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
    ON rc.CONSTRAINT_SCHEMA = fk.TABLE_SCHEMA
   AND rc.CONSTRAINT_NAME   = fk.CONSTRAINT_NAME

