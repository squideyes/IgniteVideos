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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IgniteVideos
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var dialog = new LoadingDialog();

            dialog.Show();

            var sessions = new List<Session>();

            Task.Run(async () => sessions.AddRange(await GetSessionsAsync()))
                .ConfigureAwait(true)
                .GetAwaiter()
                .OnCompleted(() =>
                {
                    dialog.Close();

                    var vm = new MainWindowViewModel(sessions);

                    var window = new MainWindow { DataContext = vm };

                    window.Closed += (s, e) =>
                    {
                        vm.SaveFavorites();

                        Current.Shutdown();
                    };

                    vm.OnHelp += (s, e) => 
                    {
                        var vm = new HelpDialogViewModel();

                        var dialog = new HelpDialog
                        {
                            DataContext = vm,
                            Owner = window
                        };

                        dialog.ShowDialog();
                    };

                    window.Show();
                });
        }

        private async Task<List<Session>> GetSessionsAsync()
        {
            var apiHelper = new IgniteApiHelper();

            var datas = await apiHelper.GetSessionsAsync();

            var folder = MiscHelpers.GetFolder();

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileNames = new HashSet<string>(Directory.GetFiles(folder));

            var sessions = new List<Session>();

            foreach (var data in datas)
            {
                var session = new Session()
                {
                    SessionId = data.SessionId,
                    Code = data.SessionCode,
                    Title = data.Title.ToSingleLine(),
                    Synopsis = data.Description.ToSingleLine(),
                    SessionUri = data.SessionUri,
                    VideoUri = data.VideoUri,
                    StreamUri = data.StreamUri,
                    Kind = data.SessionType,
                    Speakers = data.Speakers.ToList(),
                    PubDate = data.StartDateTime.Funcify(
                        v => v == null ? (DateTime?)null : v.Value.Date)
                };

                sessions.Add(session);

                if (fileNames.Contains(session.GetFullPath(folder)))
                    session.HasVideo = true;
            }

            return sessions;
        }
    }
}
