﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elmah.Io.ElasticSearch.ConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteElmahException(new Exception("this should log to elmah"));
            Console.WriteLine("Console finished, press enter to exit");
            Console.ReadLine();
        }

        private static void WriteElmahException(Exception ex)
        {
                var elmahCon = ErrorLog.GetDefault(null);
                elmahCon.Log(new Error(ex));
        }
    }
}
