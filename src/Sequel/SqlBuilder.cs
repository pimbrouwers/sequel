﻿using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sequel
{
  public class SqlBuilder
  {
    private string template;

    private readonly IDictionary<string, string> templates = new Dictionary<string, string>()
    {
      { "select", "||select|| ||top|| ||fields|| ||from|| ||join|| ||where|| ||groupby|| ||having|| ||orderby|| ||limit||" },
      { "insert", "||insert|| ||columns|| ||values||" },
      { "update", "||update|| ||set|| ||where||" },
      { "delete", "||delete|| ||from|| ||join|| ||where||" }
    };

    private readonly IDictionary<string, SqlClauseSet> clauses = new Dictionary<string, SqlClauseSet>();

    /// <summary>
    /// Using default templates
    /// </summary>
    public SqlBuilder()
    {
      template = templates["select"];
    }

    /// <summary>
    /// Using custom template
    /// </summary>
    /// <param name="template"></param>
    public SqlBuilder(string template)
    {
      this.template = template;
    }

    /// <summary>
    /// Render SQL statement as string
    /// </summary>
    /// <returns></returns>
    public override string ToString() =>
      ToSql();

    /// <summary>
    /// Render SQL statement as string
    /// </summary>
    /// <returns></returns>
    public string ToSql()
    {
      foreach (var clauseSet in clauses)
      {
        template = Regex.Replace(template, "\\|\\|" + clauseSet.Key + "\\|\\|", clauseSet.Value.ToSql(), RegexOptions.IgnoreCase);
      }

      template = Regex.Replace(template, @"\|\|[a-z]+\|\|\s{0,1}", "").Trim();

      return template;
    }

    public static SqlBuilder Sel(params string[] columns) =>
      new SqlBuilder().Select(columns);

    public static SqlBuilder Ins(string table)
      => new SqlBuilder().Insert(table);

    public static SqlBuilder Upd(string tableOrAlias)
      => new SqlBuilder().Update(tableOrAlias);

    public static SqlBuilder Del(string alias = null)
    {
      var sql = new SqlBuilder().Delete();

      if(!string.IsNullOrWhiteSpace(alias))
      {
        sql.Delete(alias);
      }

      return sql;
    }

    /// <summary>
    /// Register columns for INSERT
    /// </summary>
    /// <param name="into"></param>
    /// <returns></returns>
    public SqlBuilder Into(params string[] into) =>
      AddClause("columns", into, ", ", "(", ")");

    /// <summary>
    /// EXISTS predicate subquery
    /// </summary>
    /// <param name="sqlBuilder"></param>
    /// <returns></returns>
    public SqlBuilder Exists(SqlBuilder sqlBuilder) =>
      Where("EXISTS (" + sqlBuilder.ToSql() + ")");

    /// <summary>
    /// EXISTS predicate
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public SqlBuilder Exists(string predicate) =>
      Where("EXISTS (" + predicate + ")");

    /// <summary>
    /// FROM table
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public SqlBuilder From(string table) =>
      AddClause("from", table, null, "FROM ", null);

    /// <summary>
    /// FROM table with alias
    /// </summary>
    /// <param name="table"></param>
    /// <param name="alias"></param>
    /// <returns></returns>
    public SqlBuilder From(string table, string alias) =>
      From(table + " AS " + alias);

    /// <summary>
    /// FROM derived table
    /// </summary>
    /// <param name="derivedTable"></param>
    /// <param name="alias"></param>
    /// <returns></returns>
    public SqlBuilder From(SqlBuilder derivedTable, string alias) =>
      AddClause("from", derivedTable.ToSql(), null, "FROM (", ") as " + alias);

    /// <summary>
    /// DELETE clause
    /// </summary>
    /// <returns></returns>
    public SqlBuilder Delete()
    {
      template = templates["delete"];
      return AddClause("delete", "", null, "DELETE ", null);
    }

    /// <summary>
    /// DELETE from alias
    /// </summary>
    /// <param name="alias"></param>
    /// <returns></returns>
    public SqlBuilder Delete(string alias)
    {
      template = templates["delete"];
      return AddClause("delete", alias, null, "DELETE ", null);
    }

    /// <summary>
    /// GROUP BY columns
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public SqlBuilder GroupBy(params string[] columns) =>
      AddClause("groupby", columns, ", ", "GROUP BY ", null);

    /// <summary>
    /// HAVING predicates
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public SqlBuilder Having(params string[] predicate) =>
      AddClause("having", predicate, ", ", "HAVING ", null);

    /// <summary>
    /// INSERT INTO table
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public SqlBuilder Insert(string table)
    {
      template = templates["insert"];
      return AddClause("insert", table, null, "INSERT INTO ", null);
    }

    /// <summary>
    /// [INNER] JOIN table and predicate
    /// </summary>
    /// <param name="tableAndPredicate"></param>
    /// <returns></returns>
    public SqlBuilder Join(string tableAndPredicate) =>
      AddClause("join", tableAndPredicate, " INNER JOIN ", null, null, false);

    /// <summary>
    /// [INNER] JOIN table with predicate
    /// </summary>
    /// <param name="table"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public SqlBuilder Join(string table, string predicate) =>
      Join(table + " ON " + predicate);

    /// <summary>
    /// [INNER] JOIN table with alias and predicate
    /// </summary>
    /// <param name="table"></param>
    /// <param name="alias"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public SqlBuilder Join(string table, string alias, string predicate) =>
      Join(table + " AS " + alias + " ON " + predicate);

    /// <summary>
    /// LEFT JOIN table and predicate
    /// </summary>
    /// <param name="tableAndPredicate"></param>
    /// <returns></returns>
    public SqlBuilder LeftJoin(string tableAndPredicate) =>
      AddClause("join", tableAndPredicate, " LEFT JOIN ", null, null, false);

    /// <summary>
    /// LEFT JOIN table with predicate
    /// </summary>
    /// <param name="table"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public SqlBuilder LeftJoin(string table, string predicate) =>
      LeftJoin(table + " ON " + predicate);

    /// <summary>
    /// LEFT JOIN table with alias and predicate
    /// </summary>
    /// <param name="table"></param>
    /// <param name="alias"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public SqlBuilder LeftJoin(string table, string alias, string predicate) =>
      LeftJoin(table + " AS " + alias + " ON " + predicate);

    /// <summary>
    /// LIMIT by n rows
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public SqlBuilder Limit(int n) =>
      AddClause("limit", n.ToString(), null, "LIMIT ", null, true);

    /// <summary>
    /// ORDER BY columns
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public SqlBuilder OrderBy(params string[] columns) =>
      AddClause("orderby", columns, ", ", "ORDER BY ", null);

    /// <summary>
    /// ORDER BY DESC columns (i.e. col desc, col2 desc)
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public SqlBuilder OrderByDesc(params string[] columns)
    {
      string[] columnsDesc = new string[columns.Length];
      for (var i = 0; i < columns.Length; i++)
      {
        columnsDesc[i] = columns[i] + " DESC";
      }

      return AddClause("orderby", columnsDesc, ", ", "ORDER BY ", null);
    }

    /// <summary>
    /// SELECT columns
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public SqlBuilder Select(params string[] columns)
    {
      AddClause("select", string.Empty, string.Empty, "SELECT ", null);
      return AddClause("fields", columns, ", ", null, null);
    }

    /// <summary>
    /// SELECT columns and apply provided alias
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    public SqlBuilder SelectWithAlias(string alias, params string[] columns)
    {
      string[] columnsAliased = new string[columns.Length];
      string aliasProper = (alias[alias.Length - 1] == '.') ? alias : alias + ".";

      for (var i = 0; i < columns.Length; i++)
      {
        columnsAliased[i] = aliasProper + columns[i];
      }

      AddClause("select", string.Empty, string.Empty, "SELECT ", null);
      return AddClause("fields", columnsAliased, ", ", null, null);
    }

    /// <summary>
    /// UPDATE &gt; SET column/value pairs
    /// </summary>
    /// <param name="columnAndValuePairs"></param>
    /// <returns></returns>
    public SqlBuilder Set(params string[] columnAndValuePairs) =>
      AddClause("set", columnAndValuePairs, ", ", "SET ", null);

    /// <summary>
    /// TOP n rows
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public SqlBuilder Top(int n) =>
      AddClause("top", n.ToString(), null, "TOP ", null, true);

    /// <summary>
    /// UPDATE table
    /// </summary>
    /// <param name="tableOrAlias"></param>
    /// <returns></returns>
    public SqlBuilder Update(string tableOrAlias)
    {
      template = templates["update"];
      return AddClause("update", tableOrAlias, null, "UPDATE ", null);
    }

    /// <summary>
    /// INSERT single record
    /// </summary>
    /// <param name="columnAndValuePairs"></param>
    /// <returns></returns>
    public SqlBuilder Value(params string[] columnAndValuePairs) =>
      AddClause("values", columnAndValuePairs, ", ", "VALUES (", ")");

    /// <summary>
    /// INSERT multiple records
    /// </summary>
    /// <param name="columnAndValuePairs"></param>
    /// <returns></returns>
    public SqlBuilder Values(params string[] columnAndValuePairs) =>
      AddClause("values", columnAndValuePairs, "), (", "VALUES (", ")");

    /// <summary>
    /// WHERE [AND] predicates
    /// </summary>
    /// <param name="predicates"></param>
    /// <returns></returns>
    public SqlBuilder Where(params string[] predicates) =>
      AddClause("where", string.Join(" AND ", predicates), " AND ", "WHERE ", null);

    /// <summary>
    /// WHERE [OR] predicates
    /// </summary>
    /// <param name="predicates"></param>
    /// <returns></returns>
    public SqlBuilder WhereOr(params string[] predicates) =>
      AddClause("where", string.Join(" OR ", predicates), " OR ", "WHERE ", null);

    private SqlBuilder AddClause(string keyword, string[] sql, string glue, string pre, string post, bool singular = true) =>
      AddClause(keyword, string.Join(", ", sql), glue, pre, post, singular);

    private SqlBuilder AddClause(string keyword, string sql, string glue, string pre, string post, bool singular = true)
    {
      if (!clauses.TryGetValue(keyword, out SqlClauseSet clauseSet))
      {
        clauseSet = new SqlClauseSet(glue, pre, post, singular);
        clauses[keyword] = clauseSet;
      }

      clauseSet.Add(new SqlClause(sql, singular ? null : glue));

      return this;
    }
  }
}