using System;
using System.Text;
using System.Linq;
using Encoders;
using Panode.API;
using ByteOperation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Panode.Core
{
    public class ContactLedger
    {
        readonly static string path = "tables/contacts";
        readonly static int hostnameLength = 128;
        readonly static Random random = new Random();
        readonly HttpProvider _httpProvider;

        LinearDictionaryEncoder coder = new LinearDictionaryEncoder(path, hostnameLength, 4);

        public int ContactCount { get { return coder.LinearCoder.Count; } }

        public ContactLedger(HttpProvider httpProvider)
        {
            _httpProvider = httpProvider;

            var contacts = coder.LinearCoder.ReadData();

            if (Program.Settings.defaultNode != null)
            {
                AddContact(Program.Settings.defaultNode).Wait();
            }
        }

        public async Task<bool> AddContact(string url)
        {
            var ret = false;

            if (url != null)
            {
                var hostnameBytes = ToBytes(url);

                if ((hostnameBytes != null) && (url != Program.Settings.url))
                {
                    if (!coder.ContainsKey(ByteManipulator.BigEndianTruncate(hostnameBytes, 128)))
                    {
                        if (await Handshake(url))
                        {
                            coder.Add(hostnameBytes, ByteManipulator.GetBytes((uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds));
                            ret = true;
                        }
                    }
                }
            }

            return ret;
        }

        public void RemoveContact(string url)
        {
            var bytes = ByteManipulator.BigEndianTruncate(Encoding.UTF8.GetBytes(url), 128);
            coder.Remove(bytes);
        }

        public string GetRandomUrl()
        {
            if (coder.LinearCoder.Count != 0)
            {
                var bytes = coder.LinearCoder.Read(random.Next(0, coder.LinearCoder.Count - 1));
                bytes = ByteManipulator.TruncateMostSignificatZeroBytes(bytes.Take(bytes.Length - 4).ToArray());
                return Encoding.UTF8.GetString(bytes);
            }
            else
            {
                return null;
            }
        }

        public List<string> GetAll()
        {
            var ret = new List<string>();

            if (coder.LinearCoder.Count != 0)
            {
                var contacts = coder.LinearCoder.ReadData();

                foreach (var entry in contacts)
                {
                    ret.Add(DecodeEntry(entry).Item1);
                }
            }

            return ret;
        }

        private byte[] ToBytes(string hostname)
        {
            byte[] ret;
            var bytes = Encoding.UTF8.GetBytes(hostname);


            if (bytes.Length <= hostnameLength)
            {
                ret = bytes;
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        private Tuple<string, DateTime> DecodeEntry(byte[] entry)
        {
            return new Tuple<string, DateTime>(Encoding.UTF8.GetString(ByteManipulator.TruncateMostSignificatZeroBytes(entry.Take(entry.Length - 4).ToArray())), DateTimeOffset.FromUnixTimeSeconds(ByteManipulator.GetUInt32(entry.Skip(128).ToArray())).DateTime);
        }

        private async Task<bool> Handshake(string url)
        {
            var response = await _httpProvider.PeerPost<string, string>("hello", url + "contact/handshake");

            if (response != null && response == "ok")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
