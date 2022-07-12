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

    }
}