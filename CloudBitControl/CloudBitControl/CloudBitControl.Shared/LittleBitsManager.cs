using System;
using System.Collections.Generic;
using System.Text;

using System.Net.Http;

using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace LitttleBitsManager
{
    /// <summary>
    /// Provides connections between Windows Universal applications and 
    /// a LittleBits cloudbit device
    /// 
    /// Rob Miles 
    /// March 2015
    /// 
    /// Version 1.0
    /// 
    /// </summary>


    #region JSON wrappers for CloudBit responses

    #region Data Read classes

    public class User
    {
        public int id { get; set; }
    }

    public class Ap
    {
        public string ssid { get; set; }
        public string mac { get; set; }
        public int strength { get; set; }
    }

    public class Device
    {
        public string id { get; set; }
        public string device { get; set; }
        public string setup_version { get; set; }
        public string protocol_version { get; set; }
        public string firmware_version { get; set; }
        public string mac { get; set; }
        public string hash { get; set; }
        public Ap ap { get; set; }
    }

    public class Server
    {
        public string id { get; set; }
    }

    public class From
    {
        public User user { get; set; }
        public Device device { get; set; }
        public Server server { get; set; }
    }

    public class Payload
    {
        public int percent { get; set; }
        public int absolute { get; set; }
    }

    public class CloudBitReading
    {
        public string type { get; set; }
        public long timestamp { get; set; }
        public From from { get; set; }
        public int percent { get; set; }
        public int absolute { get; set; }
        public string name { get; set; }
        public Payload payload { get; set; }
    }

    public class StatusMessage
    {
        public int statusCode { get; set; }
        public string error { get; set; }
        public string message { get; set; }
    }

    #endregion

    #region Status Classes


    public class Percent
    {
        [JsonProperty("$cgte")]
        public int triggerGreaterorEqual { get; set; }
    }

    public class Input
    {
        public Percent percent { get; set; }
    }

    public class Data
    {
        public Input input { get; set; }
    }

    public class Subscriber
    {
        public string sid { get; set; }
        public string pid { get; set; }
        public Data data { get; set; }
    }

    public class StatusDetails
    {
        public string id { get; set; }
        public string label { get; set; }
        public int user_id { get; set; }
        public bool is_connected { get; set; }
        public object ap { get; set; }
        public List<object> subscriptions { get; set; }
        public List<Subscriber> subscribers { get; set; }
    }

    #endregion

    #endregion

    public enum CloudBitReadResultStatus
    {
        READ_OK,
        REQUEST_TIMEOUT,
        HTTTP_ERROR
    }

    public struct CloudBitReadResult
    {
        /// <summary>
        /// Overall status of the read request
        /// </summary>
        public CloudBitReadResultStatus Status;

        /// <summary>
        /// Status of the HTTP request as an HTTP error code
        /// </summary>
        public int HttpStatus;

        /// <summary>
        /// Value recieved from the cloudbit as a percentage - 0-100
        /// </summary>
        public int CloudBitInputValue;

        public override string ToString()
        {
            string result = "";

            switch (Status)
            {
                case CloudBitReadResultStatus.HTTTP_ERROR:
                    result = "HTTP Error: " + HttpStatus.ToString();
                    break;

                case CloudBitReadResultStatus.REQUEST_TIMEOUT:
                    result = "HTTP request timeout";
                    break;

                case CloudBitReadResultStatus.READ_OK:
                    result = "Read OK: " + CloudBitInputValue.ToString();
                    break;
            }
            return result;
        }
    }

    public enum CloudBitWriteStatus
    {
        SENT_OK,
        REQUEST_TIMEOUT,
        HTTP_ERROR
    }


    public struct CloudBitWriteResult
    {
        public CloudBitWriteStatus Status;
        public int HttpStatus;

        public override string ToString()
        {
            string result = "";

            switch (Status)
            {
                case CloudBitWriteStatus.HTTP_ERROR:
                    result = "HTTP Error: " + HttpStatus.ToString();
                    break;

                case CloudBitWriteStatus.REQUEST_TIMEOUT:
                    result = "HTTP request timeout";
                    break;

                case CloudBitWriteStatus.SENT_OK:
                    result = "Sent OK";
                    break;
            }
            return result;
        }
    }

    public enum CloudBitStatus
    {
        CONNECTED,
        DISCONNECTED,
        REQUEST_TIMEOUT,
        INVALID_RESPONSE,
        NOT_FOUND
    }

    class LittleBitsCloudBit
    {
        private static int requestTimeoutInMS = 5000;

        private string ID;
        private string AccessToken;


        /// <summary>
        /// Creates a new Cloudbit controller
        /// Go to http://control.littlebitscloud.cc/ to get the details for your device
        /// </summary>
        /// <param name="inID">the ID for the controller</param>
        /// <param name="inAccessToken">the access token for the controller</param>
        public LittleBitsCloudBit ( string inID, string inAccessToken)
        {
            ID = inID;
            AccessToken = inAccessToken;
        }

        /// <summary>
        /// Reads a setting from the cloud bit.
        /// Will fail with REQUEST_TIMEOUT if the cloud bit is not connected
        /// </summary>
        /// <returns>reply summary including the status</returns>
        public async Task<CloudBitReadResult> ReadSetting()
        {
            string url = @"http://api-http.littlebitscloud.cc/devices/" + ID + @"/input";

            CloudBitReadResult result;
            result.Status = CloudBitReadResultStatus.REQUEST_TIMEOUT;
            result.CloudBitInputValue = 0;
            result.HttpStatus = 0;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(requestTimeoutInMS);
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.littlebits.v2+json");
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        using (var body = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(body))
                            while (!reader.EndOfStream)
                            {
                                string message = reader.ReadLine();
                                if (message.Length == 0) continue;
                                if (message.StartsWith("{\"statusCode\""))
                                {
                                    try
                                    {
                                        var httpStatus = JsonConvert.DeserializeObject<StatusMessage>(message);
                                        result.Status = CloudBitReadResultStatus.HTTTP_ERROR;
                                        result.HttpStatus = httpStatus.statusCode;
                                        break;
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }

                                else
                                {
                                    message = message.Substring(5);
                                    try
                                    {
                                        var reading = JsonConvert.DeserializeObject<CloudBitReading>(message);
                                        result.Status = CloudBitReadResultStatus.READ_OK;
                                        result.CloudBitInputValue = reading.payload.percent;
                                        client.CancelPendingRequests();
                                        break;
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }
                            }
                    }
                }
            }
            catch
            {
                result.Status = CloudBitReadResultStatus.REQUEST_TIMEOUT;
            }

            return result;
        }

        /// <summary>
        /// Sends a setting to the cloudbit
        /// NOTE: This will indicate a successful return even if the CloudBit 
        /// is not presently connected. You need to use ReadStatus to determine
        /// that the cloudbit is present before you send a value to it. 
        /// </summary>
        /// <param name="settingPercent">value between 0 and 100 inclusive</param>
        /// <param name="settingTimeInMS">number of milliseconds to retain that value</param>
        /// <returns></returns>
        public async Task<CloudBitWriteResult> SendSetting(int settingPercent, int settingTimeInMS)
        {
            CloudBitWriteResult result;
            result.HttpStatus = 0;
            result.Status = CloudBitWriteStatus.REQUEST_TIMEOUT;

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(requestTimeoutInMS);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                string command = "{\"percent\":" + settingPercent.ToString() + ",\"duration_ms\":" + settingTimeInMS.ToString() + "}";
                HttpContent stuff = new StringContent(command);
                stuff.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                string url = @"http://api-http.littlebitscloud.cc/devices/" + ID + @"/output";
                try
                {
                    using (HttpResponseMessage response = await (client.PostAsync(url, stuff)))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            String content = await response.Content.ReadAsStringAsync();
                            result.Status = CloudBitWriteStatus.SENT_OK;
                        }
                        else
                        {
                            result.Status = CloudBitWriteStatus.HTTP_ERROR;
                            result.HttpStatus = (int)response.StatusCode;
                        }
                    }
                }
                catch
                {
                    result.Status = CloudBitWriteStatus.REQUEST_TIMEOUT;
                }
            }
            return result;
        }


        /// <summary>
        /// Reads the status of the cloudbit
        /// </summary>
        /// <returns>current connected state</returns>
        public async Task<CloudBitStatus> ReadStatus()
        {
            string url = @"http://api-http.littlebitscloud.cc/devices";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(requestTimeoutInMS);
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.littlebits.v2+json");
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                    {
                        using (var body = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(body))
                        {
                            string message = reader.ReadToEnd();

                            List<StatusDetails> statusList = null;

                            try
                            {
                                statusList = JsonConvert.DeserializeObject<List<StatusDetails>>(message);
                            }
                            catch
                            {
                                return CloudBitStatus.INVALID_RESPONSE;
                            }

                            foreach ( StatusDetails status in statusList)
                            {
                                if(status.id == ID)
                                {
                                    if (status.is_connected)
                                        return CloudBitStatus.CONNECTED;
                                    else
                                        return CloudBitStatus.DISCONNECTED;
                                }
                            }
                            return CloudBitStatus.NOT_FOUND;
                        }
                    }
                }
            }
            catch
            {
               return CloudBitStatus.REQUEST_TIMEOUT;
            }
        }
    }
}
