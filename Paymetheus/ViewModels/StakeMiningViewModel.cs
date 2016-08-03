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
    public class StakeMiningViewModel : ViewModelBase
    {
        public StakeMiningViewModel() : base()
        {
            FetchStakeInfoCommand = new DelegateCommand(FetchStakeInfoAction);
        }

        private StakeInfoProperties _stakeInfoProperties;
        public StakeInfoProperties StakeInfoProperties
        {
            get { return _stakeInfoProperties; }
            internal set { _stakeInfoProperties = value; RaisePropertyChanged(); }
        }

        public ICommand FetchStakeInfoCommand { get; }

        private async void FetchStakeInfoAction()
        {
            try
            {
                StakeInfoProperties = await App.Current.Synchronizer.WalletRpcClient.StakeInfoAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}
