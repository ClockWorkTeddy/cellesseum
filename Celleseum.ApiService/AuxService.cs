using Microsoft.EntityFrameworkCore.Query;
using System.Text;

namespace Celleseum.ApiService
{
    public class AuxService
    {
        public int Width;
        public int Height;

        public void PrintData(List<Dictionary<int, int>> data)
        {
            if (File.Exists("output.txt"))
            {
                File.Delete("output.txt");
            }
            using (var streamWriter = new StreamWriter("output.txt"))
            {
                foreach (var dict in data)
                {
                    var line = CombineArea(dict);
                    streamWriter.WriteLine(line);
                }
                streamWriter.Close();
            }

        }

        public string CombineArea(Dictionary<int, int> area)
        {
            var sb = new StringBuilder();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var key = y * Width + x;
                    if (area.ContainsKey(key))
                    {
                        sb.Append(area[key].ToString());
                    }
                    else
                    {
                        sb.Append(0.ToString());
                    }
                    sb.Append(',');
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
