﻿using NAudio.Wave;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Soncoord.Infrastructure;
using Soncoord.Infrastructure.Models;
using System.Collections.Generic;
using System.IO;

namespace Soncoord.Player.ViewModels
{
    [RegionMemberLifetime(KeepAlive = false)]
    public class SettingsViewModel : BindableBase
    {
        public SettingsViewModel()
        {
            SaveSettings = new DelegateCommand(OnSaveSettingsExecute);
            SelectedOutputSettings = LoadSettings();
        }

        public DelegateCommand SaveSettings { get; set; }

        public float MinimumGain => -12;
        
        public float MaximumGain => 12;

        public IEnumerable<DirectSoundDeviceInfo> OutputDevices
        { 
            get => DirectSoundOut.Devices;
        }
        
        private PlayerOutputSettings _selectedOutputSettings;
        public PlayerOutputSettings SelectedOutputSettings
        {
            get => _selectedOutputSettings;
            set => SetProperty(ref _selectedOutputSettings, value);
        }

        private PlayerOutputSettings LoadSettings()
        {
            if (Directory.Exists(Globals.PlayerPath))
            {
                if (File.Exists(Globals.PlayerOutputSettingsFile))
                {
                    PlayerOutputSettings settings;
                    using (var file = File.OpenText(Globals.PlayerOutputSettingsFile))
                    {
                        var serializer = new JsonSerializer();
                        settings = serializer.Deserialize(file, typeof(PlayerOutputSettings)) as PlayerOutputSettings;
                    }

                    return settings;        
                }
            } 
            
            return new PlayerOutputSettings();
        }

        private void OnSaveSettingsExecute()
        {
            if (!Directory.Exists(Globals.PlayerPath))
            {
                Directory.CreateDirectory(Globals.PlayerPath);
            }

            using (var file = File.CreateText(Globals.PlayerOutputSettingsFile))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, SelectedOutputSettings);
            }
        }
    }
}