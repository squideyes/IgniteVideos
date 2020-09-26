#region Copyright & License
// Copyright 2020 by Louis S. Berman
// 
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without 
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom 
// the Software is furnished to do so, subject to the following 
// conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Data;

namespace IgniteVideos
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string cancelOrDownload;
        private string sessionsCount;
        private CancellationTokenSource cts;
        private bool downloading;
        private string progress;
        private List<RunInfo> runInfos;
        private List<Session> sessions;
        private string filterText;
        private bool allSelected;
        private bool downloadVideos;
        private string statusPrompt;
        private FilterKind? selectedFilterKind;
        private SessionKind? selectedSessionKind;

        public event EventHandler OnHelp;

        public MainWindowViewModel(List<Session> sessions)
        {
            Title = "Ignite Video Viewer / Downloader v1.0";

            cts = new CancellationTokenSource();

            var fileName = GetFavoritesFileName(true);

            var favorites = GetFavorites(fileName);

            foreach (var session in sessions)
            {
                session.OnSelectionChanged += (s, e) =>
                {
                    if (allSelected && !e.Value)
                    {
                        allSelected = false;

                        RaisePropertyChanged(() => AllSelected);
                    }
                    else if (!allSelected && Sessions.All(c => c.Selected))
                    {
                        allSelected = true;

                        RaisePropertyChanged(() => AllSelected);
                    }

                    UpdateUI();
                };

                if (favorites.TryGetValue(session.Code, out bool isFavorite))
                    session.IsFavorite = isFavorite;
            }

            Sessions = sessions;

            SessionFilterView = (CollectionView)CollectionViewSource.GetDefaultView(Sessions);

            SessionFilterView.Filter = OnFilterTriggered;

            SessionFilterView.CurrentChanged += (s, e) =>
            {
                SessionsCount = $"{SessionFilterView.Count:N0} Sessions";
            };

            UpdateUI();

            DownloadVideos = true;

            Downloading = false;

            ShowStandardStatusPrompt();

            SelectedSessionKind = null;
        }

        public CollectionView SessionFilterView { get; set; }

        public string Title { get; }

        public string SessionsCount
        {
            get => sessionsCount;
            set => Set(ref sessionsCount, value);
        }

        public string StatusPrompt
        {
            get => statusPrompt;
            set => Set(ref statusPrompt, value);
        }

        public List<Session> Sessions
        {
            get => sessions;
            set => Set(ref sessions, value);
        }

        public bool DownloadVideos
        {
            get => downloadVideos;
            set => Set(ref downloadVideos, value);
        }

        public SessionKind? SelectedSessionKind
        {
            get => selectedSessionKind;
            set
            {
                Set(ref selectedSessionKind, value);

                ApplyFilter();
            }
        }

        public FilterKind? SelectedFilterKind
        {
            get => selectedFilterKind;
            set
            {
                Set(ref selectedFilterKind, value);

                ApplyFilter();
            }
        }

        public bool Downloading
        {
            get => downloading;
            set
            {
                downloading = value;

                CancelOrDownloadLabel = Downloading ? "Cancel" : "Download";
            }
        }

        public bool AllSelected
        {
            get
            {
                return allSelected;
            }
            set
            {
                Set(ref allSelected, value);

                foreach (var session in Sessions)
                    session.Selected = value;

                UpdateUI();
            }
        }

        public string CancelOrDownloadLabel
        {
            get => cancelOrDownload;
            set => Set(ref cancelOrDownload, value);
        }

        public string FilterText
        {
            get => filterText;
            set
            {
                Set(ref filterText, value);

                ApplyFilter();
            }
        }

        public List<RunInfo> RunInfos
        {
            get => runInfos;
            set => Set(nameof(RunInfos), ref runInfos);
        }

        public string Progress
        {
            get => progress;
            set => Set(ref progress, value);
        }

        private void ShowStandardStatusPrompt() =>
            StatusPrompt = "Click the \"Help\" button for full usage details";

        public bool OnFilterTriggered(object item)
        {
            bool ContainsFilterText(Session session)
            {
                return session.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                    || session.Code.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                    || session.Talent.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
            }

            bool PassesFilter(Session session)
            {
                return (session.Selected && (SelectedFilterKind == FilterKind.Selected))
                    || (session.IsFavorite && (SelectedFilterKind == FilterKind.Favorites));
            }

            if (item is Session session)
            {
                return (filterText == null || ContainsFilterText(session))
                    && (SelectedSessionKind == null || session.Kind == selectedSessionKind)
                    && (SelectedFilterKind == null || PassesFilter(session));
            }

            return true;
        }

        public void ApplyFilter() =>
            CollectionViewSource.GetDefaultView(Sessions).Refresh();

        private void UpdateUI()
        {
            RaisePropertyChanged(() => Downloading);
            RaisePropertyChanged(() => CancelOrDownloadLabel);
            RaisePropertyChanged(() => CancelOrDownloadCommand);
        }

        private async Task DownloadFilesAsync(List<Job> jobs)
        {
            const string SUCCESS = "Success";
            const string WARNING = "Warning";

            static string Plural(int count) => count >= 2 ? "s" : "";

            var progressLock = new object();

            long totalBytesRead = 0;
            int downloaded = 0;

            StatusPrompt = $"Downloading {jobs.Count} video file(s); click the \"Cancel\" button to cancel";

            var badJobs = new ConcurrentBag<Job>();

            var downloader = new ActionBlock<Job>(
                async job =>
                {
                    job.OnProgress += (s, e) =>
                    {
                        lock (progressLock)
                        {
                            totalBytesRead += e.BytesRead;

                            if (e.Finished)
                                downloaded++;

                            Progress = $"{downloaded:N0} of {jobs.Count:N0} ({totalBytesRead:N0} bytes)";
                        }
                    };

                    if (await job.DownloadAndSaveAsync(cts.Token))
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            job.Session.HasVideo = true;
                            job.Session.Selected = false;
                        }
                    }
                    else
                    {
                        badJobs.Add(job);
                        job.Session.Selected = false;
                    };
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cts.Token
                });

            jobs.ForEach(job => downloader.Post(job));

            downloader.Complete();

            try
            {
                await downloader.Completion;

                var sb = new StringBuilder();

                var title = SUCCESS;
                var image = MessageBoxImage.Information;

                if (jobs.Count == 1)
                {
                    var fileName = jobs[0].Session.GetCleanFileName();

                    if (badJobs.Count == 0)
                    {
                        sb.Append($"The \"");
                        sb.Append(fileName);
                        sb.Append("\" video was downloaded to the \"");
                        sb.Append(MiscHelpers.GetFolder());
                        sb.Append("\" folder.  Click on the \"Play\" button to play the video or navigate to the folder and play it from there.");
                    }
                    else
                    {
                        sb.Append($"The \"");
                        sb.Append(fileName);
                        sb.Append("\" video has no downloadable content!");
                        
                        title = WARNING;
                        image = MessageBoxImage.Warning;
                    }
                }
                else
                {
                    var goodCount = jobs.Count - badJobs.Count;

                    sb.Append(goodCount.ToString("N0"));
                    sb.Append(" video file");
                    sb.Append(Plural(goodCount));
                    sb.Append(' ');
                    sb.Append(goodCount >= 2 ? "were" : "was");
                    sb.Append(" successfully downloaded to the \"");
                    sb.Append(MiscHelpers.GetFolder());
                    sb.Append("\" folder");

                    if (badJobs.Count == 0)
                    {
                        sb.Append('.');
                    }
                    else
                    {
                        title = "Warning";
                        image = MessageBoxImage.Warning;

                        sb.Append(", but ");
                        sb.Append(badJobs.Count.ToString("N0"));
                        sb.Append(" session");
                        sb.Append(Plural(badJobs.Count));
                        sb.Append(" had no downloadable content!");
                    }
                }

                MessageBox.Show(sb.ToString(), title, MessageBoxButton.OK, image);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception error)
            {
                UpdateUI();

                MessageBox.Show(error.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                Downloading = false;

                UpdateUI();

                Progress = "";

                cts = new CancellationTokenSource();

                ShowStandardStatusPrompt();
            }
        }

        private Dictionary<string, bool> GetFavorites(string fileName)
        {
            var favorites = new Dictionary<string, bool>();

            try
            {
                if (File.Exists(fileName))
                {
                    var json = File.ReadAllText(fileName);

                    favorites = JsonSerializer.Deserialize<
                        Dictionary<string, bool>>(json);
                }
            }
            catch
            {
                favorites = new Dictionary<string, bool>();
            }

            return favorites;
        }

        private string GetFavoritesFileName(bool ensurePath = false)
        {
            var localPath = new AppInfo(typeof(MainWindowViewModel).Assembly).GetLocalAppDataPath();

            if (ensurePath && !Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            return Path.Combine(localPath, "Favorites.json");
        }

        public void SaveFavorites()
        {
            try
            {
                var favorites = sessions.Where(s => s.IsFavorite)
                    .ToDictionary(s => s.Code, s => s.IsFavorite);

                var json = JsonSerializer.Serialize(favorites);

                File.WriteAllText(GetFavoritesFileName(), json);
            }
            catch (Exception error)
            {
                MessageBox.Show($"FATAL ERROR: " + error.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand<Session> StreamVideoCommand => new RelayCommand<Session>(
            s => Shell.Execute(s.StreamUri.AbsoluteUri));

        public RelayCommand<Session> GoToSessionCommand => new RelayCommand<Session>(
            s => Shell.Execute(s.SessionUri.AbsoluteUri));

        public RelayCommand CancelOrDownloadCommand => new RelayCommand(async () =>
        {
            static List<Job> GetJobs(List<Session> sessions)
            {
                var jobs = new List<Job>();

                foreach (var session in sessions)
                    jobs.Add(new Job(session));

                return jobs;
            }

            if (Downloading)
            {
                Downloading = false;

                UpdateUI();

                cts.Cancel();
            }
            else
            {
                Downloading = true;

                UpdateUI();

                var sessions = Sessions.Where(s => s.Selected).ToList();

                if (sessions.Count > 0)
                {
                    var jobs = new List<Job>();

                    if (DownloadVideos)
                        jobs.AddRange(GetJobs(sessions));

                    if (jobs.Count > 0)
                        await DownloadFilesAsync(jobs);
                }
            }
        },
        () => Sessions.Any(s => s.Selected) || Downloading);

        public RelayCommand<Session> PlayVideoCommand => new RelayCommand<Session>(
            session =>
            {
                if (!Shell.Execute(session.GetFullPath(MiscHelpers.GetFolder())))
                    session.HasVideo = false;
            });

        public RelayCommand ClearFilterTextCommand => new RelayCommand(() => FilterText = "");

        public RelayCommand HelpCommand => new RelayCommand(() => OnHelp?.Invoke(this, EventArgs.Empty));
    }
}
