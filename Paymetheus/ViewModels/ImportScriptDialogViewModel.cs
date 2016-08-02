// Copyright (c) 2016 The btcsuite developers
// Copyright (c) 2016 The Decred developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using Grpc.Core;
using Paymetheus.Decred.Util;
using Paymetheus.Framework;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Paymetheus.ViewModels
{
    public sealed class ImportScriptDialogViewModel : DialogViewModelBase
    {
        private DelegateCommand _importScriptCommand;
        public ICommand ImportScriptCommand => _importScriptCommand;

        public ImportScriptDialogViewModel(ShellViewModel shell) : base(shell)
        {
            _importScriptCommand = new DelegateCommand(ImportScriptAction);
        }

        private byte[] _scriptBytes;
        public string ScriptHexString {
            get { return Hexadecimal.Encode(_scriptBytes); }
            set { _scriptBytes = Hexadecimal.Decode(value); }
        }
        public string Passphrase { private get; set; } = "";

        public ICommand Execute { get; }

        private async void ImportScriptAction()
        {
            try
            {
                await App.Current.Synchronizer.WalletRpcClient.ImportScriptAsync(_scriptBytes, true, 0, Passphrase);
                HideDialog();
            }
            catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.AlreadyExists)
            {
                MessageBox.Show("Script already exists");
            }
            catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.InvalidArgument)
            {
                MessageBox.Show(ex.Status.Detail);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void ImportScript()
        {
            var shell = ViewModelLocator.ShellViewModel as ShellViewModel;
            if (shell != null)
            {
                shell.VisibleDialogContent = new ImportScriptDialogViewModel(shell);
            }
        }
    }
}
