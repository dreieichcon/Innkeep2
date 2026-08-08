using Innkeep2.Cloud.Services;
using Microsoft.AspNetCore.Components;

namespace Innkeep2.Cloud.Components.Pages.Abstract;

public class AbstractDatabasePage : ComponentBase
{
	[Inject]
	private UiResultHandler Handler { get; set; } = null!;
}