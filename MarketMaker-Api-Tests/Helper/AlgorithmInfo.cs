using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Config;
using MarketMaker.Api.Models.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.Helper
{
    public class AlgorithmInfo
    {
        public AlgorithmInfo()
        {
            AlgoDictionary = new Dictionary<BookType, L2PackageDto>();
        }

        public AlgorithmInfo(string fileInstrument, string filePricer, string fileHeadger, string fileRiskLimit)
        {
            AlgoId = -1;
            InstrumentConfigInfo = JsonConvert.DeserializeObject<InstrumentConfigDto>(Util.ReadFile(Util.paramsFolder + fileInstrument));
            PricerConfigInfo = JsonConvert.DeserializeObject<PricerConfigDto>(Util.ReadFile(Util.paramsFolder + filePricer));
            HedgerConfigInfo = JsonConvert.DeserializeObject<HedgerConfigDto>(Util.ReadFile(Util.paramsFolder + fileHeadger));
            RiskLimitConfigInfo = JsonConvert.DeserializeObject<RiskLimitsConfigDto>(Util.ReadFile(Util.paramsFolder + fileRiskLimit));
            AlgoDictionary = new Dictionary<BookType, L2PackageDto>();
        }

        public AlgorithmInfo(FullInstrumentConfigDto instrument)
        {
            AlgoId = instrument.InstrumentConfig.AlgoId != null ? (long)instrument.InstrumentConfig.AlgoId : -1;
            InstrumentConfigInfo = instrument.InstrumentConfig;
            PricerConfigInfo = instrument.PricerConfig;
            HedgerConfigInfo = instrument.HedgerConfig;
            RiskLimitConfigInfo = instrument.RiskLimitsConfig;
            AlgoDictionary = new Dictionary<BookType, L2PackageDto>();
        }

        public long AlgoId { get; set; }
        public InstrumentConfigDto InstrumentConfigInfo { get; set; }
        public PricerConfigDto PricerConfigInfo { get; set; }
        public HedgerConfigDto HedgerConfigInfo { get; set; }
        public RiskLimitsConfigDto RiskLimitConfigInfo { get; set; }

        public enum BookType
        {
            TARGET,
            QUOTE,
            SOURCE,
            HEDGE,
        }

        public Dictionary<BookType, L2PackageDto> AlgoDictionary { get; set; }

        public AlgoInstrumentStatisticsDto TradeStatistic { get; set; }

        public static T CreateConfig<T>(string fileName, long id)
        {
            var config = JsonConvert.DeserializeObject<T>(Util.ReadFile(Util.paramsFolder + fileName));
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

        public bool Equals(FullInstrumentConfigDto allConfiguration)
        {
            return this.EqualInstrument(allConfiguration.InstrumentConfig, true) &&
                   this.EqualPricer(allConfiguration.PricerConfig, true) &&
                   this.EqualHedger(allConfiguration.HedgerConfig, true) &&
                   this.EqualRiskLimit(allConfiguration.RiskLimitsConfig, true);
        }

        public bool EqualInstrument(InstrumentConfigDto instrumentConfig, bool printMessage)
        {
            bool isEqual = this.InstrumentConfigInfo.AlgoId == instrumentConfig.AlgoId &&
                   this.InstrumentConfigInfo.AlgoKey == instrumentConfig.AlgoKey &&
                   this.InstrumentConfigInfo.Exchange == instrumentConfig.Exchange &&
                   this.InstrumentConfigInfo.SourceExchange == instrumentConfig.SourceExchange &&
                   this.InstrumentConfigInfo.Instrument == instrumentConfig.Instrument &&
                   this.InstrumentConfigInfo.FxLeg == instrumentConfig.FxLeg &&
                   this.InstrumentConfigInfo.Underlyings == instrumentConfig.Underlyings;
            if(!isEqual && printMessage)
                Console.WriteLine("Instrument doesn't match with added.");
            return isEqual;
        }

        public bool EqualPricer(PricerConfigDto pricerConfig, bool printMessage)
        {
            bool isEqual = this.PricerConfigInfo.AlgoId == pricerConfig.AlgoId &&
                   this.PricerConfigInfo.AlgoKey == pricerConfig.AlgoKey &&
                   this.PricerConfigInfo.SellQuoteSizes == pricerConfig.SellQuoteSizes &&
                   this.PricerConfigInfo.BuyQuoteSizes == pricerConfig.BuyQuoteSizes &&
                   this.PricerConfigInfo.BuyMargins == pricerConfig.BuyMargins &&
                   this.PricerConfigInfo.SellMargins == pricerConfig.SellMargins &&
                   this.PricerConfigInfo.AggregationMethod == pricerConfig.AggregationMethod &&
                   Util.CompareDouble(this.PricerConfigInfo.MinPriceChange, pricerConfig.MinPriceChange);
            if (!isEqual && printMessage)
                Console.WriteLine("Pricer doesn't match with added.");
            return isEqual;
        }

        public bool EqualHedger(HedgerConfigDto hedgerConfig, bool printMessage)
        {
            bool isEqual = this.HedgerConfigInfo.AlgoId == hedgerConfig.AlgoId &&
                   this.HedgerConfigInfo.AlgoKey == hedgerConfig.AlgoKey &&
                   this.HedgerConfigInfo.HedgeInstrument == hedgerConfig.HedgeInstrument &&
                   this.HedgerConfigInfo.HedgeStrategy == hedgerConfig.HedgeStrategy &&
                   this.HedgerConfigInfo.ExecutionStyle == hedgerConfig.ExecutionStyle &&
                   this.HedgerConfigInfo.PositionMaxNormSize == hedgerConfig.PositionMaxNormSize &&
                   this.HedgerConfigInfo.VenuesList == hedgerConfig.VenuesList &&
                   Util.CompareDouble(this.HedgerConfigInfo.MaxOrderSize, hedgerConfig.MaxOrderSize);
            if (!isEqual && printMessage)
                Console.WriteLine("Hedger doesn't match with added.");
            return isEqual;
        }

        public bool EqualRiskLimit(RiskLimitsConfigDto riskLimitConfig, bool printMessage)
        {
            bool isEqual = this.RiskLimitConfigInfo.AlgoId == riskLimitConfig.AlgoId &&
                   this.RiskLimitConfigInfo.AlgoKey == riskLimitConfig.AlgoKey &&
                   this.RiskLimitConfigInfo.MinBuyQuoteActiveTime == riskLimitConfig.MinBuyQuoteActiveTime &&
                   this.RiskLimitConfigInfo.MinSellQuoteActiveTime == riskLimitConfig.MinSellQuoteActiveTime &&
                   Util.CompareDouble(this.RiskLimitConfigInfo.MaxLongExposure, riskLimitConfig.MaxLongExposure) &&
                   Util.CompareDouble(this.RiskLimitConfigInfo.MaxShortExposure, riskLimitConfig.MaxShortExposure);
            if (!isEqual && printMessage)
                Console.WriteLine("Risk Limits don't match with added.");
            return isEqual;
        }

        public delegate void OnMessageHandler(AlgorithmInfo algo);

        public event OnMessageHandler QuoteMessageHandler;
        public event OnMessageHandler SourceMessageHandler;
        public event OnMessageHandler HedgeMessageHandler;
        public event OnMessageHandler TargetMessageHandler;
        public event OnMessageHandler TradeStatisticHandler;

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
            this.AlgoDictionary[bookType] = l2Book;
            OnMessageHandler bookHandler = bookType == BookType.QUOTE ? QuoteMessageHandler :
                                           bookType == BookType.SOURCE ? SourceMessageHandler :
                                           bookType == BookType.TARGET ? TargetMessageHandler : HedgeMessageHandler;
            bookHandler?.Invoke(this);
        }

        public void OnTradeStatisticMessage(AlgoInstrumentStatisticsDto[] statistics)
        {
            this.TradeStatistic = statistics.FirstOrDefault(a => a.AlgoId == this.AlgoId);
            if (this.TradeStatistic == null)
                return;
            TradeStatisticHandler?.Invoke(this);
        }
    }
}
