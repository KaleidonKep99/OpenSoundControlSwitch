﻿using Newtonsoft.Json;

namespace OpenSoundControlSwitch
{
    public partial class OSCAddress
    {
        // The first part of the address (e.g. /avatar/parameters)
        [JsonProperty("Address")]
        public string Address { get; set; }

        // The array containing all the parameters stored under that address (e.g. MouthUpper, MouthLower etc.)
        [JsonProperty("Parameters")]
        public string[] Parameters { get; set; }

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

        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("TargetAddress")]
        public string TargetAddress { get; set; }

        public OSCAddressHTTPItem[] Vars { get; set; }

        // Used to create the example JSON
        public OSCAddressHTTP(string A, string TA, OSCAddressHTTPItem[] Va)
        {
            Address = A;
            TargetAddress = TA;
            Vars = Va;
        }
    }

    public partial class OSCAddressHTTPItem
    {
        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("VarName")]
        public string VarName { get; set; }

        // The variable type
        [JsonProperty("VarType")]
        public string VarType { get; set; }

        // The address (e.g. /avatar/parameters/param)
        [JsonProperty("Value")]
        public object? Value { get; set; }

        // The minimum for the value (if required)
        [JsonProperty("MinValue")]
        public object? MinValue { get; set; }

        // The maximum for the value
        [JsonProperty("MaxValue")]
        public object? MaxValue { get; set; }

        [JsonProperty("Constant")]
        public bool Constant { get; set; } = false;

        [JsonIgnore]
        public object? PrevValue { get; set; } = null;

        public OSCAddressHTTPItem(string VN, string VT, object? Const, object? MiV, object? MaV)
        {
            if (Const != null)
            {
                Value = Const;

                switch (Const)
                {
                    case float f:
                    case double d:
                        VarType = "float";
                        break;
                    case int i:
                    case long l:
                        VarType = "int";
                        break;
                    case char c:
                    case string s:
                        VarType = "string";
                        break;
                    case bool b:
                        VarType = "bool";
                        break;
                    default:
                        VarType = "object";
                        break;
                }

                Constant = true;
            }

            VarName = VN;
            VarType = VT;
            MinValue = MiV;
            MaxValue = MaV;
        }
    }
}
