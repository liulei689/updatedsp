using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AFWDPPS.DB
{
    public static class WDPT
    {
        public static string contstr { get; set; } = "";
        public static SqlSugarClient GetInstance()
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            string dbFilePath = Path.Combine(executablePath, "db.db");
            var db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = $"Data Source={dbFilePath};",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            return db;
        }
        public static void InitCreateTable()
        {
            //mysql这里一直用性能会很感人，控制只在初始化时调用
            using (var db = GetInstance())
            {
                db.DbMaintenance.CreateDatabase();
                db.CodeFirst.InitTables(typeof(稳定平台数据));
            }
        }

        #region 智能提示表操作
        public static async Task<int> Add(稳定平台数据 ov)
        {
            InitCreateTable();
            using (var db = GetInstance())
            {
                return await db.Insertable(ov).ExecuteCommandAsync();

            }
        }
        public static async Task<List<稳定平台数据>> GetListByTime(DateTime start, DateTime end)
        {
            //InitCreateTable();
            using (var db = GetInstance())
            {
                var query = db.Queryable<稳定平台数据>()
                              .Where(it => it.时间 >= start && it.时间 <= end);

                return await query.ToListAsync();
            }
        }
        #endregion
    }
}
