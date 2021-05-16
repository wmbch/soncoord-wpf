﻿using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soncoord.Infrastructure.Interfaces;
using Soncoord.Infrastructure.Interfaces.Services;
using System;
using System.Windows.Threading;

namespace Soncoord.Business.Player
{
    public class PlayerExecuter : IPlayerExecuter
    {
        private readonly DispatcherTimer _positionTimer;
        private readonly IOutputsService _outputsService;

        private DirectSoundOut _clickTrackOutput;
        private DirectSoundOut _songTrackOutput;
        private AudioFileReader _clickTrackReader;
        private AudioFileReader _songTrackReader;

        public PlayerExecuter(IOutputsService outputsService)
        {
            _outputsService = outputsService;
            _positionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _positionTimer.Tick += PositionTimerTick;

            UpdateAudioPosition(new TimeSpan(0));
        }

        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler<TimeSpan> Started;
        public bool IsTrackRunReverted { get; set; }

        public void Play(ISongSetting settings)
        {
            Play(settings.ClickTrackPath, settings.MusicTrackPath);
        }

        public void Play(string clickTrack, string songTrack)
        {
            var selectedOutputSettings = _outputsService.GetSettings();

            _clickTrackReader = new AudioFileReader(clickTrack);
            _clickTrackOutput = new DirectSoundOut(selectedOutputSettings.ClickTrackOutputDevice.Guid);
            _clickTrackOutput.PlaybackStopped += OnPlaybackStopped;
            _clickTrackOutput.Init(
                new OffsetSampleProvider(_clickTrackReader.ToSampleProvider())
                {
                    DelayBy = TimeSpan.FromSeconds(1)
                });

            _songTrackReader = new AudioFileReader(songTrack);
            _songTrackOutput = new DirectSoundOut(selectedOutputSettings.SongTrackOutputDevice.Guid);
            _songTrackOutput.PlaybackStopped += OnPlaybackStopped;
            _songTrackOutput.Init(
                new Equalizer(
                    new OffsetSampleProvider(_songTrackReader.ToSampleProvider())
                    {
                        DelayBy = _clickTrackReader.TotalTime - _songTrackReader.TotalTime + TimeSpan.FromSeconds(1)
                    },
                    selectedOutputSettings.EqualizerBands));

            _clickTrackOutput.Play();
            _songTrackOutput.Play();
            _positionTimer.Start();

            Started?.Invoke(this, _songTrackReader.TotalTime);
        }

        public void Stop()
        {
            _clickTrackOutput?.Stop();
            _songTrackOutput?.Stop();

            _clickTrackReader.Dispose();
            _clickTrackReader = null;

            _songTrackReader.Dispose();
            _songTrackReader = null;

            _positionTimer.Stop();
            UpdateAudioPosition(new TimeSpan(0));
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (sender is DirectSoundOut directSoundOutput)
            {
                directSoundOutput.PlaybackStopped -= OnPlaybackStopped;
                directSoundOutput.Dispose();
                directSoundOutput = null;
            }
        }

        private void PositionTimerTick(object sender, EventArgs e)
        {
            UpdateAudioPosition(
                IsTrackRunReverted
                    ? _songTrackReader.TotalTime - _songTrackReader.CurrentTime
                    : _songTrackReader.CurrentTime
            );
        }

        private void UpdateAudioPosition(TimeSpan value)
        {
            PositionChanged?.Invoke(this, value);
        }
    }
}
