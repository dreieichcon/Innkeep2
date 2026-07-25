using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Innkeep2.Cloud.Components.Core;

[Authorize(Roles = "innkeep2-admin")]
public class AdminPageBase : ComponentBase
{
	
}