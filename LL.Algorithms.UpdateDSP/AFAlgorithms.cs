using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public class AFAlgorithms : IAFAlgorithms
    {

        byte SendCount = 0;
        private const byte HEAD1 = 0x78;
        private const byte HEAD2 = 0x55;

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
            COM_STATUS_DEVICE_ID,
            COM_STATUS_DEVICE_FC1,
            COM_STATUS_DEVICE_FC2,
            COM_STATUS_LEN,
            COM_STATUS_DATA
        }
        /// <summary>
        /// 获取应答数据中数据部分
        /// </summary>
        /// <param name="bt_RecBuf">接收的数据</param>
        /// <returns></returns>
        public List<byte> GetRecBufData_422(byte[] bt_RecBuf, byte devideid)
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
                        if (tmpByte == devideid)
                        {
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DEVICE_FC1;
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

                    //case (int)enum_ComStatus.COM_STATUS_DEVICE_ID: //设备ID
                    //    G_btList_RecBuf.Add(tmpByte); //测试上位机不过滤设备ID
                    //    G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DEVICE_FC1;
                    //    break;
                    case (int)enum_ComStatus.COM_STATUS_DEVICE_FC1: //设备功能字节1
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DEVICE_FC2;
                        break;
                    case (int)enum_ComStatus.COM_STATUS_DEVICE_FC2: //设备功能字节2
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_LEN;
                        break;
                    case (int)enum_ComStatus.COM_STATUS_LEN://获取数据包长度
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_RecBufLen = tmpByte + 7;
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                        break;

                    case (int)enum_ComStatus.COM_STATUS_DATA:
                        G_btList_RecBuf.Add(tmpByte);
                        //数据接收完成后的有效性判断
                        if (G_btList_RecBuf.Count == G_int_RecBufLen)  //包接收完成
                        {
                            G_btList_RecBuf_R.AddRange(G_btList_RecBuf);

                            ////检查校验和字节
                            //if ((DSP28335.CheckSumNomarl(G_btList_RecBuf.ToArray())))
                            //{
                            //    G_btList_RecBuf_R.AddRange(G_btList_RecBuf);
                            //}

                            //else
                            //{
                            //    G_btList_RecBuf.Clear();
                            //    //string str_ErrorInfo = "“";
                            //    //foreach (byte tmpbt in G_btList_RecBuf)
                            //    //{
                            //    //    str_ErrorInfo += tmpbt.ToString("X2") + " ";
                            //    //}
                            //    //str_ErrorInfo += "”帧校验和错误！";

                            //}

                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                            return G_btList_RecBuf_R;
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
    }
}
