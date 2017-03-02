using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using APIElement = Bloomberglp.Blpapi.Element;

namespace BBLib.BBEngine
{
    internal static class Functions
    {
        /// <summary>
        /// Gets a <c>T</c> Blpapi value as converted.
        /// </summary>
        /// <param name="element">Blpapi element.</param>
        /// <returns>Returns converted Blpapi element value.</returns>
        public static T GetValueAsConverted<T>(APIElement element)
        {
            try
            {
                System.Type type = typeof(T);
                if (conversions.ContainsKey(type))
                    return (T)conversions[type](element);
                else
                    throw new FormatException();
            }
            catch
            {
                throw;
            }
            
        }

        // Blpapi value conversions implementation
        private static readonly Dictionary<System.Type, Func<APIElement, object>> conversions = new Dictionary<System.Type, Func<APIElement, object>>
        {
            { typeof(bool), n => System.Convert.ToBoolean(n.GetValueAsString()) },
            { typeof(char), n => System.Convert.ToChar(n.GetValueAsString()) },
            { typeof(DateTime), n => System.Convert.ToDateTime(n.GetValueAsString()) },
            { typeof(float), n => System.Convert.ToSingle(n.GetValueAsString()) },
            { typeof(double), n => System.Convert.ToDouble(n.GetValueAsString()) },
            { typeof(int), n => System.Convert.ToInt32(n.GetValueAsString()) },
            { typeof(long), n => System.Convert.ToInt64(n.GetValueAsString()) },
            { typeof(string), n =>  System.Convert.ToString(n.GetValueAsString()) }
        };

        /// <summary>
        /// Gets a shallow copy (including errors) of a data row.
        /// </summary>
        /// <param name="modelTable">Model table to copy.</param>
        /// <param name="modelRow">Model row to copy.</param>
        /// <returns>Shallow copy (including errors) of the input data row.</returns>
        public static DataRow CopyDataRow(DataTable modelTable, DataRow modelRow)
        {
            DataRow newRow = modelTable.NewRow();
            newRow.ItemArray = (object[])modelRow.ItemArray.Clone();

            if (modelRow.HasErrors)
            {
                DataColumn[] errorColumns = modelRow.GetColumnsInError();
                foreach (DataColumn item in errorColumns)
                {
                    newRow.SetColumnError(item, modelRow.GetColumnError(item));
                }
            }

            return newRow;
        }

        /// <summary>
        /// Replaces multispaces, tabs, and line returns.
        /// </summary>
        /// <param name="input">Input string to transform.</param>
        /// <param name="replacement">Remaining spaces replacement.</param>
        /// <returns></returns>
        public static string Replace(string input, string replacement)
        {
            input = Regex.Replace(input, @"^\s*", @"");
            input = Regex.Replace(input, @"$\s*", @"");
            input = Regex.Replace(input, @"\s+", replacement);

            return input;
        }
    }
}
