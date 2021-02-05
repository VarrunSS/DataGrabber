using DataGrabberV2.LogWriter;
using DataGrabberV2.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace DataGrabberV2.Utility
{
    public static partial class DataHelper
    {

        public static string ResponseFromWeb(string Url)
        {
            String text = String.Empty;
            try
            {
                HttpWebRequest webRequest;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                webRequest = (HttpWebRequest)HttpWebRequest.Create(Url);
                webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";
                webRequest.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);

                    text = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                }
                text = text.Replace("\r\n", "").Replace("\t", "").Replace("\n", "");
            }
            catch (Exception ex)
            {
                Logger.Write("Exception while receiving the request " + ex.Message.ToString());
                text = string.Empty;
            }
            return text;
        }

        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            PropertyDescriptorCollection properties =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        //public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        //{
        //    try
        //    {
        //        List<T> list = new List<T>();

        //        foreach (var row in table.AsEnumerable())
        //        {
        //            T obj = new T();

        //            foreach (var prop in obj.GetType().GetProperties())
        //            {
        //                try
        //                {
        //                    PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
        //                    propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
        //                }
        //                catch
        //                {
        //                    continue;
        //                }
        //            }

        //            list.Add(obj);
        //        }

        //        return list;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public static string JsonSerialize<T>(T objectData)
        {
            string result = string.Empty;
            if (objectData != null)
            {
                result = JsonConvert.SerializeObject(objectData,
                                 Formatting.None,
                                 new JsonSerializerSettings
                                 {
                                     NullValueHandling = NullValueHandling.Ignore,
                                     DefaultValueHandling = DefaultValueHandling.Ignore
                                 });
            }
            return result;
        }

        public static string BuildCreateTableSql(this DataTable table)
        {
            string tableName = "SqlTempTable";
            StringBuilder sql = new StringBuilder();
            StringBuilder alterSql = new StringBuilder();

            try
            {

                sql.AppendFormat("IF OBJECT_ID ('tempdb..[#{0}]') IS NOT NULL DROP TABLE [#{0}]; \n\n", tableName);
                sql.AppendFormat("CREATE TABLE [#{0}] (", tableName);

                // Column names
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    bool isNumeric = false;
                    bool usesColumnDefault = true;
                    string columnType = table.Columns[i].DataType.ToString().ToUpper();

                    sql.AppendFormat("\n\t[{0}]", table.Columns[i].ColumnName);

                    switch (columnType)
                    {
                        case "SYSTEM.INT16":
                            sql.Append(" smallint");
                            isNumeric = true;
                            break;
                        case "SYSTEM.INT32":
                            sql.Append(" int");
                            isNumeric = true;
                            break;
                        case "SYSTEM.INT64":
                            sql.Append(" bigint");
                            isNumeric = true;
                            break;
                        case "SYSTEM.DATETIME":
                            sql.Append(" datetime");
                            usesColumnDefault = false;
                            break;
                        case "SYSTEM.STRING":
                            sql.AppendFormat(" nvarchar({0})", "max");
                            break;
                        case "SYSTEM.SINGLE":
                            sql.Append(" single");
                            isNumeric = true;
                            break;
                        case "SYSTEM.DOUBLE":
                            sql.Append(" double");
                            isNumeric = true;
                            break;
                        case "SYSTEM.DECIMAL":
                            sql.AppendFormat(" decimal(18, 6)");
                            isNumeric = true;
                            break;
                        default:
                            sql.AppendFormat(" nvarchar({0})", table.Columns[i].MaxLength);
                            break;
                    }


                    sql.Append(",");
                }

                sql.Append("\n);\n\n");

                // Table body data


                foreach (DataRow row in table.Rows)
                {
                    sql.AppendFormat("INSERT INTO [#{0}] VALUES", tableName);

                    sql.Append("(");
                    foreach (DataColumn col in table.Columns)
                    {
                        sql.AppendFormat("'{0}',", row[col.ColumnName].ToString());
                    }
                    sql.Length--;
                    sql.Append(")\n");
                }

                // Remove last comma
                sql.Length--; //sql.Remove(sql.Length - 1, 1);

                sql.Append("\n\n DECLARE @table AS [dbo].CM_WS_Whitespace_ProjectScope_Details; \n");
                sql.AppendFormat("\n INSERT INTO @table \n SELECT * FROM [#{0}]\n", tableName);



            }
            catch (Exception)
            {

            }

            return sql.ToString();
        }

        public static string SetNullIfEmpty(string Value)
        {
            string result = null;
            try
            {
                result = string.IsNullOrEmpty(Value) ? null : Value;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public static T Clone<T>(this T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }


    }

    public static partial class DataHelper
    {

        /// <summary>
        /// Splits an array into several smaller arrays.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            //dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            dtDateTime = DateTime.ParseExact(unixTimeStamp.ToString(), "M/dd/yyyy hh: mm:ss tt", CultureInfo.InvariantCulture);
            return dtDateTime;
        }

        public static string GenerateID()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static bool SetYesOrNo(this string text)
        {
            return (!string.IsNullOrEmpty(text) && text.ToLower() == "yes");
        }



    }

    public static partial class DataHelper
    {


        public static string FormatTextFromHtmlContent(string result)
        {
            try
            {
                if (string.IsNullOrEmpty(result)) return result;

                //result = result.Replace("&nbsp;", " ");
                result = Regex.Replace(result, @"\t|\n|\r", "").Trim();
                result = HttpUtility.HtmlDecode(result);
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in FormatTextFromHtmlConetent -- ScrapeProductsListInput -> DataGrabberV2. Message: " + ex.Message);
            }
            finally
            {

            }
            return result;

        }

        public static void RemoveComments(HtmlNode node)
        {
            if (node.InnerText.Contains("<!-- -->"))
            {
                foreach (var n in node.ChildNodes.ToArray())
                    RemoveComments(n);
                if (node.NodeType == HtmlNodeType.Comment)
                    node.Remove();
            }
        }

    }

}

