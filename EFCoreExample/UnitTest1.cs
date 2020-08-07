using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFCore.App.Context;
using EFCore.App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Xunit;
using Xunit.Abstractions;


namespace EFCoreExample
{
  public static class QueryableExtensions
  {
    private static object Private(this object obj, string privateField) => obj?.GetType()
      .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);

    private static T Private<T>(this object obj, string privateField) => (T) obj?.GetType()
      .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);

    public static string ToSql<TEntity>(this IQueryable<TEntity> query)
    {
      var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
      var relationalCommandCache = enumerator.Private("_relationalCommandCache");
      var selectExpression = relationalCommandCache.Private<SelectExpression>("_selectExpression");
      var factory = relationalCommandCache.Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

      var sqlGenerator = factory.Create();
      var command = sqlGenerator.GetCommand(selectExpression);

      string sql = command.CommandText;
      return sql;
    }
  }

  public class UnitTest1
  {
    private readonly ITestOutputHelper _testOutputHelper;

    private const string ConnectionString =
      "Data Source=localhost;Database=RantExample;User Id=SA;Password=yourStrong(!)Password";

    private readonly RantExampleContext _context = new RantExampleContext(ConnectionString);

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
      _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task SQL_Just_Raw_SQL_The_Shit()
    {
      // Works the first time, every time
      // group by and order by are of limited value here
      var query = @"
SELECT c.Id, c.CustomField_Id, c.Project_Id
FROM [CustomFieldValues] AS [c]
        JOIN [TempReports] AS [t] ON [c].[CustomField_Id] = [t].[Resource_Id]
        JOIN [TempReportsProject] AS [t0] ON [c].[CustomField_Id] = [t0].[Project_Id]
        GROUP BY [c].[Project_Id], c.CustomField_Id, c.Id
        ORDER BY [c].Project_Id asc
        ";
      var result = await _context.CustomFieldValues.FromSqlRaw(query).ToListAsync();
      _testOutputHelper.WriteLine(result.Count.ToString());
    }

    [Fact]
    public void SQL_Join_On_Return_Groups()
    {
      // note that the query output cannot express "lists"
      // This is because the sql group structure is analytical on a given
      // subset and will return a discrete series of ordered data as row
      // it doesn't do complex objects, so there's no translation
      // The fact old versions would even compile this into something
      // resolvable is a fucking miracle
      var result =
        (from cfv in _context.CustomFieldValues
          join tempReport in _context.TempReports on cfv.CustomFieldId
            equals tempReport.ResourceId
          join reportProject in _context.TempReportsProject on cfv.CustomFieldId
            equals reportProject.ProjectId
          group cfv by cfv.ProjectId
          into grp
          select new {grp.Key});

      /***
       *Output query
       *SELECT [c].[Project_Id] AS [Key]
        FROM [CustomFieldValues] AS [c]
        INNER JOIN [TempReports] AS [t] ON [c].[CustomField_Id] = [t].[Resource_Id]
        INNER JOIN [TempReportsProject] AS [t0] ON [c].[CustomField_Id] = [t0].[Project_Id]
        GROUP BY [c].[Project_Id]
       * 
       */

      _testOutputHelper.WriteLine(result.ToSql());
    }

    [Fact]
    public void SQL_Nested_Select_Set_Based()
    {
      // The database will only execute the inner selects once
      // This is slower than a join, so only use it if for some
      // reason you have to have this work done on the server
      // and you can't use sql
      // which is basically never
      IQueryable<CustomFieldValues> sqlNestedSelect =
        _context.CustomFieldValues.Where(row =>
          _context.TempReports.Select(v => v.ResourceId).Distinct()
            .Contains(row.CustomFieldId.Value)
          &&
          _context.TempReportsProject.Select(v => v.ProjectId).Distinct()
            .Contains(row.ProjectId.Value));

      _testOutputHelper.WriteLine(sqlNestedSelect.ToSql());

      /***
       Output query:
       SELECT [c].[Id], [c].[CustomField_Id], [c].[Project_Id]
        FROM [CustomFieldValues] AS [c]
        WHERE [c].[CustomField_Id] IN (
            SELECT DISTINCT [t].[Resource_Id]
            FROM [TempReports] AS [t]
        )
         AND [c].[Project_Id] IN (
            SELECT DISTINCT [t0].[Project_Id]
            FROM [TempReportsProject] AS [t0]
        )
       */
    }
  }
}