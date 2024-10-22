using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;

namespace Luxlib.Robot
{
    public static class ResultDeserializer
    {
        public static bool GetSignalValue(dynamic json)
        {
            // Recover value
            return json._embedded.resources[0].lvalue.Value == "0" ? false : true;
        }

        public static string GetAnalogSignalValue(dynamic json)
        {
            // Recover value
            return json._embedded.resources[0].lvalue.Value;
        }

        public static string GetGroupSignalValue(dynamic json)
        {
            // Recover value
            return json._embedded.resources[0].lvalue.Value;
        }

        public static double GetNumericValue(dynamic json)
        {
            //Force separators to avoid localization compatibility
            var _culture = ((CultureInfo)CultureInfo.CurrentCulture.Clone());
            _culture.NumberFormat.NumberDecimalSeparator = ".";
            _culture.NumberFormat.NumberGroupSeparator = ",";
            // Recover value
            return Convert.ToDouble(json.state[0].value, _culture);
        }

        public static string GetStringValue(dynamic json)
        {
            // Remove quotes at the beginning and the end of the result 
            string result = json.state[0].value;
            return result.Substring(1, result.Length - 2);
        }

        public static List<string> GetArrayValues(dynamic json)
        {
            // Recover values
            List<string> returnList = new List<string>();
            string result = json.state[0].value;
            string[] splitted = result.Substring(1, result.Length - 2).Split(',');
            Array.ForEach(splitted, x => returnList.Add(x));
            return returnList;
        }

        public static string GetErrorMessage(dynamic json)
        {
            try
            {
                string result = json.status.msg;
                return result;
            }
            catch 
            {
                return "";
            }
        }
    }
}
