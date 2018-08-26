using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyIdServer.Models
{
    public class ConsentModel
    {
        public string ReturnUrl { get; set; }
        public bool AgreesBlindlyToEverything { get; set; }
    }
}
