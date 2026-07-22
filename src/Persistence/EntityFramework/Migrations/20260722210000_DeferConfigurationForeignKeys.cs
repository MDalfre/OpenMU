// <copyright file="20260722210000_DeferConfigurationForeignKeys.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
[DbContext(typeof(EntityDataContext))]
[Migration("20260722210000_DeferConfigurationForeignKeys")]
public partial class DeferConfigurationForeignKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(GetAlterConstraintsSql("DEFERRABLE INITIALLY IMMEDIATE"));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(GetAlterConstraintsSql("NOT DEFERRABLE"));
    }

    private static string GetAlterConstraintsSql(string constraintMode)
    {
        return $$"""
            DO $migration$
            DECLARE
                foreign_key record;
            BEGIN
                FOR foreign_key IN
                    SELECT namespace.nspname AS schema_name, table_class.relname AS table_name, constraint_data.conname AS constraint_name
                    FROM pg_constraint constraint_data
                    JOIN pg_class table_class ON table_class.oid = constraint_data.conrelid
                    JOIN pg_namespace namespace ON namespace.oid = table_class.relnamespace
                    WHERE constraint_data.contype = 'f'
                      AND namespace.nspname IN ('config', 'data')
                LOOP
                    EXECUTE format(
                        'ALTER TABLE %I.%I ALTER CONSTRAINT %I {{constraintMode}}',
                        foreign_key.schema_name,
                        foreign_key.table_name,
                        foreign_key.constraint_name);
                END LOOP;
            END
            $migration$;
            """;
    }
}
