﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OmniMIDIConfigurator
{
    public partial class MIDIInPlay : Form
    {
        private static WinMM.MidiInProc midiInProc;
        private static IntPtr handle;

        private static Stopwatch WhenItGotReceived;
        private static String LastEvent = "NONE";
        private static Boolean LastEventFailed = false;

        private static Int32 DeviceCount;

        public MIDIInPlay()
        {
            InitializeComponent();
            DeviceCount = WinMM.midiInGetNumDevs();
        }

        private void SetLastEvent(String EventName, Boolean Failed)
        {
            LastEvent = EventName;
            LastEventFailed = Failed;
            WhenItGotReceived = Stopwatch.StartNew();
        }

        private void MidiProc(IntPtr hMidiIn, uint wMsg, UIntPtr dwInstance, UIntPtr dwParam1, UIntPtr dwParam2)
        {
            switch (wMsg)
            {
                case MIDIInEvent.MIM_DATA:
                    OmniMIDI.SendDirectData(dwParam1.ToUInt32());
                    SetLastEvent(dwParam1.ToUInt32().ToString("X6"), false);
                    break;
                case MIDIInEvent.MIM_LONGDATA:
                    OmniMIDI.SendDirectLongData(dwParam1);
                    SetLastEvent(dwParam1.ToUInt32().ToString("X6"), false);
                    break;
                case MIDIInEvent.MIM_ERROR:
                    MessageBox.Show(
                        String.Format("Failed to parse the following message: {0:X}", dwParam1.ToUInt32()),
                        "OmniMIDI - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    SetLastEvent("MIM_ERROR", true);
                    break;
                case MIDIInEvent.MIM_LONGERROR:
                    MessageBox.Show(
                        String.Format("Failed to parse the following long message: {0:X}", dwParam1.ToUInt32()),
                        "OmniMIDI - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    SetLastEvent("MIM_LONGERROR", true);
                    break;
            }
        }

        private void MIDIInPlay_Load(object sender, EventArgs e)
        {
            // Check count
            if (DeviceCount < 1)
            {
                // None available, close
                MessageBox.Show("No MIDI input devices available.", "OmniMIDI - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            // Initialize KDMAPI
            if (OmniMIDI.IsKDMAPIAvailable())
                OmniMIDI.InitializeKDMAPIStream();
            else
            {
                MessageBox.Show("Unable to initialize KDMAPI.", "OmniMIDI - Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            OmniMIDI.ResetKDMAPIStream();

            // Initialize MIDI inputs list
            MIDIINCAPS InCaps = new MIDIINCAPS();
            for (uint i = 0; i < DeviceCount; i++)
            {
                WinMM.midiInGetDevCaps(i, out InCaps, (uint)Marshal.SizeOf(InCaps));
                MIDIInList.Items.Add(InCaps.szPname);
            }
        }

        private void MIDIInList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (handle != IntPtr.Zero)
            {
                WinMM.midiInStop(handle);
                WinMM.midiInClose(handle);
            }

            midiInProc = new WinMM.MidiInProc(MidiProc);
            int retval = WinMM.midiInOpen(out handle, MIDIInList.SelectedIndex, midiInProc, IntPtr.Zero, WinMM.CALLBACK_FUNCTION);
            WinMM.midiInStart(handle);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (handle != IntPtr.Zero)
            {
                WinMM.midiInStop(handle);
                WinMM.midiInClose(handle);
            }

            OmniMIDI.TerminateKDMAPIStream();

            if (e.CloseReason == CloseReason.WindowsShutDown) return;
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            if (handle != IntPtr.Zero && WhenItGotReceived != null)
            {
                if (WhenItGotReceived != null && WhenItGotReceived.ElapsedMilliseconds < 200)
                {
                    ActivityPanel.BackColor = Color.DarkGreen;
                    ActivityLabel.Text = String.Format("Received {0}.", LastEvent);
                }
                else
                {
                    ActivityPanel.BackColor = Color.DarkRed;
                    ActivityLabel.Text = "No activity.";
                }
            }
            else
            {
                ActivityPanel.BackColor = Color.DarkRed;
                ActivityLabel.Text = "zZᶻ⋯";
            }
        }
    }

    public static class OmniMIDI
    {
        [DllImport("OmniMIDI.dll")]
        internal static extern void InitializeKDMAPIStream();

        [DllImport("OmniMIDI.dll")]
        internal static extern void TerminateKDMAPIStream();

        [DllImport("OmniMIDI.dll")]
        internal static extern void ResetKDMAPIStream();

        [DllImport("OmniMIDI.dll")]
        internal static extern bool IsKDMAPIAvailable();

        [DllImport("OmniMIDI.dll")]
        internal static extern int SendDirectData(uint dwMsg);

        [DllImport("OmniMIDI.dll")]
        internal static extern int SendDirectDataNoBuf(uint dwMsg);

        [DllImport("OmniMIDI.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SendDirectLongData(UIntPtr IIMidiHdr);

        [DllImport("OmniMIDI.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SendDirectLongDataNoBuf(UIntPtr IIMidiHdr);
    }

    internal static class WinMM
    {
        internal const int MMSYSERR_NOERROR = 0;
        internal const int CALLBACK_FUNCTION = 0x00030000;

        internal delegate void MidiInProc(
            IntPtr hMidiIn,
            uint wMsg,
            UIntPtr dwInstance,
            UIntPtr dwParam1,
            UIntPtr dwParam2);

        [DllImport("winmm.dll")]
        internal static extern int midiInGetNumDevs();

        [DllImport("winmm.dll")]
        internal static extern int midiInGetDevCaps(
            uint uDeviceID,
            out MIDIINCAPS caps,
            uint cbMidiInCaps);

        [DllImport("winmm.dll")]
        internal static extern int midiInClose(
            IntPtr hMidiIn);

        [DllImport("winmm.dll")]
        internal static extern int midiInOpen(
            out IntPtr lphMidiIn,
            int uDeviceID,
            MidiInProc dwCallback,
            IntPtr dwCallbackInstance,
            int dwFlags);

        [DllImport("winmm.dll")]
        internal static extern int midiInStart(
            IntPtr hMidiIn);

        [DllImport("winmm.dll")]
        internal static extern int midiInStop(
            IntPtr hMidiIn);
    }

    internal static class MIDIInEvent
    {
        // Internal
        public const int MIM_DATA = 0x3C3;
        public const int MIM_LONGDATA = 0x3C4;
        public const int MIM_ERROR = 0x3C5;
        public const int MIM_LONGERROR = 0x3C6;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIDIINCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;     // MMVERSION
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwSupport;
    }
}
