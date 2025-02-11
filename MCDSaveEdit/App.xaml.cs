﻿using MCDSaveEdit.Save.Models.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
#nullable enable

namespace MCDSaveEdit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly AppModel _model = new AppModel();
        private readonly MultiTextWriter _outputWriter = new MultiTextWriter();

        private ControlWriter? _controlWriter = null;
        private SplashWindow? _splashWindow = null;
        private Window? _busyWindow = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _outputWriter.addWriter(Console.Out);
            Console.SetOut(_outputWriter);

            bool askForGameContentLocation = e.Args.Contains("ASK_FOR_GAME_CONTENT_LOCATION");
            bool skipGameContent = e.Args.Contains("SKIP_GAME_CONTENT");
            EventLogger.init();

            showSplashWindowReplacingOldWindow();

            if (skipGameContent)
            {
                showMainWindow();
            }
            else
            {
                loadAsync(askForGameContentLocation);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            EventLogger.dispose();
            _outputWriter.Dispose();
            base.OnExit(e);
        }

        private async void loadAsync(bool askForGameContentLocation)
        {
            bool canContinue = true;
            //check default install locations
            string? paksFolderPath = _model.usableGameContentIfExists();
            if (askForGameContentLocation || string.IsNullOrWhiteSpace(paksFolderPath))
            {
                //show dialog asking for install location
                canContinue = showGameFilesWindow(ref paksFolderPath);
            }

            if (!string.IsNullOrWhiteSpace(paksFolderPath))
            {
                try
                {
                    await loadGameContentAsync(paksFolderPath!);
                }
                catch (Exception e)
                {
                    //Clear the path saved in the registry because it might be the cause of the exception
                    _model.unloadGameContent();

                    var title = $"{Constants.APPLICATION_NAME} {Constants.CURRENT_VERSION} - {R.ERROR}";
                    var message = $"{e.Message}\n\n{R.PLEASE_HAVE_LATEST_VERSION}\n\n{R.LAUNCH_WITH_LIMITED_FEATURES_QUESTION}";
                    var result = MessageBox.Show(message, title, MessageBoxButton.YesNo);
                    canContinue = result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
                }
            }
            else
            {
                //Clear the path saved in the registry
                _model.unloadGameContent();
            }

            if (!canContinue)
            {
                //User opted to Exit
                _splashWindow?.Close();
                closeBusyIndicator();
                this.MainWindow?.Close();
                this.Shutdown();
                return;
            }

            showMainWindow();
        }

        private bool showGameFilesWindow(ref string? selectedPath)
        {
            EventLogger.logEvent("showGameFilesWindow");
            var gameFilesWindow = WindowFactory.createGameFilesWindow(selectedPath, allowNoContent: true);
            gameFilesWindow.ShowDialog();
            var gameFilesWindowResult = gameFilesWindow.result;
            switch (gameFilesWindowResult)
            {
                case GameFilesWindow.GameFilesWindowResult.exit:
                    selectedPath = null;
                    return false;
                case GameFilesWindow.GameFilesWindowResult.useSelectedPath:
                    selectedPath = gameFilesWindow.selectedPath!;
                    return true;
                case GameFilesWindow.GameFilesWindowResult.noContent:
                    selectedPath = null;
                    return true;
            }
            throw new NotImplementedException();
        }

        private async Task<bool> loadGameContentAsync(string paksFolderPath)
        {
            showBusyIndicator();
            _model.initPakReader();
            await _model.loadGameContentAsync(paksFolderPath);
            await preloadImages();
            return true;
        }

        private Task<bool> preloadImages()
        {
            var tcs = new TaskCompletionSource<bool>();
            Task.Run(() => {
#if !HIDE_MAP_SCREENS
                LevelImagePanel.preload();
                MapScreen.preload(); //This takes a while
#endif
                InventoryScreen.preload();
                EquipmentScreen.preload();
                SelectionWindow.preload();
                ItemControl.preload();
                tcs.SetResult(true);
            });
            return tcs.Task;
        }

        private void showMainWindow()
        {
            EventLogger.logEvent("showMainWindow", new Dictionary<string, object>() { { "gameContentLoaded", AppModel.gameContentLoaded.ToString() } });
            var mainWindow = WindowFactory.createMainWindow();
            mainWindow.onRelaunch = onRelaunch;
            mainWindow.onReload = onReload;
            this.MainWindow = mainWindow;

            _splashWindow?.Close();
            closeBusyIndicator();

            this.MainWindow.Show();
        }

        private void onRelaunch()
        {
            showSplashWindowReplacingOldWindow();
            loadAsync(askForGameContentLocation: true);
        }

        private void onReload(string? autoReloadFilename, ProfileSaveFile? profile)
        {
            var oldMainWindow = this.MainWindow;
            var mainWindow = WindowFactory.createMainWindow();
            mainWindow.onRelaunch = onRelaunch;
            mainWindow.onReload = onReload;
            this.MainWindow = mainWindow;
            oldMainWindow?.Close();
            this.MainWindow.Show();

            if (!string.IsNullOrWhiteSpace(autoReloadFilename))
            {
                if(profile != null)
                {
                    if(mainWindow.model == null)
                    {
                        mainWindow.model = new ProfileViewModel();
                    }
                    mainWindow.model!.filePath = autoReloadFilename;
                    mainWindow.model!.profile.setValue = profile;
                }
                else
                {
                    mainWindow.handleFileOpenAsync(autoReloadFilename!);
                }
            }
        }

        private void showSplashWindowReplacingOldWindow()
        {
            var oldMainWindow = this.MainWindow;
            _splashWindow = WindowFactory.createSplashWindow();
            if (Constants.IS_DEBUG)
            {
                _controlWriter = new ControlWriter(_splashWindow.textbox);
                _outputWriter.addWriter(_controlWriter);
            }

            MainWindow = _splashWindow;
            oldMainWindow?.Close();
            this.MainWindow.Show();
        }

        private void showBusyIndicator()
        {
            closeBusyIndicator();

            _busyWindow = WindowFactory.createBusyWindow();
            _busyWindow.Show();
        }

        private void closeBusyIndicator()
        {
            if (_busyWindow != null)
            {
                _busyWindow!.Close();
                _busyWindow = null;
            }
        }

    }
}
