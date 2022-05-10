using Bespoke.Osc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace VRChatOSCSwitch
{
    public partial class OSCProgram
    {
        // The name of the program that you want the switch to forward to
        [JsonProperty("Name")]
        public string Name { get; set; }

        // The executable of the OSC program, optional
        [JsonProperty("ExecutablePath")]
        public string? ExecutablePath { get; set; }

        // The command line of the OSC program, might be required to change its default ports (e.g. with VRCFT)
        [JsonProperty("CommandLine")]
        public string? CommandLine { get; set; }

        // This is the forwarded input port of VRChat, hosted by the switch. The switch will forward from this port to target OSC app's port.
        [JsonProperty("FwdInPort")]
        public int InPort { get; set; }

        // This is the input port of the OSC app that is being forwarded
        [JsonProperty("FwdOutPort")]
        public int OutPort { get; set; }

        // Do not use for now
        [JsonProperty("SourceIP")]
        public string? SourceIP { get; set; }

        // Do not use for now
        [JsonProperty("TargetIP")]
        public string? TargetIP { get; set; }

        // If you want the console to be hosted by the switch app, set this to true.
        // If set to false, the switch will create another console window.
        [JsonProperty("SeparateConsole")]
        public bool? SeparateConsole { get; set; }

        // Used internally by the OSC switch
        [JsonIgnore]
        public int ServerInPort { get; set; }

        [JsonProperty("Addresses")]
        public OSCAddress[] Addresses { get; set; }

        private OscServer Host;
        private LogSystem OSCProgramL;
        private OSCMsgHandler MsgHandler = new OSCMsgHandler();

        // Used by the OSC switch to forward the packets to the OSC app (e.g. VRCFT)
        [JsonIgnore]
        public IPEndPoint AppDestination { get; set; }

        // Used by the OSC app to send packets to the target OSC app (e.g. VRChat)
        [JsonIgnore]
        public IPEndPoint SrvDestination { get; set; }

        // Unused
        public OSCProgram() { }

        // Used to create the example JSON
        public OSCProgram(string N, string? SIP, string? TIP, bool SC, int I, int O, int SI, string EP, string CL, OSCAddress[] A)
        {
            Name = N;
            SourceIP = SIP;
            TargetIP = TIP;
            SeparateConsole = SC;
            InPort = I;
            OutPort = O;
            ServerInPort = SI;
            ExecutablePath = EP;
            CommandLine = CL;
            Addresses = A;
        }

        // Forward every packet to the target OSC app
        public void AnalyzeData(object? sender, IPEndPoint Source, string Address, IList<object> Data, Type DataType)
        {
            OscMessage? Msg = MsgHandler.BuildMsg(Address, SrvDestination, Data.ToArray());
            if (Msg != null) Msg.Send(SrvDestination);
        }

        // Get the message and analyze it
        public void Bundle(object? sender, OscBundleReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Bundle.SourceEndPoint, Var.Bundle.Address, Var.Bundle.Data, Var.GetType());
        }

        // Get the message and analyze it
        public void Message(object? sender, OscMessageReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Message.SourceEndPoint, Var.Message.Address, Var.Message.Data, Var.GetType());
        }

        // This function prepares the forwarder for the OSC app
        public void PrepareClient()
        {
            /*
            
            // Do not use for now.

            IPAddress? SIP = null, TIP = null;

            if (SourceIP != null)
                SIP = IPAddress.Parse(SourceIP);

            if (TargetIP != null)
                TIP = IPAddress.Parse(TargetIP);

            */

            OSCProgramL = new LogSystem(Name);

            // Create the forwarder server
            Host = new OscServer(Bespoke.Common.Net.TransportType.Udp, /*SIP != null ? SIP : */ IPAddress.Loopback, InPort);
            AppDestination = new IPEndPoint(/*SIP != null ? SIP : */ IPAddress.Loopback, OutPort);
            SrvDestination = new IPEndPoint(/*TIP != null ? TIP : */ IPAddress.Loopback, ServerInPort);

            Host.BundleReceived += Bundle;
            Host.MessageReceived += Message;

            // If an executable is specified, run it
            if (!string.IsNullOrEmpty(ExecutablePath))
            {
                // If there's a command line to be used, replace the two dummy values with the actual forward ports specified in the JSON
                if (!string.IsNullOrEmpty(CommandLine))
                    CommandLine = CommandLine.Replace("$OutPort$", OutPort.ToString()).Replace("$InPort$", InPort.ToString());
                // Otherwise, f**k off?
                else
                    CommandLine = "";

                // Prepare the PSI
                ProcessStartInfo Exec = new ProcessStartInfo();
                Exec.Arguments = CommandLine;
                Exec.FileName = ExecutablePath;

                // Check if the user wants to forward the app's stdout to the OSC switch's console
                Exec.UseShellExecute = SeparateConsole != null ? (bool)SeparateConsole : false;

                // Execute it
                Process? ExecProc = null;
                try { ExecProc = Process.Start(Exec); }
                catch { ExecProc = null; }

                // Oops?
                if (ExecProc == null)
                    OSCProgramL.PrintMessage(LogSystem.MsgType.Warning, "The process failed to start. You might have to run it manually.", ExecutablePath, CommandLine);
            }

            // Register the methods that the app is supposed to receive
            foreach (OSCAddress Address in Addresses)
            {
                foreach (string Parameter in Address.Parameters)
                {
                    String Par = String.Format("{0}/{1}", Address.Address, Parameter);
                    Host.RegisterMethod(Par);
                }
            }

            Host.Start();

            OSCProgramL.PrintMessage(LogSystem.MsgType.Information, String.Format("OSC forwarder for {0} is ready.", Name), String.Format("Input: {0}", InPort), String.Format("Output: {0}", OutPort), Host.IsRunning);
        }
    }
}
