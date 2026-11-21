using CSV.Enums;
using CSV.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace CSV
{
    public class CSVHelper
    {
        delegate void SetterDelegate(object data, object value);

        static SetterDelegate CreateSetter(PropertyInfo property)
        {
            var targetParm = Expression.Parameter(typeof(object));
            var valueParm = Expression.Parameter(typeof(object));

            Expression castTarget = Expression.Convert(targetParm, property.DeclaringType);
            Expression castvalue = Expression.Convert(valueParm, property.PropertyType);

            MethodCallExpression methodCallExpression = Expression.Call(castTarget, property.GetSetMethod(), castvalue);

            SetterDelegate setter = Expression.Lambda<SetterDelegate>(methodCallExpression, targetParm, valueParm).Compile();

            return setter;
        }

        delegate object GetterDelegate(object value);

        static GetterDelegate CreateGetter(PropertyInfo property)
        {
            var valueParm = Expression.Parameter(typeof(object));
            Expression castvalue = Expression.Convert(valueParm, property.DeclaringType);

            MethodCallExpression methodCallExpression = Expression.Call(castvalue, property.GetGetMethod());

            GetterDelegate getter = Expression.Lambda<GetterDelegate>(methodCallExpression, valueParm).Compile();

            return getter;
        }

        // 每個 T 各自一份快取，CLR 保證型別初始化只執行一次且執行緒安全
        static class Accessors<T>
        {
            public static readonly PropertyInfo[] Properties = typeof(T).GetProperties();
            public static readonly SetterDelegate[] Setters = Properties.Select(CreateSetter).ToArray();
            public static readonly GetterDelegate[] Getters = Properties.Select(CreateGetter).ToArray();
        }

        public static void Write<T>(string path, T obj)
        {
            string[] splitPath = path.Split('/');
            string dirPath = String.Join("/", splitPath.Take(splitPath.Length - 1));
            string fileName = splitPath[splitPath.Length - 1];
            string extName = fileName.Split('.')[1];
            if (extName != "csv")
            {
                throw new ArgumentException("必須使用 csv 檔案", nameof(path));
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
                throw new ArgumentException("必須使用 csv 檔案", nameof(path));
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
                throw new FileNotFoundException("檔案不存在", path);
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
                            throw new InvalidDataException("檔案欄位不符");
                        }
                        isHeader = true;
                        dataSetColumn.Add(col);
                    }
                    continue;
                }

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

        public static List<T> Read<T>(string path, int startIndex, int batchSize) where T : class, new()
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("檔案不存在", path);
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
                            throw new InvalidDataException("檔案欄位不符");
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

        public static List<T> OptimizeRead<T>(string path) where T : class, new()
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("檔案不存在", path);
            }

            List<T> objs = new List<T>();

            StreamReader reader = new StreamReader(path, System.Text.Encoding.UTF8);
            SetterDelegate[] setters = Accessors<T>.Setters;

            // 跳過標題列
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                T obj = new T();
                string line = reader.ReadLine();
                ReadOnlySpan<char> data = line.AsSpan();

                int fieldStart = 0;
                int length = 0;
                int fieldIndex = 0;

                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == ',' || i == data.Length - 1)
                    {
                        setters[fieldIndex](obj, data.Slice(fieldStart, data[i] == ',' ? length : length + 1).ToString());
                        length = 0;
                        fieldIndex++;
                        fieldStart = i + 1;
                    }
                    else
                    {
                        length++;
                    }
                }

                objs.Add(obj);
            }
            reader.Close();

            return objs;
        }

        public static List<T> OptimizeRead<T>(string path, int startIndex, int batchSize) where T : class, new()
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("檔案不存在", path);
            }

            List<T> objs = new List<T>();

            StreamReader reader = new StreamReader(path, System.Text.Encoding.UTF8);
            SetterDelegate[] setters = Accessors<T>.Setters;

            // 跳過標題列
            reader.ReadLine();

            int dataIndex = 0;
            while (!reader.EndOfStream && objs.Count < batchSize)
            {
                string line = reader.ReadLine();

                if (dataIndex < startIndex)
                {
                    dataIndex++;
                    continue;
                }

                dataIndex++;

                T obj = new T();
                int fieldStart = 0;
                int length = 0;
                int fieldIndex = 0;
                ReadOnlySpan<char> data = line.AsSpan();

                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == ',' || i == data.Length - 1)
                    {
                        setters[fieldIndex](obj, data.Slice(fieldStart, data[i] == ',' ? length : length + 1).ToString());
                        length = 0;
                        fieldIndex++;
                        fieldStart = i + 1;
                    }
                    else
                    {
                        length++;
                    }
                }

                objs.Add(obj);
            }
            reader.Close();

            return objs;
        }

        public static void OptimizeWrite<T>(string path, T obj)
        {
            string[] splitPath = path.Split('/');
            string dirPath = String.Join("/", splitPath.Take(splitPath.Length - 1));

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


            GetterDelegate[] getters = Accessors<T>.Getters;
            StringBuilder builder = new StringBuilder(90);
            for (int i = 0; i < getters.Length; i++)
            {
                builder.Append(getters[i](obj));
                if (i < getters.Length - 1)
                    builder.Append(",");
            }
            char[] buffer = new char[builder.Length];
            builder.CopyTo(0, buffer, 0, builder.Length);
            bool isEmptyFile = new FileInfo(path).Length == 0;
            StreamWriter streamWriter = new StreamWriter(path, true, Encoding.UTF8);
            if (isEmptyFile)
                streamWriter.WriteLine(String.Join(",", Accessors<T>.Properties.Select(p => p.Name)));
            streamWriter.WriteLine(buffer, 0, builder.Length);
            builder.Clear();
            streamWriter.Flush();
            streamWriter.Close();
        }

        public static void OptimizeWrite<T>(string path, List<T> objList)
        {
            string[] splitPath = path.Split('/');
            string dirPath = String.Join("/", splitPath.Take(splitPath.Length - 1));

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            if (!File.Exists(path))
            {
                FileStream stream = File.Create(path);
                stream.Close();
            }

            bool isEmptyFile = new FileInfo(path).Length == 0;
            StreamWriter streamWriter = new StreamWriter(path, true, Encoding.UTF8);
            if (isEmptyFile)
                streamWriter.WriteLine(String.Join(",", Accessors<T>.Properties.Select(p => p.Name)));

            GetterDelegate[] getters = Accessors<T>.Getters;
            StringBuilder builder = new StringBuilder(90);
            char[] buffer = new char[90];
            foreach (var obj in objList)
            {
                for (int i = 0; i < getters.Length; i++)
                {
                    builder.Append(getters[i](obj));
                    if (i < getters.Length - 1)
                        builder.Append(",");
                }
                if (buffer.Length < builder.Length)
                    buffer = new char[builder.Length];
                builder.CopyTo(0, buffer, 0, builder.Length);
                streamWriter.WriteLine(buffer, 0, builder.Length);
                builder.Clear();
            }
            streamWriter.Flush();
            streamWriter.Close();
        }
        // HW 套用並測試
    }
}
