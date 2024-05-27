using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GomBuild.Model
{
    public class DupFile
    {
        public DupFile() { }
        public string NAME { get; set; }
        public string EXTEND { get; set; }
        public string SOURCEPATH { get; set; }
        public string BUILDPATH { get; set; }
        public DupFile(string NAME, string EXTEND, string SOURCEPATH, string BUILDPATH)
        {
            this.NAME = NAME;
            this.EXTEND = EXTEND;
            this.SOURCEPATH = SOURCEPATH;
            this.BUILDPATH = BUILDPATH;
        }
    }
}
