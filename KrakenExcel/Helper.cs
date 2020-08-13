using Jayrock.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KrakenExcel
{
    public class Helper
    {
        private static KrakenClient.KrakenClient client = new KrakenClient.KrakenClient();

        #region For Excel
        public static string CreateSellOrder(string pair, decimal volume, decimal price)
        {
            var addOrderRes = client.AddOrder(pair, "sell", "limit", volume, price, null, @"none", "", "", "", "", "", false, null);
            var objError = (JsonArray)addOrderRes["error"];
            if (objError.Count > 0) return objError[0].ToString();
            var objOrder = (JsonObject)addOrderRes["result"];
            return ((JsonArray)objOrder["txid"])[0].ToString();
        }
        public static string CreateSellOrderMarket(string pair, decimal volume)
        {
            var addOrderRes = client.AddOrder(pair, "sell", "market", volume, null, null, @"none", "", "", "", "", "", false, null);
            var objError = (JsonArray)addOrderRes["error"];
            if (objError.Count > 0) return objError[0].ToString();
            var objOrder = (JsonObject)addOrderRes["result"];
            return ((JsonArray)objOrder["txid"])[0].ToString();
        }
        public static string CreateBuyOrder(string pair, decimal volume, decimal price)
        {
            var addOrderRes = client.AddOrder(pair, "buy", "limit", volume, price, null, @"none", "", "", "", "", "", false, null);
            var objError = (JsonArray)addOrderRes["error"];
            if (objError.Count > 0) return objError[0].ToString();
            var objOrder = (JsonObject)addOrderRes["result"];
            return ((JsonArray)objOrder["txid"])[0].ToString();
        }
        public static string CloseOrder(string key)
        {
            var cancelOrder = client.CancelOrder(key);
            var objError = (JsonArray)cancelOrder["error"];
            if (objError.Count > 0) return objError[0].ToString();
            return "Done";
        }

        public async static Task<decimal> GetBalance(string asset)
        {
            decimal balanceAsset = 0;
            await Task.Run(() =>
            {
                var response = client.GetBalance();
                JsonObject balances = (JsonObject)response["result"];
                balanceAsset = Convert.ToDecimal(balances[asset]);
            });
            return balanceAsset;
        }
        public async static Task<object[,]> GetBalances()
        {
            object[,] excelBalances = null;
            await Task.Run(() =>
            {
                var response = client.GetBalance();
                JsonObject balances = (JsonObject)response["result"];
                // Pack an array for Excel
                excelBalances = new object[balances.Count, 2];
                var i = 0;
                foreach (var balance in balances)
                {
                    excelBalances[i, 0] = balance.Name;
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
                var response = client.GetTradeBalance(null, asset);
                JsonObject balances = (JsonObject)response["result"];
                balanceAsset = Convert.ToDecimal(balances[value]);
            });
            return balanceAsset;
        }
        public async static Task<decimal> GetTicker(string pair, string value)
        {
            decimal price = 0;
            await Task.Run(() =>
            {
                var response = client.GetTicker(new List<string> { pair });
                var prices = (JsonObject)response["result"];
                price = Convert.ToDecimal(((object[])((JsonObject)prices[pair])[value])[0].ToString());
            });
            return price;
        }
        public async static Task<object[,]> GetPrices(object[] pair)
        {
            object[,] excelPrices = null;
            await Task.Run(() =>
            {
                var response = client.GetTicker(pair.OfType<string>().ToList());
                var prices = (JsonObject)response["result"];
                // Pack an array for Excel
                excelPrices = new object[prices.Count, 3];
                var i = 0;
                foreach (var price in prices)
                {
                    excelPrices[i, 0] = price.Name;
                    var values = (JsonObject)price.Value;
                    excelPrices[i, 1] = Convert.ToDecimal(((JsonArray)values["a"])[0]);
                    excelPrices[i, 2] = Convert.ToDecimal(((JsonArray)values["b"])[0]);
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
                var response = client.GetTradesHistory("", "all", false, start.ToString(), end.ToString());
                var result = (JsonObject)response["result"];
                var trades = (JsonObject)result["trades"];

                // Pack an array for Excel
                excelTrades = new object[trades.Count, 7];
                var i = 0;
                foreach (var trade in trades)
                {
                    excelTrades[i, 0] = ((JsonObject)trade.Value)["pair"];
                    excelTrades[i, 1] = (new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((int)Convert.ToDecimal(((JsonObject)trade.Value)["time"]))).ToString("dd/MM/yyyy hh:mm:ss");
                    excelTrades[i, 2] = ((JsonObject)trade.Value)["type"];
                    excelTrades[i, 3] = Convert.ToDecimal(((JsonObject)trade.Value)["vol"]);
                    excelTrades[i, 4] = Convert.ToDecimal(((JsonObject)trade.Value)["price"]);
                    excelTrades[i, 5] = Convert.ToDecimal(((JsonObject)trade.Value)["cost"]);
                    excelTrades[i, 6] = Convert.ToDecimal(((JsonObject)trade.Value)["fee"]);
                    i++;
                }
            });
            return excelTrades;
        }

        public async static Task<object[,]> GetOpenOrders()
        {
            object[,] excelOrders = null;
            await Task.Run(() =>
            {
                var response = client.GetOpenOrders();
                var result = (JsonObject)response["result"];
                var orders = (JsonObject)result["open"];
                // Pack an array for Excel
                excelOrders = new object[orders.Count, 6];
                var i = 0;
                foreach (var order in orders)
                {
                    var descr = (JsonObject)((JsonObject)order.Value)["descr"];
                    excelOrders[i, 0] = order.Name;
                    excelOrders[i, 1] = descr["pair"];
                    excelOrders[i, 2] = descr["type"];
                    excelOrders[i, 3] = Convert.ToDecimal(((JsonObject)order.Value)["vol"]);
                    excelOrders[i, 4] = Convert.ToDecimal(descr["price"]);
                    excelOrders[i, 5] = descr["order"];
                    i++;
                }
            });
            return excelOrders;
        }
        #endregion
    }
}