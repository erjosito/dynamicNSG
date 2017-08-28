using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dynamicNSG.Models
{
    public class NSGrule
    {
        public int Id { get; set; }
        public string nsgName { get; set; }
        [RegularExpression("inbound|outbound")]
        public string direction { get; set; }
        public int order { get; set; }
        [RegularExpression("permit|deny")]
        public string action { get; set; }
        public string srcIp { get; set; }
        public string srcProt { get; set; }
        public string srcPort { get; set; }
        public string dstIp { get; set; }
        public string dstProt { get; set; }
        public string dstPort { get; set; }
    }
}