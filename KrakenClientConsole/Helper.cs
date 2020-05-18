using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using KrakenClient;
using NLog;

namespace KrakenClientConsole
{
    internal class Helper
    {
        private static NLog.Logger nLogger = LogManager.GetCurrentClassLogger();

        private static KrakenClient.KrakenClient client = new KrakenClient.KrakenClient();
        private static Broker broker = new Broker();

        private static decimal fxRateAsk = (decimal)1.079;
        private static decimal fxRateBid = (decimal)1.079;

        private static decimal commission = (decimal)0.26;


        private static decimal tresholdUsd = (decimal)-1.9;
        private static decimal tresholdEur = (decimal)5.5;
        private static decimal limitTrade = (decimal)252;

        private static int delayBalance = 6000;
        private static int delayCycle = 5000;
        private static int delayOrder = 1000;

        private static string cryptos = "XBT,ETH,LTC,XRP,XMR,ZEC,ETC,DASH,BCH";

        private static decimal usdAmount = 0;
        private static decimal eurAmount = 0;

        private static decimal balanceUsd = 0;
        private static decimal balanceEur = 0;

        private static decimal riskFactorUsd = 10;
        private static decimal riskFactorEur = 10;

        private static decimal krakenLimitAmount = 10;

        private static void LogAlert(string format, params object[] arg)
        {
            var message = string.Format(format, arg);
            LogInfo(message);
            if (Environment.MachineName.ToUpper() == "BOBROVYURY") { return; }
            else { Telegram.SendMessage(message); }
        }
        private static void LogInfo(string format, params object[] arg)
        {
            var message = string.Format(format, arg);
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + message);
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Info, "KrakenAPI", message);
            nLogger.Log(theEvent);
        }
        private static void LogDebug(string format, params object[] arg)
        {
            var message = string.Format(format, arg);
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + message);
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Debug, "KrakenAPI", message);
            nLogger.Log(theEvent);
        }
        private static void LogAudit(decimal maxEur, decimal maxUsd)
        {
            System.IO.File.AppendAllText(@"Spread.csv", string.Format("{0},{1:0.0000},{2:0.0000}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), maxEur, maxUsd));
        }

        private static void SetSettings()
        {
            tresholdUsd = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["Usd2Eur"]);
            tresholdEur = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["Eur2Usd"]);
            delayCycle = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["delayCycle"]);
            delayOrder = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["delayOrder"]);
            delayBalance = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["delayBalance"]);
            limitTrade = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["limitTrade"].ToString());
            cryptos = System.Configuration.ConfigurationManager.AppSettings["cryptos"].ToString();
            riskFactorUsd = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["riskFactorUsd"]);
            riskFactorEur = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["riskFactorEur"]);
            commission = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["commission"]);
            LogAlert("Commision: {0} Risk factors: USD = {1}, EUR = {2}", commission, riskFactorUsd, riskFactorEur);
        }
        private static bool SetBalances()
        {
            var balance = client.GetBalance();
            //LogDebug("Response: {0}", balance);
            var jsSerializer = new JavaScriptSerializer();
            var result = jsSerializer.DeserializeObject(balance["result"].ToString());
            var objBalances = (Dictionary<string, object>)result;
            balanceEur = Convert.ToDecimal(objBalances["ZEUR"]);
            balanceUsd = Convert.ToDecimal(objBalances["ZUSD"]);
            //fxRate = 

            var totalEur = GetBalanceAsset("ZEUR");

            if (totalEur < limitTrade)
            {
                LogInfo("!!!!!!!!!!!!!!!!! {0} below limit of {1}. Stop Loss !!!!!!!!!!!!!!!!!!!!", totalEur, limitTrade);
                Thread.Sleep(delayBalance);
                return false;
            }

            LogDebug("-------------------------------------------------------------------------------");
            LogInfo("Balances: USD = {0}, EUR = {1}, Total = {2}, Limit = {3}", balanceUsd, balanceEur, totalEur, limitTrade);
            return true;
        }

        public static void GetAssets()
        {
            var assets = client.GetActiveAssets();
            LogDebug("Response: {0}", assets);

            //var assetPairs = client.GetAssetPairs(new List<string> { "DASHEUR", "BCHEUR", "REPUSD" });
            //LogDebug("Response: {0}", assets);
        }

        public static decimal GetBalance(string pair)
        {
            string asset;
            decimal balanceAsset = (decimal)0.0;

            if (pair.StartsWith("X") || pair.StartsWith("DASH"))
            {
                asset = pair.Substring(0, 4);
            }
            else
            {
                asset = pair.Substring(0, 3);
            }

            Thread.Sleep(delayOrder);
            var balance = client.GetBalance();
            LogDebug("Response: {0}", balance);
            var jsSerializer = new JavaScriptSerializer();
            var result = jsSerializer.DeserializeObject(balance["result"].ToString());
            var objBalances = (Dictionary<string, object>)result;
            try
            {
                balanceAsset = Convert.ToDecimal(objBalances[asset]);
            }
            catch
            {
                LogDebug("Asset balance not found, default to 0.0: {0}", asset);
            }

            LogDebug("Balance {0}: {1}", asset, balanceAsset);
            return balanceAsset;
        }
        public static decimal GetBalanceFiat(string pair)
        {
            string asset;
            if (pair.StartsWith("X"))
            {
                asset = pair.Substring(4, 4);
            }
            else if (pair.StartsWith("DASH"))
            {
                asset = pair.Substring(4, 3);
            }
            else
            {
                asset = pair.Substring(3, 3);
            }

            Thread.Sleep(delayOrder);
            var balance = client.GetTradeBalance(null, asset);
            //LogDebug("Response: {0}", balance);
            var jsSerializer = new JavaScriptSerializer();
            var result = jsSerializer.DeserializeObject(balance["result"].ToString());
            var objBalances = (Dictionary<string, object>)result;
            var balanceAsset = Convert.ToDecimal(objBalances["tb"]);
            LogDebug("Balance {0}: {1}", asset, balanceAsset);
            return balanceAsset;
        }
        public static decimal GetBalanceAsset(string asset)
        {
            var balance = client.GetTradeBalance(null, asset);
            //LogDebug("Response: {0}", balance);
            var jsSerializer = new JavaScriptSerializer();
            var result = jsSerializer.DeserializeObject(balance["result"].ToString());
            var objBalances = (Dictionary<string, object>)result;
            var balanceAsset = Convert.ToDecimal(objBalances["eb"]);
            return balanceAsset;
        }

        public static void CheckPath()
        {
            SetSettings();
            ShowBalances();

            var listCrypto = new List<string>(cryptos.Split(','));
            var listPairs = new List<string>();
            foreach (var crypto in listCrypto)
            {
                var usdPair = string.Format("{0}USD", crypto);
                var eurPair = string.Format("{0}EUR", crypto);

                switch (crypto)
                {
                    case "XBT":
                    case "LTC":
                    case "XRP":
                    case "XLM":
                    case "ETH":
                    case "ETC":
                    case "ZEC":
                        usdPair = string.Format("X{0}ZUSD", crypto);
                        eurPair = string.Format("X{0}ZEUR", crypto);
                        break;
                }

                listPairs.Add(usdPair);
                listPairs.Add(eurPair);
            }

            listPairs.Add("ZEURZUSD");

            while (true)
            {
                var maxUsd = (decimal)-100.0;
                var maxEur = (decimal)-100.0;

                decimal volumeUsd = 0;
                decimal volumeEur = 0;

                var pairFoundUsd = "";
                var pairFoundEur = "";

                try
                {
                    SetBalances();

                    var ticker = client.GetTicker(listPairs);

                    var jsSerializer = new JavaScriptSerializer();
                    var result = jsSerializer.DeserializeObject(ticker["result"].ToString());
                    var obj2 = (Dictionary<string, object>)result;

                    var objFxRate = (Dictionary<string, object>)obj2["ZEURZUSD"];

                    fxRateAsk = Convert.ToDecimal(((object[])objFxRate["a"])[0].ToString());
                    fxRateBid = Convert.ToDecimal(((object[])objFxRate["b"])[0].ToString());

                    foreach (var crypto in listCrypto)
                    {
                        var usdPair = string.Format("{0}USD", crypto);
                        var eurPair = string.Format("{0}EUR", crypto);

                        switch (crypto)
                        {
                            case "XBT":
                            case "LTC":
                            case "XRP":
                            case "XLM":
                            case "ETH":
                            case "ETC":
                            case "ZEC":
                                usdPair = string.Format("X{0}ZUSD", crypto);
                                eurPair = string.Format("X{0}ZEUR", crypto);
                                break;
                        }

                        var obj3 = (Dictionary<string, object>)obj2[usdPair];

                        var usdAsk = Convert.ToDecimal(((object[])obj3["a"])[0].ToString());
                        var usdBid = Convert.ToDecimal(((object[])obj3["b"])[0].ToString());

                        obj3 = (Dictionary<string, object>)obj2[eurPair];

                        var eurAsk = Convert.ToDecimal(((object[])obj3["a"])[0].ToString());
                        var eurBid = Convert.ToDecimal(((object[])obj3["b"])[0].ToString());

                        var eur2Usd = Math.Round((usdBid / fxRateBid - eurAsk) * 100 / eurAsk, 2);
                        var usd2Eur = Math.Round((eurBid * fxRateAsk - usdAsk) * 100 / usdAsk, 2);

                        if (eur2Usd > maxEur)
                        {
                            eurAmount = balanceEur / riskFactorEur;
                            volumeEur = Math.Round(eurAmount / eurAsk, 10);
                            pairFoundEur = eurPair;
                            maxEur = eur2Usd;
                        }

                        if (usd2Eur > maxUsd)
                        {
                            usdAmount = balanceUsd / riskFactorUsd;
                            volumeUsd = Math.Round(usdAmount / usdAsk, 10);
                            pairFoundUsd = usdPair;
                            maxUsd = usd2Eur;
                        }

                        LogInfo("{0} usd {1:0.####}/{2:0.####}, eur: {3:0.####}/{4:0.####}, eur2usd = {5:0.##}, usd2eur = {6:0.##}",
                            crypto, usdAsk, usdBid, eurAsk, eurBid, eur2Usd, usd2Eur);
                    }

                    LogInfo("MaxEUR: {0}/{4} ({2}) MaxUSD: {1}/{5} ({3})", maxEur - commission, maxUsd - commission, pairFoundEur, pairFoundUsd, tresholdEur, tresholdUsd);
                    LogAudit(maxEur, maxUsd);

                    if (maxUsd - commission >= tresholdUsd)
                    {
                        LogAlert("Best USD path detected {0} at {1}%, balance USD = {2}, try volume = {3}",
                            pairFoundUsd, maxUsd, balanceUsd, volumeUsd);

                        if (usdAmount >= krakenLimitAmount)
                        {
                            LogInfo("Amount {0} sufficient, proceed with orders", usdAmount);
                            LogInfo("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            LogAlert("Started {0} USD={1}, EUR={2}", pairFoundUsd, balanceUsd, balanceEur);
                            if (CreateBuyOrder(pairFoundUsd, volumeUsd) > 0)
                            {
                                CreateSellOrder(pairFoundUsd.Replace("USD", "EUR"));
                                LogAlert("Completed {0}", pairFoundUsd);
                                SetBalances();
                                LogAlert("Balances: USD={0}, EUR={1}", balanceUsd, balanceEur);
                            }
                        }
                        else
                        {
                            LogInfo("Amount {0} insufficient, no orders", usdAmount);
                        }
                    }
                    if (maxEur - commission >= tresholdEur)
                    {
                        LogAlert("Best EUR path detected {0} at {1}%, balance EUR = {2}, try volume = {3}", pairFoundEur, maxEur, balanceEur, volumeEur);
                        if (eurAmount >= krakenLimitAmount)
                        {
                            LogInfo("Amount {0} sufficient, proceed with orders", eurAmount);
                            LogInfo("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            LogAlert("Started {0} USD={1}, EUR={2}", pairFoundEur, balanceUsd, balanceEur);
                            if (CreateBuyOrder(pairFoundEur, volumeEur) > 0)
                            {
                                CreateSellOrder(pairFoundEur.Replace("EUR", "USD"));
                                LogAlert("Completed {0}", pairFoundEur);
                                SetBalances();
                                LogAlert("Balances: USD={0}, EUR={1}", balanceUsd, balanceEur);
                            }
                        }
                        else
                        {
                            LogInfo("Amount {0} insufficient, no orders", eurAmount);
                        }
                    }
                    Thread.Sleep(delayCycle);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            }
        }
        public static void Range(string pair, decimal priceBuy, decimal priceSell)
        {
            var listPairs = new List<string>();
            listPairs.Add(pair);
            var isBuy = false;

            while (true)
            {
                try
                {
                    var volume = GetBalance(pair);
                    if (volume < (decimal)0.1)
                    {
                        LogInfo("No holding target to buy for {0}", priceBuy);
                        isBuy = true;
                    }
                    else
                    {
                        LogInfo("Holding {0} target to sell for {1}", volume, priceSell);
                        isBuy = false;
                    }

                    var ticker = client.GetTicker(listPairs);
                    var jsSerializer = new JavaScriptSerializer();
                    var result = jsSerializer.DeserializeObject(ticker["result"].ToString());
                    var obj2 = (Dictionary<string, object>)result;
                    var obj3 = (Dictionary<string, object>)obj2[pair];
                    var askPrice = Convert.ToDecimal(((object[])obj3["a"])[0].ToString());
                    var buyPrice = Convert.ToDecimal(((object[])obj3["b"])[0].ToString());

                    LogInfo("Range for {0} buy {1:0.000000} sell {2:0.000000} current {3:0.000000}/{4:0.000000}", pair, priceBuy, priceSell, askPrice, buyPrice);

                    if (isBuy)
                    {
                        if (buyPrice <= priceBuy)
                        {
                            var balance = Helper.GetBalanceFiat(pair) - (decimal)1;
                            var volumeToBuy = Math.Round(balance / buyPrice, 8);
                            LogInfo("Buy condition detected! Buy {0} volume {1}", pair, volumeToBuy);
                            CreateBuyOrder(pair, volumeToBuy);
                        }
                    }
                    else
                    {
                        if (askPrice >= priceSell)
                        {
                            LogInfo("Sell condition detected!");
                            CreateSellOrder(pair);
                        }
                    }
                    Thread.Sleep(delayCycle);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            }
        }
        public static void StopLoss(string pair, decimal price)
        {
            var listPairs = new List<string>();
            listPairs.Add(pair);
            while (true)
            {
                try
                {
                    var volume = GetBalance(pair);
                    if (volume < (decimal)0.001)
                    {
                        LogInfo("No holding for {0}, exiting.", pair);
                        return;
                    }

                    var ticker = client.GetTicker(listPairs);
                    var jsSerializer = new JavaScriptSerializer();
                    var result = jsSerializer.DeserializeObject(ticker["result"].ToString());
                    var obj2 = (Dictionary<string, object>)result;
                    var obj3 = (Dictionary<string, object>)obj2[pair];
                    var ask = Convert.ToDecimal(((object[])obj3["a"])[0].ToString());
                    LogInfo("Stop loss for {0} at ({1:0.000000}) current {2:0.000000}", pair, price, ask);

                    if (ask <= price)
                    {
                        LogInfo("Stop loss detected");
                        CreateSellOrder(pair);
                    }
                    Thread.Sleep(delayCycle);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            }
        }
        public static void CloseAllOrders()
        {
            Thread.Sleep(delayOrder);
            var openOrders = client.GetOpenOrders();
            LogInfo("Response: {0}", openOrders.ToString());

            var jsSerializer = new JavaScriptSerializer();
            var result = jsSerializer.DeserializeObject(openOrders["result"].ToString());
            var objOrder = (Dictionary<string, object>)result;
            var ordDetails = (Dictionary<string, object>)objOrder["open"];

            foreach (var key in ordDetails.Keys)
            {
                LogInfo("Cancel order {0}", key);
                var cancelOrder = client.CancelOrder(key);
                LogInfo("Response: {0}", cancelOrder);
            }
        }

        public static decimal CreateBuyOrder(string pair, decimal volume, decimal price)
        {
            string orderId = "";
            var tryAgain = true;
            decimal balanceAsset = (decimal)0.0;

            while (tryAgain)
                try
                {
                    LogInfo("Try to create BUY {0} volume {1} price {2}", pair, volume, price);

                    Thread.Sleep(delayOrder);
                    var addOrderRes = client.AddOrder(pair, "buy", "market", volume, price, null, @"none", "", "", "", "", "", false, null);
                    LogDebug("Response: {0}", addOrderRes.ToString());

                    //tryAgain = false;

                    var jsSerializer = new JavaScriptSerializer();
                    var resOrder = jsSerializer.DeserializeObject(addOrderRes["result"].ToString());
                    var objOrder = (Dictionary<string, object>)resOrder;
                    orderId = ((object[])objOrder["txid"])[0].ToString();
                    LogInfo("Order created {0}: {1}", orderId, addOrderRes);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            return balanceAsset;
        }
        public static decimal CreateBuyOrder(string pair, decimal volume)
        {
            string orderId = "";
            var tryAgain = true;
            decimal balanceAsset = (decimal)0.0;

            while (tryAgain)
                try
                {
                    balanceAsset = GetBalance(pair);
                    if (balanceAsset > volume / 2) return balanceAsset;

                    LogInfo("Try to create BUY order {0}: {1}", pair, volume);

                    Thread.Sleep(delayOrder);
                    var addOrderRes = client.AddOrder(pair, "buy", "market", volume, null, null, @"none", "", "", "", "", "", false, null);
                    LogDebug("Response: {0}", addOrderRes.ToString());

                    //tryAgain = false;

                    var jsSerializer = new JavaScriptSerializer();
                    var resOrder = jsSerializer.DeserializeObject(addOrderRes["result"].ToString());
                    var objOrder = (Dictionary<string, object>)resOrder;
                    orderId = ((object[])objOrder["txid"])[0].ToString();
                    LogInfo("Order created {0}: {1}", orderId, addOrderRes);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            return balanceAsset;
        }
        public static bool CreateSellOrder(string pair, decimal volume, decimal price)
        {
            var orderId = "";
            var tryAgain = true;

            while (tryAgain)
                try
                {
                    LogInfo("Try to create SELL {0} volume {1} price {2}", pair, volume, price);

                    Thread.Sleep(delayOrder);
                    var addOrderRes = client.AddOrder(pair, "sell", "limit", volume, price, null, @"none", "", "", "", "", "", false, null);
                    LogDebug("Response: {0}", addOrderRes.ToString());
                    //tryAgain = false;

                    var jsSerializer = new JavaScriptSerializer();
                    var resOrder = jsSerializer.DeserializeObject(addOrderRes["result"].ToString());
                    var objOrder = (Dictionary<string, object>)resOrder;
                    orderId = ((object[])objOrder["txid"])[0].ToString();
                    LogInfo("Order created {0}: {1}", orderId, addOrderRes);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            return false;
        }
        public static bool CreateSellOrder(string pair)
        {
            var orderId = "";
            var tryAgain = true;
            decimal volume = (decimal)0.0;

            while (tryAgain)
                try
                {
                    volume = GetBalance(pair);
                    if (volume < (decimal)0.001)
                    {
                        LogInfo("Not enough holdings for {0}", pair);
                        return true;
                    }

                    LogInfo("Try to create SELL order {0}: {1}", pair, volume);

                    Thread.Sleep(delayOrder);
                    var addOrderRes = client.AddOrder(pair, "sell", "market", volume, null, null, @"none", "", "", "", "", "", false, null);
                    LogDebug("Response: {0}", addOrderRes.ToString());
                    //tryAgain = false;

                    var jsSerializer = new JavaScriptSerializer();
                    var resOrder = jsSerializer.DeserializeObject(addOrderRes["result"].ToString());
                    var objOrder = (Dictionary<string, object>)resOrder;
                    orderId = ((object[])objOrder["txid"])[0].ToString();
                    LogInfo("Order created {0}: {1}", orderId, addOrderRes);
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message);
                }
            return false;
        }

        public static bool IsClosed(string orderId)
        {
            var tryAgain = true;
            while (tryAgain)
            {
                LogInfo("Check order: {0}", orderId);
                try
                {
                    Thread.Sleep(delayOrder);
                    var queryOrders = client.QueryOrders(orderId);
                    LogDebug("Response: {0}", queryOrders);
                    tryAgain = false;

                    var jsSerializer = new JavaScriptSerializer();
                    var resOrder = jsSerializer.DeserializeObject(queryOrders["result"].ToString());
                    var objOrder = (Dictionary<string, object>)resOrder;
                    var objDetails = (Dictionary<string, object>)objOrder[orderId];
                    var status = objDetails["status"].ToString();

                    if (status == "closed") return true;
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message + "\n\n");
                }
            }
            return false;
        }
        public static bool IsCancelled(string orderId)
        {
            var tryAgain = true;
            while (tryAgain)
            {
                LogInfo("Check order: {0}", orderId);
                try
                {
                    Thread.Sleep(delayOrder);
                    var queryOrders = client.QueryOrders(orderId);
                    LogDebug("Response: {0}", queryOrders);
                    tryAgain = false;

                    var jsSerializer = new JavaScriptSerializer();
                    var resOrder = jsSerializer.DeserializeObject(queryOrders["result"].ToString());
                    var objOrder = (Dictionary<string, object>)resOrder;
                    var objDetails = (Dictionary<string, object>)objOrder[orderId];
                    var status = objDetails["status"].ToString();

                    if (status == "cancelled") return true;
                }
                catch (Exception ex)
                {
                    LogInfo("Error: " + ex.Message + "\n\n");
                }
            }
            return false;
        }

        private static void ShowBalances()
        {
            var balance = client.GetBalance();
            LogDebug("Response: {0}", balance);
        }
        #region Form API

        public static void PlaceOrder(ref KrakenOrder order, bool wait)
        {
            try
            {
                LogInfo("Placing order...");

                var placeOrderResult = broker.PlaceOrder(ref order, wait);

                switch (placeOrderResult.ResultType)
                {
                    case PlaceOrderResultType.error:
                        LogInfo("An error occured while placing the order");
                        foreach (var item in placeOrderResult.Errors)
                            LogInfo(item);
                        break;
                    case PlaceOrderResultType.success:
                        LogInfo("Succesfully placed order {0}", order.TxId);
                        break;
                    case PlaceOrderResultType.partial:
                        LogInfo("Partially filled order {0}. {1} of {2}", order.TxId, order.VolumeExecuted,
                            order.Volume);
                        break;
                    case PlaceOrderResultType.txid_null:
                        LogInfo("Order was not placed. Unknown reason");
                        break;
                    case PlaceOrderResultType.canceled_not_partial:
                        LogInfo("The order was cancelled. Reason: {0}", order.Reason);
                        break;
                    case PlaceOrderResultType.exception:
                        LogInfo("Something went wrong. {0}", placeOrderResult.Exception.Message);
                        break;
                    default:
                        LogInfo("unknown PlaceOrderResultType {0}", placeOrderResult.ResultType);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogInfo("Something went wrong " + ex.Message);
                throw;
            }
        }

        public static void ClosePositionAndWaitForConfirmation(ref KrakenOrder openingOrder, decimal limitPrice)
        {
            try
            {
                LogInfo("Closing position...");

                var closingOrder = broker.CreateClosingOrder(openingOrder, limitPrice, false);

                var closePositionResult = broker.PlaceOrder(ref closingOrder, true);

                switch (closePositionResult.ResultType)
                {
                    case PlaceOrderResultType.error:
                        LogInfo("An error occured while placing the order");
                        foreach (var item in closePositionResult.Errors)
                            LogInfo(item);
                        break;
                    case PlaceOrderResultType.success:
                        LogInfo("Succesfully placed order {0}", closingOrder.TxId);
                        break;
                    case PlaceOrderResultType.partial:
                        LogInfo("Partially filled order {0}. {1} of {2}", closingOrder.TxId,
                            closingOrder.VolumeExecuted, closingOrder.Volume);
                        break;
                    case PlaceOrderResultType.txid_null:
                        LogInfo("Order was not placed. Unknown reason");
                        break;
                    case PlaceOrderResultType.canceled_not_partial:
                        LogInfo("The order was canceled. Reason: {0}", closingOrder.Reason);
                        break;
                    case PlaceOrderResultType.exception:
                        LogInfo("Something went wrong. {0}", closingOrder.Reason);
                        break;
                    default:
                        LogInfo("unknown PlaceOrderResultType {0}", closePositionResult.ResultType);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogInfo("Something went wrong. {0}", ex.Message);
                throw;
            }
        }

        public static void CancelOrder(ref KrakenOrder order)
        {
            try
            {
                LogInfo("Cancelling order...");

                var cancelOrderResult = broker.CancelOrder(ref order);

                switch (cancelOrderResult.ResultType)
                {
                    case CancelOrderResultType.error:
                        LogInfo("An error occured while cancelling the order");
                        foreach (var item in cancelOrderResult.Errors)
                            LogInfo(item);
                        break;
                    case CancelOrderResultType.success:
                        LogInfo("Succesfully cancelled order {0}", order.TxId);
                        break;
                    case CancelOrderResultType.exception:
                        LogInfo("Something went wrong. {0}", order.Reason);
                        break;
                    default:
                        LogInfo("unknown CancelOrderResultType {0}", cancelOrderResult.ResultType);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogInfo("Something went wrong " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Simple requests

        //var time = client.GetServerTime();
        //LogInfo("time: " + time.ToString() + "\n\n");

        //var assets = client.GetActiveAssets();
        //LogInfo("assets: " + assets.ToString() + "\n\n");

        //var assetPairs = client.GetAssetPairs(new List<string> { "DASHEUR", "BCHEUR", "REPUSD" });
        //LogInfo("asset pairs: " + assetPairs.ToString() + "\n\n");

        //var ticker = client.GetTicker("XXBTZUSD");
        //LogInfo("ticker: " + ticker.ToString() + "\n\n");

        //var depth = client.GetOrderBook("XXBTZUSD", 1);
        //LogInfo("depth: " + depth.ToString() + "\n\n");

        //var trades = client.GetRecentTrades("XXBTZEUR", 137589964200000000);
        //LogInfo("trades: " + trades.ToString() + "\n\n");

        //var spreads = client.GetRecentSpreadData("XXBTZEUR", 137589964200000000);
        //LogInfo("spreads: " + spreads.ToString() + "\n\n");

        //var balance = client.GetBalance();
        //LogInfo("balance: " + balance.ToString() + "\n\n");

        //var tradeBalance = client.GetTradeBalance("currency", string.Empty);
        //LogInfo("trade balance: " + tradeBalance.ToString() + "\n\n");

        //var openOrders = client.GetOpenOrders();
        //LogInfo("open orders: " + openOrders.ToString() + "\n\n");

        //var closedOrders = client.GetClosedOrders();
        //LogInfo("closed orders: " + closedOrders.ToString() + "\n\n");

        //var queryOrders = client.QueryOrders(string.Empty);
        //LogInfo("query orders: " + queryOrders + "\n\n");

        //var tradesHistory = client.GetTradesHistory(string.Empty);
        //LogInfo("trades history: " + tradesHistory.ToString() + "\n\n");

        //var queryTrades = client.QueryTrades();
        //LogInfo("query trades: " + queryTrades.ToString() + "\n\n");

        //var openPositions = client.GetOpenPositions();
        //LogInfo("open positions: " + openPositions.ToString() + "\n\n");

        //var ledgers = client.GetLedgers();
        //LogInfo("ledgers: " + ledgers.ToString() + "\n\n");

        //var queryLedgers = client.QueryLedgers();
        //LogInfo("query ledgers: " + queryLedgers.ToString() + "\n\n");

        //var tradeVolume = client.GetTradeVolume();
        //LogInfo("trade volume: " + tradeVolume.ToString() + "\n\n");

        #endregion

        #region Simple trading requests

        //var closeDictionary = new Dictionary<string, string>();
        //closeDictionary.Add("ordertype", "stop-loss-profit");
        //closeDictionary.Add("price", "#5%");
        //closeDictionary.Add("price2", "#10%");

        //var addOrderRes = client.AddOrder("XXBTZUSD",
        //    "buy",
        //    "limit",
        //    (decimal)0.01,
        //    (decimal)10000.0000,
        //    null,
        //    @"none",
        //    "",
        //    "",
        //    "",
        //    "",
        //    "",
        //    false,
        //    null);
        ////closeDictionary);

        //LogInfo("add order result: " + addOrderRes.ToString());

        //var cancelOrder = client.CancelOrder("OKLQWN-WT2VA-VI6ZAG");
        //LogInfo("cancel order : " + cancelOrder.ToString());

        #endregion

        #region Using the broker helper

        //KrakenOrder openingOrder = broker.CreateOpeningOrder2(OrderType.buy, KrakenOrderType.stop_loss, 420.1M, 10,415M,viqc:true,validateOnly: false);

        //PlaceOrder(ref openingOrder, true);

        //CancelOrder(ref openingOrder);

        //Stopwatch stopwatch = new Stopwatch();
        //KrakenOrder order = new KrakenOrder();
        //order.TxId = "OYNRKT-RQB5J-OM4DQU";
        //for (int i = 1; i <= 10; i++)
        //{

        //    stopwatch.Start();
        //    var res = broker.RefreshOrder(ref order);
        //    stopwatch.Stop();
        //    LogInfo(stopwatch.Elapsed.ToString());
        //    stopwatch.Start();
        //}

        #endregion
    }
}