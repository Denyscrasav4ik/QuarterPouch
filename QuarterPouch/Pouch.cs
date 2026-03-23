using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuarterPouch
{
    public class Pouch
    {
        public Pouch(Items item, string str, double spend, Dictionary<Items, double> converstionRates, string id)
        {
            actingItems = new Items[] { item };
            formatString = str;
            itemConversionRates = converstionRates;
            spendPerUse = spend;
            _id = id;
        }
        private string _id;
        public virtual string id => _id;
        public Items[] actingItems;
        public double spendPerUse;
        public readonly Dictionary<Items, double> itemConversionRates;
        public double amount = 0;
        public string formatString = Singleton<LocalizationManager>.Instance.GetLocalizedText("Pouch_Quarter");
        public virtual string DisplayString()
        {
            return String.Format(formatString, amount);
        }

        public virtual bool CanFit(Items itm)
        {
            return true;
        }

        public virtual void AddAmount(double amt)
        {
            amount += amt;
        }

        public virtual void ResetAmountTo(double amnt)
        {
            amount = amnt;
        }

        public virtual bool AddConversionRateIfAvailable(string name, double amt)
        {
            Items i;
            try
            {
                i = EnumExtensions.GetFromExtendedName<Items>(name);
            }
            catch
            {
                return false;
            }
            itemConversionRates.Add(i, amt);
            return true;
        }

        public virtual bool Spend(Items itemUsed)
        {
            if (amount >= spendPerUse)
            {
                amount -= spendPerUse;
                return true;
            }
            return false;
        }
    }

    public class QuarterPouch : Pouch
    {
        public QuarterPouch() : base(Items.Quarter, "${0}", 0.25, new Dictionary<Items, double>() { { Items.Quarter, 0.25 } }, "quarter") { AddConversionRateIfAvailable("BeastQuarter", 0.25); }

        public override string id => "quarter";

        double myCap => QuarterPouchPlugin.QuarterSizeLimit.Value * 0.25;

        public override string DisplayString()
        {
            return String.Format(formatString, amount.ToString("0.00"));
        }

        public override bool CanFit(Items itm)
        {
            return (amount + itemConversionRates[itm]) <= myCap;
        }

        public override bool Spend(Items itemUsed)
        {
            double toSpend = itemConversionRates[itemUsed];
            if (amount >= toSpend)
            {
                amount -= toSpend;
                return true;
            }
            return false;
        }
    }
}
