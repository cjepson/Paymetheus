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
        public PurchaseTicketsViewModel() : base()
        {
            //PurchaseTicketsCommand = new DelegateCommand(PurchaseTicketsAction);
        }

        private List<Blake256Hash> _purchaseTicketHashes;

        private bool _poolChecked = false;
        public bool PoolChecked
        {
            get { return _poolChecked; }
            set { _poolChecked = value; MessageBox("Pool checked"); RaisePropertyChanged(); }
        }

        private bool _feesChecked = false;
        public bool FeesChecked
        {
            get { return _feesChecked; }
            set { _feesChecked = value; RaisePropertyChanged(); }
        }

        public ICommand PurchaseTicketsCommand { get; }

        /*
        private async void PurchaseTicketsAction()
        {
            try
            {
               // StakeInfoProperties = await App.Current.Synchronizer.WalletRpcClient.StakeInfoAsync();
            }
            catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.FailedPrecondition)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
        */
    }
}
