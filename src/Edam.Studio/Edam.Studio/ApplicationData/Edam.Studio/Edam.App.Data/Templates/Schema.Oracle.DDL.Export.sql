-- Run DBMS_STATS to update the NUM_ROWS
-- EXEC dbms_stats.gather_database_stats;

-- Update the 'WHERE' conditions to include only OWNER's or Tables that are
-- part of the Application
SELECT 'oracle' dbms,
       ORA_DATABASE_NAME,
	   t.OWNER,
	   t.TABLE_NAME,
	   c.COLUMN_NAME,
	   c.COLUMN_ID,
	   c.DATA_TYPE,
	   c.DATA_LENGTH,
       c.DATA_PRECISION,
       c.DATA_SCALE,
       '' IsOutput,
       '' IsReadOnly,
	   '' IsIdentity,
       c.NULLABLE,
       'TABLE' ObjectType,
	   n.CONSTRAINT_TYPE,
	   r.OWNER REFERENCE_SCHEMA_NAME,
	   r.TABLE_NAME REFERENCE_TABLE_NAME,
	   r.COLUMN_NAME REFERENCE_COLUMN_NAME,
       '' TAGS,
       cc.comments COLUMN_DESCRIPTION
  FROM ALL_TABLES t 
  LEFT JOIN ALL_TAB_COLS c 
    ON t.OWNER=c.OWNER 
   AND t.TABLE_NAME=c.TABLE_NAME 
  LEFT JOIN ALL_CONS_COLUMNS nc 
    ON c.OWNER=nc.OWNER 
   AND c.TABLE_NAME=nc.TABLE_NAME 
   AND c.COLUMN_NAME=nc.COLUMN_NAME 
  LEFT JOIN ALL_CONSTRAINTS n 
    ON nc.OWNER=n.OWNER 
   AND nc.CONSTRAINT_NAME=n.CONSTRAINT_NAME 
   AND n.CONSTRAINT_TYPE IN('P','U','R')
  LEFT JOIN ALL_CONS_COLUMNS r 
    ON n.R_OWNER=r.OWNER 
   AND n.R_CONSTRAINT_NAME=r.CONSTRAINT_NAME
   AND nc.POSITION=r.POSITION
  LEFT JOIN USER_COL_COMMENTS cc
    ON cc.column_name = c.column_name
   AND cc.table_name = c.table_name
 WHERE t.OWNER NOT IN ('SYS','CTXSYS','OUTLN','XDB','MDSYS','FLOWS_FILES','APEX_040000')
   AND t.TABLESPACE_NAME is not null
   AND t.TABLESPACE_NAME NOT IN ('SYSAUX')
   AND t.NUM_ROWS <> 0
   
/*
SELECT * FROM ALL_TABLES t
 WHERE t.OWNER NOT IN ('SYS','CTXSYS','OUTLN','XDB','MDSYS','FLOWS_FILES','APEX_040000')
   AND t.TABLESPACE_NAME is not null
   AND t.TABLESPACE_NAME NOT IN ('SYSAUX')
   
SELECT * FROM USER_TABLES 
 */
 
/*

-- get the last modified date of any table in last 365 days

select tab.owner as table_schema,
       tab.table_name,
       obj.last_ddl_time as last_modify
  from all_tables tab
  join all_objects obj 
    on tab.owner = obj.owner
   and tab.table_name = obj.object_name
 where tab.owner in ('SYSTEM')
   and obj.last_ddl_time > (current_date - INTERVAL '365' DAY)
 order by last_modify desc;
 
-- modify the previous to include other table owners (schemas)
-- modify the previous to include an extended or shorter period...

 */