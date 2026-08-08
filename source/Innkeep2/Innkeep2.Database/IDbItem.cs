using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Innkeep2.Database.Model;

namespace Innkeep2.Database;

public interface IDbItem
{
	public int Id { get; set; }
	
	public Operation Operation { get; set; }
}