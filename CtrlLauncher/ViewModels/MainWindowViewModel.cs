﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using CtrlLauncher.Models;

namespace CtrlLauncher.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        #region LauncherCoreViewModel プロパティ
        public LauncherCoreViewModel LauncherCoreViewModel { get; private set; }
        #endregion

        #region SourceCodeContentViewModel変更通知プロパティ
        private SourceCodeContentViewModel _SourceCodeContentViewModel;

        public SourceCodeContentViewModel SourceCodeContentViewModel
        {
            get
            { return _SourceCodeContentViewModel; }
            set
            { 
                if (_SourceCodeContentViewModel == value)
                    return;
                _SourceCodeContentViewModel = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region IsMaintenanceModeプロパティ
        public bool IsMaintenanceMode { get; private set; }
        #endregion

        #region Titleプロパティ
        public string Title
        {
            get
            {
                return "CTRL Launcher" + (IsMaintenanceMode ? " [メンテナンスモード]" : "");
            }
        }
        #endregion

        #region SelectedAppInfo変更通知プロパティ
        private AppInfoViewModel _SelectedAppInfo = null;

        public AppInfoViewModel SelectedAppInfo
        {
            get
            { return _SelectedAppInfo; }
            set
            { 
                if (_SelectedAppInfo == value)
                    return;
                _SelectedAppInfo = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsVisibleNoSelectionText");
                StartCommand.RaiseCanExecuteChanged();
                ShowSourceCodeCommand.RaiseCanExecuteChanged();
                OpenDirectoryCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region IsVisibleNoSelectionText変更通知プロパティ
        public bool IsVisibleNoSelectionText { get { return !IsLoading && !LauncherCoreViewModel.IsAppsEmpty && SelectedAppInfo == null; } }
        #endregion

        #region IsLoading変更通知プロパティ
        private bool _IsLoading;

        public bool IsLoading
        {
            get
            { return _IsLoading; }
            set
            { 
                if (_IsLoading == value)
                    return;
                _IsLoading = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsVisibleNoSelectionText");
            }
        }
        #endregion

        #region IsCheckedVisibleStartCount変更通知プロパティ
        private bool _IsCheckedVisibleStartCount = false;

        public bool IsCheckedVisibleStartCount
        {
            get
            { return _IsCheckedVisibleStartCount; }
            set
            { 
                if (_IsCheckedVisibleStartCount == value)
                    return;
                _IsCheckedVisibleStartCount = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsVisibleStartCount");
            }
        }
        #endregion

        #region IsVisibleStartCount変更通知プロパティ
        public bool IsVisibleStartCount { get { return IsMaintenanceMode && IsCheckedVisibleStartCount; } }
        #endregion

        #region IsOpenSourceCodeFlyout変更通知プロパティ
        private bool _IsOpenSourceCodeFlyout;

        public bool IsOpenSourceCodeFlyout
        {
            get
            { return _IsOpenSourceCodeFlyout; }
            set
            { 
                if (_IsOpenSourceCodeFlyout == value)
                    return;
                _IsOpenSourceCodeFlyout = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region IsOpenAboutFlyout変更通知プロパティ
        private bool _IsOpenAboutFlyout = false;

        public bool IsOpenAboutFlyout
        {
            get
            { return _IsOpenAboutFlyout; }
            set
            { 
                if (_IsOpenAboutFlyout == value)
                    return;
                _IsOpenAboutFlyout = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        #region StartCommand
        private ViewModelCommand _StartCommand;

        public ViewModelCommand StartCommand
        {
            get
            {
                if (_StartCommand == null)
                {
                    _StartCommand = new ViewModelCommand(Start, CanStart);
                }
                return _StartCommand;
            }
        }

        public bool CanStart()
        {
            if (SelectedAppInfo == null) return false;
            return SelectedAppInfo.IsAvailableExecutable;
        }

        public void Start()
        {
            if (!CanStart()) return;
            var app = SelectedAppInfo;
            try
            {
                LauncherCoreViewModel.StartApp(app, () => Messenger.Raise(new InteractionMessage("Timeout")));
            }
            catch(Exception ex)
            {
                if (IsMaintenanceMode)
                    Messenger.Raise(new InformationMessage(
                        ex.Message + "\r\n\r\nファイルの配置とアクセス権限を確認して下さい。\r\nディレクトリ: " + app.Path + "\r\n実行ファイル相対パス: " + app.AppSpec.ExecutablePath,
                        "エラー", System.Windows.MessageBoxImage.Error, "Information"));
                else
                    Messenger.Raise(new InformationMessage(ex.Message, "エラー", System.Windows.MessageBoxImage.Error, "Information"));
            }
        }
        #endregion

        #region ShowSourceCodeCommand
        private ViewModelCommand _ShowSourceCodeCommand;

        public ViewModelCommand ShowSourceCodeCommand
        {
            get
            {
                if (_ShowSourceCodeCommand == null)
                {
                    _ShowSourceCodeCommand = new ViewModelCommand(ShowSourceCode, CanShowSourceCode);
                }
                return _ShowSourceCodeCommand;
            }
        }

        public bool CanShowSourceCode()
        {
            if (SelectedAppInfo == null) return false;
            return SelectedAppInfo.IsAvailableSourceCode;
        }

        public void ShowSourceCode()
        {
            if (!CanShowSourceCode()) return;
            SourceCodeContentViewModel.AppInfoViewModel = SelectedAppInfo;
            IsOpenSourceCodeFlyout = true;
        }
        #endregion

        #region OpenDirectoryCommand
        private ViewModelCommand _OpenDirectoryCommand;

        public ViewModelCommand OpenDirectoryCommand
        {
            get
            {
                if (_OpenDirectoryCommand == null)
                {
                    _OpenDirectoryCommand = new ViewModelCommand(OpenDirectory, CanOpenDirectory);
                }
                return _OpenDirectoryCommand;
            }
        }

        public bool CanOpenDirectory()
        {
            return SelectedAppInfo != null;
        }

        public void OpenDirectory()
        {
            SelectedAppInfo.OpenDirectory();
        }
        #endregion

        #region ExportCountDataCommand
        private ListenerCommand<SavingFileSelectionMessage> _ExportCountDataCommand;

        public ListenerCommand<SavingFileSelectionMessage> ExportCountDataCommand
        {
            get
            {
                if (_ExportCountDataCommand == null)
                {
                    _ExportCountDataCommand = new ListenerCommand<SavingFileSelectionMessage>(ExportCountData);
                }
                return _ExportCountDataCommand;
            }
        }

        public async void ExportCountData(SavingFileSelectionMessage parameter)
        {
            if (parameter.Response != null && parameter.Response.Count() == 1)
            {
                try
                {
                    await LauncherCoreViewModel.ExportCountDataAsync(parameter.Response[0]);
                    Messenger.Raise(new InformationMessage("データが保存されました。", "開始回数データをエクスポート", System.Windows.MessageBoxImage.Information, "Information"));
                }
                catch (Exception ex)
                {
                    Messenger.Raise(new InformationMessage(ex.Message, "エラー", System.Windows.MessageBoxImage.Error, "Information"));
                }
            }
        }
        #endregion

        #region ClearCountCommand
        private ViewModelCommand _ClearCountCommand;

        public ViewModelCommand ClearCountCommand
        {
            get
            {
                if (_ClearCountCommand == null)
                {
                    _ClearCountCommand = new ViewModelCommand(ClearCount);
                }
                return _ClearCountCommand;
            }
        }

        public void ClearCount()
        {
            var msg = new ConfirmationMessage("開始回数データをすべて消去します。よろしいですか?", "警告", System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxButton.YesNo, "Confirmation");
            Messenger.Raise(msg);
            if (msg.Response ?? false == true)
            {
                LauncherCoreViewModel.ClearCount();
                Initialize();
            }
        }
        #endregion

        #region OpenAboutFlyoutCommand
        private ViewModelCommand _OpenAboutFlyoutCommand;

        public ViewModelCommand OpenAboutFlyoutCommand
        {
            get
            {
                if (_OpenAboutFlyoutCommand == null)
                {
                    _OpenAboutFlyoutCommand = new ViewModelCommand(OpenAboutFlyout);
                }
                return _OpenAboutFlyoutCommand;
            }
        }

        public void OpenAboutFlyout()
        {
            IsOpenAboutFlyout = true;
        }
        #endregion

        public MainWindowViewModel()
        {
#if DEBUG
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                IsMaintenanceMode = false;
            else
                IsMaintenanceMode = true;
#else
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                IsMaintenanceMode = true;
            else
                IsMaintenanceMode = false;
#endif

            LauncherCoreViewModel = new LauncherCoreViewModel();
            CompositeDisposable.Add(new PropertyChangedEventListener(LauncherCoreViewModel, (sender, e) =>
            {
                if (e.PropertyName == "IsAppsEmpty") RaisePropertyChanged("IsVisibleNoSelectionText");
            }));

            SourceCodeContentViewModel = new SourceCodeContentViewModel();
        }

        public async void Initialize()
        {
            IsLoading = true;
            await LauncherCoreViewModel.LoadAppsAsync();
            IsLoading = false;
        }
    }
}
