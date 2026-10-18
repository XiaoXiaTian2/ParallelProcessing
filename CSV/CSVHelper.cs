using CSV.Enums;
using CSV.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSV
{
    public class CSVHelper
    {
        public static void Write<T>(string path, T obj)
        {
            string[] splitPath = path.Split('/');
            string dirPath = String.Join("/", splitPath.Take(splitPath.Length - 1));
            string fileName = splitPath[splitPath.Length - 1];
            string extName = fileName.Split('.')[1];
            if (extName != "csv")
            {
                throw new Exception("必須使用 csv 檔案");
            }

            if (splitPath.Length > 1)
            {
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
            }

            if (!File.Exists(path))
            {
                FileStream stream = File.Create(path);
                stream.Close();
            }

            //var properties = obj.GetType().GetProperties();
            HeaderType type = HeaderManager.CheckHeader<T>(path);
            Type tp = Type.GetType($"CSV.Writers.{type.ToString()}Writer");
            AWriter writer = (AWriter)Activator.CreateInstance(tp);
            writer.AddData(path, new List<T>() { obj });
            //WriteHeader(path, properties.Select(p => p.Name).ToList());

            //StreamWriter streamWriter = new StreamWriter(path, true);
            //var values = properties.Select(p => p.GetValue(obj)).ToList();
            //string data = String.Join(",", values);
            //streamWriter.WriteLine(data);
            //streamWriter.Flush();
            //streamWriter.Close();
        }

        public static void WriteList<T>(string path, List<T> objList)
        {
            string[] splitPath = path.Split('/');
            string dirPath = String.Join("/", splitPath.Take(splitPath.Length - 1));

            string fileName = splitPath[splitPath.Length - 1];
            string extName = fileName.Split('.')[1];
            if (extName != "csv")
            {
                throw new Exception("必須使用 csv 檔案");
            }

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            if (!File.Exists(path))
            {
                FileStream stream = File.Create(path);
                stream.Close();
            }

            HeaderType type = HeaderManager.CheckHeader<T>(path);
            Type tp = Type.GetType($"CSV.Writers.{type.ToString()}Writer");
            AWriter writer = (AWriter)Activator.CreateInstance(tp);
            writer.AddData(path, objList);

            //StreamWriter streamWriter = new StreamWriter(path, true);
            //foreach (var obj in objList)
            //{
            //    var values = obj.GetType()
            //        .GetProperties()
            //        .Select(p => p.GetValue(obj))
            //        .ToList();
            //    string data = String.Join(",", values); Console.WriteLine(data);
            //    streamWriter.WriteLine(data);
            //}
            //streamWriter.Flush();
            //streamWriter.Close();
        }

        public static List<T> Read<T>(string path) where T : class, new()
        {
            if (!File.Exists(path))
            {
                throw new Exception("檔案不存在");
            }

            List<T> objs = new List<T>();

            StreamReader reader = new StreamReader(path, System.Text.Encoding.UTF8);
            var properties = typeof(T).GetProperties();
            Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
            for (int i = 0; i < properties.Length; i++)
            {
                keyValuePairs.Add(properties[i].Name, i);
            }

            bool isHeader = false;
            List<string> dataSetColumn = new List<string>();
            while (!reader.EndOfStream)
            {
                T obj = new T();
                string line = reader.ReadLine();
                string[] data = line.Split(',');
                if (!isHeader)
                {
                    foreach (var col in data)
                    {
                        if (!keyValuePairs.TryGetValue(col, out int idx))
                        {
                            throw new Exception("檔案欄位不符");
                        }
                        isHeader = true;
                        dataSetColumn.Add(col);
                    }
                    continue;
                }

                //if (line == String.Join(",", properties.Select(p => p.Name)))
                //    continue;

                Dictionary<string, string> dataColumn = new Dictionary<string, string>();
                for (int i = 0; i < data.Length; i++)
                {
                    dataColumn.Add(dataSetColumn[i], data[i]);
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i].SetValue(obj, dataColumn[properties[i].Name]);
                }
                objs.Add(obj);
            }
            reader.Close();


            return objs;
        }

        //HW 完成func
        public static List<T> Read<T>(string path, int startIndex, int batchSize) where T : class, new()
        {
            if (!File.Exists(path))
            {
                throw new Exception("檔案不存在");
            }

            List<T> objs = new List<T>();

            StreamReader reader = new StreamReader(path, System.Text.Encoding.UTF8);
            var properties = typeof(T).GetProperties();
            Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
            for (int i = 0; i < properties.Length; i++)
            {
                keyValuePairs.Add(properties[i].Name, i);
            }

            bool isHeader = true;
            int dataIndex = 0;
            List<string> dataSetColumn = new List<string>();

            while (!reader.EndOfStream && objs.Count < batchSize)
            {
                string line = reader.ReadLine();
                if (isHeader)
                {
                    string[] data2 = line.Split(',');

                    foreach (var col in data2)
                    {
                        if (!keyValuePairs.TryGetValue(col, out int idx))
                        {
                            throw new Exception("檔案欄位不符");
                        }
                        isHeader = false;
                        dataSetColumn.Add(col);
                    }

                    continue;
                }
                if (dataIndex < startIndex)
                {
                    dataIndex++;
                    continue;
                }

                string[] data = line.Split(',');
                dataIndex++;

                T obj = new T();
                Dictionary<string, string> dataColumn = new Dictionary<string, string>();
                for (int i = 0; i < data.Length; i++)
                {
                    dataColumn.Add(dataSetColumn[i], data[i]);
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i].SetValue(obj, dataColumn[properties[i].Name]);
                }
                objs.Add(obj);
            }
            reader.Close();

            return objs;
        }
    }
}
