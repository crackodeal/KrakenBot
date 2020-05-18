using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Script.Serialization;
using Jayrock.Json;
using KrakenClient;

namespace KrakenClientConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Helper.CheckPath();
            }
            else if (args[0].ToString().ToLower() == "sell")
            {
                Console.WriteLine("Sell {0} volume {1} price {2}. Press any key to proceed.", args[1], args[2], args[3]);
                Console.ReadKey();
                Helper.CreateSellOrder(args[1], Convert.ToDecimal(args[2]), Convert.ToDecimal(args[3]));
                Console.ReadKey();
            }
            else if (args[0].ToString().ToLower() == "buy")
            {
                Console.WriteLine("Buy {0} volume {1} price {2}. Press any key to proceed.", args[1], args[2], args[3]);
                Console.ReadKey();
                Helper.CreateBuyOrder(args[1], Convert.ToDecimal(args[2]), Convert.ToDecimal(args[3]));
                Console.ReadKey();
            }
            else if (args[0].ToString().ToLower() == "sellm")
            {
                Console.WriteLine("Sell {0} at market price. Press any key to proceed.", args[1]);
                Console.ReadKey();
                Helper.CreateSellOrder(args[1]);
            }
            else if (args[0].ToString().ToLower() == "stop")
            {
                Console.WriteLine("Stop loss {0} at price {1}. Press any key to proceed.", args[1], args[2]);
                Console.ReadKey();
                Helper.StopLoss(args[1], Convert.ToDecimal(args[2]));
                Console.ReadKey();
            }
            else if (args[0].ToString().ToLower() == "range")
            {
                Console.WriteLine("Range {0} buy {1} sell {2}. Press any key to proceed.", args[1], args[2], args[3]);
                Console.ReadKey();
                Helper.Range(args[1], Convert.ToDecimal(args[2]), Convert.ToDecimal(args[3]));
                Console.ReadKey();
            }
            else if (args[0].ToString().ToLower() == "cancell")
            {
                bool repeat = true;

                while (repeat)
                {
                    try
                    {
                        Helper.CloseAllOrders();
                        repeat = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
            }
        }
    }
}