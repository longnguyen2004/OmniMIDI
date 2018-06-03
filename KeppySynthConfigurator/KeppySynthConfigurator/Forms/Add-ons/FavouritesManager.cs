﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace KeppySynthConfigurator
{
    public partial class KeppySynthFavouritesManager : Form
    {
        private CommonOpenFileDialog AddFolderDialog = new CommonOpenFileDialog();
        private string LastBrowserPath { get; set; }
        private string FolderListPathWithFile { get; set; }
        private string folderlistpath = Environment.GetEnvironmentVariable("USERPROFILE").ToString() + "\\Keppy's Synthesizer\\";

        public KeppySynthFavouritesManager()
        {
            InitializeComponent();
            AddFolderDialog.IsFolderPicker = true;
        }

        private void SaveFavourites()
        {
            using (StreamWriter sw = new StreamWriter(FolderListPathWithFile))
            {
                try
                {
                    foreach (var item in FolderList.Items) sw.WriteLine(item.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error while saving the blacklist!\n\n.NET error:\n" + ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void InitializeLastPath()
        {
            try
            {
                RegistryKey SynthPaths = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's Synthesizer\\Paths", true);
                if (SynthPaths.GetValue("lastpathblacklist", null) != null)
                {
                    LastBrowserPath = SynthPaths.GetValue("lastpathblacklist").ToString();
                    AddFolderDialog.InitialDirectory = LastBrowserPath;
                }
                else
                {
                    SynthPaths.SetValue("lastpathblacklist", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), RegistryValueKind.String);
                    LastBrowserPath = SynthPaths.GetValue("lastpathblacklist").ToString();
                    AddFolderDialog.InitialDirectory = LastBrowserPath;
                }
                SynthPaths.Close();
            }
            catch
            {
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's Synthesizer\\Paths");
                RegistryKey SynthPaths = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's Synthesizer\\Paths", true);
                SynthPaths.SetValue("lastpathblacklist", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), RegistryValueKind.String);
                LastBrowserPath = SynthPaths.GetValue("lastpathblacklist").ToString();
                AddFolderDialog.InitialDirectory = LastBrowserPath;
                SynthPaths.Close();
            }
        }

        private void KeppyDriverFavouritesManager_Load(object sender, EventArgs e)
        {
            InitializeLastPath();

            // Initialize blacklist
            FolderListPathWithFile = folderlistpath + "keppymididrv.favlist";

            try
            {
                // Import the blacklist file
                using (StreamReader r = new StreamReader(FolderListPathWithFile))
                {
                    string line;
                    while ((line = r.ReadLine()) != null) FolderList.Items.Add(line);
                }
            }
            catch
            {
                File.Create(FolderListPathWithFile).Dispose();
            }
        }

        private void AddFolder_Click(object sender, EventArgs e)
        {
            if (FolderAdvancedMode.Checked == true)
            {
                FolderList.Items.Add(ManualFolder.Text);
            }
            else
            {
                if (AddFolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    RegistryKey SynthPaths = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's Synthesizer\\Paths", true);
                    LastBrowserPath = AddFolderDialog.FileName;
                    SynthPaths.SetValue("lastpathblacklist", LastBrowserPath, RegistryValueKind.String);
                    SynthPaths.Close();
                    FolderList.Items.Add(AddFolderDialog.FileName);
                }
            }
            SaveFavourites();
        }

        private void RemoveFolder_Click(object sender, EventArgs e)
        {
            for (int i = FolderList.SelectedIndices.Count - 1; i >= 0; i--)
            {
                FolderList.Items.RemoveAt(FolderList.SelectedIndices[i]);
            }
            SaveFavourites();
        }

        private void FolderAdvancedMode_CheckedChanged(object sender, EventArgs e)
        {
            if (FolderAdvancedMode.Checked == true)
            {
                FolderDef.Text = "Type the full path to the folder in the textbox.";
                ManualListLabel.Enabled = true;
                ManualFolder.Enabled = true;
            }
            else
            {
                FolderDef.Text = "Add a folder to the favourites by clicking \"Add folder\".";
                ManualListLabel.Enabled = false;
                ManualFolder.Enabled = false;
            }
        }
    }
}
