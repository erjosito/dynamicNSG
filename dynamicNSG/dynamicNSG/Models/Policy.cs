using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace dynamicNSG.Models
{
    public class Policy
    {
        public int Id { get; set; }

        [DisplayName("Sequence")]
        public int Order { get; set; }

        [DisplayName("Action")]
        [RegularExpression("permit|deny", ErrorMessage = "Can be one of these: permit, deny")]
        public string Action { get; set; }

        [DisplayName("Source")]
        public string Src { get; set; }

        [DisplayName("Destination")]
        public string Dst { get; set; }

        [DisplayName("Protocol")]
        [RegularExpression("tcp|udp", ErrorMessage = "Can be one of these: tcp, udp")]
        public string Prot { get; set; }

        [DisplayName("Port Range")]
        [RegularExpression("^[0-9]+-[0-9]+$", ErrorMessage = "Needs to have the format 80-81. Enter 22-22 for a single port")]
        public string Range { get; set; }


    }
}