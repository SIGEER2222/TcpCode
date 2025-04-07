using Serilog;
using SqlSugar;
using System;

public static partial class SqlSugarHelp {

  public static SqlSugarClient Db => new SqlSugarClient(new ConnectionConfig() {
    ConnectionString = "Host=10.10.4.203;Port=30386;Database=mom_log_database;Username=mom_log;Password=mom_log;Search Path=mom;",
    DbType = SqlSugar.DbType.PostgreSQL,
    IsAutoCloseConnection = true,
    LanguageType = LanguageType.Chinese,
    InitKeyType = InitKeyType.Attribute
  }, db =>
  {
    db.Aop.OnLogExecuted = (sql, pars) =>
    {
      var newSql = UtilMethods.GetSqlString(SqlSugar.DbType.PostgreSQL, sql, pars);
    };
    db.Aop.OnError = (exp) =>
    {
      Log.Error(" ", exp.Message);
    };
  });
}

[SplitTable(SplitType.Season)]
[SugarTable("fab_Barcode_Scanner_Log_{year}{month}{day}")]
public class BarcodeScannerLog {
  [SugarColumn(IsPrimaryKey = true)]
  public long Id { get; set; }

  public string IP { get; set; }
  [SugarColumn(IsNullable = true)]
  public string RequestURL { get; set; }

  [SugarColumn(IsNullable = true, ColumnDataType = "longtext,text,clob")]
  public string Message { get; set; }

  [SugarColumn(IsNullable = true, ColumnDataType = "longtext,text,clob")]
  public string Code { get; set; }

  [SugarColumn(IsNullable = true)]
  public string EqpName { get; set; }

  [SplitField]
  public DateTime LogTime { get; set; }

  [SugarColumn(IsNullable = true)]
  public JobType JobType { get; set; }
}
