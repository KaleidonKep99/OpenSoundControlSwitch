using Newtonsoft.Json;

namespace VRChatOSCSwitch
{
    public partial class OSCAddress
    {
        // The first part of the address (e.g. /avatar/parameters)
        [JsonProperty("Address")]
        public string Address { get; set; }

        // The array containing all the parameters stored under that address (e.g. MouthUpper, MouthLower etc.)
        [JsonProperty("Parameters")]
        public string[] Parameters { get; set; }

        // Unused
        public OSCAddress() { }

        // Used to create the example JSON
        public OSCAddress(string A, string[] P)
        {
            Address = A;
            Parameters = P;
        }
    }
}
