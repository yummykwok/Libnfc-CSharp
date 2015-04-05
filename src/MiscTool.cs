using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace LibNFC4CSharp
{
    class MiscTool
    {
        public static void memcpy(byte[] dest, int offsetd, byte[] src, int offsets, int size)
        {

            for (int i = 0; i < size; i++)
            {
                dest[i + offsetd] = src[i + offsets];
            }
        }
        public static void memset(byte[] m, byte value, int size)
        {
            for (int i = 0; i < size; i++) m[i] = value;
        }
        public static void memset<T>(T[] m, T value, int size)
        {
            for (int i = 0; i < size; i++)
            {
                m[i] = value;
            }
        }

        public static int memcmp(byte[] a, byte[] b, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (a[i] == b[i]) continue;
                return a[i] - b[i];
            }
            return 0;
        }

        public static byte[] SubBytes(byte[] abtRx, int pbt)
        {
            byte[] b = new byte[abtRx.Length - pbt];
            for (int i = 0; i < b.Length; i++) b[i] = abtRx[pbt + i];
            return b;
        }

        #region 方法
        /// <summary>
        /// 比较--两个类型一样的实体类对象的值
        /// </summary>
        /// <param name="oneT"></param>
        /// <returns></returns>
        public static bool CompareType<T>(T oneT, T twoT)
        {
            #region
            bool result = true;//两个类型作比较时使用,如果有不一样的就false
            Type typeOne = oneT.GetType();
            Type typeTwo = twoT.GetType();
            //如果两个T类型不一样  就不作比较
            if (!typeOne.Equals(typeTwo)) { return false; }
            PropertyInfo[] pisOne = typeOne.GetProperties(); //获取所有公共属性(Public)
            PropertyInfo[] pisTwo = typeTwo.GetProperties();
            //如果长度为0返回false
            if (pisOne.Length <= 0 || pisTwo.Length <= 0)
            {
                return false;
            }
            //如果长度不一样，返回false
            if (!(pisOne.Length.Equals(pisTwo.Length))) { return false; }
            //遍历两个T类型，遍历属性，并作比较
            for (int i = 0; i < pisOne.Length; i++)
            {
                //获取属性名
                string oneName = pisOne[i].Name;
                string twoName = pisTwo[i].Name;
                //获取属性的值
                object oneValue = pisOne[i].GetValue(oneT, null);
                object twoValue = pisTwo[i].GetValue(twoT, null);
                //比较,只比较值类型
                if ((pisOne[i].PropertyType.IsValueType || pisOne[i].PropertyType.Name.StartsWith("String")) && (pisTwo

[i].PropertyType.IsValueType || pisTwo[i].PropertyType.Name.StartsWith("String")))
                {
                    if (oneName.Equals(twoName))
                    {
                        if (oneValue == null)
                        {
                            if (twoValue != null)
                            {
                                result = false;
                                break; //如果有不一样的就退出循环
                            }
                        }
                        else if (oneValue != null)
                        {
                            if (twoValue != null)
                            {
                                if (!oneValue.Equals(twoValue))
                                {
                                    result = false;
                                    break; //如果有不一样的就退出循环
                                }
                            }
                            else if (twoValue == null)
                            {
                                result = false;
                                break; //如果有不一样的就退出循环
                            }
                        }
                    }
                    else
                    {
                        result = false;
                        break;
                    }
                }
                else
                {
                    //如果对象中的属性是实体类对象，递归遍历比较
                    bool b = CompareType(oneValue, twoValue);
                    if (!b) { result = b; break; }
                }
            }
            return result;
            #endregion
        }
        #endregion
    
    }
}
