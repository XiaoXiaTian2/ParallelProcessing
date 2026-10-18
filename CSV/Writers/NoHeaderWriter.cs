using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV.Writers
{
    internal class NoHeaderWriter : AWriter
    {
        public override void AddData<T>(string path, List<T> model)
        {
            var properties = typeof(T).GetProperties();

            StreamWriter streamWriter = new StreamWriter(path, true, Encoding.UTF8);
            streamWriter.WriteLine(String.Join(",", properties.Select(p => p.Name)));
            foreach (var row in model)
            {
                streamWriter.WriteLine(String.Join(",", properties.Select(p => p.GetValue(row)).ToList()));
            }
            streamWriter.Flush();
            streamWriter.Close();
        }
    }
}
