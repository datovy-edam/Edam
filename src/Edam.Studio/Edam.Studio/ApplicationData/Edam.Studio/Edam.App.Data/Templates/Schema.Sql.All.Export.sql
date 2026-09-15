DECLARE @instance VARCHAR(80) = 'sqldb'

SELECT @instance InstanceName,
       DB_NAME() CatalogName,
	   SchemaName,
	   ObjectName,
	   ColumnName,
	   OrdinalPosition,
	   DataType,
	   CharacterMaxLength,
	   Precision,
	   Scale,
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
       isnull(c.CHARACTER_MAXIMUM_LENGTH, 0) CharacterMaxLength,
	   isnull(c.NUMERIC_PRECISION, 0) Precision,
	   isnull(c.NUMERIC_SCALE, 0) Scale,
	   cast(0 as bit) IsOutput,
	   cast(0 as bit) IsReadOnly,
	   cast(0 as bit) IsNullable,
       IsIdentity = cast(
          case when COLUMNPROPERTY(object_id(t.TABLE_SCHEMA+'.'+t.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1
		       then 1 else 0 end as bit),
       ObjectType = case when t.TABLE_TYPE = 'BASE TABLE'
                         then 'TABLE' else t.TABLE_TYPE end,
       isnull(n.CONSTRAINT_TYPE,'') ConstraintType,
       isnull(k2.TABLE_SCHEMA,'') ReferenceTableSchema,
       isnull(k2.TABLE_NAME,'') ReferenceTableName,
       isnull(k2.COLUMN_NAME,'') ReferenceColumnName
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
	   ColumnName = case when p.PARAMETER_NAME = ''
                         then 'ReturnValue' else p.PARAMETER_NAME end,
	   p.ORDINAL_POSITION OrdinalPosition,
	   p.DATA_TYPE DataType,
	   isnull(p.CHARACTER_MAXIMUM_LENGTH, 0) CharacterMaxLength,
	   isnull(p.NUMERIC_PRECISION, 0) Precision,
	   isnull(p.NUMERIC_SCALE, 0) Scale,
	   IsOutput = case when p.PARAMETER_MODE = 'INOUT'
                       then 1 else 0 end,
	   cast(0 as bit) IsReadOnly,
	   cast(0 as bit) IsNullable,
	   cast(0 as bit) IsIdentity,
       r.ROUTINE_TYPE ObjectType,
	   cast('' as varchar(128)) ConstraintType,
       cast('' as varchar(128)) ReferenceTableSchema,
       cast('' as varchar(128)) ReferenceTableName,
       cast('' as varchar(128)) ReferenceColumnName
  FROM INFORMATION_SCHEMA.ROUTINES r
  JOIN INFORMATION_SCHEMA.PARAMETERS p
    ON r.ROUTINE_CATALOG = p.SPECIFIC_CATALOG
   AND r.ROUTINE_SCHEMA = p.SPECIFIC_SCHEMA
   AND r.ROUTINE_NAME = p.SPECIFIC_NAME) x
 ORDER BY ObjectType, SchemaName, ObjectName, 
          OrdinalPosition, ColumnName
