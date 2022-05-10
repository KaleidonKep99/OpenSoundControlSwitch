using Bespoke.Osc;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VRChatOSCSwitch
{
    public partial class OSCServer
    {
        // This is the input port of VRChat (or the target OSC app)
        [JsonProperty("InPort")]
        public int InPort { get; set; }

        // This is the input port of the OSC switch (this program)
        [JsonProperty("OutPort")]
        public int OutPort { get; set; }

        // This is the input port of the remote control program that is controlling the switch remotely
        [JsonProperty("ControlInPort")]
        public int? ControlInPort { get; set; }

        // This is the input port of the OSC switch, used by the remote control program
        [JsonProperty("ControlOutPort")]
        public int? ControlOutPort { get; set; }

        // Don't show your public IP in the log when enabling the remote control
        [JsonProperty("HidePublicIP")]
        public bool? HidePublicIP { get; set; }

        // This contains all the programs that the switch will forwards the ports to
        [JsonProperty("OSCPrograms")]
        public OSCProgram[] Programs { get; set; }

        // Internal functions used by the switch to do the forwarding and logging
        private OscServer Host, Control;
        private LogSystem OSCServerL = new LogSystem("OSCServerL");
        private OSCMsgHandler MsgHandler = new OSCMsgHandler();

        // Unused
        public OSCServer() {}

        // Used to create the example JSON
        public OSCServer(int I, int O, int CI, int CO, bool HPIP, OSCProgram[] P)
        {
            InPort = I;
            OutPort = O;
            ControlInPort = CI;
            ControlOutPort = CO;
            HidePublicIP = HPIP;
            Programs = P;
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
        // and if it matches, it forwards the packet to it
        public void AnalyzeData(object? sender, IPEndPoint Source, string Address, IList<object> Data, Type DataType)
        {
            foreach (OSCProgram OProgram in Programs)
            {
                foreach (OSCAddress OAddress in OProgram.Addresses)
                {
                    foreach (string Param in OAddress.Parameters)
                    {
                        String Target = String.Format("{0}/{1}", OAddress.Address, Param);
                        if (Target.Equals(Address))
                        {
                            OscMessage Msg = MsgHandler.BuildMsg(Target, OProgram.AppDestination, Data.ToArray());
                            Msg.Send(OProgram.AppDestination);
                        }
                    }
                }
            }
        }

        // Get the bundle and analyze it
        public void Bundle(object? sender, OscBundleReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Bundle.SourceEndPoint, Var.Bundle.Address, Var.Bundle.Data, Var.GetType());
        }

        // Get the message and analyze it
        public void Message(object? sender, OscMessageReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Message.SourceEndPoint, Var.Message.Address, Var.Message.Data, Var.GetType());
        }

        // --
        public void BundleR(object? sender, OscBundleReceivedEventArgs Var)
        {
            IPEndPoint Src = Var.Bundle.SourceEndPoint;
            OSCServerL.PrintMessage(LogSystem.MsgType.Information, "Received remote bundle.", Src.Address, Src.Port);
            Bundle(sender, Var);
        }

        // --
        public void MessageR(object? sender, OscMessageReceivedEventArgs Var)
        {
            IPEndPoint Src = Var.Message.SourceEndPoint;
            OSCServerL.PrintMessage(LogSystem.MsgType.Information, "Received remote message.", Src.Address, Src.Port);
            Message(sender, Var);
        }

        // This function prepares the server
        public void PrepareServer()
        {
            // Prepare each client
            foreach (OSCProgram Program in Programs)
                Program.PrepareClient();

            // Create the switch server
            Host = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, OutPort);
            Host.BundleReceived += Bundle;
            Host.MessageReceived += Message;

            // If the control ports are specified, open the remote control server
            if (ControlInPort != null && ControlOutPort != null)
            {
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
                                    try { Control = new OscServer(Bespoke.Common.Net.TransportType.Udp, IP.Address, (int)ControlOutPort); }
                                    catch
                                    {
                                        OSCServerL.PrintMessage(LogSystem.MsgType.Error, "Failed to bind to local IP. Trying another one if available...", IP.Address);
                                        continue; 
                                    }

                                    Control.BundleReceived += BundleR;
                                    Control.MessageReceived += MessageR;

                                    // Do not filter methods, accept all
                                    Control.FilterRegisteredMethods = false;

                                    Control.Start();

                                    OSCServerL.PrintMessage(LogSystem.MsgType.Information, String.Format("VRChat OSC switch remote control is ready. Public IP is {0}, LAN IP is {1}.", GetPublicIPAddress(), IP.Address));

                                    break;
                                }
                            }
                        }

                        if (Control != null)
                            if (Control.IsRunning) 
                                break;
                    }
                }
                catch (Exception ex)
                {
                    OSCServerL.PrintMessage(LogSystem.MsgType.Error, "An error has occurred.", ex.ToString());
                }
            }

            // Begin registering the addresses for every program
            foreach (OSCProgram Program in Programs)
            {
                Program.SrvDestination = new IPEndPoint(IPAddress.Loopback, InPort);
                foreach (OSCAddress Address in Program.Addresses)
                {
                    foreach (string Parameter in Address.Parameters)
                    {
                        // Create the final address (address + param, e.g. /avatar/parameters + /param)
                        String Par = String.Format("{0}/{1}", Address.Address, Parameter);

                        // Register it to the host
                        Host.RegisterMethod(Par);
                        OSCServerL.PrintMessage(LogSystem.MsgType.Information, "Added parameter to VRChat OSC switch.", Par);
                    }
                }

                // In case the program is waiting for a dummy packet, send one
                OscMessage Msg = MsgHandler.BuildMsg("/oscswitchheartbeat/", Program.SrvDestination, true);
                Msg.Send(Program.SrvDestination);
            }

            Host.Start();

            OSCServerL.PrintMessage(LogSystem.MsgType.Information, "VRChat OSC switch is ready.", OutPort, Host.IsRunning);
        }

        public void TerminateServer()
        {
            if (Host.IsRunning)
            {
                foreach (OSCProgram Program in Programs)
                    Program.TerminateClient();

                Host.Stop();
                Host.ClearMethods();

                Host.BundleReceived -= Bundle;
                Host.MessageReceived -= Message;
            }

        }
    }
}
