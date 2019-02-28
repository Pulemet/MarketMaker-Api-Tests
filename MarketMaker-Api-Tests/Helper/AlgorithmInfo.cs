using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Config;
using MarketMaker.Api.Models.Statistics;
using MarketMaker.Api.Rest;
using MarketMaker_Api_Tests.CryptoCortex.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.Helper
{
    public enum BookType
    {
        TARGET,
        QUOTE,
        SOURCE,
        HEDGE,
    }

    public class AlgorithmInfo
    {
        public AlgorithmInfo()
        {
            AlgoId = -1;
            AlgoDictionary = new Dictionary<BookType, L2PackageDto>();
            IsDelete = false;
            InitTradeStatistic = null;
            Executions = new List<ExecutionDto>();
            Orders = new List<OrderDto>();
            Alerts = new List<TradingAlertDto>();
        }

        public AlgorithmInfo(string fileInstrument, string filePricer, string fileHeadger, string fileRiskLimit) : this()
        {
            InstrumentConfigInfo = JsonConvert.DeserializeObject<InstrumentConfigDto>(Util.ReadFile(fileInstrument));
            PricerConfigInfo = JsonConvert.DeserializeObject<PricerConfigDto>(Util.ReadFile(filePricer));
            HedgerConfigInfo = JsonConvert.DeserializeObject<HedgerConfigDto>(Util.ReadFile(fileHeadger));
            RiskLimitConfigInfo = JsonConvert.DeserializeObject<RiskLimitsConfigDto>(Util.ReadFile(fileRiskLimit));
            IsDelete = true;
        }

        public AlgorithmInfo(FullInstrumentConfigDto instrument) : this()
        {
            AlgoId = instrument.InstrumentConfig.AlgoId != null ? (long)instrument.InstrumentConfig.AlgoId : -1;
            InstrumentConfigInfo = instrument.InstrumentConfig;
            PricerConfigInfo = instrument.PricerConfig;
            HedgerConfigInfo = instrument.HedgerConfig;
            RiskLimitConfigInfo = instrument.RiskLimitsConfig;
        }

        private bool IsDelete { get; set; }
        public double ChangePositionSize { get; set; }
        public OrderCrypto OrderToSend { get; set; }
        public long AlgoId { get; set; }
        public InstrumentConfigDto InstrumentConfigInfo { get; set; }
        public PricerConfigDto PricerConfigInfo { get; set; }
        public HedgerConfigDto HedgerConfigInfo { get; set; }
        public RiskLimitsConfigDto RiskLimitConfigInfo { get; set; }

        public Dictionary<BookType, L2PackageDto> AlgoDictionary { get; set; }
        public AlgoInstrumentStatisticsDto InitTradeStatistic { get; set; }
        public AlgoInstrumentStatisticsDto TradeStatistic { get; set; }
        public List<ExecutionDto> Executions { get; set; }
        public List<OrderDto> Orders { get; set; }
        public List<TradingAlertDto> Alerts { get; set; }
        public double OrdersBuyQty { get; set; }
        public double OrdersSellQty { get; set; }
        public bool IsChangedOrders { get; set; }

        public static T CreateConfig<T>(string fileName, long id)
        {
            var config = JsonConvert.DeserializeObject<T>(Util.ReadFile(fileName));
            var type = typeof(T);
            var property = type.GetProperty("AlgoId");
            property?.SetValue(config, id);
            return config;
        }

        public void SetAlgoId(long? id)
        {
            long algoId = id != null ? (long)id : 0;
            AlgoId = algoId;
            InstrumentConfigInfo.AlgoId = algoId;
            PricerConfigInfo.AlgoId = algoId;
            HedgerConfigInfo.AlgoId = algoId;
            RiskLimitConfigInfo.AlgoId = algoId;
        }

        public void StopDeleteAll(IMarketMakerRestService service)
        {
            if(AlgoId == -1 || !IsDelete)
                return;
            if (HedgerConfigInfo.Running)
            {
                service.StopHedger(AlgoId);
                Thread.Sleep(500);
            }
            if (PricerConfigInfo.Running)
            {
                service.StopPricer(AlgoId);
                Thread.Sleep(500);
            }
            if (InstrumentConfigInfo.Running)
            {
                service.StopInstrument(AlgoId);
                Thread.Sleep(500);
            }
            service.DeleteAlgorithm(AlgoId);
        }

        public bool Equals(FullInstrumentConfigDto allConfiguration)
        {
            return EqualInstrument(allConfiguration.InstrumentConfig, true) &&
                   EqualPricer(allConfiguration.PricerConfig, true) &&
                   EqualHedger(allConfiguration.HedgerConfig, true) &&
                   EqualRiskLimit(allConfiguration.RiskLimitsConfig, true);
        }

        public bool EqualInstrument(InstrumentConfigDto instrumentConfig, bool printMessage)
        {
            bool isEqual = InstrumentConfigInfo.AlgoId == instrumentConfig.AlgoId &&
                   InstrumentConfigInfo.AlgoKey == instrumentConfig.AlgoKey &&
                   InstrumentConfigInfo.Exchange == instrumentConfig.Exchange &&
                   InstrumentConfigInfo.SourceExchange == instrumentConfig.SourceExchange &&
                   InstrumentConfigInfo.Instrument == instrumentConfig.Instrument &&
                   InstrumentConfigInfo.FxLeg == instrumentConfig.FxLeg &&
                   InstrumentConfigInfo.Underlyings == instrumentConfig.Underlyings;
            if(!isEqual && printMessage)
                Console.WriteLine("Instrument doesn't match with added.");
            return isEqual;
        }

        public bool EqualPricer(PricerConfigDto pricerConfig, bool printMessage)
        {
            bool isEqual = PricerConfigInfo.AlgoId == pricerConfig.AlgoId &&
                   PricerConfigInfo.AlgoKey == pricerConfig.AlgoKey &&
                   PricerConfigInfo.SellQuoteSizes == pricerConfig.SellQuoteSizes &&
                   PricerConfigInfo.BuyQuoteSizes == pricerConfig.BuyQuoteSizes &&
                   PricerConfigInfo.BuyMargins == pricerConfig.BuyMargins &&
                   PricerConfigInfo.SellMargins == pricerConfig.SellMargins &&
                   PricerConfigInfo.AggregationMethod == pricerConfig.AggregationMethod &&
                   Util.CompareDouble(PricerConfigInfo.MinPriceChange, pricerConfig.MinPriceChange);
            if (!isEqual && printMessage)
                Console.WriteLine("Pricer doesn't match with added.");
            return isEqual;
        }

        public bool EqualHedger(HedgerConfigDto hedgerConfig, bool printMessage)
        {
            bool isEqual = HedgerConfigInfo.AlgoId == hedgerConfig.AlgoId &&
                   HedgerConfigInfo.AlgoKey == hedgerConfig.AlgoKey &&
                   HedgerConfigInfo.HedgeInstrument == hedgerConfig.HedgeInstrument &&
                   HedgerConfigInfo.HedgeStrategy == hedgerConfig.HedgeStrategy &&
                   HedgerConfigInfo.ExecutionStyle == hedgerConfig.ExecutionStyle &&
                   HedgerConfigInfo.PositionMaxNormSize == hedgerConfig.PositionMaxNormSize &&
                   HedgerConfigInfo.VenuesList == hedgerConfig.VenuesList &&
                   Util.CompareDouble(HedgerConfigInfo.MaxOrderSize, hedgerConfig.MaxOrderSize);
            if (!isEqual && printMessage)
                Console.WriteLine("Hedger doesn't match with added.");
            return isEqual;
        }

        public bool EqualRiskLimit(RiskLimitsConfigDto riskLimitConfig, bool printMessage)
        {
            bool isEqual = RiskLimitConfigInfo.AlgoId == riskLimitConfig.AlgoId &&
                   RiskLimitConfigInfo.AlgoKey == riskLimitConfig.AlgoKey &&
                   RiskLimitConfigInfo.MinBuyQuoteActiveTime == riskLimitConfig.MinBuyQuoteActiveTime &&
                   RiskLimitConfigInfo.MinSellQuoteActiveTime == riskLimitConfig.MinSellQuoteActiveTime &&
                   Util.CompareDouble(RiskLimitConfigInfo.MaxLongExposure, riskLimitConfig.MaxLongExposure) &&
                   Util.CompareDouble(RiskLimitConfigInfo.MaxShortExposure, riskLimitConfig.MaxShortExposure);
            if (!isEqual && printMessage)
                Console.WriteLine("Risk Limits don't match with added.");
            return isEqual;
        }

        public static double GetQuoteSize(string quote)
        {
            if (quote.Contains("["))
            {
                int index = quote.IndexOf("[");
                return Double.Parse(quote.Substring(0, index).Trim(' '));
            }

            return quote.Split(' ').Sum(x => Double.Parse(x));
        }

        public static double GetSpreadFromParams(List<L2EntryDto> book, string paramSpread)
        {
            string spread = paramSpread.Trim(' ');
            int index = spread.IndexOf("bps", StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                if(book != null)
                    return Math.Round(CalculateBps(book) * Double.Parse(spread.Substring(0, index)), Util.OrderPricePrecision);
                return 0;
            }
            return Math.Round(Double.Parse(spread), Util.OrderPricePrecision);
        }

        public static double CalculateBps(List<L2EntryDto> book)
        {
            return CalculateMidPrice(book) * Util.Bps;
        }

        public static double CalculateMidPrice(List<L2EntryDto> book)
        {
            double summary = 0;
            double count = 0;
            foreach (var entry in book)
            {
                summary += entry.Quantity * entry.Price;
                count += entry.Quantity;
            }
            return Math.Round(summary / count, Util.OrderPricePrecision);
        }

        public static double CalculateSpread(List<L2EntryDto> book)
        {
            double minSellPrice = 0, maxBuyPrice = 0;
            foreach (var entry in book)
            {
                if (entry.Side == Side.SELL)
                {
                    if (entry.Level == 0)
                        minSellPrice = entry.Price;
                }
                if (entry.Side == Side.BUY)
                {
                    if (entry.Level == 0)
                        maxBuyPrice = entry.Price;
                }
            }
            Debug.WriteLine("Min Sell Price: {0}, Max Buy Price: {1}", minSellPrice, maxBuyPrice);
            return Math.Round(minSellPrice - maxBuyPrice, Util.OrderPricePrecision);
        }

        public static double GetExecutionSizeFromAlert(string description)
        {
            string textBuy = "BUY", textSell = "SELL";
            int textLength = description.Contains(textSell) ? textSell.Length : textBuy.Length;
            description = description.Replace(" ", String.Empty);
            int indexTextSide = 0, indexStartSize = 0, lenghtTextSize = 0;
            int indexAtSymbol = description.IndexOf("@");

            indexTextSide = description.Contains(textSell) ? description.IndexOf(textSell) : description.IndexOf(textBuy);

            if (indexTextSide > 0 && indexAtSymbol > 0)
            {
                indexStartSize = indexTextSide + textLength;
                lenghtTextSize = indexAtSymbol - indexTextSide - textLength;
                return Double.Parse(description.Substring(indexStartSize, lenghtTextSize));
            }

            return 0;
        }

        private void ReplaceOrders(List<OrderDto> orders)
        {
            IsChangedOrders = false;
            foreach (var order in orders)
            {
                bool isExists = false;
                for (int i = 0; i < Orders.Count; i++)
                {
                    if (order.CorrelationId == Orders[i].CorrelationId)
                    {
                        IsChangedOrders = IsChangedOrders || !Orders[i].EqualsExceptPrice(order);
                        Orders[i] = (OrderDto)order.Clone();
                        isExists = true;
                        break;
                    }
                }
                if (!isExists)
                {
                    Orders.Add(order);
                    IsChangedOrders = true;
                }
            }
        }

        public void CalculateOrdersQty()
        {
            OrdersBuyQty = 0;
            OrdersSellQty = 0;
            foreach (var order in Orders)
            {
                if (order.OrderStatus == OrderStatus.PARTIALLY_FILLED || order.OrderStatus == OrderStatus.NEW)
                {
                    if (order.Side == Side.BUY)
                    {
                        OrdersBuyQty += order.Size - order.ExecutedSize;
                    }
                    if (order.Side == Side.SELL)
                    {
                        OrdersSellQty += order.Size - order.ExecutedSize;
                    }
                }
            }
        }

        public delegate void OnMessageHandler(AlgorithmInfo algo);

        public event OnMessageHandler QuoteMessageHandler;
        public event OnMessageHandler SourceMessageHandler;
        public event OnMessageHandler HedgeMessageHandler;
        public event OnMessageHandler TargetMessageHandler;
        public event OnMessageHandler TradeStatisticHandler;
        public event OnMessageHandler ExecutionsHandler;
        public event OnMessageHandler OrdersHandler;
        public event OnMessageHandler AlertsHandler;

        public void OnQuoteMessage(L2PackageDto l2Book)
        {
            OnBookMessage(l2Book, BookType.QUOTE);
        }

        public void OnSourceMessage(L2PackageDto l2Book)
        {
            OnBookMessage(l2Book, BookType.SOURCE);
        }

        public void OnTargetMessage(L2PackageDto l2Book)
        {
            OnBookMessage(l2Book, BookType.TARGET);
        }

        public void OnHedgeMessage(L2PackageDto l2Book)
        {
            OnBookMessage(l2Book, BookType.HEDGE);
        }

        public void OnBookMessage(L2PackageDto l2Book, BookType bookType)
        {
            if (l2Book.Type != L2PackageType.SNAPSHOT_FULL_REFRESH)
                return;
            AlgoDictionary[bookType] = l2Book;
            OnMessageHandler bookHandler = bookType == BookType.QUOTE ? QuoteMessageHandler :
                                           bookType == BookType.SOURCE ? SourceMessageHandler :
                                           bookType == BookType.TARGET ? TargetMessageHandler : HedgeMessageHandler;
            bookHandler?.Invoke(this);
        }

        public void OnTradeStatisticMessage(AlgoInstrumentStatisticsDto[] statistics)
        {
            TradeStatistic = statistics.FirstOrDefault(a => a.AlgoId == AlgoId);
            if (TradeStatistic == null)
                return;
            TradeStatisticHandler?.Invoke(this);
        }

        public void OnExecutionMessage(ExecutionDto[] executions)
        {
            Executions = new List<ExecutionDto>(executions);
            if(Executions.Count == 0)
                return;
            ExecutionsHandler?.Invoke(this);
        }

        public void OnOrderMessage(OrderDto[] orders)
        {
            if (orders.Length == 0)
                return;
            ReplaceOrders(new List<OrderDto>(orders));
            OrdersHandler?.Invoke(this);
        }

        public void OnAlertMessage(TradingAlertDto[] alerts)
        {
            Alerts = alerts.Where(a => a.AlgoId == AlgoId && a.Description.Contains("Execution")).ToList();
            if (Alerts.Count == 0)
                return;
            AlertsHandler?.Invoke(this);
        }
    }
}
