using Bespoke.Osc;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VRChatOSCSwitch
{
    public partial class OSCServer
    {   
        // This is the input port of the OSC switch (this program)
        [JsonProperty]
        public int SwitchInPort { get; set; }

        // This is the "output port of the switch program" / "input port the target OSC app"
        [JsonProperty]
        public int SwitchOutPort { get; set; }

        // This is where your remote control app should send events to
        [JsonProperty]
        public int? RemoteControlInPort { get; set; }

        // This is the address/port the switch program sends the data to in response to the remote control
        [JsonProperty]
        public string? RemoteControlOut { get; set; }

        // Don't show your public IP in the log when enabling the remote control
        [JsonProperty]
        public bool? HidePublicIP { get; set; }

        // Useful to check if the messages/bundles are going through
        [JsonProperty]
        public bool? DebugMessages { get; set; }

        // This contains all the programs that the switch will forwards the ports to
        [JsonProperty]
        public OSCProgram[] OSCPrograms { get; set; }

        // This contains all the programs that the switch will send packets to
        public OSCAddressHTTP[]? GETTargets { get; set; } = null;

        // Internal functions used by the switch to do the forwarding and logging
        private OscServer Host, ControlR, ControlL;
        private LogSystem OSCServerL = new LogSystem("OSCServerL");
        private OSCMsgHandler MsgHandler = new OSCMsgHandler();
        private MathFuncs MFuncs = new MathFuncs();
        private AsyncHTTPGET GETThingy = new AsyncHTTPGET();
        private IPEndPoint? BounceIP = null;

        // Used to create the example JSON
        public OSCServer(int SOP, int SIP, int RCIP, string RCO, bool HPIP, bool DM, OSCProgram[] OSCP, OSCAddressHTTP[] GETT)
        {
            SwitchOutPort = SOP;
            SwitchInPort = SIP;
            RemoteControlInPort = RCIP;
            RemoteControlOut = RCO;
            HidePublicIP = HPIP;
            DebugMessages = DM;
            OSCPrograms = OSCP;
            GETTargets = GETT;
        }

        private string GetPublicIPAddress()
        {
            if (HidePublicIP == null)
                HidePublicIP = true;

            if ((bool)HidePublicIP)
                return "<PUBLIC IP HIDDEN>";

            string? PublicIP;

            HttpClient DDNSReq = new HttpClient();
            using (HttpResponseMessage Response = DDNSReq.GetAsync("http://checkip.dyndns.org").Result)
            using (StreamReader Stream = new StreamReader(Response.Content.ReadAsStream()))
            {
                string AfterThis = "Address: ";

                PublicIP = Stream.ReadToEnd();

                if (PublicIP != null)
                {
                    int Begin = PublicIP.IndexOf(AfterThis) + AfterThis.Length, End = PublicIP.LastIndexOf("</body>");
                    PublicIP = PublicIP.Substring(Begin, End - Begin);
                }
            }

            return PublicIP != null ? PublicIP : "<UNAVAILABLE>";
        }

        // This function scans every packet received, and if it matches one of the programs,
        // it forwards the packet to it
        public void AnalyzeData(object? sender, IPEndPoint Source, string Address, IList<object> Data)
        {
            try
            {
                if (DebugMessages != null && DebugMessages == true) 
                    OSCServerL.PrintMessage(LogSystem.MsgType.Information, Address, Data[0]);

                foreach (OSCProgram OProgram in OSCPrograms)
                {
                    if (Source.Address != OProgram.AppDestination.Address && Source.Port != OProgram.AppDestination.Port)
                    {
                        foreach (OSCAddress OAddress in OProgram.Addresses)
                        {
                            foreach (string Param in OAddress.Parameters)
                            {
                                String Target = String.Format("{0}/{1}", OAddress.Address, Param);

                                if (DebugMessages != null && DebugMessages == true)
                                    OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("Target will be {0}", Target));

                                if (Target.Equals(Address))
                                {
                                    OscMessage Msg = MsgHandler.BuildMsg(Target, OProgram.AppDestination, Data.ToArray());
                                    Msg.Send(OProgram.AppDestination);

                                    if (DebugMessages != null && DebugMessages == true)
                                        OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("Sent from {0} and relayed to {1}", Source, OProgram.AppDestination));

                                    goto NextProgram;
                                }
                            }
                        }
                    }

                    NextProgram:;
                }

                if (GETTargets != null)
                {
                    foreach (OSCAddressHTTP GETTarget in GETTargets)
                    {
                        if (GETTarget.TargetAddress.Equals(Address))
                        {
                            int Who = 0;
                            string Target = GETTarget.Address;

                            foreach (OSCAddressHTTPItem FTarget in GETTarget.Vars)
                            {
                                if (FTarget.Constant)
                                    Target += String.Format("{0}{1}={2}", Who < 1 ? "?" : "&", FTarget.VarName, FTarget.Value);
                                else
                                {
                                    try
                                    {
                                        switch (FTarget.VarType)
                                        {
                                            case "i":
                                            case "int":
                                                if (FTarget.PrevValue == null || FTarget.PrevValue.GetType() != typeof(int))
                                                    FTarget.PrevValue = 0;

                                                long v = FTarget.MaxValue != null ? (long)FTarget.MaxValue : 100;
                                                int i = (int)MFuncs.FtoI((float)Data[0], Convert.ToInt32(v));
                                                if (FTarget.PrevValue.Equals(i))
                                                    return;

                                                FTarget.PrevValue = i;

                                                break;
                                            case "f":
                                            case "float":
                                                if (FTarget.PrevValue == null || FTarget.PrevValue.GetType() != typeof(float))
                                                    FTarget.PrevValue = 0.0f;

                                                float f = (float)Data[0];
                                                if (FTarget.PrevValue.Equals(f))
                                                    return;

                                                FTarget.PrevValue = f;

                                                break;
                                            case "b":
                                            case "bool":
                                            case "boolean":
                                                if (FTarget.PrevValue == null || FTarget.PrevValue.GetType() != typeof(bool))
                                                    FTarget.PrevValue = false;

                                                bool b = (bool)Data[0];
                                                if (FTarget.PrevValue.Equals(b))
                                                    return;

                                                FTarget.PrevValue = b;

                                                break;
                                            case "s":
                                            case "string":
                                                if (FTarget.PrevValue == null || FTarget.PrevValue.GetType() != typeof(string))
                                                    FTarget.PrevValue = string.Empty;

                                                string s = (string)Data[0];
                                                if (FTarget.PrevValue.Equals(s))
                                                    return;

                                                FTarget.PrevValue = s;

                                                break;
                                            default:
                                                FTarget.PrevValue = Data[0];
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        OSCServerL.PrintMessage(LogSystem.MsgType.Error, "Oops?", ex);
                                    }

                                    Target += String.Format("{0}{1}={2}", Who < 1 ? "?" : "&", FTarget.VarName, FTarget.PrevValue);
                                }

                                Who++;
                            }

                            try
                            {
                                GETThingy.AddToQ(Target);
                            }
                            catch { }

                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OSCServerL.PrintMessage(LogSystem.MsgType.Error, "An error has occurred while forwarding one of the packets!", ex);
            }
        }

        // Get the bundle and analyze it
        public void Bundle(object? sender, OscBundleReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Bundle.SourceEndPoint, Var.Bundle.Address, Var.Bundle.Data);
        }

        // Get the message and analyze it
        public void Message(object? sender, OscMessageReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Message.SourceEndPoint, Var.Message.Address, Var.Message.Data);
        }

        // --
        public void BundleR(object? sender, OscBundleReceivedEventArgs Var)
        {
            Bundle(sender, Var);

            // Bounce it to the target
            if (BounceIP != null)
                Var.Bundle.Send(BounceIP);
        }

        // --
        public void MessageR(object? sender, OscMessageReceivedEventArgs Var)
        {
            Message(sender, Var);

            // Bounce it to the target
            if (BounceIP != null)
                Var.Message.Send(BounceIP);
        }

        // This function prepares the server
        public void PrepareServer()
        {
            // Create the switch server
            Host = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, SwitchInPort, false);
            Host.BundleReceived += Bundle;
            Host.MessageReceived += Message;

            // If the control ports are specified, open the remote control server
            if (RemoteControlInPort != null && RemoteControlOut != null)
            {
                string[] RCO = RemoteControlOut.Split(' ');

                try
                {
                    IPAddress RemoteControlOutIP = IPAddress.Parse(RCO[0]);
                    int RemoteControlOutPort = Convert.ToInt32(RCO[1]);

                    BounceIP = new IPEndPoint(RemoteControlOutIP, (int)RemoteControlOutPort);

                    OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("The switch will listen on {0} and bounce to {1}:{2}.", RemoteControlInPort, RCO[0], RemoteControlOutPort));
                }
                catch { BounceIP = null; }

                // Create the local input server
                ControlL = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, (int)RemoteControlInPort, false);
                ControlL.BundleReceived += BundleR;
                ControlL.MessageReceived += MessageR;
                ControlL.FilterRegisteredMethods = false;
                ControlL.Start();

                try
                {
                    // We need to get the local IP of the network interface we're using, to be able to access packets
                    // from the Internet. IPAddress.Loopback will only accept packets from the local network.
                    foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (Interface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || Interface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        {
                            foreach (UnicastIPAddressInformation IP in Interface.GetIPProperties().UnicastAddresses)
                            {
                                if (IP.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    // We got the IP, we'll use it when creating the remote control OSC server
                                    try { ControlR = new OscServer(Bespoke.Common.Net.TransportType.Udp, IP.Address, (int)RemoteControlInPort, false); }
                                    catch
                                    {
                                        OSCServerL.PrintMessage(LogSystem.MsgType.Error, "Failed to bind to local IP. Trying another one if available...", IP.Address);
                                        continue;
                                    }

                                    ControlR.BundleReceived += BundleR;
                                    ControlR.MessageReceived += MessageR;

                                    // Do not filter methods, accept all
                                    ControlR.FilterRegisteredMethods = false;

                                    ControlR.Start();

                                    OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("VRChat OSC switch remote control is ready! Public IP is {0}, LAN IP is {1}.", GetPublicIPAddress(), IP.Address));

                                    break;
                                }
                            }
                        }

                        if (ControlR != null)
                            if (ControlR.IsRunning)
                                break;
                    }
                }
                catch (Exception ex)
                {
                    OSCServerL.PrintMessage(LogSystem.MsgType.Error, "An error has occurred.", ex.ToString());
                }
            }

            // Begin registering the addresses for every program
            foreach (OSCProgram Program in OSCPrograms)
            {
                Program.SOutPort = SwitchOutPort;
                Program.SInPort = SwitchInPort;
                Program.SrvDestination = new IPEndPoint(IPAddress.Loopback, SwitchOutPort);

                foreach (OSCAddress Address in Program.Addresses)
                {
                    foreach (string Parameter in Address.Parameters)
                    {
                        // Create the final address (address + param, e.g. /avatar/parameters + /param)
                        String Par = String.Format("{0}/{1}", Address.Address, Parameter);

                        // Register it to the host
                        Host.RegisterMethod(Par);
                        OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("Bound \"{0}\" method to {1}.", Par, Program.Name));
                    }
                }

                Program.PrepareClient();

                // In case the program is waiting for a dummy packet, send one
                OscMessage Msg = MsgHandler.BuildMsg("/oscswitchheartbeat/", Program.AppDestination, true);
                Msg.Send(Program.AppDestination);
            }

            if (GETTargets != null)
            {
                foreach (OSCAddressHTTP GETTarget in GETTargets)
                {
                    Host.RegisterMethod(GETTarget.TargetAddress);
                    OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("Bound \"{0}\" method to {1}.", GETTarget.TargetAddress, GETTarget.Address));
                }
            }

            Host.Start();

            OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("VRChat OSC switch {0}. Listening on {1} and forwarding to {2}.", Host.IsRunning ? "is ready" : "failed to start", SwitchInPort, SwitchOutPort));
        }

        public void TerminateServer()
        {
            if (Host == null) return;

            if (Host.IsRunning)
            {
                foreach (OSCProgram Program in OSCPrograms)
                    Program.TerminateClient();

                Host.Stop();
                Host.ClearMethods();

                Host.BundleReceived -= Bundle;
                Host.MessageReceived -= Message;
            }

        }
    }
}
