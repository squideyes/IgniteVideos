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

using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace IgniteVideos
{
    internal static class MiscHelpers
    {
        public static R Funcify<T, R>(this T value, Func<T, R> getResult) => getResult(value);

        public static string GetFolder() => Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments), nameof(IgniteVideos));

        public static SessionKind ToSessionKind(this string value)
        {
            return value switch
            {
                "Ask the Experts" => SessionKind.AskTheExperts,
                "Connection Zone" => SessionKind.ConnectZone,
                "Digital Breakout" => SessionKind.Breakout,
                "Microsoft Ignite Live" => SessionKind.IgniteLive,
                "Pre-Recorded for On Demand" => SessionKind.PreRecorded,
                "Key Segment" => SessionKind.KeySegment,
                "Learning Zone" => SessionKind.LearningZone,
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };
        }

        public static string GetDescription(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false)
                is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }

        private static readonly DateTimeZone pacificZone =
            DateTimeZoneProviders.Tzdb.GetZoneOrNull("US/Pacific");

        public static DateTime ToPacificFromUtc(this DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentOutOfRangeException(nameof(value));

            return Instant.FromDateTimeUtc(value)
                .InZone(pacificZone).ToDateTimeUnspecified();
        }

        public static List<string> ToLines(this string value)
        {
            var reader = new StringReader(value);

            var lines = new List<string>();

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lines.Add(line);
            }

            return lines;
        }

        public static string ToSingleLine(this string value)
        {
            var lines = value.ToLines();

            if (lines.Count == 0)
                return string.Empty;

            return string.Join("; ", lines);
        }
    }
}
