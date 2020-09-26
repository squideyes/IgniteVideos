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
using System.Text.Json.Serialization;

namespace IgniteVideos
{
    public class Data
    {
        [JsonConverter(typeof(JsonStringSessionTypeConverter))]
        public SessionKind SessionType { get; set; }

        public Guid SessionId { get; set; }
        public string SessionCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Speaker[] Speakers { get; set; }
        public DateTime? StartDateTime { get; set; }
        public string Level { get; set; }
        public int DurationInMinutes { get; set; }

        public Uri SessionUri =>
            new Uri($"https://myignite.microsoft.com/sessions/{SessionId}");

        public Uri StreamUri => new Uri(
            $"https://medius.studios.ms/embed/video-nc/IG20-{SessionCode}");

        public Uri VideoUri => new Uri(
            $"https://medius.studios.ms/video/asset/HIGHMP4/IG20-{SessionCode}");
    }
}
