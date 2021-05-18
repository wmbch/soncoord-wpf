﻿using Newtonsoft.Json;
using Soncoord.Infrastructure;
using Soncoord.Infrastructure.Interfaces;
using Soncoord.Infrastructure.Interfaces.Services;
using Soncoord.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Soncoord.Business.SongManager
{
    public class SongsService : ISongsService
    {
        private readonly HttpClient _httpClient;

        public SongsService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.streamersonglist.com")
            };

            Songs = new ObservableCollection<ISong>();
            LoadSongs();
        }

        public event EventHandler Imported;
        internal ObservableCollection<ISong> Songs { get; set; }

        public void Import()
        {
            ImportSongs();
        }

        public ISong GetSongById(string songId)
        {
            return Songs.FirstOrDefault(song => song.Id == songId);
        }

        public IEnumerable<ISong> GetSongs()
        {
            return Songs;
        }

        public ISongSetting GetSettings(ISong song)
        {
            return LoadSongSettings(song);
        }

        public void SaveSettings(ISong song, ISongSetting settings)
        {
            SaveSongSettings(song, settings);
        }

        private void LoadSongs()
        {
            var files = Directory.GetFiles(Globals.SongsPath);
            if (files != null && files.Length > 0)
            {
                Songs.Clear();

                foreach (var item in files)
                {
                    using (var file = File.OpenText(item))
                    {
                        var serializer = new JsonSerializer();
                        var song = serializer.Deserialize(file, typeof(Song)) as ISong;
                        Songs.Add(song);
                    }
                }
            }
        }

        private ISongSetting LoadSongSettings(ISong song)
        {
            var fileToOpen = $"{Globals.SongSettingsPath}\\{song.Id}.json";
            if (File.Exists(fileToOpen))
            {
                ISongSetting setting;
                using (var file = File.OpenText(fileToOpen))
                {
                    var serializer = new JsonSerializer();
                    setting = serializer.Deserialize(file, typeof(SongSetting)) as ISongSetting;
                }

                return setting;
            }

            return new SongSetting();
        }

        private void SaveSongSettings(ISong song, ISongSetting settings)
        {
            if (!Directory.Exists(Globals.SongSettingsPath))
            {
                Directory.CreateDirectory(Globals.SongSettingsPath);
            }

            using (var file = File.CreateText($"{Globals.SongSettingsPath}\\{song.Id}.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, settings);
            }
        }

        private void SaveSongs(ICollection<ISong> songs)
        {
            if (!Directory.Exists(Globals.SongsPath))
            {
                Directory.CreateDirectory(Globals.SongsPath);
            }

            foreach (var song in songs)
            {
                using (var file = File.CreateText($"{Globals.SongsPath}\\{song.Id}.json"))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, song);
                }
            }
        }

        private async void ImportSongs()
        {
            var songs = await LoadSongs(100, 0, new Collection<ISong>());
            SaveSongs(songs);
            AssignMediaFilesToSongs(songs);
            LoadSongs();
            Imported?.Invoke(this, null);
        }

        private async Task<ICollection<ISong>> LoadSongs(int size, int current, ICollection<ISong> collection)
        {
            var builder = new UriBuilder($"{_httpClient.BaseAddress}v1/streamers/wampe/songs");
            //var builder = new UriBuilder($"{_httpClient.BaseAddress}v1/streamers/2557/songs");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["size"] = size.ToString();
            query["current"] = current.ToString();
            query["current"] = current.ToString();
            builder.Query = query.ToString();

            var response = await _httpClient.GetAsync(builder.Uri);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var songQuery = JsonConvert.DeserializeObject<SongQuery>(result);

            foreach (var item in songQuery.Items)
            {
                collection.Add(item);
            }

            if (collection.Count < songQuery.Total)
            {
                var songs = await LoadSongs(size, current + 1, collection);
                collection.Concat(songs);
            }

            return collection;
        }

        private void AssignMediaFilesToSongs(ICollection<ISong> songs)
        {
            var files = Directory.GetFiles(Globals.TracksSourcePath);

            foreach (var item in songs)
            {
                var parts = Regex.Escape(
                    item.Title
                        .ToLower()
                        .Replace(" ", "_")
                        .Replace("-", "_")
                        .Replace(" / ", "_")
                        .Replace("'", "")
                        .Replace("`", "")
                        .Replace("´", "")
                        .Replace("!", "")
                        .Replace("?", "")
                        .Replace(", ", "_")
                        .Replace("ä", "a")
                        .Replace("ü", "u")
                        .Replace("ö", "o"));

                var clickTrack = files
                    .FirstOrDefault(
                        file => Regex.Escape(file).ToLower().Contains(parts)
                            && Regex.Escape(file).ToLower().Contains("Klick"));

                var songTrack = files
                    .FirstOrDefault(
                        file => Regex.Escape(file).ToLower().Contains(parts)
                            && !Regex.Escape(file).ToLower().Contains("Klick"));

                if (string.IsNullOrEmpty(clickTrack) && string.IsNullOrEmpty(songTrack))
                {
                    continue;
                }

                SaveSettings(item, new SongSetting { ClickTrackPath = clickTrack, MusicTrackPath = songTrack });
            }
        }
    }
}
