namespace FashionPro.Services
{
	public interface IBalanceService
	{
		Task UpdatePartyBalance(int partyId);
		Task UpdateFabricBalance(int fabricId);
		Task UpdateProductionBalance(int transactionId);
		Task UpdateSenderBalance(int senderId);
	}
}