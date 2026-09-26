
SELECT t.TABLE_SCHEMA AS SchemaName,
       t.TABLE_NAME AS ObjectName,
       c.COLUMN_NAME AS ColumnName,
       c.ORDINAL_POSITION AS OrdinalPosition,
       c.DATA_TYPE AS DataType,
       c.CHARACTER_MAXIMUM_LENGTH AS CharacterMaxLength,
       c.NUMERIC_PRECISION AS Precision,
       c.NUMERIC_SCALE AS Scale,
       FALSE AS IsOutput, -- Not directly available in INFORMATION_SCHEMA
       FALSE AS IsReadOnly, -- Not directly available in INFORMATION_SCHEMA
       c.IS_NULLABLE AS IsNullable,
       CASE WHEN c.IS_IDENTITY = 'YES' THEN TRUE ELSE FALSE END AS IsIdentity,
       'TABLE' AS ObjectType,
       tc.CONSTRAINT_TYPE AS ConstraintType,
       '' AS ReferenceTableSchema,
       '' AS ReferenceTableName,
       '' AS ReferenceColumnName,
	   '' AS PrivacyTag,
	   c.COMMENT ColumnComment
  FROM INFORMATION_SCHEMA.TABLES t
  JOIN INFORMATION_SCHEMA.COLUMNS c 
	ON t.TABLE_SCHEMA = c.TABLE_SCHEMA 
   AND t.TABLE_NAME = c.TABLE_NAME
  LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
    ON t.TABLE_SCHEMA = tc.TABLE_SCHEMA 
   AND t.TABLE_NAME = tc.TABLE_NAME
 WHERE t.TABLE_TYPE = 'BASE TABLE'
   AND t.TABLE_SCHEMA NOT IN ('INFORMATION_SCHEMA')

-- COMMENT ON TABLE schema_name.table_name IS 'This is a table description';
-- COMMENT ON COLUMN schema_name.table_name.column_name IS 'This is a column description';
