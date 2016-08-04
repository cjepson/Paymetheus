using Paymetheus.Decred;
using Paymetheus.Decred.Util;
using Paymetheus.Decred.Wallet;
using Paymetheus.Framework;
using Grpc.Core;
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

        private Address _ticketAddress = null;
        public string TicketAddressString
        {
            get { return _ticketAddress.ToString(); }
            set {
                _ticketAddress = null;
                if (value == "") {
                    return;
                }

                try
                {
                    _ticketAddress = Address.Decode(value);
                }
                finally
                {
                    _settingsAreValid();
                }
            }
        }

        private Address _poolAddress = null;
        public string PoolAddressString
        {
            get { return _poolAddress.ToString(); }
            set
            {
                _poolAddress = null;
                if (value == "")
                {
                    return;
                }

                try
                {
                    _poolAddress = Address.Decode(value);
                }
                finally
                {
                    _settingsAreValid();
                }
            }
        }

        private double _poolFees = 0.0;
        public double PoolFees
        {
            get { return _poolFees; }
            set
            {
                var _testPoolFees = value * 100.0;
                if (_testPoolFees != Math.Floor(_testPoolFees))
                {
                    _poolFees = 0.0;
                    throw new ArgumentException("Bad pool fees; pool fees must be between 0.00 and 100.00%");
                }

                if (_testPoolFees > 10000.0)
                {
                    _poolFees = 0.0;
                    throw new ArgumentException("Bad pool fees; pool fees must be less than 100.00%");
                }

                if (_testPoolFees < 1.0)
                {
                    _poolFees = 0.0;
                    throw new ArgumentException("Bad pool fees; pool fees must be greater than 0.00%");
                }

                _poolFees = value;
                _settingsAreValid();
            }
        }

        private long _number = 0;
        public long Number
        {
            get { return _number; }
            set
            {
                if (value < 0)
                {
                    _number = 0;
                    throw new ArgumentException("Negative number of tickets passed");
                }

                if (_selectedAccount == null)
                {
                    _number = 0;
                    throw new ArgumentException("No account selected");
                }

                if (_selectedAccount.Balances.SpendableBalance <= 0) {
                    _number = 0;
                    throw new ArgumentException("Empty account");
                }

                if ((Amount)(_stakeDifficultyProperties.Price * value) > _selectedAccount.Balances.SpendableBalance)
                {
                    _number = 0;
                    string errorString = "Not enough funds; have " +
                        _selectedAccount.Balances.SpendableBalance.ToString() + " want " +
                        ((Amount)(_stakeDifficultyProperties.Price * value)).ToString();
                    throw new ArgumentException(errorString);
                }

                _number = value;
                _settingsAreValid();
            }
        }

        // The default expiry is 16.
        private int _expiry = 16;
        private int _minExpiry = 2;
        public int Expiry
        {
            get { return _expiry; }
            set
            {
                if (value <= _minExpiry)
                {
                    _expiry = 0;
                    throw new ArgumentException("Expiry must be a minimum of 2 blocks");
                }

                _expiry = value;
                _settingsAreValid();
            }
        }

        // TODO Declare this as a global somewhere?
        private Amount _minFeeAmount = Denomination.Decred.AmountFromDouble(0.01000000);
        private Amount _maxFeeAmount = Denomination.Decred.AmountFromDouble(0.99999999);

        private Amount _splitFee = 0;
        public double SplitFee
        {
            get { return _splitFee; }
            set
            {
                var _testAmount = Denomination.Decred.AmountFromDouble(value);

                if (_testAmount < _minFeeAmount)
                {
                    _splitFee = 0;
                    throw new ArgumentException("Too small fee passed (must be >= 0.01000000 DCR/KB)");
                }

                if (_testAmount > _maxFeeAmount)
                {
                    _splitFee = 0;
                    throw new ArgumentException("Too big fee passed (must be <= 0.99999999 DCR/KB)");
                }

                _splitFee = _testAmount;
                _settingsAreValid();
            }
        }

        private Amount _ticketFee = 0;
        public double TicketFee
        {
            get { return _ticketFee; }
            set
            {
                var _testAmount = Denomination.Decred.AmountFromDouble(value);

                if (_testAmount < _minFeeAmount)
                {
                    _ticketFee = 0;
                    throw new ArgumentException("Too small fee passed (must be >= 0.01000000 DCR/KB)");
                }

                if (_testAmount > _maxFeeAmount)
                {
                    _ticketFee = 0;
                    throw new ArgumentException("Too big fee passed (must be <= 0.99999999 DCR/KB)");
                }

                _ticketFee = _testAmount;
                _settingsAreValid();
            }
        }

        private void _settingsAreValid()
        {
            if (_selectedAccount == null)
            {
                _purchaseTickets.Executable = false;
                return;
            }

            MessageBox.Show(_ticketAddress.ToString());
            if (_ticketAddress == null)
            {
                _purchaseTickets.Executable = false;
                return;
            }

            if (_expiry < _minExpiry) {
                _purchaseTickets.Executable = false;
                return;
            }

            if (_number <= 0)
            {
                _purchaseTickets.Executable = false;
                return;
            }

            if (_poolChecked)
            {
                if (_poolAddress == null)
                {
                    _purchaseTickets.Executable = false;
                    return;
                }

                if (_poolFees == 0.0)
                {
                    _purchaseTickets.Executable = false;
                    return;
                }
            }

            if (_feesChecked)
            {
                if (_splitFee == 0)
                {
                    _purchaseTickets.Executable = false;
                    return;
                }

                if (_ticketFee == 0)
                {
                    _purchaseTickets.Executable = false;
                    return;
                }
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
