using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Innkeep2.Database.Model;

public abstract class AbstractDbItem : IDbItem
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[NotMapped]
	public Operation Operation { get; set; }
}