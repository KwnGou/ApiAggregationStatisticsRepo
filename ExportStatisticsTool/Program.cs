// Command line stuff
using ExportStatisticsTool.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text;

namespace ExportStatisticsTool
{

    public class Program
    {
        static int Main(string[] args)
        {
            string exportPath = string.Empty;
            string exportFileName = string.Empty;

            RootCommand root = new RootCommand()
            {
                Description = "Exports statistics from aggragation API."
            };

            Command generateStatistics = new Command("generate", "Generate statistics file");
            Option<bool> interactive = new Option<bool>("-i", "--interactive")
            {
                Description = "Shows progress information (default: silent)",
                DefaultValueFactory = parseResult => false
            };
            generateStatistics.Options.Add(interactive);

            Option<DateTime> fromDate = new Option<DateTime>("-f", "--from-date")
            {
                Description = "Start date for statistics (default: 7 days ago)",
                DefaultValueFactory = parseResult => DateTime.Today.AddDays(-7),
                Required = false
            };
            generateStatistics.Options.Add(fromDate);

            Option<DateTime> toDate = new Option<DateTime>("-t", "--to-date")
            {
                Description = "End date for statistics (default: today)",
                DefaultValueFactory = parseResult => DateTime.Today,
                Required = false
            };
            generateStatistics.Options.Add(toDate);

            generateStatistics.SetAction(parseResult =>
            {
                IConfiguration config = null;
                var interactive = parseResult.GetValue<bool>("-i");
                try
                {
                    // get configuration
                    config = new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json", false, true)
                       .Build();
                    // get options
                    var exportSettings = config.GetSection("Output").Get<OutputSettings>();
                    exportPath = exportSettings.Path;
                    exportFileName = exportSettings.FileName;
                }
                catch (Exception ex)
                {
                    ReportError(interactive, $"Error when reading configuration: {ex.Message}");
                    Console.ReadLine();
                    return -1;
                }

                ReportImportant(interactive, "Processing Statistics ...");
                var fn = string.Format(exportFileName, DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"), DateTime.Today.ToString("yyyy-MM-dd"));
                var ffn = $"{exportPath}\\{fn}";
                //Console.WriteLine($"Exporting statistics to {ffn}");
                try
                {
                    DbContextOptionsBuilder<AppDbContext> builder = new DbContextOptionsBuilder<AppDbContext>();
                    var conStr = config.GetConnectionString("ApiAggregationDB");
                    var options = builder.UseSqlServer(conStr).Options;

                    using var ctx = new AppDbContext(options);
                    var from = parseResult.GetValue<DateTime>("-f");
                    var to = parseResult.GetValue<DateTime>("-t");
                    var data = new List<Statistic>();
                    //if (to < from)
                    //{
                    //    data = null;
                    //}
                    //else
                    //{
                        // if no dates given, get data using default date range (last 7 days)
                        data = ctx.Statistics
                            .Where(s => s.RequestDate >= DateOnly.FromDateTime(from) &&
                                        s.RequestDate <= DateOnly.FromDateTime(to))
                            .ToList();
                    //}
                    CsvWriter.WriteCsv(ffn, data);
                }
                catch (Exception ex)
                {
                    ReportError(interactive, $"Error when connecting to database: {ex.Message}");
                    Console.ReadLine();
                    return -2;
                }

                //return exit code
                ReportImportant(interactive, "\r\nOperation completed successfully.");
                return 0;
            });

            root.Subcommands.Add(generateStatistics);

            // Do it
            ParseResult parseResult = root.Parse(args);
            if (parseResult.Errors.Count == 0)
            {
                var parsedCommand = parseResult.CommandResult.Command;

                if (parsedCommand != root)
                {
                    generateStatistics.Parse(args).Invoke();
                }
                else // root (with -h)
                {
                    Console.WriteLine(string.Format("ExportStatisticsTool version {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion));
                    Console.WriteLine("Syntax: ExportStatisticsTool generate");
                    Console.WriteLine("Options:");
                    Console.WriteLine("   -i, --interactive: Shows progress information (default: silent)");
                    Console.WriteLine("   -f, --from-date: Start date for statistics (default: 7 days ago)");
                    Console.WriteLine("   -t, --to-date: End date for statistics (default: today)");
                    Console.WriteLine("Return values:");
                    Console.WriteLine(" 0: Success");
                    Console.WriteLine("-1: Incorrect configuration file. Run in interactive mode to see the exact error");
                    Console.WriteLine("-2: Failure when processing database data. Run in interactive mode to see the exact error");
                    Console.WriteLine("-3: No data available.");
                    Console.WriteLine(" 1: Command line error");
                }
            }
            foreach (ParseError parseError in parseResult.Errors)
            {
                Console.Error.WriteLine(parseError.Message);
                Console.Error.WriteLine("ExportStatisticsTool -h: shows syntax");
                Console.ReadLine();
                return 1;
            }
            Console.ReadLine();
            return 0;
        }
        #region Info/log functions

        public static void Report(bool verbose, string msg, bool noNewLine = false)
        {
            if (verbose)
            {
                if (noNewLine)
                {
                    Console.Write(msg);
                }
                else
                {
                    Console.WriteLine(msg);
                }
            }
        }

        public static void ReportImportant(bool verbose, string msg, bool noNewLine = false)
        {
            if (verbose)
            {
                var fg = Console.ForegroundColor;
                var bg = Console.BackgroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Report(verbose, msg, noNewLine);
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }
        }

        public static void ReportError(bool verbose, string msg, bool noNewLine = false)
        {
            if (verbose)
            {
                var fg = Console.ForegroundColor;
                var bg = Console.BackgroundColor;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Report(verbose, msg, noNewLine);
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }
        }
        #endregion
        public static class CsvWriter
        {
            public static void WriteCsv<T>(string filePath, IEnumerable<T> items)
            {
                var props = typeof(T).GetProperties();
                var sb = new StringBuilder();

                // Header
                sb.AppendLine(string.Join(",", props.Select(p => p.Name)));

                // Rows
                foreach (var item in items)
                {
                    var values = props.Select(p =>
                    {
                        var value = p.GetValue(item);
                        if (value == null) return "";

                        var str = value.ToString().Replace("\"", "\"\"");
                        return str.Contains(",") ? $"\"{str}\"" : str;
                    });

                    sb.AppendLine(string.Join(",", values));
                }

                File.WriteAllText(filePath, sb.ToString());
            }
        }
    }
}