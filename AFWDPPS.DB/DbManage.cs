using SqlSugar;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AFWDPPS.DB
{
    public static class ChengYun
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
        public static async Task<int> UpdateAddZNAsync(稳定平台数据 ov)
        {
            InitCreateTable();
            using (var db = GetInstance())
            {
                return await db.Insertable(ov).ExecuteCommandAsync();

            }
        }

        #endregion
    }
}
