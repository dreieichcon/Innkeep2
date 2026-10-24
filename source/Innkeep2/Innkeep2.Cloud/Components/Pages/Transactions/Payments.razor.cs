using Innkeep2.Cloud.Services;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Pages.Transactions;

public partial class Payments
{
    private MudDataGrid<PaymentOverviewRow> _grid = null!;
    private PaymentRequestStatus? _status;
    private bool _onlyNeedingAttention;
    private bool _isBusy;

    [Inject]
    public PaymentRequestRepository PaymentRepository { get; set; } = null!;

    [Inject]
    public CardPaymentService CardPayments { get; set; } = null!;

    [Inject]
    public CardPaymentOptions Options { get; set; } = null!;

    [Inject]
    public TimeProvider TimeProvider { get; set; } = null!;

    [Inject]
    public UiResultHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    private PaymentOverviewRow? Selected { get; set; }

    private DateTime UtcNow => TimeProvider.GetUtcNow().UtcDateTime;

    private async Task<GridData<PaymentOverviewRow>> LoadAsync(GridState<PaymentOverviewRow> state, CancellationToken ct = default)
    {
        var result = await PaymentRepository.GetOverviewAsync(
            state.Page * state.PageSize,
            state.PageSize,
            _status,
            _onlyNeedingAttention,
            CardPaymentRules.ReaderBusyCutoff(UtcNow, Options),
            ct);

        if (!result.IsSuccess)
        {
            Snackbar.Add(result.Error!.Message, Severity.Error);
            return new GridData<PaymentOverviewRow> { Items = [], TotalItems = 0 };
        }

        return new GridData<PaymentOverviewRow> { Items = result.Value!.Items, TotalItems = result.Value.TotalCount };
    }

    private async Task OnStatusChangedAsync(PaymentRequestStatus? status)
    {
        _status = status;
        await ReloadAsync();
    }

    private async Task OnAttentionChangedAsync(bool value)
    {
        _onlyNeedingAttention = value;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        Selected = null;
        await _grid.ReloadServerData();
    }

    private async Task RecheckAsync(PaymentOverviewRow row)
    {
        _isBusy = true;

        var response = await Handler.TryExecuteAsync(
            () => CardPayments.RecheckAsync(row.Payment.RequestId),
            errorPrefix: "Prüfung bei SumUp fehlgeschlagen");

        if (response is not null)
            Snackbar.Add($"Status: {response.Status}", Severity.Info);

        _isBusy = false;
        await ReloadAsync();
    }

    private async Task RetryOrderAsync(PaymentOverviewRow row)
    {
        _isBusy = true;

        var response = await Handler.TryExecuteAsync(
            () => CardPayments.RetryOrderAsync(row.Payment.RequestId),
            successMessage: "Bestellung angelegt",
            errorPrefix: "Bestellung konnte nicht angelegt werden");

        _isBusy = false;

        if (response is not null)
            await ReloadAsync();
    }

    private static bool CanRecheck(PaymentOverviewRow row)
        => row.Payment.Status is PaymentRequestStatus.Pending or PaymentRequestStatus.Paid;

    private bool CanRetryOrder(PaymentOverviewRow row)
        => CardPaymentRules.GetAttention(row.Payment, row.HasOrder, UtcNow, Options)
            is PaymentAttention.PaidNotCompleted or PaymentAttention.CompletedWithoutOrder;

    private string? AttentionText(PaymentOverviewRow row)
        => CardPaymentRules.GetAttention(row.Payment, row.HasOrder, UtcNow, Options) switch
        {
            PaymentAttention.HangingPending => "Hängt, bei SumUp prüfen",
            PaymentAttention.PaidNotCompleted => "Bezahlt, Bestellung nicht abgeschlossen",
            PaymentAttention.CompletedWithoutOrder => "Abgeschlossen ohne Bestellung",
            _ => null
        };

    private static string OrderText(PaymentOverviewRow row)
        => row.HasOrder
            ? $"Ja (Pretix: {row.PretixStatus}, Fiskaly: {row.FiskalyStatus})"
            : "Nein";

    private static Color StatusColor(PaymentRequestStatus status)
        => status switch
        {
            PaymentRequestStatus.Completed => Color.Success,
            PaymentRequestStatus.Pending or PaymentRequestStatus.Paid => Color.Info,
            PaymentRequestStatus.Failed => Color.Error,
            _ => Color.Default
        };
}
