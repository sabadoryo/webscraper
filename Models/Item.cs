using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Endterm.Models
{
    public class Item
    {
        [Key]
        public int ItemId { get; set; }
        public string ItemUrl { get; set; }
        public string Name { get; set; }
        public string Descriptipn { get; set; }
        public uint Price { get; set; }
    }
}
