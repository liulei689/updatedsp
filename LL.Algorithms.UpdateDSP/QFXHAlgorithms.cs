using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public class QFXHAlgorithms : IQFXHAlgorithms
    {

        byte SendCount = 0;
        private const byte HEAD1 = 0xAA;
        private const byte HEAD2 = 0x55;

        public void SetHexHead(byte[] data)
        {
            data[0] = HEAD1;
            data[1] = HEAD2;
            if (SendCount < 255)
            {
                SendCount++;
            }
            else
            {
                SendCount = 0;
            }
            data[2] = SendCount;
            data[3] = 0x80;
        }
        /// <summary>
        /// 通讯数据接收状态机标志
        /// </summary>
        private int G_int_ComStatus = 0;
        private List<byte> G_btList_RecBuf = new List<byte>();
        private List<byte> G_btList_RecBuf_R = new List<byte>();
        private int G_int_RecBufLen = 0;
        /// <summary>
        /// 协议解析状态
        /// </summary>
        private enum enum_ComStatus
        {
            COM_STATUS_HEAD1 = 0,
            COM_STATUS_HEAD2,
            // COM_STATUS_ID,
            COM_STATUS_HEARTBEAT,
            COM_STATUS_LEN,
            COM_STATUS_DATA
        }
        /// <summary>
        /// 获取应答数据中数据部分
        /// </summary>
        /// <param name="bt_RecBuf">接收的数据</param>
        /// <returns></returns>
        public List<byte> GetRecBufData_422(params byte[] bt_RecBuf)
        {
            G_btList_RecBuf_R.Clear();
            foreach (byte tmpByte in bt_RecBuf)
            {
                switch (G_int_ComStatus)
                {
                    case (int)enum_ComStatus.COM_STATUS_HEAD1:
                        G_btList_RecBuf.Clear();

                        if (tmpByte == HEAD1)
                        {
                            // tmpHEAD1 = tmpByte;
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD2;
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        break;

                    case (int)enum_ComStatus.COM_STATUS_HEAD2:
                        if (tmpByte == HEAD2)
                        {
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEARTBEAT;
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        else if (tmpByte == HEAD1)  //此处代码起到保护帧头1的下一个字节不被本函数丢掉
                        {
                            G_btList_RecBuf.Clear();
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        else
                        {
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                        }
                        break;

                    case (int)enum_ComStatus.COM_STATUS_HEARTBEAT:
                        G_btList_RecBuf.Add(tmpByte); //测试上位机不过滤设备ID
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_LEN;
                        break;

                    case (int)enum_ComStatus.COM_STATUS_LEN://获取数据包长度
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_RecBufLen = tmpByte;
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                        break;

                    case (int)enum_ComStatus.COM_STATUS_DATA:
                        G_btList_RecBuf.Add(tmpByte);

                        //数据接收完成后的有效性判断
                        if (G_btList_RecBuf.Count == G_int_RecBufLen)  //包接收完成
                        {
                            //检查校验和字节
                            if ((DSP28335.CheckSumNomarl(G_btList_RecBuf.ToArray())))
                            {
                                G_btList_RecBuf_R.AddRange(G_btList_RecBuf);
                            }

                            else
                            {
                                G_btList_RecBuf.Clear();
                                //string str_ErrorInfo = "“";
                                //foreach (byte tmpbt in G_btList_RecBuf)
                                //{
                                //    str_ErrorInfo += tmpbt.ToString("X2") + " ";
                                //}
                                //str_ErrorInfo += "”帧校验和错误！";

                            }

                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                        }

                        //数据包长度超限检查
                        if (G_btList_RecBuf.Count >= 128)
                        {
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;

                            //str_ErrorInfo += "“";
                            //for (int i = 0; i < 6; i++)
                            //{
                            //    str_ErrorInfo += G_btList_RecBuf[i].ToString("X2") + " ";
                            //}
                            //str_ErrorInfo += "......”该帧数据长度超限！";

                            //清空相关缓存
                            G_btList_RecBuf.Clear();
                        }
                        break;

                    default:
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                        break;
                }
            }
            return G_btList_RecBuf_R;
        }

        public string GetQFXHCommAckResult(byte[] data)
        {
            // 将字节数组转换为字符串  
            if (data.Length < 31) return "应答过短，不合法，无法解析";
            string code = data[30].ToString("x2") + data[31].ToString("x2"); ;
            string str;
            switch (code.ToUpper())
            {
                case "0000":
                    str = "空";
                    break;
                case "1111":
                    str = "传输中";
                    break;
                case "2222":
                    str = "传输成功";
                    break;
                case "3333":
                    str = "传输失败";
                    break;
                case "4444":
                    str = "固化中";
                    break;
                case "5555":
                    str = "固化成功";
                    break;
                case "6666":
                    str = "固化失败";
                    break;
                case "7777":
                    str = "校验中";
                    break;
                case "8888":
                    str = "校验成功";
                    break;
                case "9999":
                    str = "校验失败";
                    break;
                default:
                    str = "应答无法解析";
                    break;
            }
            return str;
        }

        public string GetQFXHCommAckResult(byte code)
        {
            string str;
            switch (code)
            {
                case 0x01:
                    str = "扇区擦除错误";
                    break;
                case 0x02:
                    str = "扇区写入错误";
                    break;
                case 0x03:
                    str = "固件数据校验码错误，请尝试重新加载固件";
                    break;
                case 0x04:
                    str = "数据包校验失败，请尝试重新加载固件";
                    break;
                case 0x05:
                    str = "固件数据写入成功";
                    break;
                case 0x06:
                    str = "超出FLASH容量范围";
                    break;
                case 0x07:
                    str = "Boot串码不符错误,请尝试重新加载固件";
                    break;
                case 0x08:
                    str = "扇区擦除成功";
                    break;
                case 0x09:
                    str = "成功应答";
                    break;
                case 0x10:
                    str = "开始载入";
                    break;
                case 0xFF:
                    str = "非法数据包，,请尝试重新加载固件";
                    break;
                default:
                    str = "应答无法解析";
                    break;
            }
            return str;
        }
    }
}
