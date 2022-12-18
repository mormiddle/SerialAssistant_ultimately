using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortData
{
    internal class PortData
    {
        /// <summary>
        /// 保存涡流数据
        /// </summary>
        /// <param name="EC_Save"></param>
        /// <param name="doubles">要保存的二维数组</param>
        public static void Save_ECData(StreamWriter EC_Save, double[][] doubles)
        {
            for (int i = 0; i < doubles[0].Length; i++)
            {
                for (int j = 0; j < doubles.Length; j++)
                {
                    EC_Save.Write(doubles[j][i] + "\t");
                }
                EC_Save.WriteLine();
            }
            EC_Save.Flush();
        }

        public static double[][] SeparateData(List<string> list)
        {
            string[][] intList = new string[40][];//将list分组
            int[][] realData = new int[20][];
            double[][] finalData = new double[20][];//特征值
            int avg = 0;

            for (int i = 0; i < 20; i++)
            {

                finalData[i] = new double[10];
            }

            for (int i = 0; i < 40; i++)
            {
                intList[i] = new string[list.Count / 40];
            }

            for (int i = 0; i < realData.Length; i++)
            {
                realData[i] = new int[list.Count / 40];
            }

            for (int i = 0; i < 40; i++)
            {
                for (int j = 0; j < intList[0].Length; j++)
                {
                    intList[i][j] = list[j * 40 + i];
                }

            }

            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < realData[0].Length; j++)
                {
                    realData[i][j] = Convert.ToInt32(intList[i * 2][j] + intList[i * 2 + 1][j], 16);
                }
            }

            
       
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    for (int g=0; g < realData[0].Length/10; g++)
                    {
                        avg+=realData[i][g+j*(realData[0].Length / 10)];
                    }
                    finalData[i][j] = (double)avg / (realData[0].Length /10);
                    avg = 0;
                }
            }
            return finalData;
        }




        /// <summary>
        /// 输入字符串 经过校验位判断  取得并符合校验位的字符 
        /// </summary>
        /// <param name="str">输入的字符串</param>
        /// <returns>不包含校验位正确的字符</returns>
        public static double[][] DataControl(string str)
        {
            string[] Ordata = str.Trim().Split(' ').ToArray();
            List<string> list = new List<string>();
            List<string> Condata = new List<string>();

            for (int i = 0; i < Ordata.Length; i++)
            {
                if (Ordata[i] == "AA" && Ordata[i + 43] == "80")
                {
                    list.AddRange(Ordata.Skip(i).Take(44).ToArray());
                    i += 43;
                }
            }

            for (int i = 0; i < list.Count / 44; i++)
            {
                string[] Orstr = list.Skip(i * 44).Take(44).ToArray();

                if (StrDispose(Orstr))//如果CRC校验错误 去除此次数据
                {
                    list.RemoveRange(i * 44, Orstr.Length);
                }
            }
            for (int i = 0; i < list.Count / 44; i++)//要在校验之后 去除帧头 帧尾 长度 校验位
            {
                Condata.AddRange(list.Skip(i * 44 + 2).Take(40));
            }



            return SeparateData(Condata);

        }

        /// <summary>
        /// 一组数据的判断
        /// </summary>
        /// <param name="str">一组16进制字符串</param>
        /// <returns>校验结果</returns>
        public static bool StrDispose(string[] Orstr)
        {
            //string[] Orstr = str.Trim().Split(' ').ToArray();//传入的数据
            string[] str1 = Orstr.Skip(2).Take(Convert.ToInt32(Orstr[1], 16) - 1).ToArray();

            byte[] bytes = new byte[str1.Length];

            for (int i = 0; i < str1.Length; i++)
            {
                bytes[i] = Convert.ToByte(str1[i], 16);
            }

            byte[] by = Crc8(bytes);
            string str2 = Convert.ToString(by[0], 16).ToUpper();

            if (str2 == Orstr[Orstr.Length - 2])//倒数第二位为CRC校验位
            {
                return false;
            }
            else
                return true;
        }

        /// **********************************************************************
        /// Name: CRC8    x8+x2+x+1
        /// Poly: 0x07
        /// Init: 0x00
        /// Refin: false
        /// Refout: false
        /// Xorout: 0x00    
        ///*************************************************************************
        private static byte[] Crc8(byte[] buffer, int start = 0, int len = 0)
        {
            if (buffer == null || buffer.Length == 0) return null;
            if (start < 0) return null;
            if (len == 0) len = buffer.Length - start;
            int length = start + len;
            if (length > buffer.Length) return null;
            byte crc = 0;// Initial value
            for (int i = start; i < length; i++)
            {
                crc ^= buffer[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x80) > 0)
                        crc = (byte)((crc << 1) ^ 0x07);
                    else
                        crc = (byte)(crc << 1);
                }
            }
            return new byte[] { crc };
        }
    }
}
