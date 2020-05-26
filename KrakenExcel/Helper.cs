using KrakenClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace KrakenExcel
{
    public class Helper
    {
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
        public async static Task<object[,]> GetBalances()
        {
            object[,] excelBalances = null;
            await Task.Run(() =>
            {
                var jsSerializer = new JavaScriptSerializer();
                var response = client.GetBalance();
                var result = jsSerializer.DeserializeObject(response["result"].ToString());
                var balances = (Dictionary<string, object>)result;

                // Pack an array for Excel
                excelBalances = new object[balances.Count, 2];
                var i = 0;
                foreach (var balance in balances)
                {
                    excelBalances[i, 0] = balance.Key;
                    excelBalances[i, 1] = Convert.ToDecimal(balance.Value);
                    i++;
                }
            });
            return excelBalances;
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
        public async static Task<object[,]> GetPrices(object[] pair)
        {
            object[,] excelPrices = null;
            await Task.Run(() =>
            {
                var ticker = client.GetTicker( pair.OfType<string>().ToList() );
                var jsSerializer = new JavaScriptSerializer();
                var prices = (Dictionary<string, object>)jsSerializer.DeserializeObject(ticker["result"].ToString());

                // Pack an array for Excel
                excelPrices = new object[prices.Count, 3];
                var i = 0;
                foreach (var price in prices)
                {
                    excelPrices[i, 0] = price.Key;
                    excelPrices[i, 1] = Convert.ToDecimal(((object[])((Dictionary<string, object>)price.Value)["a"])[0]);
                    excelPrices[i, 2] = Convert.ToDecimal(((object[])((Dictionary<string, object>)price.Value)["b"])[0]);
                    i++;
                }
            });
            return excelPrices;
        }
        public async static Task<object[,]> GetTradesHistory(DateTime fromDate, DateTime toDate)
        {
            object[,] excelTrades = null;
            await Task.Run(() =>
            {
                long start = Convert.ToInt64(fromDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                long end = Convert.ToInt64(toDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                var tradesHistory = client.GetTradesHistory("", "all", false, start.ToString(), end.ToString());
                var jsSerializer = new JavaScriptSerializer();
                var result = (Dictionary<string, object>)jsSerializer.DeserializeObject(tradesHistory["result"].ToString());
                var trades = (Dictionary<string, object>)result["trades"];

                // Pack an array for Excel
                excelTrades = new object[trades.Count, 7];
                var i = 0;
                foreach (var trade in trades)
                {
                    excelTrades[i, 0] = ((Dictionary<string, object>)trade.Value)["pair"];
                    excelTrades[i, 1] = (new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToInt64(((Dictionary<string, object>)trade.Value)["time"]))).ToString("dd/MM/yyyy hh:mm:ss");
                    excelTrades[i, 2] = ((Dictionary<string, object>)trade.Value)["type"];
                    excelTrades[i, 3] = Convert.ToDecimal(((Dictionary<string, object>)trade.Value)["vol"]);
                    excelTrades[i, 4] = Convert.ToDecimal(((Dictionary<string, object>)trade.Value)["price"]);
                    excelTrades[i, 5] = Convert.ToDecimal(((Dictionary<string, object>)trade.Value)["cost"]);
                    excelTrades[i, 6] = Convert.ToDecimal(((Dictionary<string, object>)trade.Value)["fee"]);
                    i++;
                }
            });
            return excelTrades;
        }
        #endregion
    }
}