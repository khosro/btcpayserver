﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Operations;

namespace BTCPayServer.Ethereum.Client
{
    public enum DatabaseType
    {
        Sqlite,
        Postgres,
        MySQL,
    }

    public class EthereumClientApplicationDbContextFactory
    {
        string _ConnectionString;
        DatabaseType _Type;
        public EthereumClientApplicationDbContextFactory(DatabaseType type, string connectionString)
        {
            _ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _Type = type;
        }

        public DatabaseType Type
        {
            get
            {
                return _Type;
            }
        }

        public EthereumClientApplicationDbContext CreateContext()
        {
            var builder = new DbContextOptionsBuilder<EthereumClientApplicationDbContext>();
            ConfigureBuilder(builder);
            return new EthereumClientApplicationDbContext(builder.Options);
        }

        class CustomNpgsqlMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
        {
            public CustomNpgsqlMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies) : base(dependencies)
            {
            }

            protected override void Generate(NpgsqlCreateDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
            {
                builder
                     .Append("CREATE DATABASE ")
                     .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));

                // POSTGRES gotcha: Indexed Text column (even if PK) are not used if we are not using C locale
                builder
                    .Append(" TEMPLATE ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier("template0"));

                builder
                    .Append(" LC_CTYPE ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier("C"));

                builder
                    .Append(" LC_COLLATE ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier("C"));

                builder
                    .Append(" ENCODING ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier("UTF8"));

                if (operation.Tablespace != null)
                {
                    builder
                        .Append(" TABLESPACE ")
                        .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Tablespace));
                }

                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

                EndStatement(builder, suppressTransaction: true);
            }
        }

        public void ConfigureBuilder(DbContextOptionsBuilder builder)
        {
            if (_Type == DatabaseType.Sqlite)
                builder.UseSqlite(_ConnectionString);
            else if (_Type == DatabaseType.Postgres)
                builder
                    .UseNpgsql(_ConnectionString)
                    .ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();
            else if (_Type == DatabaseType.MySQL)
                builder.UseMySql(_ConnectionString);
        }
    }
}
