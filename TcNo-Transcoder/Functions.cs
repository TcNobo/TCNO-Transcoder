﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Globalization;

namespace TcNo_Transcoder
{
    class Functions
    {
        public static void Information(string SIn, bool exit)
        {
            int BeforeLeft = Console.CursorLeft;
            int BeforeTop = Console.CursorTop;
            switch (SIn)
            {
                case "Audio":
                    string[] lines = GlobalStrings.InfoAudio.Split('#');
                    Console.WriteLine(lines[0]);
                    RunProgram(Global.NvexeFull, "--check-encoders");

                    Console.WriteLine(lines[1]);
                    RunProgram(Global.NvexeFull, "--check-decoders");

                    Console.WriteLine(lines[2]);
                    break;
                case "Devices":
                    Console.WriteLine(GlobalStrings.InfoDevices, Global.Nvexe);
                    RunProgram(Global.NvexeFull, "--check-device");
                    break;
                case "Help":
                    Console.WriteLine(GlobalStrings.InfoHelp, Constants.Version, Constants.NvencCVersion, Constants.MinNvidiaDriver, Global.ExeLocation);
                    break;
                case "Video":
                    Console.WriteLine(GlobalStrings.InfoVideo, Global.Nvexe);
                    break;
                case "Info":
                    Console.WriteLine(GlobalStrings.Info, Constants.Version, Constants.NvencCVersion, Constants.MinNvidiaDriver);
                    break;
                case "Shell info":
                    Console.WriteLine(GlobalStrings.InfoShell);
                    break;
            }
            Console.WriteLine();
            if (exit)
            {
                Console.WriteLine(GlobalStrings.PrgAnyKeyToClose);
                ReadKeyScrollUpDown(BeforeLeft, BeforeTop);
                System.Environment.Exit(1);
            }
        }
        public static void ReadKeyScrollUpDown(int BeforeLeft, int BeforeTop)
        {  
            // Get cursor position to set it back later. Stops weird text overwriting in console.
            int AfterLeft = Console.CursorLeft;
            int AfterTop = Console.CursorTop;
            // Bring cursor to top (to scroll user up) and hide.
            Console.CursorVisible = false;
            Console.SetCursorPosition(BeforeLeft, BeforeTop);

            Console.ReadKey();
            // Brings cursor back down to end of output above, and shows.
            Console.SetCursorPosition(AfterLeft, AfterTop);
            Console.CursorVisible = true;
        }
        public static void RunProgram(string SProgram, string SArguments)
        {
            var info = new ProcessStartInfo(SProgram, SArguments)
            {
                UseShellExecute = false
            };
            var proc = Process.Start(info);
            proc.WaitForExit();
        }

