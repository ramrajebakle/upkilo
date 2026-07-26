CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Fast index performance queries for slow query monitoring
CREATE OR REPLACE VIEW pg_stat_statements_human AS
SELECT 
    query, 
    calls, 
    total_exec_time, 
    min_exec_time, 
    max_exec_time, 
    mean_exec_time, 
    stddev_exec_time, 
    rows, 
    100.0 * shared_blks_hit / nullif(shared_blks_hit + shared_blks_read, 0) AS hit_percent
FROM pg_stat_statements 
ORDER BY total_exec_time DESC;
