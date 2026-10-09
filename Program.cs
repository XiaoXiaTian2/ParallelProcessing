using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSV;

namespace ParallelProcessing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            object a = new object();

            //HW winform專案 建置完(debug) 防止雙開app => mutex實現
            int count = 15_000_000;
            int batchSize = 1_000_000;
            string inputPath = $"E:/csharp_course/data/MOCK_DATA_{count}.csv";
            string outputDir = "E:/csharp_course/outputData";

            // 計算分批
            int batchCount = (int)Math.Ceiling((double)count / batchSize);

            double[] readTimes = new double[batchCount];
            double[] writeTimes = new double[batchCount];
            if (Directory.Exists($"{outputDir}/{count}"))
            {
                Directory.Delete($"{outputDir}/{count}", true);
            }

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < batchCount; i++)
            {
                int batchNumber = i;

                // 每批處理
                Task task = Task.Run(() =>
                {
                    //string outputPath = $"{outputDir}/{count}/MOCK_DATA_{count}_{batchNumber + 1}.csv";
                    string outputPath = $"{outputDir}/{count}/MOCK_DATA_{count}.csv";

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    List<DataModel> batch = CSVHelper.Read<DataModel>(inputPath, batchNumber * batchSize, batchSize);
                    readTimes[batchNumber] = stopwatch.ElapsedMilliseconds / 1000.0;

                    stopwatch.Restart();
                    lock (a)
                    {
                        CSVHelper.WriteList(outputPath, batch);
                    }
                    writeTimes[batchNumber] = stopwatch.ElapsedMilliseconds / 1000.0;
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks); // 等全部的批次都做完
            totalStopwatch.Stop();

            double readTimeMedium = readTimes.Medium(x => x);
            double writeTimeMedium = writeTimes.Medium(x => x);
            double totalTime = totalStopwatch.ElapsedMilliseconds / 1000.0;
            Console.WriteLine($"| 筆數 | 總讀取時間 | 總寫入時間 | 總時間 |\n| {count.ToString("N0")} | {readTimeMedium} | {writeTimeMedium} | {totalTime} |");
            Console.ReadKey();
        }
    }
}
