using ExcelDna.Integration;
using ExcelDna.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KrakenExcel
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            ExcelRegistration.GetExcelFunctions()
                           .ProcessAsyncRegistrations(nativeAsyncIfAvailable: false)
                           .RegisterFunctions();
        }
        public void AutoClose()
        {
        }

    }
    public static class Functions
    {
        [ExcelFunction(Name = "Kraken.GetBalance", Description = "Kraken API get asset balance")]
        public async static Task<decimal> GetBalance(string name)
        {
            return await Helper.GetBalance(name);
        }
        [ExcelFunction(Name = "Kraken.GetTradeBalance", Description = "Kraken API get trade balance")]
        public async static Task<decimal> GetTradeBalance(string name)
        {
            return await Helper.GetTradeBalance(name, "td");
        }
        [ExcelFunction(Name = "Kraken.GetEquityBalance", Description = "Kraken API get equity balance")]
        public async static Task<decimal> GetEquityBalance(string name)
        {
            return await Helper.GetTradeBalance(name, "eb");
        }

        [ExcelFunction(Name = "Kraken.GetTickerAsk", Description = "Kraken API get ticker ask price", ExplicitRegistration = true)]
        public static async Task<decimal> GetTickerAsk([ExcelArgument(Name = "Asset")] string name)
        {
            return await Helper.GetTicker(name.ToString(), "a");
        }
        [ExcelFunction(Name = "Kraken.GetTickerBid", Description = "Kraken API get ticker bid price")]
        public static async Task<decimal> GetTickerBid(string name)
        {
            return await Helper.GetTicker(name, "b");
        }
        [ExcelFunction(Name = "Kraken.GetTradesHistory", Description = "Kraken API get trades for dates")]
        public static async Task<object[,]> GetTradesHistory(DateTime fromDate, DateTime toDate)
        {
            return await Helper.GetTradesHistory(fromDate, toDate);
        }
        [ExcelFunction(Name = "Kraken.GetBalances", Description = "Kraken API get balances")]
        public static async Task<object[,]> GetBalances()
        {
            return await Helper.GetBalances();
        }
        [ExcelFunction(Name = "Kraken.GetPrices", Description = "Kraken API get prices")]
        public static async Task<object[,]> GetPrices(object[] range)
        {
            return await Helper.GetPrices(range);
        }
    }
}
