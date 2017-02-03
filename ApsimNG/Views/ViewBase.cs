﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using APSIM.Shared.Utilities;
using MonoMac.AppKit;
using Gtk;


namespace UserInterface
{
    public class ViewBase
    {
        static string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        protected ViewBase _owner = null;
        protected Widget _mainWidget = null;

        public ViewBase Owner { get { return _owner; } }

        public Widget MainWidget { get { return _mainWidget; } }

        public ViewBase(ViewBase owner) { _owner = owner; }

        protected Gdk.Window mainWindow { get { return MainWidget == null ? null : MainWidget.Toplevel.GdkWindow; } }
        private bool waiting = false;

        protected bool hasResource(string name)
        {
            return resources.Contains(name);
        }
        public bool WaitCursor
        {
            get
            {
                return waiting;
            }

            set
            {
                if (mainWindow != null)
                {
                    if (value == true)
                        mainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                    else
                        mainWindow.Cursor = null;
                    while (Gtk.Application.EventsPending())
                        Gtk.Application.RunIteration();
                    waiting = value;
                }
            }
        }

        /// <summary>Ask user for a filename to open on Windows.</summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        static public string WindowsFileDialog(string prompt, string fileSpec, FileChooserAction action, string initialPath)
        {
            string fileName = null;
            FileDialog dialog;
            if (action == FileChooserAction.Open)
                dialog = new OpenFileDialog();
            else
                dialog = new SaveFileDialog();
            dialog.Title = prompt;
            if (!String.IsNullOrEmpty(fileSpec))
                dialog.Filter = fileSpec + "|All files (*.*)|*.*";

            if (File.Exists(initialPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                dialog.FileName = Path.GetFileName(initialPath);
            }
            else if (Directory.Exists(initialPath))
                dialog.InitialDirectory = initialPath;
            else
                dialog.InitialDirectory = Utility.Configuration.Settings.PreviousFolder;

            if (dialog.ShowDialog() == DialogResult.OK)
                fileName = dialog.FileName;

            return fileName;
        }

        /// <summary>Ask user for a filename to open on Windows.</summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        static public string OSXFileDialog(string prompt, string fileSpec, FileChooserAction action, string initialPath)
        {
            string fileName = null;
            int result = 0;
            NSSavePanel panel;
            if (action == FileChooserAction.Open)
                panel = new NSOpenPanel();
            else
                panel = new NSSavePanel();
            panel.Title = prompt;

            if (!String.IsNullOrEmpty(fileSpec))
            {
                string[] specParts = fileSpec.Split(new Char[] { '|' });
                int nExts = 0;
                string[] allowed = new string[specParts.Length / 2];
                for (int i = 0; i < specParts.Length; i += 2)
                {
                    string pattern = Path.GetExtension(specParts[i + 1]);
                    if (!String.IsNullOrEmpty(pattern))
                    {
                        pattern = pattern.Substring(1); // Get rid of leading "."
                        if (!String.IsNullOrEmpty(pattern))
                            allowed[nExts++] = pattern;
                    }
                }
                if (nExts > 0)
                {
                    Array.Resize(ref allowed, nExts);
                    panel.AllowedFileTypes = allowed;
                }
            }
            panel.AllowsOtherFileTypes = true;

            if (File.Exists(initialPath))
            {
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(Path.GetDirectoryName(initialPath));
                panel.NameFieldStringValue = Path.GetFileName(initialPath);
            }
            else if (Directory.Exists(initialPath))
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(initialPath);
            else
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(Utility.Configuration.Settings.PreviousFolder);

            result = panel.RunModal();
            if (result == 1 /*NSFileHandlingPanelOKButton*/)
            {
                fileName = panel.Url.Path;
            }
            return fileName;
        }

        /// <summary>Ask user for a filename to open.</summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        static public string AskUserForFileName(string prompt, string fileSpec, FileChooserAction action = FileChooserAction.Open, string initialPath = "")
        {

            string fileName = null;

            if (ProcessUtilities.CurrentOS.IsWindows)
                return WindowsFileDialog(prompt, fileSpec, action, initialPath);
            else if (ProcessUtilities.CurrentOS.IsMac)
                return OSXFileDialog(prompt, fileSpec, action, initialPath);
            else
            {
                string btnText = "Open";
                if (action == FileChooserAction.Save)
                    btnText = "Save";
                FileChooserDialog fileChooser = new FileChooserDialog(prompt, null, action, "Cancel", ResponseType.Cancel, btnText, ResponseType.Accept);

                if (!String.IsNullOrEmpty(fileSpec))
                {
                    string[] specParts = fileSpec.Split(new Char[] { '|' });
                    for (int i = 0; i < specParts.Length; i += 2)
                    {
                        FileFilter fileFilter = new FileFilter();
                        fileFilter.Name = specParts[i];
                        fileFilter.AddPattern(specParts[i + 1]);
                        fileChooser.AddFilter(fileFilter);
                    }
                }

                FileFilter allFilter = new FileFilter();
                allFilter.AddPattern("*");
                allFilter.Name = "All files";
                fileChooser.AddFilter(allFilter);

                if (File.Exists(initialPath))
                    fileChooser.SetFilename(initialPath);
                else if (Directory.Exists(initialPath))
                    fileChooser.SetCurrentFolder(initialPath);
                else
                    fileChooser.SetCurrentFolder(Utility.Configuration.Settings.PreviousFolder);
                if (fileChooser.Run() == (int)ResponseType.Accept)
                    fileName = fileChooser.Filename;
                fileChooser.Destroy();
            }
            return fileName;
        }
    }
}