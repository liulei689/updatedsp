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
            //InitCreateTable();
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
        public static async Task<List<稳定平台数据>> GetList()
        {
            //InitCreateTable();
            using (var db = GetInstance())
            {
                var query = db.Queryable<稳定平台数据>();

                return await query.ToListAsync();
            }
        }
        public static int AdjustSonarPitchAngle()
        {
            using (var db = GetInstance())
            {
                string sql = @"
            UPDATE 稳定平台数据
            SET 声呐俯仰角度 = CASE 
                WHEN 声呐俯仰角度 BETWEEN 8 AND 9 THEN 声呐俯仰角度 *0.2
                WHEN 声呐俯仰角度 BETWEEN 7 AND 8 THEN 声呐俯仰角度 *0.22
                WHEN 声呐俯仰角度 BETWEEN 6 AND 7 THEN 声呐俯仰角度 *0.28
                WHEN 声呐俯仰角度 BETWEEN 5 AND 6 THEN 声呐俯仰角度 *0.35
                WHEN 声呐俯仰角度 BETWEEN 4 AND 5 THEN 声呐俯仰角度 *0.38
                WHEN 声呐俯仰角度 BETWEEN 3 AND 4 THEN 声呐俯仰角度 *0.47
                WHEN 声呐俯仰角度 BETWEEN 2 AND 3 THEN 声呐俯仰角度 *0.66
                ELSE 声呐俯仰角度
            END
            WHERE 声呐俯仰角度 > 2;
        ";

                return db.Ado.ExecuteCommand(sql);
            }
        }
        public static int AdjustSonarPitchAnglehx()
        {
            using (var db = GetInstance())
            {
                string sql = @"
            UPDATE 稳定平台数据
            SET 声呐横滚角度 = CASE 
                WHEN 声呐横滚角度 BETWEEN 8 AND 9 THEN 声呐横滚角度 *0.2
                WHEN 声呐横滚角度 BETWEEN 7 AND 8 THEN 声呐横滚角度 *0.22
                WHEN 声呐横滚角度 BETWEEN 6 AND 7 THEN 声呐横滚角度 *0.28
                WHEN 声呐横滚角度 BETWEEN 5 AND 6 THEN 声呐横滚角度 *0.35
                WHEN 声呐横滚角度 BETWEEN 4 AND 5 THEN 声呐横滚角度 *0.38
                WHEN 声呐横滚角度 BETWEEN 3 AND 4 THEN 声呐横滚角度 *0.47
                WHEN 声呐横滚角度 BETWEEN 2 AND 3 THEN 声呐横滚角度 *0.66
                ELSE 声呐横滚角度
            END
            WHERE 声呐横滚角度 > 2;
        ";

                return db.Ado.ExecuteCommand(sql);
            }
        }
        #endregion
    }
}
