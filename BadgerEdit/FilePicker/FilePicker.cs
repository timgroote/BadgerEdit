using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;

namespace BadgerEdit.FilePicker
{
    public enum FilePickerMode
    {
        Open,
        Save
    }

    public unsafe class FilePicker
    {
        private byte[] FilenameInput = new byte[128];
        public FilePickerMode Mode = FilePickerMode.Open;

        private DriveInfo SelectedDrive { get; set; }
        private List<DriveInfo> VisibleDrives { get; set; }
        private FileInfo SelectedFile;
        private List<FileInfo> VisibleFiles { get; set; }
        private List<DirectoryInfo> VisibleDirectories { get; set; }

        private DirectoryInfo _currentDirectory { get; set; }
        public DirectoryInfo CurrentDirectory {
            get => _currentDirectory;
            set
            {
                _currentDirectory = value;
                LoadVisiblePaths();
            }
        }

        protected Func<FileInfo, bool> OnSelect { get; set; }


        public FilePicker()
        {
            CurrentDirectory = new DirectoryInfo(".");
            SelectedDrive = new DriveInfo(CurrentDirectory.Root.FullName);
        }

        public void Show(FilePickerMode mode, Func<FileInfo, bool> f = null)
        {
            CurrentDirectory = new DirectoryInfo(".");
            OnSelect = f;
            SelectedFile = null;
            Mode = mode;
            Open = true;
        }

        public bool Open { get; private set; }

        private void LoadVisiblePaths()
        {
            VisibleDrives = DriveInfo.GetDrives().ToList();
            try
            { 
                VisibleDirectories = _currentDirectory.GetDirectories().ToList();
            }
            catch(Exception e)
            {
                //security violation?
                VisibleDirectories = new List<DirectoryInfo>();
            }
            try
            { 
                VisibleFiles = _currentDirectory.GetFiles().ToList();
            }
            catch(Exception e)
            {
                //security violation?
                VisibleFiles = new List<FileInfo>();
            }
        }

        public void GotoDrive(DriveInfo drv)
        {
            SelectedDrive = drv;
            MoveTo(SelectedDrive.RootDirectory);
        }

        public void MoveTo(DirectoryInfo dir)
        {
            CurrentDirectory = dir;
        }

        private void MoveUp()
        {
            MoveTo(CurrentDirectory.Parent);
        }
        

        public void Draw()
        {
            if (!Open)
                return;

            ImGui.SetNextWindowSize(new Vector2(250, 400), Condition.Always );
            ImGui.OpenPopup("###filePicker");
            if (ImGui.BeginPopup("###filePicker"))
            {
                ImGui.Text(_currentDirectory.FullName);
                
                if (Mode == FilePickerMode.Save)
                {
                    if (ImGui.InputText("##filename", FilenameInput, 128, InputTextFlags.Default, TextEditCallback))
                    {
                    }

                    if (ImGui.Button("Save"))
                    {
                        SelectedFile = new FileInfo(CurrentDirectory.FullName + "/" + Encoding.Convert(Encoding.UTF8, Encoding.UTF8, FilenameInput));
                        Reset();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    Reset();
                }


                ImGui.PushStyleColor(ColorTarget.Button, new Vector4(0.4f, 0.4f, 0, 1));
                foreach(var driveInfo in VisibleDrives)
                {
                    if(ImGui.Button(driveInfo.Name))
                    {
                        GotoDrive(driveInfo);
                    }

                    if (VisibleDrives.IndexOf(driveInfo) < VisibleDrives.Count - 1)
                    {
                        ImGui.SameLine();
                    }
                }
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ColorTarget.Button, new Vector4(1,0,0,1));

                if(CurrentDirectory.Parent != null)
                { 
                    if (ImGui.Button(".."))
                    {
                        MoveUp();
                    }
                }

                foreach (var dir in VisibleDirectories)
                {
                    if (ImGui.Button(dir.Name))
                    {
                        MoveTo(dir);
                    }
                }
                ImGui.PopStyleColor();
                if (Mode == FilePickerMode.Open)
                {
                    foreach (var fl in VisibleFiles)
                    {
                        if (ImGui.Button($"{fl.Name}"))
                        {
                            Selectfile(fl);
                        }
                    }
                }

                ImGui.EndPopup();
            }
        }

        private void Reset(bool removeSelectedfile = false)
        {
            if (removeSelectedfile)
            {
                SelectedFile = null;
            }
            CurrentDirectory = new DirectoryInfo(".");
            Open = false;
        }

        private int TextEditCallback(TextEditCallbackData* data)
        {
            return 1; //wat?
        }

        private void Selectfile(FileInfo fl)
        {
            SelectedFile = fl;
            OnSelect?.Invoke(fl);
            Open = false;
        }
    }
}
