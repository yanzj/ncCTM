﻿using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CTM.Core.Util
{
    public static class EnumHelper
    {
        /// <summary>
        /// 扩展方法：根据枚举值得到相应的枚举定义字符串
        /// </summary>
        /// <param name="value"></param>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static string ToEnumString(this int value, Type enumType)
        {
            NameValueCollection nvc = GetEnumStringFromEnumValue(enumType);
            return nvc[value.ToString()];
        }

        /// <summary>
        /// 扩展方法：根据枚举值得到属性Description中的描述, 如果没有定义此属性则返回空串
        /// </summary>
        /// <param name="value"></param>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static string ToEnumDescriptionString(this int value, Type enumType)
        {
            NameValueCollection nvc = GetNVCFromEnumValue(enumType);
            return nvc[value.ToString()];
        }

        /// <summary>
        /// 根据枚举类型得到其所有的 值 与 枚举定义字符串 的集合
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static NameValueCollection GetEnumStringFromEnumValue(Type enumType)
        {
            NameValueCollection nvc = new NameValueCollection();
            Type typeDescription = typeof(DescriptionAttribute);
            System.Reflection.FieldInfo[] fields = enumType.GetFields();
            string strText = string.Empty;
            string strValue = string.Empty;
            foreach (var field in fields)
            {
                if (field.FieldType.IsEnum)
                {
                    strValue = ((int)enumType.InvokeMember(field.Name, System.Reflection.BindingFlags.GetField, null, null, null)).ToString();
                    nvc.Add(strValue, field.Name);
                }
            }
            return nvc;
        }

        /// <summary>
        /// 根据枚举类型得到其所有的 值 与 枚举定义Description属性 的集合
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static NameValueCollection GetNVCFromEnumValue(Type enumType)
        {
            NameValueCollection nvc = new NameValueCollection();
            Type typeDescription = typeof(DescriptionAttribute);
            System.Reflection.FieldInfo[] fields = enumType.GetFields();
            string strText = string.Empty;
            string strValue = string.Empty;
            foreach (var field in fields)
            {
                if (field.FieldType.IsEnum)
                {
                    strValue = ((int)enumType.InvokeMember(field.Name, System.Reflection.BindingFlags.GetField, null, null, null)).ToString();
                    object[] arr = field.GetCustomAttributes(typeDescription, true);
                    if (arr.Length > 0)
                    {
                        DescriptionAttribute da = (DescriptionAttribute)arr[0];
                        strText = da.Description;
                    }
                    else
                    {
                        strText = "";
                    }
                    nvc.Add(strValue, strText);
                }
            }
            return nvc;
        }
    }
}