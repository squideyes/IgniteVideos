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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IgniteVideos
{
    public class IgniteApiHelper
    {
        private class Root
        {
            public Data[] Data { get; set; }
            public int Total { get; set; }
        }

        private static readonly HttpClient client = new HttpClient();

        private static readonly Regex repeatRegex = new Regex(@"-R\d$", RegexOptions.Compiled);

        public async Task<List<Data>> GetSessionsAsync()
        {
            static bool IsValid(Data data)
            {
                if (repeatRegex.IsMatch(data.SessionCode))
                    return false;

                if (data.SessionType == SessionKind.PreRecorded)
                    return true;

                var minDate = DateTime.UtcNow.ToPacificFromUtc().Date;

                return data.StartDateTime.HasValue && data.StartDateTime.Value.Date <= minDate;
            }

            var uri = new Uri("https://api.myignite.microsoft.com/api/session/search");

            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var request = new Request();

            var content = new StringContent(
                JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri, content);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var root = JsonSerializer.Deserialize<Root>(json, options);

            return root.Data.Where(d => IsValid(d)).ToList();
        }
    }
}
