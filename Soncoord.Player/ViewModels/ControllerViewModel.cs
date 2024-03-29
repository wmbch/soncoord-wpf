﻿using Prism.Commands;
using Prism.Mvvm;
using Soncoord.Business.Broadcasting;
using Soncoord.Business.Player;
using Soncoord.Infrastructure;
using Soncoord.Infrastructure.Interfaces;
using Soncoord.Infrastructure.Interfaces.Services;
using System;
using System.IO;

namespace Soncoord.Player.ViewModels
{
    public class ControllerViewModel : BindableBase
    {
        private readonly IPlaylistService _playlistService;
        private readonly IOutputsService _outputsService;

        private BroadcasterService _broadcaster;

        public ControllerViewModel(IPlaylistService playlistService, IOutputsService outputsService)
        {
            _outputsService = outputsService;
            _playlistService = playlistService;
            _isRandomSceneRotation = true;

            Stop = new DelegateCommand(OnStopCommandExecute);
            Play = new DelegateCommand(OnPlayCommandExecute, OnCommandCanExecute)
                .ObservesProperty(() => IsPlaying);
            Next = new DelegateCommand(OnNextCommandExecute, OnCommandCanExecute)
                .ObservesProperty(() => IsPlaying);
        }

        public DelegateCommand Play { get; set; }
        public DelegateCommand Stop { get; set; }
        public DelegateCommand Next { get; set; }
        private SongExecuter SongExecuter { get; set; }

        private bool _isRandomSceneRotation;
        public bool IsRandomSceneRotation
        {
            get => _isRandomSceneRotation;
            set => SetProperty(ref _isRandomSceneRotation, value);
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        private ISong _selectedSong;
        public ISong SelectedSong
        {
            get => _selectedSong;
            set => SetProperty(ref _selectedSong, value);
        }

        private TimeSpan _position;
        public TimeSpan Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private TimeSpan _totalTime;
        public TimeSpan TotalTime
        {
            get => _totalTime;
            set => SetProperty(ref _totalTime, value);
        }

        private bool OnCommandCanExecute()
        {
            return !IsPlaying;
        }

        private void OnStopCommandExecute()
        {
            SongExecuter?.Stop();
            CurrentSongToFile(true);
        }

        private void OnPlayCommandExecute()
        {
            PlaySong();
        }

        private void OnNextCommandExecute()
        {
            PlayNextSong(false);
        }

        private void PositionUpdated(object sender, TimeSpan e)
        {
            Position = e;
        }

        private void PlayerStarted(object sender, TimeSpan e)
        {
            TotalTime = e;
        }

        private void PlayerEnded(object sender, EventArgs e)
        {
            UnregisterExecuter();
            PlayNextSong(true);
        }

        private void PlayerStopped(object sender, EventArgs e)
        {
            UnregisterExecuter();
        }

        private void PlayNextSong(bool isSongPlayed)
        {
            if (SelectedSong != null)
            {
                _playlistService.Remove(SelectedSong, isSongPlayed);
            }

            PlaySong();
        }

        private void PlaySong()
        {
            SelectedSong = _playlistService.GetNextSong();
            if (SelectedSong == null)
            {
                CurrentSongToFile(true);
                // ToDo:
                // Call for Moderation music if no Song is in the playlist

                return;
            }

            var songSettings = _playlistService.GetSongSettings(SelectedSong);
            var outputSettings = _outputsService.GetSettings();

            SongExecuter = new SongExecuter(songSettings, outputSettings);
            SongExecuter.PositionChanged += PositionUpdated;
            SongExecuter.Started += PlayerStarted;
            SongExecuter.Stopped += PlayerStopped;
            SongExecuter.Ended += PlayerEnded;

            SongExecuter.Play();
            IsPlaying = true;
            CurrentSongToFile(false);

            if (IsRandomSceneRotation)
            {
                _broadcaster = new BroadcasterService();
                _broadcaster.StartRandomSceneRoation();
            }
        }

        private void UnregisterExecuter()
        {
            SongExecuter.PositionChanged -= PositionUpdated;
            SongExecuter.Started -= PlayerStarted;
            SongExecuter.Stopped -= PlayerStopped;
            SongExecuter.Ended -= PlayerEnded;
            SongExecuter.Dispose();
            SongExecuter = null;

            IsPlaying = false;

            if (IsRandomSceneRotation)
            {
                _broadcaster.StopRandomSceneRotation();
            }
        }

        private void CurrentSongToFile(bool reset)
        {
            if (!Directory.Exists(Globals.PlayerPath))
            {
                Directory.CreateDirectory(Globals.PlayerPath);
            }

            using (var file = File.CreateText(Globals.PlayerOutputCurrentSongFile))
            {
                if (reset)
                {
                    file.Write($"No Song being played");
                }
                else
                {
                    file.Write($"{SelectedSong.Artist} - {SelectedSong.Title}");
                }
            }
        }
    }
}