        // Settings functions
        // -----------------------------------
        #region Settings Functions
        public static void LoadSettingsFile()
        {
            try
            {
                Global.SettingsFileLocation = String.Format(@"{0}\settings.cfg", Global.ExeLocation);
                if (!File.Exists(Global.SettingsFileLocation))
                {
                    Console.WriteLine(GlobalStrings.InfoSettingsOptions + "\n\n");
                    while (true)
                    {
                        Console.Write(GlobalStrings.InfoSettingsOptionsResponse + ' ');
                        string response = Console.ReadLine();
                        if (response == "1" || response.ToLower() == "y")
                        {
                            Console.Clear();
                            File.WriteAllLines(Global.SettingsFileLocation, GlobalStrings._LOCALISEDSETTINGS_SIMPLE.Split('\n'));
                            break;
                        }
                        else if (response == "2" || response.ToLower() == "n")
                        {
                            Console.Clear();
                            File.WriteAllLines(Global.SettingsFileLocation, GlobalStrings._LOCALISEDSETTINGS_ADVANCED.Split('\n'));
                            break;
                        }
                    }

                    Console.WriteLine(GlobalStrings.ErrFailedSettingsFind + '\n');
                }


                string[] lines = System.IO.File.ReadAllLines(Global.SettingsFileLocation);
                foreach (var line in lines)
                {
                    if (line.Contains('='))
                    {
                        GetSetting(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(GlobalStrings.ErrFailedSettingsRead, e.Message);
            }
        }

        public static void GetSetting(string input)
        {
            string SSetting = input.Split('=')[0];
            string SValue = input.Split('=')[1];
            Global.Settings.Add(SSetting, SValue);
        }

        public static void VerifySettingsFile()
        {
            string[] SettingsList = new string[] { "OutputFormat", "CopyAudio", "AudioCodec", "Suffix", "OutputDirectory", "Resolution", "FPS",
                "VideoCodec", "EncoderProfile", "Level", "Preset", "OutputDepth", "CUDASchedule", "SampleAspectRatio", "Lookahead",
                "GOPLength", "BFrames", "ReferenceFrames", "MVPrecision", "Colormatrix", "CABAC", "Deblock", "Bitrate", "VBRQuality", "MaxBitrate",
                "GPU", "OtherArgs", "Override", "DeleteOldQueue", "AfterCompletion", "DelQueueOnNew", "OverwriteExisting" };
            foreach (var s in SettingsList)
            {
                try
                {
                    string temp = Global.Settings[s];
                }
                catch (Exception)
                {
                    if (File.Exists(Global.AttemptFile))
                    {
                        Console.WriteLine(GlobalStrings.ErrSettingNotFound2, s);
                        File.Delete(Global.AttemptFile);
                        File.Move(Global.SettingsFileLocation, Global.SettingsFileLocation + ".old");
                    }
                    else
                    {
                        Console.WriteLine(GlobalStrings.ErrSettingNotFound, s);
                        File.Create(Global.AttemptFile);
                    }
                    Console.WriteLine();
                    AnyKeyToClose();
                }
            }
            if (File.Exists(Global.AttemptFile))
                File.Delete(Global.AttemptFile);
        }
        #endregion
        // -----------------------------------

        public static void AnyKeyToClose()
        {
            Console.WriteLine(GlobalStrings.PrgAnyKeyToClose);
            Console.ReadKey();
            System.Environment.Exit(1);
        }
        public static void AnyKeyToContinue()
        {
            Console.WriteLine(GlobalStrings.PrgAnyKeyToContinue);
            Console.ReadKey();
        }

        public static void CheckOSBit()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                Global.Bit = 64;
                Global.Nvexe = "NVEncC64.exe";
            }
            else
            {
                Global.Bit = 32;
                Global.Nvexe = "NVEncC.exe";
            }
            Global.NvexeFull = String.Format(@"{0}\x{1}\{2}", Global.ExeLocation, Global.Bit.ToString(), Global.Nvexe);
        }

        public static void CheckIfCustomFolder()
        {
            if (!Functions.SettingNull(Global.Settings["OutputDirectory"]))
            {
                Global.CustomFolder = true;
            }
        }

        public static string GetTaskArgs(string inputFile, string outputFolder)
        {
            string Arg = "-i \"" + inputFile + "\"";
            if (Global.Settings["Override"] == "" || Global.Settings["Override"] == String.Empty || Global.Settings["Override"] == "0")
            {
                Arg += " --codec " + Global.Settings["VideoCodec"];
                Arg += " --fps " + Global.Settings["FPS"];
                Arg += " --output-res " + Global.Settings["Resolution"];
                Arg += " --profile " + Global.Settings["EncoderProfile"];
                Arg += " --level " + Global.Settings["Level"];
                Arg += " --" + Global.Settings["Bitrate"];
                Arg += " --preset " + Global.Settings["Preset"];
                Arg += " --lookahead " + Global.Settings["Lookahead"];
                Arg += " --cuda-schedule " + Global.Settings["CUDASchedule"];
                Arg += " --gop-len " + Global.Settings["GOPLength"];
                Arg += " --bframes " + Global.Settings["BFrames"];
                Arg += " --ref " + Global.Settings["ReferenceFrames"];
                Arg += " --mv-precision " + Global.Settings["MVPrecision"];
                Arg += " --colormatrix " + Global.Settings["Colormatrix"];

                // Copy audio true, or false to use a custom codec, or blank to not copy audio at all.
                if (Global.Settings["CopyAudio"] == "1")
                {
                    Arg += " --audio-copy";
                }
                else if (Global.Settings["CopyAudio"] == "0")
                {
                    Arg += " --audio-codec" + Global.Settings["AudioCodec"];
                }
                if (!SettingNull(Global.Settings["SampleAspectRatio"]))
                {
                    Arg += " --sar " + Global.Settings["SampleAspectRatio"];
                }
                if (Global.Settings["VideoCodec"] == "h264" && Global.Settings["CABAC"] == "1") // Only for h264.
                {
                    Arg += " --cabac";
                }
                if (Global.Settings["VideoCodec"] == "h264" && Global.Settings["Deblock"] == "1") // Only for h264.
                {
                    Arg += " --deblock";
                }
                if (Global.Settings["GPU"] != "")
                {
                    Arg += " --device " + Global.Settings["GPU"];
                }
            }
            else
                Console.WriteLine("OVERRIDE ENABLED! Input and Output automated, everything else is up to what you add to OtherArgs.");
            Global.InputFile = inputFile;
            Arg += " --output \"" + Global.OutputFile + "\"";
            if (Global.Settings["OtherArgs"] != "")
            {
                Arg += " " + Global.Settings["OtherArgs"];
            }
            return Arg;
        }

        public static string OutputFileString(string outputFolder, string inputFile)
        {
            return outputFolder + "\\" + Path.GetFileNameWithoutExtension(inputFile) + Global.Settings["Suffix"] + "." + Global.Settings["OutputFormat"];
        }

        public static bool SettingNull(string inString)
        {
            return inString == " " || inString == "" || inString == string.Empty;
        }

        public static void ProcessFileOrFolder(string inObject, string outFolder)
        {
            if (IsDirectory(inObject))
            {
                // Directory
                foreach (string file in Directory.EnumerateFiles(inObject, "*.*", SearchOption.AllDirectories))
                {
                    outFolder = GetOutputDiretory(file);
                    ProcessFile(file, outFolder);
                }
            }
            else
            {
                // File
                outFolder = GetOutputDiretory(inObject);
                ProcessFile(inObject, outFolder);
            }
        }

        public static string GetOutputDiretory(string obj)
        {
            if (!Global.CustomFolder)
            {
                return Path.GetDirectoryName(obj);
            }
            return Global.Settings["OutputDirectory"];
        }

        public static void ListFileOrFolder(string inObject)
        {
            if (IsDirectory(inObject))
            {
                // Directory
                foreach (string file in Directory.EnumerateFiles(inObject, "*.*", SearchOption.AllDirectories))
                {
                    Console.WriteLine(file);
                }
            }
            else
            {
                // File
                Console.WriteLine(inObject);
            }
        }


        public static void ProcessFile(string inFile, string outFolder)
        {
            Console.WriteLine("\n" + Global.LineString + "\n" + String.Format(GlobalStrings.PrgNowProcessing, inFile, OutputFileString(outFolder, inFile)));
            Global.OutputFile = OutputFileString(outFolder, inFile);
            if (Global.Settings["OverwriteExisting"] != "1")
            {
                if (File.Exists(Global.OutputFile))
                {
                    Console.WriteLine(String.Format(GlobalStrings.ErrTranscodeExists, Global.OutputFile));
                    return;
                }
            }
            RunProgram(Global.NvexeFull, Functions.GetTaskArgs(inFile, outFolder));
        }

        public static bool IsDirectory(string inObject)
        {
            return (File.GetAttributes(inObject) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public static bool IsFileOrFolder(string inObject)
        {
            try
            {
                if ((File.GetAttributes(inObject) & FileAttributes.Directory) == FileAttributes.Directory) { }
                return true;
                
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool QueueExists()
        {
            return File.Exists(Global.QueueFile);
        }
        public static void LoadQueue()
        {
            Global.QueueText = File.ReadAllText(Global.QueueFile);
            Global.QueueValid = Global.QueueText.Length > 5;
        }

        public static void QueryQueue()
        {
            Console.Write(GlobalStrings.PrgQueueList, Global.QueueText);
            while (true)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Y:
                        ProcessQueue();
                        break;
                    case ConsoleKey.N:
                        AnyKeyToClose();
                        break;
                }
            }
        }
        public static void NewQueue()
        {
            if (File.Exists(Global.QueueFile)){
                if (Global.Settings["DelQueueOnNew"] == "1")
                {
                    File.Delete(Global.QueueFile);
                }
                else
                {
                    DateQueueMove();
                }
                System.Environment.Exit(1);
            }
            else
            {
                Console.WriteLine(GlobalStrings.ErrNoQueue);
                AnyKeyToClose();
            }
        }
        public static void DateQueueMove()
        {
            DateTime d = DateTime.Now;
            string OldQueue = "extra/queue-" + d.Day + '-' + d.Month + '-' + d.Year + ' ' + d.Hour + '-' + d.Minute + '-' + d.Second + ".txt";
            File.Move(Global.QueueFile, OldQueue);
        }

        public static void ProcessQueue()
        {
            // Prompts the user that if they want to change the directory, they must change it under settings.
            Console.WriteLine("\n\n" + String.Format(GlobalStrings.PrgChangeDirQueue, "NVEncC x" + Global.Bit.ToString(), Global.Settings["Resolution"], Global.Settings["FPS"], Global.Settings["VideoCodec"]) + "\n");
            Console.ReadKey();

            Global.QueueStartTime = DateTime.Now;
            foreach (var l in Global.QueueText.Split('\n'))
            {
                if (l != string.Empty)
                {
                    string line = l.Replace("\"", "").Replace("\r", "");
                    // Set output folder to user settings, or original file's dir, if not specified.
                    string outputFolder = Functions.GetOutputDiretory(line);
                    try
                    {
                        if (Functions.IsDirectory(line))
                        {
                            Console.WriteLine(String.Format(GlobalStrings.PrgQueueFollowingFiles, line) + "\n" + Global.LineString);
                            Functions.ListFileOrFolder(line);
                            Console.WriteLine(Global.LineString + "\n");
                        }
                        Global.EncodeStartTime = DateTime.Now;
                        Functions.ProcessFileOrFolder(line, outputFolder);
                        Console.WriteLine("\n" + GlobalStrings.InfoItemComplete, Global.EncodeStartTime, DateTime.Now);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        Console.WriteLine(String.Format(GlobalStrings.ErrFileNotFoundException, line));
                    }
                }
            }
            QueueComplete();
            Console.WriteLine("\n" + GlobalStrings.InfoQueueComplete, Global.QueueStartTime, DateTime.Now);
        }

        public static void QueueComplete()
        {
            if (Global.Settings["DeleteOldQueue"] == "1")
            {
                try
                {
                    File.Delete(Global.QueueFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Format(GlobalStrings.ErrDeleteQueue, ex.ToString()));
                }
            }
            else
            {
                DateQueueMove();
            }

            if (!(Global.Settings["AfterCompletion"] == "" || Global.Settings["AfterCompletion"] == String.Empty))
            {
                    string AC = Global.Settings["AfterCompletion"].Replace("%outF%", Global.OutputFile);

                    Process ContextProcessADD = Process.Start(AC);
                    ContextProcessADD.WaitForExit();
            }
        }

        public static void GetNvidiaDriver()
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
                {
                    if (obj["Description"].ToString().ToLower().Contains("nvidia"))
                    {
                        //gpuName = obj["Description"].ToString().Trim();
                        string GPUDriver_s = obj["DriverVersion"].ToString().Replace(".", string.Empty).Substring(5);
                        GPUDriver_s = GPUDriver_s.Substring(0, 3) + "." + GPUDriver_s.Substring(3); // add dot
                        Global.GPUDriver = float.Parse(GPUDriver_s, CultureInfo.InvariantCulture.NumberFormat);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine(GlobalStrings.ErrNoNV);
            }
        }
        public static void GraphicsDriverMet()
        {
            float MinNvidiaDriver_f = float.Parse(Constants.MinNvidiaDriver, CultureInfo.InvariantCulture.NumberFormat);
            if (MinNvidiaDriver_f > Global.GPUDriver)
            {
                Console.WriteLine(String.Format(GlobalStrings.InfoNVDriverNotMet, Global.GPUDriver.ToString(), MinNvidiaDriver_f.ToString()));
            }
        }

    }
}
