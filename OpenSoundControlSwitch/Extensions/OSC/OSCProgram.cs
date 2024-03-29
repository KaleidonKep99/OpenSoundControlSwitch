﻿using Bespoke.Osc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace OpenSoundControlSwitch
{
    public partial class OSCProgram
    {
        // The name of the program that you want the switch to forward to
        [JsonProperty]
        public string Name { get; set; }

        // The executable of the OSC program, optional
        [JsonProperty]
        public string? ExecutablePath { get; set; }

        // The command line of the OSC program, might be required to change its default ports (e.g. with VRCFT)
        [JsonProperty]
        public string? CommandLine { get; set; }

        // Other programs that must be running because they're required by the OSC program (e.g. with VRCFT)
        [JsonProperty]
        public string[]? ProgramDependencies { get; set; }

        // This is the forwarded input port of the target OSC app, hosted by the switch
        [JsonProperty]
        public int ProgramInPort { get; set; }

        // This is the input port of the OSC app that is being forwarded
        [JsonProperty]
        public int ProgramOutPort { get; set; }

        // If you want the console to be hosted by the switch app, set this to true.
        // If set to false, the switch will create another console window.
        [JsonProperty]
        public bool? SeparateConsole { get; set; }

        // Used internally by the OSC switch
        [JsonIgnore]
        public int SOutPort { get; set; }
        // ---------------------------------
        [JsonIgnore]
        public int SInPort { get; set; }
        // Used internally by the OSC switch

        [JsonProperty]
        public OSCAddress[] Addresses { get; set; }

        private OscServer Host;
        private LogSystem OSCProgramL;
        private OSCMsgHandler MsgHandler;
        private Process? ExecProc = null;
        private Process[]? DependenciesProc = null;

        // Used by the OSC switch to forward the packets to the OSC app (e.g. VRCFT)
        [JsonIgnore]
        public IPEndPoint AppDestination { get; set; }

        // Used by the OSC app to send packets to the target OSC app (e.g. VRChat)
        [JsonIgnore]
        public IPEndPoint SrvDestination { get; set; }

        // Used to create the example JSON
        public OSCProgram(string N, bool SC, int POP, int PIP, string EP, string CL, OSCAddress[] A)
        {
            Name = N;
            SeparateConsole = SC;
            ProgramInPort = PIP;
            ProgramOutPort = POP;
            ExecutablePath = EP;
            CommandLine = CL;
            Addresses = A;
            MsgHandler = new OSCMsgHandler();
        }

        // Forward every packet to the target OSC app
        public void AnalyzeData(object? sender, IPEndPoint Source, string Address, object Data)
        {
            try
            {
                if (Data != null)
                {
                    switch (Data)
                    {
                        case OscMessage OSCM:
                            OSCM.Send(SrvDestination);
                            break;
                        case OscBundle OSCB:
                            OSCB.Send(SrvDestination);
                            break;
                    }
                }
            }
            catch { }
        }

        // Get the message and analyze it
        public void Bundle(object? sender, OscBundleReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Bundle.SourceEndPoint, Var.Bundle.Address, Var.Bundle);
        }

        // Get the message and analyze it
        public void Message(object? sender, OscMessageReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Message.SourceEndPoint, Var.Message.Address, Var.Message);
        }

        // This function prepares the forwarder for the OSC app
        public void PrepareClient()
        {
            ProcessStartInfo Exec;
            OSCProgramL = new LogSystem(Name);

            // Create the forwarder server
            Host = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, ProgramOutPort, false);
            AppDestination = new IPEndPoint(IPAddress.Loopback, ProgramInPort);
            SrvDestination = new IPEndPoint(IPAddress.Loopback, SOutPort);

            Host.BundleReceived += Bundle;
            Host.MessageReceived += Message;

            // If dependencies are specified, run them
            //
            // What's the deal with this piece of code? Shouldn't the apps do it themselves?
            // Well yes and technically... no
            // Apps such as VRCFT don't close the SRanipal program, even though THEY'RE SUPPOSED TO.
            // So I just handle everything myself from here. Hehe!
            if (ProgramDependencies != null)
            {
                DependenciesProc = new Process[ProgramDependencies.Length];
                for (int C = 0; C < ProgramDependencies.Length; C++)
                {
                    // Prepare the PSI
                    Exec = new ProcessStartInfo();
                    Exec.FileName = ProgramDependencies[C];
                    Exec.UseShellExecute = true;

                    // Execute it
                    try { DependenciesProc[C] = Process.Start(Exec); }
                    catch { DependenciesProc[C] = null; }

                    // Oops?
                    if (DependenciesProc[C] == null)
                        OSCProgramL.PrintMessage(LogSystem.MsgType.Warning, "The process failed to start. You might have to run it manually.", ProgramDependencies[C]);
                }
            }

            // If an executable is specified, run it
            if (!string.IsNullOrEmpty(ExecutablePath))
            {
                // If there's a command line to be used, replace the two dummy values with the actual forward ports specified in the JSON
                if (!string.IsNullOrEmpty(CommandLine))
                    CommandLine = CommandLine.Replace("$OutPort$", ProgramOutPort.ToString()).Replace("$InPort$", ProgramInPort.ToString());
                // Otherwise, f**k off?
                else
                    CommandLine = "";

                // Prepare the PSI
                Exec = new ProcessStartInfo();
                Exec.Arguments = CommandLine;
                Exec.FileName = ExecutablePath;

                // Check if the user wants to forward the app's stdout to the OSC switch's console
                Exec.UseShellExecute = SeparateConsole != null ? (bool)SeparateConsole : false;

                // Execute it
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

            OSCProgramL.PrintMessage(
                LogSystem.MsgType.Information, 
                String.Format("OSC forwarder for {0} {1}. ({2} >>> {3}, {4} >>> {5})", Name, Host.IsRunning ? "is ready" : "failed to start", SInPort, ProgramInPort, ProgramOutPort, SOutPort));
        }

        public void TerminateClient()
        {
            if (ExecProc != null)
                ExecProc.Kill();

            if (DependenciesProc != null)
                foreach (Process Proc in DependenciesProc)
                    if (Proc != null)
                        Proc.Kill();

            Host.Stop();
            Host.ClearMethods();

            Host.BundleReceived -= Bundle;
            Host.MessageReceived -= Message;
        }
    }
}
