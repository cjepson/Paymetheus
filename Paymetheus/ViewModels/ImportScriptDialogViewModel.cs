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
        public ImportScriptDialogViewModel(ShellViewModel shell) : base(shell)
        {
            _importScript = new DelegateCommandAsync(ImportScriptAction);
        }

        private byte[] _scriptBytes;
        public string ScriptHexString {
            get { return Hexadecimal.Encode(_scriptBytes); }
            set {
                 _scriptBytes = Hexadecimal.Decode(value);
                _importScript.Executable = false;
                if (_scriptBytes != null) {
                    _importScript.Executable = true;
                }
            }        
        }
        public string Passphrase { private get; set; } = "";

        private DelegateCommandAsync _importScript;
        public ICommand Execute => _importScript;

        private async Task ImportScriptAction()
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
    }
}
