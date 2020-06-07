using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Analytics
{
	public struct NetworkData
	{
		public DateTime Date;
		public long Duration;
		public float Mbps;
		public string Message;

		public static List<NetworkData> Parse(string filename)
		{
			var results = new List<NetworkData>();
			foreach(var line in File.ReadAllLines(filename))
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				var parts = line.Split('\t');
				if (parts.Length < 3) throw new Exception("Invalid input line : " + line);
				results.Add( new NetworkData()
				{
					Date = DateTime.Parse(parts[0]),
					Duration = Convert.ToInt64(parts[1]),
					Mbps = Convert.ToSingle(parts[2]),
					Message = (parts.Length > 3) ? parts[3] : ""
				});
			}

			return results;
		}
	}

	public class NetworkStats
	{
		public NetworkStats(DateTime date)
		{
			Date = date;
			MaxDate = default(DateTime);
			MinDate = DateTime.Now.AddDays(365);
			MaxDuration = 0;
			MinDuration = Int64.MaxValue;
			MaxMbps = 0f;
			MinMbps = Single.MaxValue;
		}

		public DateTime Date { get; private set; }

		// stats
		public DateTime MinDate { get; private set; }
		public DateTime MaxDate { get; private set; }
		public long MinDuration { get; private set; }
		public long MaxDuration { get; private set; }
		public float MinMbps { get; private set; }
		public float MaxMbps { get; private set; }

		// counts
		public long Count { get; private set; }
		public long ErrorCount { get; private set; }

		// total
		public long Duration { get; private set; }
		public float Mbps { get; private set; }
		public long ErrorDuration { get; private set; }
		public float WallClock { get { return (float)((MaxDate - MinDate).TotalMilliseconds); } }
		public float Fitness { get { return (float)Duration/(float)WallClock; } } 

		// average
		public float AvgDuration { get { return (float)Duration / (float)Count; } }
		public float AvgMbps { get { return (float)Mbps / (float)Count; } }
		public float AvgErrors { get { return (float)ErrorCount / (float)(ErrorCount+Count); } }
		public float AvgErrorDuration  { get { return ErrorCount == 0 ? 0f : (float)ErrorDuration / (float)ErrorCount; } }

		public void Add(NetworkData data)
		{
			if (data.Date < MinDate) MinDate = data.Date;
			if (data.Date > MaxDate) MaxDate = data.Date;

			if (data.Mbps < 0)
			{
				// this is an error
				if (string.IsNullOrWhiteSpace(data.Message)) throw new Exception("Error without a message");
				ErrorCount++;
				ErrorDuration += data.Duration;
			}
			else
			{
				// this is a success case
				if (!string.IsNullOrWhiteSpace(data.Message)) throw new Exception("Success with a message");
				Count++;
				Duration += data.Duration;
				Mbps += data.Mbps;

				if (data.Duration < MinDuration) MinDuration = data.Duration;
				if (data.Duration > MaxDuration) MaxDuration = data.Duration;
				if (data.Mbps < MinMbps) MinMbps = data.Mbps;
				if (data.Mbps > MaxMbps) MaxMbps = data.Mbps;
			}
		}
	}

	public enum TimeOfDay { Second, Minute, Hour, Day };
	
	public class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("./analytics InputDirectory <Day|Hour|Minute|Second>");
				Console.WriteLine("  this tool will parse the *.tsv files");
				Console.WriteLine("  and provide analytics");
				return 1;
			}

			// check that the TimeOfDay has the first letter upper case
			var todstr = args[1].ToLower();
			todstr = Char.ToUpper(todstr[0]) + todstr.Substring(1);
	
			// input
			var path = args[0];
			var tod = Enum.Parse<TimeOfDay>(todstr);

			// load data
			var results = new Dictionary<string, List<NetworkData>>();
			foreach(var file in Directory.GetFiles(path, "*.tsv"))
			{
				Console.WriteLine($"Opening {file}...");
				var data = NetworkData.Parse(file);
				results.Add(Path.GetFileNameWithoutExtension(file), data);
			}

			// gather day long stats
			DisplayHeader();
			foreach(var kvp in results)
			{
				var stats = Analyze(kvp.Value, tod);
				Display(kvp.Key, stats);
			}

			return 0;
		}

		private static void DisplayHeader()
		{
			Console.WriteLine("Name\tDate\tMin_Date\tMax_Date\tWallClock\tFitness\tCount\tError_Count\tTotal_Duration\tTotal_Mbps\tTotal_ErrorDuration\tAvg_Duration\tAvg_Mbps\tAvg_Errors\tAvg_ErrorDuration\tMin_Duration\tMax_Duration\tMin_Mbps\tMax_Mbps");
		}

		private static void Display(string name, List<NetworkStats> stats)
		{
			foreach(var stat in stats)
			{
				Console.WriteLine($"{name}\t{stat.Date:o}\t{stat.MinDate:o}\t{stat.MaxDate:o}\t{stat.WallClock}\t{stat.Fitness}\t{stat.Count}\t{stat.ErrorCount}\t{stat.Duration}\t{stat.Mbps}\t{stat.ErrorDuration}\t{stat.AvgDuration}\t{stat.AvgMbps}\t{stat.AvgErrors}\t{stat.AvgErrorDuration}\t{stat.MinDuration}\t{stat.MaxDuration}\t{stat.MinMbps}\t{stat.MaxMbps}");
			}
		}

		private static List<NetworkStats> Analyze(List<NetworkData> input, TimeOfDay tod)
		{
			var results = new Dictionary<DateTime,NetworkStats>();
			foreach(var data in input)
			{
				// get the timeframe
				DateTime date = default(DateTime);
				switch(tod)
				{
					case TimeOfDay.Day: date = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day, hour: 0, minute: 0, second: 0, millisecond: 0); break;
					case TimeOfDay.Hour: date = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day, hour: data.Date.Hour, minute: 0, second: 0, millisecond: 0); break;
					case TimeOfDay.Minute: date = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day, hour: data.Date.Hour, minute: data.Date.Minute, second: 0, millisecond: 0); break;
					case TimeOfDay.Second: date = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day, hour: date.Date.Hour, minute: data.Date.Minute, second: data.Date.Second, millisecond: 0); break;
					defalt: throw new Exception("Unknown timeofday : " + tod);
				}

				if (!results.TryGetValue(date, out NetworkStats stats))
				{
					stats = new NetworkStats(date);
					results.Add(date, stats);
				}
				stats.Add(data);
			}

			return results.Values.ToList();
		}
	}
}
