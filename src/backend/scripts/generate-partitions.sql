-- Usage: Run this script against PostgreSQL to create monthly partitions for high volume tables.
-- Targets: audit_logs, bookings

-- 1. Create a function to auto-generate monthly partitions
CREATE OR REPLACE FUNCTION create_monthly_partitions(table_name TEXT, start_date DATE, num_months INT)
RETURNS VOID AS $$
DECLARE
    i INT;
    partition_date DATE;
    partition_name TEXT;
    start_str TEXT;
    end_str TEXT;
BEGIN
    FOR i IN 0..num_months-1 LOOP
        partition_date := start_date + (i || ' month')::interval;
        partition_name := table_name || '_' || to_char(partition_date, 'YYYY_MM');
        
        start_str := to_char(partition_date, 'YYYY-MM-DD');
        end_str := to_char(partition_date + '1 month'::interval, 'YYYY-MM-DD');
        
        EXECUTE format(
            'CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L);',
            partition_name, table_name, start_str, end_str
        );
        
        RAISE NOTICE 'Partition % created for dates % to %', partition_name, start_str, end_str;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- 2. Ensure targeted tables are partitioned (Run BEFORE inserting data usually)
-- ALTER TABLE "AuditEntries" RENAME TO "AuditEntries_old";
-- CREATE TABLE "AuditEntries" (LIKE "AuditEntries_old" INCLUDING ALL) PARTITION BY RANGE ("PerformedAt");
-- INSERT INTO "AuditEntries" SELECT * FROM "AuditEntries_old";
-- DROP TABLE "AuditEntries_old";

-- 3. Run the function for the next 12 months
-- SELECT create_monthly_partitions('AuditEntries', '2024-01-01', 12);
-- SELECT create_monthly_partitions('Bookings', '2024-01-01', 12);
