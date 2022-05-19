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

    public partial class OSCAddressHTTP
    {
        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("Address")]
        public string Address { get; set; }

        public OSCAddressHTTPItem[] Vars { get; set; }

        // Used to create the example JSON
        public OSCAddressHTTP(string A, OSCAddressHTTPItem[] Va)
        {
            Address = A;
            Vars = Va;
        }
    }

    public partial class OSCAddressHTTPItem
    {
        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("Constant")]
        public string VarName { get; }

        // The variable type
        [JsonProperty("VarType")]
        public Type VarType { get; }

        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("Constant")]
        public bool Constant { get; }

        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("Value")]
        public object? Value { get; }

        // The minimum for the value (if required)
        [JsonProperty("MinValue")]
        public object? MinValue { get; }

        // The maximum for the value
        [JsonProperty("MaxValue")]
        public object? MaxValue { get; }

        public OSCAddressHTTPItem(string VN, string VT, object? Const, object? MiV, object? MaV)
        {
            if (Const != null)
            {
                Constant = true;
                Value = Const;

                switch (Const)
                {
                    case float f:
                    case double d:
                        VarType = typeof(float);
                        break;
                    case int i:
                    case long l:
                        VarType = typeof(int);
                        break;
                    case char c:
                    case string s:
                        VarType = typeof(string);
                        break;
                    case bool b:
                        VarType = typeof(bool);
                        break;
                    default:
                        VarType = typeof(object);
                        break;
                }
            }
            else
            {
                Constant = false;

                switch (VT)
                {
                    case "float":
                        VarType = typeof(float);
                        break;
                    case "int":
                        VarType = typeof(int);
                        break;
                    case "string":
                        VarType = typeof(string);
                        break;
                    case "boolean":
                        VarType = typeof(bool);
                        break;
                    default:
                        VarType = typeof(object);
                        break;
                }
            }

            MinValue = MiV;
            MaxValue = MaV;
        }
    }
}
