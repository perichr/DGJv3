using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;//先导入这个使用正则表达式

namespace DGJv3
{

    public class ShouZiMu
    {
        //汉字每个首字母的拼音中最小的字，顺序不能乱
        private static char[] firstcn = { '帀', '丫', '夕', '屲', '他', '仨', '呥', '七',
                                   '妑', '噢', '拏', '嘸', '垃', '咔', '丌', '铪',
                                   '旮', '发', '妸', '咑', '嚓', '八', '吖' };
        //所有汉字首字母
        private static char[] firsten = { 'Z', 'Y', 'X', 'W', 'T', 'S', 'R', 'Q',
                                   'P', 'O', 'N', 'M', 'L', 'K', 'J', 'H',
                                   'G', 'F', 'E', 'D', 'C', 'B', 'A' };

        /// <summary> 
        /// 汉字转化为拼音首字母
        /// </summary> 
        /// <param name="str">汉字</param> 
        /// <returns>首字母</returns> 
        public static string GetFirstPinyin(string strcn)
        {
            int intlen = strcn.Length;
            int index = 0;
            char chartemp = char.MinValue;
            string strtemp = string.Empty;
            Regex reg = new Regex(@"[\u4e00-\u9fa5]");//\u4e00-\u9fa5用来判断是不是中文的正则表达式
            CultureInfo pinyin = new CultureInfo(0x804);//保存区域特定的信息，如关联的语言、子语言、国家/地区、日历和区域性约定,这里表示中文
            if (intlen > 0)
            {
                char[] strchar = new char[intlen + 1];
                for (int i = 0; i < intlen; i++)
                {
                    strchar[i] = Convert.ToChar(strcn.Substring(i, 1));
                }
                foreach (char cstr in strchar)
                {
                    chartemp = char.MinValue;
                    if (reg.IsMatch(cstr.ToString()))//对于中文汉字，不包括中文特定字符
                    {
                        foreach (char fstr in firstcn)
                        {
                            if (string.Compare(cstr.ToString(), fstr.ToString(), pinyin, CompareOptions.None) >= 0)//将汉字与设定的汉字按拼音比较大小
                            {
                                index = Array.IndexOf(firstcn, fstr);
                                chartemp = firsten[index];
                                break;
                            }
                        }
                    }
                    else//对于非中文汉字，不作拼音码处理
                    {
                        chartemp = cstr;
                    }
                    strtemp += chartemp;
                }
            }
            return strtemp;
        }



        /// <summary>

        /// 在指定的字符串列表CnStr中检索符合拼音索引字符串

        /// </summary>

        /// <param name="CnStr">汉字字符串</param>

        /// <returns>相对应的汉语拼音首字母串</returns>

        public static string GetSpellCode(string CnStr)
        {

            string strTemp = "";

            int iLen = CnStr.Length;

            int i = 0;

            for (i = 0; i <= iLen - 1; i++)
            {

                strTemp += GetCharSpellCode(CnStr.Substring(i, 1));

            }

            return strTemp;

        }

        /// <summary>

        /// 得到一个汉字的拼音第一个字母，如果是一个英文字母则直接返回

        /// </summary>

        /// <param name="CnChar">单个汉字</param>

        /// <returns>单个大写字母</returns>

        private static string GetCharSpellCode(string CnChar)
        {

            long iCnChar;

            byte[] ZW = System.Text.Encoding.Default.GetBytes(CnChar);

            //如果是字母，则直接返回

            if (ZW.Length == 1)
            {

                return CnChar;

            }

            else
            {

                // get the array of byte from the single char

                int i1 = (short)(ZW[0]);

                int i2 = (short)(ZW[1]);

                iCnChar = i1 * 256 + i2;

            }

            // iCnChar match the constant

            if ((iCnChar >= 45217) && (iCnChar <= 45252))
            {

                return "A";

            }

            else if ((iCnChar >= 45253) && (iCnChar <= 45760))
            {

                return "B";

            }
            else if ((iCnChar >= 45761) && (iCnChar <= 46317))
            {

                return "C";

            }
            else if ((iCnChar >= 46318) && (iCnChar <= 46825))
            {

                return "D";

            }
            else if ((iCnChar >= 46826) && (iCnChar <= 47009))
            {

                return "E";

            }
            else if ((iCnChar >= 47010) && (iCnChar <= 47296))
            {

                return "F";

            }
            else if ((iCnChar >= 47297) && (iCnChar <= 47613))
            {

                return "G";

            }
            else if ((iCnChar >= 47614) && (iCnChar <= 48118))
            {

                return "H";

            }
            else if ((iCnChar >= 48119) && (iCnChar <= 49061))
            {

                return "J";

            }
            else if ((iCnChar >= 49062) && (iCnChar <= 49323))
            {

                return "K";

            }
            else if ((iCnChar >= 49324) && (iCnChar <= 49895))
            {

                return "L";

            }
            else if ((iCnChar >= 49896) && (iCnChar <= 50370))
            {

                return "M";

            }
            else if ((iCnChar >= 50371) && (iCnChar <= 50613))
            {

                return "N";

            }
            else if ((iCnChar >= 50614) && (iCnChar <= 50621))
            {

                return "O";

            }
            else if ((iCnChar >= 50622) && (iCnChar <= 50905))
            {

                return "P";

            }
            else if ((iCnChar >= 50906) && (iCnChar <= 51386))
            {

                return "Q";

            }
            else if ((iCnChar >= 51387) && (iCnChar <= 51445))
            {

                return "R";

            }
            else if ((iCnChar >= 51446) && (iCnChar <= 52217))
            {

                return "S";

            }
            else if ((iCnChar >= 52218) && (iCnChar <= 52697))
            {

                return "T";

            }
            else if ((iCnChar >= 52698) && (iCnChar <= 52979))
            {

                return "W";

            }
            else if ((iCnChar >= 52980) && (iCnChar <= 53640))
            {

                return "X";

            }
            else if ((iCnChar >= 53689) && (iCnChar <= 54480))
            {

                return "Y";

            }
            else if ((iCnChar >= 54481) && (iCnChar <= 55289))
            {

                return "Z";

            }
            else

                return ("*");

        }


    }
}