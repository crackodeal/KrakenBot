using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using KrakenClient;
using KrakenClientConsole;
using NLog;

namespace KrakenExcel
{
    public class Helper
    {
        private static NLog.Logger nLogger = LogManager.GetCurrentClassLogger();
        private static KrakenClient.KrakenClient client = new KrakenClient.KrakenClient();
        private static Broker broker = new Broker();

        #region For Excel
        public async static Task<decimal> GetBalance(string asset)
        {
            decimal balanceAsset = 0;
            await Task.Run(() =>
            {
                var balance = client.GetBalance();
                var jsSerializer = new JavaScriptSerializer();
                var result = jsSerializer.DeserializeObject(balance["result"].ToString());
                var objBalances = (Dictionary<string, object>)result;
                balanceAsset = Convert.ToDecimal(objBalances[asset]);
            });
            return balanceAsset;
        }
        public async static Task<decimal> GetTradeBalance(string asset, string value)
        {
            decimal balanceAsset = 0;
            await Task.Run(() =>
            {
                var balance = client.GetTradeBalance(null, asset);
                var jsSerializer = new JavaScriptSerializer();
                var result = jsSerializer.DeserializeObject(balance["result"].ToString());
                var objBalances = (Dictionary<string, object>)result;
                balanceAsset = Convert.ToDecimal(objBalances[value]);
            });
            return balanceAsset;
        }
        public async static Task<decimal> GetTicker(string pair, string value)
        {
            decimal price = 0;
            await Task.Run(() =>
            {
                var ticker = client.GetTicker(new List<string> { pair });
                var jsSerializer = new JavaScriptSerializer();
                var result = jsSerializer.DeserializeObject(ticker["result"].ToString());
                var objPrices = (Dictionary<string, object>)result;
                var objPrice = (Dictionary<string, object>)objPrices[pair];
                price = Convert.ToDecimal(((object[])objPrice[value])[0].ToString());
            });
            return price;
        }
        #endregion
    }
}