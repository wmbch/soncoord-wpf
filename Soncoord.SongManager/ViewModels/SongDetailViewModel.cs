﻿using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Soncoord.Infrastructure;
using Soncoord.Infrastructure.Interfaces;
using Soncoord.Infrastructure.Models;
using System.IO;

namespace Soncoord.SongManager.ViewModels
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class SongDetailViewModel : BindableBase, INavigationAware
    {
        public SongDetailViewModel()
        {
            SaveSettings = new DelegateCommand(OnSaveSettingsExecute);
            SelectClickTrack = new DelegateCommand(OnSelectClickTrackExecute);
            SelectMusicTrack = new DelegateCommand(OnSelectMusicTrackExecute);
        }

        public DelegateCommand SaveSettings { get; set; }
        public DelegateCommand SelectClickTrack { get; set; }
        public DelegateCommand SelectMusicTrack { get; set; }

        private ISongSetting _songSettings;
        public ISongSetting SongSettings
        {
            get => _songSettings;
            set => SetProperty(ref _songSettings, value);
        }

        private ISong _selectedSong;
        public ISong SelectedSong
        {
            get => _selectedSong;
            set => SetProperty(ref _selectedSong, value);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            SelectedSong = navigationContext.Parameters.GetValue<ISong>("Song");
            SongSettings = LoadSettings();
        }

        private void OnSelectMusicTrackExecute()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                SongSettings.MusicTrackPath = openFileDialog.FileName;
            }
        }

        private void OnSelectClickTrackExecute()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                SongSettings.ClickTrackPath = openFileDialog.FileName;
            }
        }

        private ISongSetting LoadSettings()
        {
            var fileToOpen = $"{Globals.SongSettingsPath}\\{SelectedSong.Id}.json";
            ISongSetting setting;
            if (File.Exists(fileToOpen))
            {
                using (var file = File.OpenText(fileToOpen))
                {
                    var serializer = new JsonSerializer();
                    setting = serializer.Deserialize(file, typeof(SongSetting)) as ISongSetting;
                }

                return setting;
            }

            return new SongSetting();
        }

        private void OnSaveSettingsExecute()
        {
            if (!Directory.Exists(Globals.SongSettingsPath))
            {
                Directory.CreateDirectory(Globals.SongSettingsPath);
            }

            using (var file = File.CreateText($"{Globals.SongSettingsPath}\\{SelectedSong.Id}.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, SongSettings);
            }
        }
    }
}