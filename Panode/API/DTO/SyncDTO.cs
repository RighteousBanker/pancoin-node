using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Panode.API.DTO
{
    [DataContract]
    public class SyncDTO
    {
        [DataMember]
        public List<string> Blocks { get; set; }

        [DataMember]
        public bool Synced { get; set; }
    }
}
