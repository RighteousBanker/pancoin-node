using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Panode.API
{
    [DataContract]
    public class ViewModel
    {
        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public bool IsAccessible { get; set; }

        [DataMember]
        public string Hostname { get; set; }

        [DataMember]
        public object Data { get; set; }

        public ViewModel(object data)
        {
            Version = Program.Version;
            IsAccessible = Program.Settings.accessible;
            Hostname = Program.Settings.accessible ? Program.Settings.url : "";
            Data = data;
        }
    }

    [DataContract]
    public class ViewModel<T>
    {
        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public bool IsAccessible { get; set; }

        [DataMember]
        public string Hostname { get; set; }

        [DataMember]
        public T Data { get; set; }

        public ViewModel(T data)
        {
            Version = Program.Version;
            IsAccessible = Program.Settings.accessible;
            Hostname = Program.Settings.accessible ? Program.Settings.url : "";
            Data = data;
        }
    }
}
