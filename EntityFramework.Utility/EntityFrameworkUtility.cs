using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace EntityFramework
{
    public static class EntityFrameworkUtility
    {
        public static class PostgresContant
        {
            public static readonly string ErrorCodeValue = "Code";
            public static readonly string MessageTextValue = "MessageText";
        }

        private static readonly Regex SqlServerUniqueConstraintRegex = new Regex("'UniqueError_([a-zA-Z0-9]*)_([a-zA-Z0-9]*)'", RegexOptions.Compiled);
        private static readonly Regex PostgresExceptionUniqueConstraintRegex = new Regex("IX_([a-zA-Z0-9]*)_([a-zA-Z0-9]*)", RegexOptions.Compiled);
     
        public static void HandleException(this Exception exception)
        {
            if (exception is DbUpdateConcurrencyException concurrencyEx)
            {
                throw new ConcurrencyException(concurrencyEx.Message, concurrencyEx.InnerException);
            }
            else if (exception is DbUpdateException dbUpdateEx)
            {
                if (dbUpdateEx.InnerException != null)
                {
                    if (dbUpdateEx.InnerException.InnerException != null && dbUpdateEx.InnerException.InnerException is SqlException sqlException)
                    {   //TODO.I have not test in depth for Sql Server
                        switch (sqlException.Number)
                        {
                            case 2627:  // Unique constraint error
                                throw new UniqueConstraintException(dbUpdateEx.Message, dbUpdateEx.InnerException);
                            /*
                             * case 547:   // Constraint check violation
                              case 2601:  // Duplicated key row error , Constraint violation exception
                              */
                            default:
                                DefaultDatabaseException(dbUpdateEx);
                                break;
                        }
                    }
                    else if (dbUpdateEx.InnerException is PostgresException postgresException)
                    {
                        int errorCode = 0;
                        if (postgresException.Data.Contains(PostgresContant.ErrorCodeValue))
                        {
                            int.TryParse(postgresException.Data[PostgresContant.ErrorCodeValue].ToString(), out errorCode);
                        }
                        switch (errorCode)
                        {
                            case 23505:  // Unique constraint error
                                throw new UniqueConstraintException(UniqueErrorFormatterPostgres(postgresException).ErrorMessage, dbUpdateEx.InnerException);
                            default:
                                DefaultDatabaseException(dbUpdateEx);
                                break;
                        }
                    }
                }
                else
                {
                    ThrowException(exception);
                }
            }
            else
            {
                ThrowException(exception);
            }
        }

        static void DefaultDatabaseException(Exception ex)
        {
            throw new DatabaseAccessException(ex.Message, ex.InnerException);
        }


        static void ThrowException(Exception ex)
        {
            throw ex;
        }

        /// <summary>
        /// Generalised Unique Key handler. if it finds a string in the form 'UniqueError_EntityName_PropertyName'
        /// Then makes a friendly message, otherwise returns null
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        static ValidationResult UniqueErrorFormatterSqlServer(SqlException ex)
        {
            //TODO.I have not test in depth for Sql Server
            var message = ex.Errors[0].Message;
            return UniqueErrorFormatter(message, SqlServerUniqueConstraintRegex);
        }

        /// <summary>
        /// Generalised Unique Key handler. if it finds a string in the form 'IX_EntityName_PropertyName'
        /// Then makes a friendly message, otherwise returns null
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        static ValidationResult UniqueErrorFormatterPostgres(PostgresException ex)
        {
            string message = "";
            if (ex.Data.Contains(PostgresContant.MessageTextValue))
            {
                message = ex.Data[PostgresContant.MessageTextValue].ToString();
            }
            return UniqueErrorFormatter(message, PostgresExceptionUniqueConstraintRegex);
        }

        static ValidationResult UniqueErrorFormatter(string message, Regex uniqueConstraintRegex)
        {
            var matches = uniqueConstraintRegex
                .Matches(message);

            if (matches.Count == 0)
                return null;

            var returnError = "Cannot have a duplicate " +
                matches[0].Groups[2].Value + " in " +
                matches[0].Groups[1].Value + ".";

            var openingBadValue = message.IndexOf("(");
            if (openingBadValue > 0)
            {
                var dupPart = message.Substring(openingBadValue + 1,
                    message.Length - openingBadValue - 3);
                returnError += $" Duplicate value was '{dupPart}'.";
            }

            return new ValidationResult(returnError,
                new[] { matches[0].Groups[2].Value });
        }
    }
}
