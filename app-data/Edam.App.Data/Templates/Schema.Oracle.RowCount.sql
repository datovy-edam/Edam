
-- ORACLE 11g or above.

-- You may need to refresh the statistics with the following:
-- EXEC dbms_stats.gather_database_stats;

SELECT owner,
       table_name,
       num_rows,
       sample_size,
       last_analyzed
  FROM all_tables
 WHERE Owner = 'SYSTEM';

