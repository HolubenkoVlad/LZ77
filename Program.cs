using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LZ77
{
    class Program
    {
        static void Main(string[] args)
        {
            LZ77 lz77 = new LZ77();
            lz77.Encode();
            lz77.unpack();
            Console.ReadKey();
        }
    }
}
