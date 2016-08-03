using Paymetheus.Decred;
using Paymetheus.Decred.Wallet;
using Paymetheus.Framework;
using Grpc.Core;
using Paymetheus.Decred.Util;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Paymetheus.ViewModels
{
    class PurchaseTicketsViewModel : ViewModelBase
    {
        private DelegateCommandAsync _purchaseTickets;
        public ICommand Execute => _purchaseTickets;

        public PurchaseTicketsViewModel() : base()
        {
            FetchStakeDifficultyCommand = new DelegateCommand(FetchStakeDifficultyAction);
            FetchStakeDifficultyCommand.Execute(null);

            _purchaseTickets = new DelegateCommandAsync(PurchaseTicketsAction);
            _purchaseTickets.Executable = false;
            //PurchaseTicketsCommand = new DelegateCommand(PurchaseTicketsAction);
        }

        private List<Blake256Hash> _purchaseTicketHashes;

        private bool _poolChecked = false;
        public bool PoolChecked
        {
            get { return _poolChecked; }
            set { _poolChecked = value; RaisePropertyChanged(); }
        }

        private bool _feesChecked = false;
        public bool FeesChecked
        {
            get { return _feesChecked; }
            set { _feesChecked = value; RaisePropertyChanged(); }
        }

        private AccountViewModel _selectedAccount;
        public AccountViewModel SelectedAccount
        {
            get { return _selectedAccount; }
            set { _selectedAccount = value; RaisePropertyChanged(); }
        }

        private Address _ticketAddress;
        public string TicketAddressString
        {
            get { return _ticketAddress.ToString(); }
            set {
                if (value == "") {
                    _ticketAddress = null;
                    return;
                }

                try { _ticketAddress = Address.Decode(value); }
                catch (Exception ex) {
                    _ticketAddress = null;
                    MessageBox.Show(ex.Message, "Invalid ticket address");
                }

                RaisePropertyChanged();
            }
        }

        private Address _poolAddress;
        public string PoolAddressString
        {
            get { return _poolAddress.ToString(); }
            set
            {
                if (value == "")
                {
                    _poolAddress = null;
                    return;
                }

                try { _poolAddress = Address.Decode(value); }
                catch (Exception ex)
                {
                    _poolAddress = null;
                    MessageBox.Show(ex.Message, "Invalid pool address");
                }

                RaisePropertyChanged();
            }
        }

        private double _poolFees;
        public string PoolFeesString
        {
            get { return _poolFees.ToString(); }
            set
            {
                if (value == "")
                {
                    _poolFees = 0.0;
                    return;
                }

                try { _poolFees = double.Parse(value); }
                catch (Exception ex)
                {
                    _poolFees = 0.0;
                    MessageBox.Show(ex.Message, "Invalid pool fees set");
                    return;
                }

                var _testPoolFees = _poolFees * 100.0;
                if (_testPoolFees != Math.Floor(_testPoolFees))
                {
                    _poolFees = 0.0;
                    MessageBox.Show("Bad pool fees; pool fees must be between 0.00 and 100.00%");
                    return;
                }

                if (_testPoolFees > 10000.0)
                {
                    _poolFees = 0.0;
                    MessageBox.Show("Bad pool fees; pool fees must be less than 100.00%");
                    return;
                }

                if (_testPoolFees < 1.0)
                {
                    _poolFees = 0.0;
                    MessageBox.Show("Bad pool fees; pool fees must be greater than 0.00%");
                    return;
                }

                RaisePropertyChanged();
            }
        }

        private int _number = 0;
        public string NumberString
        {
            get { return _number.ToString(); }
            set
            {
                if (value == "")
                {
                    _number = 0;
                    return;
                }

                try { _number = int.Parse(value); }
                catch (Exception ex)
                {
                    _number = 0;
                    MessageBox.Show(ex.Message, "Invalid number of tickets set");
                    return;
                }

                if (_number < 0)
                {
                    _number = 0;
                    MessageBox.Show("Negative number of tickets set", "Number of tickets error");
                    return;
                }

                if (_selectedAccount == null)
                {
                    MessageBox.Show("No account selected", "Number of tickets error");
                    return;
                }

                if (_selectedAccount.Balances.SpendableBalance <= 0) {
                    MessageBox.Show("Not enough funds", "Number of tickets error");
                    return;
                }

                if (_stakeDifficultyProperties.Price * (Amount)_number < _selectedAccount.Balances.SpendableBalance)
                {
                    _number = 0;
                    MessageBox.Show("Not enough funds", "Number of tickets error");
                    return;
                } 
            }
        }

        private void _settingsAreValid()
        {
            if (_ticketAddress == null)
            {
                _purchaseTickets.Executable = false;
                return;
            }

            if (_number <= 0)
            {
                _purchaseTickets.Executable = false;
                return;
            }

            _purchaseTickets.Executable = true;
        }

        private StakeDifficultyProperties _stakeDifficultyProperties;
        public StakeDifficultyProperties StakeDifficultyProperties
        {
            get { return _stakeDifficultyProperties; }
            internal set { _stakeDifficultyProperties = value; RaisePropertyChanged(); }
        }

        public ICommand FetchStakeDifficultyCommand { get; }

        private async void FetchStakeDifficultyAction()
        {
            try
            {
                StakeDifficultyProperties = await App.Current.Synchronizer.WalletRpcClient.StakeDifficultyAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private async Task PurchaseTicketsAction()
        {
            await App.Current.Synchronizer.WalletRpcClient.StakeDifficultyAsync();
        }

        public ICommand PurchaseTicketsCommand { get; }

        /*
        private void PurchaseTickets()
        {
            var shell = ViewModelLocator.ShellViewModel as ShellViewModel;
            if (shell != null)
            {
                Func<string, Task<bool>> action =
                    passphrase => PurchaseTicketsWithPassphrase(passphrase);
                shell.VisibleDialogContent = new PassphraseDialogViewModel(shell, "Enter passphrase to purchase tickets", "PURCHASE", action);
            }
        }

        private async Task<bool> PurchaseTicketsWithPassphrase(string passphrase, Account account, 
            Amount spendLimit, int reqConfs, Address ticketAddress, uint number, Address poolAddress,
            double poolFees, uint expiry, Amount txFee, Amount ticketFee)
        {
            Tuple<Transaction, bool> signingResponse;
            var walletClient = App.Current.Synchronizer.WalletRpcClient;
            try
            {
                signingResponse = await walletClient.SignTransactionAsync(passphrase, tx);
            }
            catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.InvalidArgument)
            {
                MessageBox.Show(ex.Status.Detail);
                return false;
            }
            var complete = signingResponse.Item2;
            if (!complete)
            {
                MessageBox.Show("Failed to create transaction input signatures.");
                return false;
            }
            var signedTransaction = signingResponse.Item1;

            if (!publishImmediately)
            {
                MessageBox.Show("Reviewing signed transaction before publishing is not implemented yet.");
                return false;
            }

            var serializedTransaction = signedTransaction.Serialize();
            var publishedTxHash = await walletClient.PublishTransactionAsync(signedTransaction.Serialize());

            _pendingTransaction = null;
            _unusedChangeScripts.Remove(SelectedAccount.Account);
            PendingOutputs.Clear();
            AddPendingOutput();
            PublishedTxHash = publishedTxHash.ToString();

            return true;
        }

        private string _publishedTxHash = "";
        public string PublishedTxHash
        {
            get { return _publishedTxHash; }
            set { _publishedTxHash = value; RaisePropertyChanged(); }
        }
        */
    }
}
