using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NuGet.Protocol.Plugins;

namespace FashionPro.Models
{
	[Table("Parties")]
	public class Party
	{
		[Key]
		public int PartyId { get; set; }

		[Required]
		[MaxLength(150)]
		public string PartyName { get; set; }

		[Required]
		[MaxLength(50)]
		public string PartyType { get; set; }

		[MaxLength(20)]
		public string Phone { get; set; }

		[MaxLength(300)]
		public string Address { get; set; }

		public decimal CurrentBalance { get; set; } = 0;

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		// Navigation Properties
		public virtual ICollection<Fabric> Fabrics { get; set; }
		public virtual ICollection<FabricPayment> FabricPayments { get; set; }
		public virtual ICollection<ProductionTransaction> ProductionTransactions { get; set; }
		public virtual ICollection<ProductionPayment> ProductionPayments { get; set; }
		public virtual ICollection<Sender> Senders { get; set; }
		public virtual ICollection<Receiver> Receivers { get; set; }
	}
}