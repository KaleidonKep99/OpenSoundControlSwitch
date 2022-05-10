using Bespoke.Osc;
using Newtonsoft.Json;
using System.Net;

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
        public OSCServer(int I, int O, int CI, int CO, OSCProgram[] P)
        {
            InPort = I;
            OutPort = O;
            ControlInPort = CI;
            ControlOutPort = CO;
            Programs = P;
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
                Control = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, (int)ControlOutPort);

                Control.BundleReceived += Bundle;
                Control.MessageReceived += Message;

                // Do not filter methods, accept all
                Control.FilterRegisteredMethods = false;

                Control.Start();

                OSCServerL.PrintMessage(LogSystem.MsgType.Information, "VRChat OSC switch remote control is ready.", ControlOutPort, Control.IsRunning);
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
    }
}
