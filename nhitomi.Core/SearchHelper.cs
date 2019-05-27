using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public static class SearchHelper
    {
        public static IQueryable<T> FullTextSearch<T>(this IQueryable<T> queryable, string query, DbContext db)
        {
            switch (db.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql": return queryable.MySqlMatch(query);
                case "Microsoft.EntityFrameworkCore.Sqlite": return queryable.SqliteMatch(query);
            }

            throw new NotSupportedException($"Unknown database provider {db.Database.ProviderName}.");
        }

        //todo:
        public static IQueryable<T> MySqlMatch<T>(this IQueryable<T> queryable, string query) => queryable;
        public static IQueryable<T> SqliteMatch<T>(this IQueryable<T> queryable, string query) => queryable;
    }
}
