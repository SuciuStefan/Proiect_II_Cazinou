using System;
using System.Linq;

namespace CasinoApp.BusinessLogic.Services
{
    public record WheelSlot(
        string Icon,
        string Label,
        string SubLabel,
        string Color,
        decimal CashValue,
        bool IsMystery,
        int SortOrder,
        string PrizeName = "",
        string ImageUrl = ""
    );

    public record WheelSpinSetup(
        int WinIndex,
        WheelSlot WonSlot,
        double NewWheelRotation
    );

    public record WheelSpinResult(
        WheelSlot WonSlot,
        double NewBalance,
        bool HasCashPrize,
        string ResultMsg,
        string BetStatus
    );

    public interface IWheelOfFortuneService
    {
        WheelSlot[] Slots { get; }
        int SpinDurationMs { get; }
        double CurrentRotation { get; }

        WheelSpinSetup PrepareSpin();
        WheelSpinResult ResolveSpin(WheelSlot wonSlot, double currentBalance);
    }

    public class WheelOfFortuneService : IWheelOfFortuneService
    {
        public int SpinDurationMs { get; } = 5200;
        public double CurrentRotation { get; private set; } = 0;

        private const int FullSpins = 7;
        private readonly Random _rng = new();

        public WheelSlot[] Slots { get; } = new[]
        {
            new WheelSlot("🎁", "SURPRIZA", "",    "#6b1212", 0,      true,  10,
                "🚗 Marele premiu",
                "https://images.unsplash.com/photo-1665439334045-391ec22ac8dd?w=900&auto=format&fit=crop&q=85"),

            new WheelSlot("💰", "5",        "RON", "#555",    5m,     false, 1),
            new WheelSlot("💵", "10.000",   "RON", "#8a6900", 10000m, false, 7),

            new WheelSlot("🎁", "SURPRIZA", "",    "#1a3a4a", 0,      true,  10,
                "📱 Premiu",
                "https://crystalpng.com/wp-content/uploads/2025/10/IPhone-17-Pro-Max-PNG.png"),

            new WheelSlot("💴", "100",      "RON", "#145a32", 100m,   false, 4),
            new WheelSlot("💵", "25",       "RON", "#4a235a", 25m,    false, 2),
            new WheelSlot("💶", "1.000",    "RON", "#a04000", 1000m,  false, 6),
            new WheelSlot("💵", "500",      "RON", "#1a3a6a", 500m,   false, 5),
        };

        public WheelSpinSetup PrepareSpin()
        {
            int winIndex = _rng.Next(Slots.Length);
            var wonSlot = Slots[winIndex];

            double landing = 360.0 - (winIndex + 0.5) * (360.0 / Slots.Length);
            CurrentRotation += FullSpins * 360.0 + landing;

            return new WheelSpinSetup(winIndex, wonSlot, CurrentRotation);
        }

        public WheelSpinResult ResolveSpin(WheelSlot wonSlot, double currentBalance)
        {
            if (wonSlot.IsMystery)
            {
                return new WheelSpinResult(
                    WonSlot: wonSlot,
                    NewBalance: currentBalance,
                    HasCashPrize: false,
                    ResultMsg: "",
                    BetStatus: "Won"
                );
            }

            if (wonSlot.CashValue > 0)
            {
                double newBalance = currentBalance + (double)wonSlot.CashValue;
                return new WheelSpinResult(
                    WonSlot: wonSlot,
                    NewBalance: newBalance,
                    HasCashPrize: true,
                    ResultMsg: $"FELICITĂRI! AI CÂȘTIGAT {wonSlot.CashValue:N0} RON!",
                    BetStatus: "Won"
                );
            }

            return new WheelSpinResult(
                WonSlot: wonSlot,
                NewBalance: currentBalance,
                HasCashPrize: false,
                ResultMsg: "",
                BetStatus: "Lost"
            );
        }
    }
}