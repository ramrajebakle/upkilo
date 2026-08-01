namespace Upkilo.Infrastructure.Data;

/// <summary>
/// Documentation and strategy for safe database migration rollbacks.
/// </summary>
public static class MigrationRollbackStrategy
{
    /*
     * STRATEGY: Zero-Downtime Migration & Safe Rollbacks
     * 
     * 1. Never Rename/Delete Columns Directly:
     *    - If a column is no longer needed, mark it as [Obsolete] and leave it for one release.
     *    - Only drop it once all code paths are confirmed to not use it.
     * 
     * 2. Backward Compatibility:
     *    - Ensure new code can run on the old schema.
     *    - Ensure old code can run on the new schema.
     * 
     * 3. Rollback Procedure:
     *    - BEFORE Rollback: Check if the "New" schema has data that isn't in the "Old" schema.
     *    - Example: If you added a 'CustomerEmail' column and filled it, a rollback that drops it will lose PII.
     *    - SCRIPT: Use `UPDATE OldTable SET OldColumn = NewColumn` sync scripts during the transition phase.
     * 
     * 4. State Verification:
     *    - Verify data integrity after `dotnet ef database update <PreviousMigration>`.
     */

    public static string GetRollbackCheckQuery(string tableName, string columnName)
    {
        return $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"{columnName}\" IS NOT NULL;";
    }
}
