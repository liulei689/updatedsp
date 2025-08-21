using AFWDPPS.DB;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WpfApp3D.Models
{

    public static class AsyncLogger
    {
        private static readonly BlockingCollection<稳定平台数据> _logQueue = new BlockingCollection<稳定平台数据>();
        private static Task _workerTask;

        public static void Initialize()
        {
            // 启动后台工作者线程
            _workerTask = Task.Run(ProcessLogQueue);
        }

        public static void Log(稳定平台数据 logEntry)
        {
            try
            {
                _logQueue.Add(logEntry); // 添加日志条目到队列
            }
            catch
            {
            }
        }

        private static async Task ProcessLogQueue()
        {
            foreach (var logEntry in _logQueue.GetConsumingEnumerable())
            {
                try
                {
                    await 存储数据Async(logEntry);
                }
                catch (Exception ex)
                {
                }
            }
        }
        private static 稳定平台数据 上一组数据;
        public static async Task 存储数据Async(稳定平台数据 当前数据)
        {
            if (上一组数据 == null)
            {
                // 如果是第一次存储，直接存储
                上一组数据 = 当前数据;
                return;
            }

            // 检查关键角度是否发生变化
            bool 角度变化 = false;
            if (当前数据.船俯仰角度 != 上一组数据.船俯仰角度 ||
                当前数据.声呐俯仰角度 != 上一组数据.声呐俯仰角度 ||
                当前数据.俯仰电机动作角度 != 上一组数据.俯仰电机动作角度 ||
                当前数据.船横滚角度 != 上一组数据.船横滚角度 ||
                当前数据.声呐横滚角度 != 上一组数据.声呐横滚角度 ||
                当前数据.横滚电机动作角度 != 上一组数据.横滚电机动作角度)
            {
                角度变化 = true;
            }

            if (角度变化)
            {
                // 如果有角度变化，则存储当前数据
                上一组数据 = 当前数据;
                await AFWDPPS.DB.WDPT.Add(当前数据);

            }
            else
            {

            }
        }

        // 用于通知日志系统停止接收新日志条目，并完成处理现有条目
        public static void Shutdown()
        {
            _logQueue.CompleteAdding();

            // 等待工作者线程处理完所有日志条目
            try
            {
                _workerTask.Wait();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // 处理取消的情况
            }
        }
    }


}
