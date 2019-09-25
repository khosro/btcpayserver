using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BTCPayServer.Ethereum.Model
{
    public class EthWalletSendModel
    {
        private string _addressTo;

        public EthWalletSendModel()
        {
            Addresses = new List<SelectListItem>();
        }

        public void InitAddress(Dictionary<string, decimal> addresse2Balances)
        {
            foreach (var addresse2Balance in addresse2Balances)
            {
                Addresses.Add(new SelectListItem() { Value = addresse2Balance.Key, Text = $"{addresse2Balance.Value}- {addresse2Balance.Key}" });
            }
        }

        public string AddressTo { get { return _addressTo == null ? "" : _addressTo; } set => _addressTo = value; }
        public decimal AmountInEther { get; set; }
        public string GasPrice { get; set; }
        public List<SelectListItem> Addresses { get; set; }
        public decimal CurrentBalance { get; set; }
        public string CryptoCode { get; set; }
        public string Error { get; set; }
        public ulong? Gas { get; set; }
        public ulong? Nonce { get; set; }
        public string Data { get; set; }
        //public string SelectedAccount { get { return Addresses.SingleOrDefault(t => t.Selected).Value; } }
        public string SelectedAccount { get; set; }
    }
}
