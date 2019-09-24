using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nethereum.Hex.HexTypes;
using Transaction = Nethereum.RPC.Eth.DTOs.Transaction;

namespace BTCPayServer.Ethereum.Client
{
    public class EthereumClientTransactionRepository
    {
        private EthereumClientApplicationDbContextFactory _ContextFactory;

        public EthereumClientTransactionRepository(EthereumClientApplicationDbContextFactory contextFactory)
        {
            _ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task<IEnumerable<EthereumClientTransactionData>> FindEthereumClientTransactionDataByAddresses(List<string> addresses)
        {
            if (addresses == null || !addresses.Any())
                return new List<EthereumClientTransactionData>();

            using (var ctx = _ContextFactory.CreateContext())
            {
                var result = ctx.EthereumClientTransactions.Where(t => addresses.Contains(t.From) || addresses.Contains(t.To));
                return await result.ToArrayAsync();
            }
        }

        public async Task<IEnumerable<Transaction>> FindTransactionByAddresses(List<string> addresses)
        {
            if (addresses == null || !addresses.Any())
                return new List<Transaction>();

            var transDatas = await FindEthereumClientTransactionDataByAddresses(addresses);

            List<Transaction> transes = new List<Transaction>();

            foreach (var transData in transDatas)
            {
                transes.Add(new Transaction()
                {
                    TransactionHash = transData.TransactionHash,
                    TransactionIndex = new HexBigInteger(transData.TransactionIndex),
                    BlockHash = transData.BlockHash,
                    BlockNumber = new HexBigInteger(transData.BlockNumber),
                    From = transData.From,
                    To = transData.To,
                    Gas = new HexBigInteger(transData.Gas),
                    GasPrice = new HexBigInteger(transData.GasPrice),
                    Value = new HexBigInteger(transData.Value),
                    Input = transData.Input,
                    Nonce = new HexBigInteger(transData.Nonce),
                });
            }
            return transes;
        }

        public async Task SaveOrUpdateTransaction(Transaction transaction)
        {
            EthereumClientTransactionData entity = new EthereumClientTransactionData()
            {
                TransactionHash = transaction.TransactionHash,
                TransactionIndex = transaction.TransactionIndex.Value.ToString(),
                BlockHash = transaction.BlockHash,
                BlockNumber = transaction.BlockNumber.Value.ToString(),
                From = transaction.From,
                To = transaction.To,
                Gas = transaction.Gas.Value.ToString(),
                GasPrice = transaction.GasPrice.Value.ToString(),
                Value = transaction.Value.ToString(),
                Input = transaction.Input,
                Nonce = transaction.Nonce.Value.ToString(),
            };

            using (var ctx = _ContextFactory.CreateContext())
            {
                var existing = await ctx.EthereumClientTransactions.SingleOrDefaultAsync(t => t.TransactionHash == transaction.TransactionHash).ConfigureAwait(false);

                if (existing == null)
                {
                    ctx.Add(entity);
                }
                else
                {
                    ctx.Entry(existing).CurrentValues.SetValues(entity);
                }
                await ctx.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
