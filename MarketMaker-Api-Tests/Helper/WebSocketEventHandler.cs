using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.Helper
{
    public class WebSocketEventHandler
    {
        public WebSocketEventHandler()
        {
            Algorithms = new List<AlgorithmInfo>();
        }

        public List<AlgorithmInfo> Algorithms { get; set; }

        public void CompareQuoteAgainstParams(AlgorithmInfo algo)
        {
            double originalSellQty = algo.PricerConfigInfo.SellQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double originalBuyQty = algo.PricerConfigInfo.BuyQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double sellQty = 0.0, buyQty = 0.0;
            double minSellPrice = 0.0, maxBuyPrice = 0.0, spread = 0.0;

            int originalSellLevels = algo.PricerConfigInfo.SellQuoteSizes.Split(' ').Length;
            int originalBuyLevels = algo.PricerConfigInfo.BuyQuoteSizes.Split(' ').Length;

            int sellLevels = 0, buyLevels = 0;
            foreach (var ens in algo.AlgoDictionary[AlgorithmInfo.BookType.QUOTE].Entries)
            {
                if (ens.Side == Side.SELL)
                {
                    sellQty += ens.Quantity;
                    sellLevels++;
                    if (ens.Level == 0)
                        minSellPrice = ens.Price;
                }
                if (ens.Side == Side.BUY)
                {
                    buyQty += ens.Quantity;
                    buyLevels++;
                    if (ens.Level == 0)
                        maxBuyPrice = ens.Price;
                }
            }

            spread = minSellPrice - maxBuyPrice;

            Debug.WriteLine("Sell levels: {0}", sellLevels);
            if (Util.CompareDouble(sellQty, originalSellQty))
                Debug.WriteLine("OK! Sell Qty: {0}; Original: {1}", sellQty, originalSellQty);
            else
                Debug.WriteLine("Error! Sell Qty: {0}; Original: {1}", sellQty, originalSellQty);

            Debug.WriteLine("Buy levels: {0}", buyLevels);
            if (Util.CompareDouble(buyQty, originalBuyQty))
                Debug.WriteLine("OK! Buy Qty: {0}; Original: {1}", buyQty, originalBuyQty);
            else
                Debug.WriteLine("Error! Buy Qty: {0}; Original: {1}", buyQty, originalBuyQty);
            Debug.WriteLine("Spread: {0}; MinSpread: {1}", spread, algo.PricerConfigInfo.MinSpread);

            Assert.AreEqual(true, Util.CompareDouble(sellQty, originalSellQty), "Sell qty is not equal to original qty");
            Assert.AreEqual(true, Util.CompareDouble(buyQty, originalBuyQty), "Buy qty is not equal to original qty");
            Assert.AreEqual(originalSellLevels, sellLevels, "Sell levels are not equal to original levels");
            Assert.AreEqual(originalBuyLevels, buyLevels, "Buy levels are not equal to original levels");
            Assert.AreEqual(true, spread - algo.PricerConfigInfo.MinSpread >= 0, "Spread are not equal to original levels");
        }

        public void CompareSourceAgainstTargetBook(AlgorithmInfo algo)
        {
            if (algo.AlgoDictionary.ContainsKey(AlgorithmInfo.BookType.SOURCE) &&
                this.Algorithms[0].AlgoDictionary.ContainsKey(AlgorithmInfo.BookType.TARGET))
                Assert.AreEqual(true,
                                CompareBooks(algo.AlgoDictionary[AlgorithmInfo.BookType.SOURCE],
                                             this.Algorithms[0].AlgoDictionary[AlgorithmInfo.BookType.TARGET]),
                                "Test is failed");
            else
                Console.WriteLine("Book is NULL");
        }

        public void CompareStatisticAgainstTargetBook(AlgorithmInfo algo)
        {
            if (!algo.AlgoDictionary.ContainsKey(AlgorithmInfo.BookType.TARGET) || algo.TradeStatistic == null)
            {
                Console.WriteLine("Data is NULL");
                return;
            }

            double sellQty = 0.0, buyQty = 0.0;
            foreach (var ens in algo.AlgoDictionary[AlgorithmInfo.BookType.TARGET].Entries)
            {
                if (ens.Side == Side.SELL)
                {
                    sellQty += ens.Quantity;
                }
                if (ens.Side == Side.BUY)
                {
                    buyQty += ens.Quantity;
                }
            }
            Debug.WriteLine("Trade Statistic Sell Qty: {0}, Target book: {1}", algo.TradeStatistic.OpenSellQty, sellQty);
            Debug.WriteLine("Trade Statistic Buy Qty: {0}, Target book: {1}", algo.TradeStatistic.OpenBuyQty, buyQty);
            Assert.AreEqual(algo.TradeStatistic.OpenBuyQty, buyQty,
                            "Buy Qty doesn't matched. OpenSellQty: {0}, Sell Qty from book: {1}", algo.TradeStatistic.OpenBuyQty, buyQty);
            Assert.AreEqual(algo.TradeStatistic.OpenSellQty, sellQty,
                            "Sell Qty doesn't matched. OpenBuyQty: {0}, Buy Qty from book: {1}", algo.TradeStatistic.OpenSellQty, sellQty);
        }

        public bool CompareBooks(L2PackageDto sourceBook, L2PackageDto targetBook)
        {
            bool result = true;
            Debug.WriteLine("Number Source: {0}, Target: {1}", sourceBook.SequenceNumber, targetBook.SequenceNumber);
            Debug.WriteLine("Count of levels. Source {0}, Target: {1}", sourceBook.Entries.Count, targetBook.Entries.Count);
            if (sourceBook.Entries.Count != targetBook.Entries.Count)
            {
                Console.WriteLine("Count of levels is not equal. Source {0}, Quotes: {1}", sourceBook.Entries.Count, targetBook.Entries.Count);
                return false;
            }

            foreach (var level in sourceBook.Entries)
            {
                var targetLevel = targetBook.Entries.FirstOrDefault(l => l.Side == level.Side && l.Level == level.Level);
                if (targetLevel != null)
                {
                    Debug.WriteLine("Side {3} Level {0}. Source {1}; Target: {2}", level.Level, level.Quantity, targetLevel.Quantity, targetLevel.Side);
                    bool temp = Util.CompareDouble(targetLevel.Quantity, level.Quantity);
                    if (!temp)
                        Console.WriteLine("Qty is not the same at level {0} for {1}. Source {2}; Target: {3}", level.Level, level.Side, level.Quantity, targetLevel.Quantity);
                    result = temp && result;
                }
                else
                {
                    Console.WriteLine("Level {0} doesn't exist for {1}", level.Level, level.Side);
                    return false;
                }
            }
            return result;
        }
    }
}
