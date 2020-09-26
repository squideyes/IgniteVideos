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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IgniteVideos
{
    internal class Job
    {
        private const int BUFFER_SIZE = 1024 * 1024 * 4;

        private static readonly HttpClient client = new HttpClient();

        public Job(Session session)
        {
            Session = session;
        }

        public Session Session { get; }

        public event EventHandler<ProgressArgs> OnProgress;

        public async Task<bool> DownloadAndSaveAsync(CancellationToken cancellationToken)
        {
            var folder = MiscHelpers.GetFolder();

            var startedOn = DateTime.UtcNow;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fullPath = Session.GetFullPath(folder);

            var buffer = new byte[BUFFER_SIZE];

            var response = await client.GetAsync(Session.VideoUri,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            if (cancellationToken.IsCancellationRequested)
                return false;

            var target = new MemoryStream();

            var fileSize = response.Content.Headers.ContentLength.Value;

            if (fileSize == 22)
            {
                OnProgress?.Invoke(this, new ProgressArgs(22, true));

                return false;
            }

            using (var source = await response.Content.ReadAsStreamAsync())
            {
                int bytesRead;

                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        return false;

                    bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        target.Write(buffer, 0, bytesRead);

                        OnProgress?.Invoke(this, new ProgressArgs(bytesRead, false));
                    }
                }
                while (bytesRead != 0);

                OnProgress?.Invoke(this, new ProgressArgs(bytesRead, true));
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            target.Position = 0;

            using var saveTo = File.Open(fullPath, FileMode.Create);

            await target.CopyToAsync(saveTo, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return false;

            return true;
        }
    }
}
