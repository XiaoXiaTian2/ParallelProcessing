using CSV;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelProcessing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            object a = new object();

            //HW winform專案 建置完(debug) 防止雙開app => mutex實現
            int count = 15_000_000;
            int batchSize = 1_000_000;
            string inputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["InputFolder"] ?? "data");
            string inputPath = $"{inputDir}/MOCK_DATA_{count}.csv";
            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["OutputFolder"] ?? "outputData");

            // 計算分批
            int batchCount = (int)Math.Ceiling((double)count / batchSize);

            double[] readTimes = new double[batchCount];
            double[] writeTimes = new double[batchCount];
            if (Directory.Exists($"{outputDir}/{count}"))
            {
                Directory.Delete($"{outputDir}/{count}", true);
            }

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            // 每批處理，同時最多 6 批
            Parallel.For(0, batchCount, new ParallelOptions() { MaxDegreeOfParallelism = 6 }, batchNumber =>
            {
                //string outputPath = $"{outputDir}/{count}/MOCK_DATA_{count}_{batchNumber + 1}.csv";
                string outputPath = $"{outputDir}/{count}/MOCK_DATA_{count}.csv";

                Stopwatch stopwatch = Stopwatch.StartNew();
                List<DataModel> batch = CSVHelper.Read<DataModel>(inputPath, batchNumber * batchSize, batchSize);
                readTimes[batchNumber] = stopwatch.ElapsedMilliseconds / 1000.0;

                stopwatch.Restart();
                //Mutex appMutex = new Mutex(false, "MutexPractice");
                //appMutex.WaitOne();
                //appMutex.ReleaseMutex();

                lock (a)
                {
                    CSVHelper.WriteList(outputPath, batch);
                }
                writeTimes[batchNumber] = stopwatch.ElapsedMilliseconds / 1000.0;
            }); // Parallel.For 會等全部的批次都做完才往下

            totalStopwatch.Stop();

            double readTimeMedium = readTimes.Medium(x => x);
            double writeTimeMedium = writeTimes.Medium(x => x);
            double totalTime = totalStopwatch.ElapsedMilliseconds / 1000.0;
            Console.WriteLine($"| 筆數 | 總讀取時間 | 總寫入時間 | 總時間 |\n| {count.ToString("N0")} | {readTimeMedium} | {writeTimeMedium} | {totalTime} |");
            Console.ReadKey();
        }
    }
}
