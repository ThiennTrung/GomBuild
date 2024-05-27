using GomBuild.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GomBuild.Model
{
    public class TypeFile
    {
        public TypeFile() { }
        public int ID { get; set; }
        public string NAME { get; set; }

        public TypeFile(int ID, string NAME)
        {
            this.ID = ID;
            this.NAME = NAME;
        }

    }

    enum _TypeFile
    {
        Stored = 0, 
        StoredNonReport = 1,
        Report = 2,
        Form = 3,
        Script = 4,
        Template = 5,
        Other = 6
    }
}
