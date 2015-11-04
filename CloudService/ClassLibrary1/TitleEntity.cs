using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerLibrary
{
    public class TitleEntity
    {
        public TitleEntity() { }
        public TitleEntity(Tuple<string, int, string, string> l)
        {
            this.Item1 = l.Item1;
            this.Item2 = l.Item2;
            this.Item3 = l.Item3;
            this.Item4 = l.Item4;
        }
        public string Item1 { get; set; }
        public int Item2 { get; set; }
        public string Item3 { get; set; }
        public string Item4 { get; set; }

    }
}
