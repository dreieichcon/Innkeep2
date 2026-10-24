using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.Ui.Shared.Components.Config;

public partial class SumUpReaderManager
{
    private static SumUpReaderCredential CreateNew() => new()
    {
        Name = string.Empty,
        ReaderId = string.Empty,
        MerchantCode = string.Empty
    };

    private static SumUpReaderCredential Clone(SumUpReaderCredential source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Active = source.Active,
        ReaderId = source.ReaderId,
        MerchantCode = source.MerchantCode,
        DeviceModel = source.DeviceModel,
        DeviceIdentifier = source.DeviceIdentifier,
        PairedAt = source.PairedAt
    };
}
