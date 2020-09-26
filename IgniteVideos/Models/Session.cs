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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IgniteVideos
{
    public class Session : ObservableObject
    {
        private bool selected = false;
        private bool hasVideo = false;
        private bool isFavorite = false;

        public event EventHandler OnIsFavoriteChanged;
        public event EventHandler<SelectionChangedArgs> OnSelectionChanged;

        public bool IsFavorite
        {
            get => isFavorite;
            set
            {
                Set(ref isFavorite, value);

                OnIsFavoriteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                Set(ref selected, value);

                OnSelectionChanged?.Invoke(this, new SelectionChangedArgs(value));
            }
        }

        public Guid SessionId { get; set; }
        public string Code { get; set; }
        public DateTime? PubDate { get; set; }
        public Uri SessionUri { get; set; }
        public Uri StreamUri { get; set; }
        public Uri VideoUri { get; set; }
        public TimeSpan Duration { get; set; }
        public string Title { get; set; }
        public string Synopsis { get; set; }
        public SessionKind Kind { get; set; }
        public List<Speaker> Speakers { get; set; }

        public bool HasVideo
        {
            get => hasVideo;
            set => Set(ref hasVideo, value);
        }

        public bool CanFetchVideo => !HasVideo;

        public string Talent =>
            string.Join(",", Speakers.Select(s => s.DisplayName));

        public string GetCleanFileName()
        {
            return Path.GetInvalidFileNameChars().Aggregate(Code + "-" + Title,
                (current, c) => current.Replace(c.ToString(), " ")) + ".mp4";
        }

        public string GetFullPath(string folder) =>
            Path.Combine(folder, GetCleanFileName());

        public override string ToString() => Code + " - " + Title;
    }
}
