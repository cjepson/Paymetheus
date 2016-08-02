// Copyright (c) 2016 The Decred developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using Paymetheus.Framework;
using System;
using System.Windows;
using System.Windows.Input;

namespace Paymetheus.ViewModels
{
    public sealed class ScriptsViewModel : ViewModelBase
    {
        private DelegateCommand _importScriptCommand;
        public ICommand ImportScriptCommand => _importScriptCommand;

        public ScriptsViewModel() : base()
        {
            _importScriptCommand = new DelegateCommand(ImportScriptCommand);
        }
    }
}
