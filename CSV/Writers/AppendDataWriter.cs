using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV.Writers
{
    internal class AppendDataWriter : AWriter
    {
        public override void AddData<T>(string path, List<T> model)
        {
            var properties = typeof(T).GetProperties();

            StreamWriter streamWriter = new StreamWriter(path, true, Encoding.UTF8);
            foreach (var row in model)
            {
                string modelData = String.Join(",", properties.Select(p => p.GetValue(row)));
                streamWriter.WriteLine(modelData);
            }

            streamWriter.Flush();
            streamWriter.Close();
        }
    }
}
