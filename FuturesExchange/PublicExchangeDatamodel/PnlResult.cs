using System;
using System.Runtime.Serialization;

namespace PublicExchangeDatamodel
{
    [DataContract]
    public class PnlResult
    {
        [DataMember(Name = "book")]
        public string Book { get; set; }

        [DataMember(Name = "totalLongVolume")]
        public double TotalLongVolume { get; set; }

        [DataMember(Name = "totalShortVolume")]
        public double TotalShortVolume { get; set; }

        [DataMember(Name = "signedTotalOutstandingVolume")]
        public double SignedTotalOutstandingVolume { get; set; }

        [DataMember(Name = "totalDebit")]
        public double TotalDebit { get; set; }

        [DataMember(Name = "totalCredit")]
        public double TotalCredit { get; set; }

        [DataMember(Name = "impliedPnl")]
        public double ImpliedPnl { get; set; }

        public override string ToString()
        {
            return $"Book: {Book}, " +
                   $"TotalLongVolume: {TotalLongVolume}, " +
                   $"TotalShortVolume: {TotalShortVolume}, " +
                   $"SignedTotalOutstandingVolume: {SignedTotalOutstandingVolume}, " +
                   $"TotalDebit: {TotalDebit}, " +
                   $"TotalCredit: {TotalCredit}, " +
                   $"ImpliedPnl: {ImpliedPnl}";
        }

        public PnlResult(string book, double totalLongVolume, double totalShortVolume, double signedTotalOutstandingVolume, double totalDebit, double totalCredit, double impliedPnl)
        {
            Book = book;
            TotalLongVolume = totalLongVolume;
            TotalShortVolume = totalShortVolume;
            SignedTotalOutstandingVolume = signedTotalOutstandingVolume;
            TotalDebit = totalDebit;
            TotalCredit = totalCredit;
            ImpliedPnl = impliedPnl;
        }
    }
}