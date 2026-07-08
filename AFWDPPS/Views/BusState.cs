using System;
using System.Collections.Generic;

namespace AFWDPP.Views
{
    /// <summary>
    /// WP 模块与 MU 模块之间的全局共享数据。
    /// 所有字段静态，MU 后台线程写入，WP UI 线程定时器读取。
    /// </summary>
    public static class BusState
    {
        /// <summary>
        /// MU 解析出的最新船姿字节 [H3, L3, H4, L4]。
        /// MU 接收线程写入，WP 定时器读取后填入帧 [6-9] 位置。
        /// </summary>
        public static readonly byte[] ShipAttitude = new byte[4];

        /// <summary>
        /// MU 是否在线：最近 MU_TIMEOUT_SECONDS 秒内收到过有效帧。
        /// </summary>
        public static bool MuAlive = false;

        /// <summary>
        /// 上次收到 MU 有效帧的时间（用于判断 MuAlive）。
        /// </summary>
        public static DateTime LastMuTime = DateTime.MinValue;

        /// <summary>
        /// MU 离线时船姿字段 [6-9] 的填充策略：
        /// true  = 填 0（清零）
        /// false = 保持上一组值（保留 ShipAttitude 不变）
        /// 由 MU 模块页面的开关控制。
        /// </summary>
        public static bool OfflineFillZero = true;

        /// <summary>
        /// 用户按按钮触发的指令队列。
        /// 按钮点击时入队，定时器 80ms 一帧轮流出队发送。
        /// </summary>
        public static readonly Queue<CmdItem> ButtonCmds = new Queue<CmdItem>();

        /// <summary>
        /// 自检是否启用（Senbit 循环用）。
        /// </summary>
        public static bool SelfCheckEnabled = false;

        /// <summary>
        /// 自检当前帧状态位（Senbit 计算后写入）。
        /// </summary>
        public static byte SelfCheckStatus = 0x00;

        /// <summary>
        /// 自检当前帧横摇指令角度（×1000，Senbit 写入）。
        /// </summary>
        public static short SelfCheckX = 0;

        /// <summary>
        /// 自检当前帧纵倾指令角度（×1000，Senbit 写入）。
        /// </summary>
        public static short SelfCheckY = 0;

        /// <summary>
        /// 按钮点发指令默认发送次数（你定的统一值）。
        /// </summary>
        public const int DEFAULT_CMD_TIMES = 10;

        /// <summary>
        /// MU 离线判定超时秒数。
        /// </summary>
        public const int MU_TIMEOUT_SECONDS = 3;
    }
}
