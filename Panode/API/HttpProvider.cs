using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Panode.API
{
    public class HttpProvider
    {
        readonly HttpClient httpClient;

        public HttpProvider()
        {
            httpClient = new HttpClient();
        }

        public async Task<Tout> PostRequest<Tin, Tout>(Tin input, string addr)
        {
            Tout ret;

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(addr, content);

                if (response.IsSuccessStatusCode)
                {
                    ret = JsonConvert.DeserializeObject<Tout>(await response.Content.ReadAsStringAsync());

                    var data = new
                    {
                        Request = JsonConvert.SerializeObject(input),
                        Response = await response.Content.ReadAsStringAsync(),
                        Address = addr
                    };
                }
                else
                {
                    ret = default(Tout);
                }
            }
            catch (Exception e)
            {
                ret = default(Tout);
            }

            return ret;
        }

        public async Task<Tout> PeerPost<Tin, Tout>(Tin input, string addr)
        {
            var ret = default(Tout);
            var response = await PostRequest<ViewModel<Tin>, ViewModel<Tout>>(new ViewModel<Tin>(input), addr);
            
            if (response != null && response.Data != null)
            {
                ret = response.Data;
            }

            return ret;
        }
    }
}
