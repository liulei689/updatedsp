namespace AFWDPP.Views
{
    /// <summary>
    /// 按钮点发指令队列的元素。
    /// 按钮点击时入队，定时器每 80ms 出队发送一帧，TimesLeft-- 直至 0 丢弃。
    /// </summary>
    public class CmdItem
    {
        /// <summary>
        /// 状态位（0x01~0x06，A5 协议指令标识）。
        /// </summary>
        public byte Status;

        /// <summary>
        /// 横摇指令角度（已 ×1000）。
        /// </summary>
        public short X;

        /// <summary>
        /// 纵倾指令角度（已 ×1000）。
        /// </summary>
        public short Y;

        /// <summary>
        /// 剩余发送帧数：扣到 0 丢弃。
        /// </summary>
        public int TimesLeft;

        /// <summary>
        /// 构造一个待发指令项。
        /// </summary>
        /// <param name="status">状态位</param>
        /// <param name="x">横摇指令角度 ×1000</param>
        /// <param name="y">纵倾指令角度 ×1000</param>
        /// <param name="timesLeft">剩余发送帧数</param>
        public CmdItem(byte status, short x, short y, int timesLeft)
        {
            Status = status;
            X = x;
            Y = y;
            TimesLeft = timesLeft;
        }
    }
}
