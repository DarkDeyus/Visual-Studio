using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VisualStudent
{
    public class Error
    {
        public string Error1 { get; set; }
        public string Error2 { get; set; }
        public string Error3 { get; set; }

        public Error(string s1, string s2, string s3)
        {
            Error1 = s1;
            Error2 = s2;
            Error3 = s3;
        }
      
    }        
}
