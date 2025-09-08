using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AFWDPPS.DB
{
    public static class FistDbManager
    {
        // 每个串口一个队列
        private static readonly BlockingCollection<船体姿态数据> queue1 = new BlockingCollection<船体姿态数据>(new ConcurrentQueue<船体姿态数据>());
        private static readonly BlockingCollection<声呐姿态数据> queue2 = new BlockingCollection<声呐姿态数据>(new ConcurrentQueue<声呐姿态数据>());
        private static readonly BlockingCollection<控制数据> queue3 = new BlockingCollection<控制数据>(new ConcurrentQueue<控制数据>());
        private static readonly BlockingCollection<船体姿态数据陀螺> queue4 = new BlockingCollection<船体姿态数据陀螺>(new ConcurrentQueue<船体姿态数据陀螺>());

        public static void Run()
        {
            // 后台数据库写线程
            var writerTask = Task.Run(() => DbWriter());

            //Console.WriteLine("按任意键退出...");
            //Console.ReadKey();

            //// 通知生产完成
            //queue1.CompleteAdding();
            //queue2.CompleteAdding();
            //queue3.CompleteAdding();

            //await writerTask;

            //Console.WriteLine("所有数据写入完成。");
        }




        public static void AddBoardData(this 船体姿态数据 data)
        {
            queue1.Add(data);
        }

        public static void AddSonarData(this 声呐姿态数据 data)
        {
            queue2.Add(data);
        }

        public static void AddControlData(this 控制数据 data)
        {
            queue3.Add(data);
        }
        public static void AddTLData(this 船体姿态数据陀螺 data)
        {
            queue4.Add(data);
        }
        static int buffecounts = 500;
        // 后台数据库写线程
        private static async Task DbWriter()
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = "DataSource=test_multi.db",
                DbType = DbType.Sqlite,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true
            });

            db.CodeFirst.InitTables<船体姿态数据, 声呐姿态数据, 控制数据, 船体姿态数据陀螺>();
            db.Ado.ExecuteCommand("PRAGMA journal_mode=WAL;");
            db.Ado.ExecuteCommand("PRAGMA synchronous=NORMAL;");

            var buffer1 = new List<船体姿态数据>();
            var buffer2 = new List<声呐姿态数据>();
            var buffer3 = new List<控制数据>();
            var buffer4 = new List<船体姿态数据陀螺>();
            var lastFlush = DateTime.Now;

            while (!queue1.IsCompleted || !queue2.IsCompleted || !queue3.IsCompleted && !queue4.IsCompleted)
            {
                // 尝试取队列数据
                while (queue1.TryTake(out var p1))
                    buffer1.Add(p1);

                while (queue2.TryTake(out var p2))
                    buffer2.Add(p2);

                while (queue3.TryTake(out var p3))
                    buffer3.Add(p3);

                while (queue4.TryTake(out var p4))
                    buffer4.Add(p4);

                // 条件：每100条或每100ms提交一次
                if (buffer1.Count + buffer2.Count + buffer3.Count + buffer4.Count >= 300 ||
                    (DateTime.Now - lastFlush).TotalMilliseconds > 2000)
                {
                    if (buffer1.Count + buffer2.Count + buffer3.Count + buffer4.Count > 0)
                    {
                        await db.Ado.UseTranAsync(async () =>
                        {
                            if (buffer1.Count > 0) await db.Insertable(buffer1).ExecuteCommandAsync();
                            if (buffer2.Count > 0) await db.Insertable(buffer2).ExecuteCommandAsync();
                            if (buffer3.Count > 0) await db.Insertable(buffer3).ExecuteCommandAsync();
                            if (buffer4.Count > 0) await db.Insertable(buffer4).ExecuteCommandAsync();

                        });

                        buffer1.Clear();
                        buffer2.Clear();
                        buffer3.Clear();
                        buffer4.Clear();
                        lastFlush = DateTime.Now;
                    }
                }
            }

            // flush 剩余数据
            if (buffer1.Count + buffer2.Count + buffer3.Count + buffer4.Count > 0)
            {
                await db.Ado.UseTranAsync(async () =>
                {
                    if (buffer1.Count > 0) await db.Insertable(buffer1).ExecuteCommandAsync();
                    if (buffer2.Count > 0) await db.Insertable(buffer2).ExecuteCommandAsync();
                    if (buffer3.Count > 0) await db.Insertable(buffer3).ExecuteCommandAsync();
                    if (buffer4.Count > 0) await db.Insertable(buffer4).ExecuteCommandAsync();

                });
            }
        }
    }

    // 基类
    public class BaseData
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(ColumnDataType = "TEXT")]
        public DateTime Timestamp { get; set; }


    }

    [SugarTable("船体姿态数据")]
    public class 船体姿态数据 : BaseData
    {

        [SugarColumn(IsNullable = true)]
        public double 船俯仰角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 船横滚角度 { get; set; }
    }
    [SugarTable("船体姿态数据陀螺")]
    public class 船体姿态数据陀螺 : BaseData
    {

        [SugarColumn(IsNullable = true)]
        public double 船俯仰角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 船横滚角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double x { get; set; }
        [SugarColumn(IsNullable = true)]
        public double y { get; set; }
        [SugarColumn(IsNullable = true)]
        public double z { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 温度 { get; set; }
    }
    [SugarTable("声呐姿态数据")]
    public class 声呐姿态数据 : BaseData
    {

        [SugarColumn(IsNullable = true)]
        public double 船俯仰角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 船横滚角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 声呐俯仰角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 俯仰电机动作角度 { get; set; }

        [SugarColumn(IsNullable = true)]
        public double 声呐横滚角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 横滚电机动作角度 { get; set; }
    }
    [SugarTable("控制数据")] public class 控制数据 : BaseData { }
}
