using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Net;
using Telegram.Bot;

namespace cAlgo
{
    [Levels(30.0, 50.0, 70.0)]
    [Indicator(TimeZone = TimeZones.UTC, IsOverlay = false, AutoRescale = true, ScalePrecision = 2, AccessRights = AccessRights.FullAccess)]
    public class RSITelegramAlerts : Indicator
    {
        #region User settings

        [Parameter("Period", Group = "Indicator Data", DefaultValue = 14)]
        public int Period { get; set; }

        [Parameter("Source", Group = "Indicator Data")]
        public DataSeries Source { get; set; }

        [Parameter("Upper Threshold", Group = "Indicator Levels", DefaultValue = 70)]
        public double HigherLevel { get; set; }

        [Parameter("Lower Threshold", Group = "Indicator Levels", DefaultValue = 30)]
        public double LowerLevel { get; set; }

        [Parameter("Send a Telegram?", DefaultValue = false, Group = "Telegram Notifications")]
        public bool IncludeTelegram { get; set; }

        [Parameter("Bot Token", DefaultValue = "", Group = "Telegram Notifications")]
        public string BotToken { get; set; }

        [Parameter("Chat ID", DefaultValue = "", Group = "Telegram Notifications")]
        public string ChatID { get; set; }

        [Output("Main", PlotType = PlotType.Line, LineColor = "LimeGreen", Thickness = 2)]
        public IndicatorDataSeries Result { get; set; }

        #endregion

        public RelativeStrengthIndex RSI { get; set; }

        // Single alert pattern
        bool BuySignal { get; set; }
        bool SellSignal { get; set; }

        protected override void Initialize()
        {
            RSI = Indicators.RelativeStrengthIndex(Source, Period);

            // configure Telegram security protocol.
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            Result[index] = RSI.Result[index];

            // Only send alerts on the last candle when it closes.
            if (IsLastBar)
            {
                // Capture RSI oversold event, buy signal
                if (RSI.Result.LastValue < LowerLevel && !BuySignal)
                {
                    SendTelegram(SymbolName + " is oversold at " + LowerLevel.ToString());

                    // Only send alert once when RSI < lower level
                    BuySignal = true;
                    SellSignal = false;
                }

                // Capture RSI oversold event, sell signal.
                if (RSI.Result.LastValue > HigherLevel && !SellSignal)
                {
                    SendTelegram(SymbolName + " is overbought at " + HigherLevel.ToString());

                    // Only send alert once when RSI > upper level
                    BuySignal = false;
                    SellSignal = true;
                }
            }
        }

        public async void SendTelegram(string telegramMessage)
        {
            try
            {
                var bot = new TelegramBotClient(BotToken);
                await bot.SendTextMessageAsync(ChatID, telegramMessage);
            }
            catch (Exception ex)
            {
                Print("ERROR: " + ex.Message);
            }

        }
    }
}