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
        public PurchaseTicketsViewModel() : base()
        {
            var synchronizer = ViewModelLocator.SynchronizerViewModel as SynchronizerViewModel;
            if (synchronizer != null)
            {
                SelectedAccount = synchronizer.Accounts[0];
            }

            FetchStakeDifficultyCommand = new DelegateCommand(FetchStakeDifficultyAction);
            FetchStakeDifficultyCommand.Execute(null);

            _purchaseTickets = new DelegateCommand(PurchaseTicketsAction);
            _purchaseTickets.Executable = false;
        }

        private DelegateCommand _purchaseTickets;
        public ICommand Execute => _purchaseTickets;

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
        private string _ticketAddressString;
        public string TicketAddressString
        {
            get { return _ticketAddress.ToString(); }
            set {
                _ticketAddressString = value;

                _ticketAddress = null;
                if (_ticketAddressString == "") {
                    return;
                }

                try
                {
                    _ticketAddress = Address.Decode(_ticketAddressString);
                }
                finally
                {
                    _settingsAreValid();
                }
            }
        }

        private Address _poolAddress = null;
        private string _poolAddressString;
        public string PoolAddressString
        {
            get { return _poolAddress.ToString(); }
            set
            {
                _poolAddressString = value;

                _poolAddress = null;
                if (_poolAddressString == "")
                {
                    return;
                }

                try
                {
                    _poolAddress = Address.Decode(_poolAddressString);
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
            get { return Denomination.Decred.DoubleFromAmount(_splitFee); }
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
            get { return Denomination.Decred.DoubleFromAmount(_ticketFee); ; }
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
                int _windowSize = 144;
                int _heightOfChange = ((StakeDifficultyProperties.Height / _windowSize) + 1) * _windowSize;
                BlocksToRetarget = _heightOfChange - StakeDifficultyProperties.Height;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private int _blocksToRetarget;
        public int BlocksToRetarget
        {
            get { return _blocksToRetarget; }
            internal set { _blocksToRetarget = value; RaisePropertyChanged(); }
        }

        private void PurchaseTicketsAction()
        {
            var shell = ViewModelLocator.ShellViewModel as ShellViewModel;
            if (shell != null)
            {
                var _account = SelectedAccount.Account;
                var _spendLimit = StakeDifficultyProperties.Price;
                int _requiredConfirms = 2; // TODO allow user to set
                uint _expiryHeight = (uint)_expiry + (uint)StakeDifficultyProperties.Height;

                Amount _splitFeeLocal = 0;
                Amount _ticketFeeLocal = 0;
                if (_feesChecked)
                {
                    _splitFeeLocal = _splitFee;
                    _ticketFeeLocal = _ticketFee;
                }

                Func<string, Task<bool>> action =
                    passphrase => PurchaseTicketsWithPassphrase(passphrase, _account, _spendLimit, 
                    _requiredConfirms, _ticketAddress, (uint)_number, _poolAddress, _poolFees,
                    _expiryHeight, _splitFeeLocal, _ticketFeeLocal);
                shell.VisibleDialogContent = new PassphraseDialogViewModel(shell, 
                    "Enter passphrase to purchase tickets", 
                    "PURCHASE", 
                    action);
            }
        }

        private string _responseString = "";
        public string ResponseString
        {
            get { return _responseString; }
            set { _responseString = value; RaisePropertyChanged(); }
        }

        private async Task<bool> PurchaseTicketsWithPassphrase(string passphrase, Account account, 
            Amount spendLimit, int reqConfs, Address ticketAddress, uint number, Address poolAddress,
            double poolFees, uint expiry, Amount txFee, Amount ticketFee)
        {
            List<Blake256Hash> purchaseResponse;
            var walletClient = App.Current.Synchronizer.WalletRpcClient;
            try
            {
                purchaseResponse = await walletClient.PurchaseTicketsAsync(account, spendLimit, 
                    reqConfs, ticketAddress, number, poolAddress, poolFees, expiry, txFee,
                    ticketFee, passphrase);
            }
            catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.InvalidArgument)
            {
                ResponseString = "Invalid argument error: " + ex.ToString();
                return true;
            }
            catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.FailedPrecondition)
            {
                ResponseString = "Failed precondition error: " + ex.ToString();
                return true;
            }
            catch (Exception ex)
            {
                ResponseString = "Unexpected error: " + ex.ToString();
                return true;
            }

            ResponseString = "Success! \n" + string.Join("\n", purchaseResponse);
            return true;
        }
    }
}
