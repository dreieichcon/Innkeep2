namespace Innkeep2.Models.Fiskaly.Transaction;

public static class VatRateMapper
{
    public static VatRate FromTaxRate(decimal taxRate) => taxRate switch
    {
        19m => VatRate.Normal,
        7m => VatRate.Reduced1,
        0m => VatRate.Null,
        _ => VatRate.Null
    };
}