using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Configuration;

namespace ffmpegSplitter
{
    class videoFile
    {
        public string name { get; set; }
        public float duration_sec { get; set; }
        public float size { get; set; }
        public string format { get; set; }
        public string path { get; set; }
        public bool _skip = false;
        public List<splitTime> _splitter = new List<splitTime>();
        public int getSplitTimes()
        {
            int retValue = 0;
            float mbSize = this.size / (1024 * 1024);
            if ((int)mbSize > (int)size)
            {
                mbSize /= 1024;
                retValue = Convert.ToInt16(mbSize);
            }

            return retValue;
        }

        public void setSplitter()
        {
            int splits = this.getSplitTimes();
            if(splits == 0)
            {
                this._splitter.Add(new splitTime() { Start=0, End=duration_sec });
                if(string.Compare(this.format, ConfigurationManager.AppSettings["convertTo"]) ==0 ) this._skip = true;
            } else
            {
                float start = 0, end = this.duration_sec, durAdd = (this.duration_sec / splits);

                for(int i = 0; i < splits; i++)
                {
                    start = i * durAdd;
                    end = (i + 1) * durAdd;

                    if (i == (splits - 1)) end = this.duration_sec;
                    else end = (i + 1) * durAdd;

                    this._splitter.Add(new splitTime() { Start = start, End = end });
                }
            }
        }

    }

    class splitTime
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    class Program
    {

        private static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? "";
            }
            catch (ConfigurationErrorsException)
            {
                return "Not Found";
            }
        }

        static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        static string binFiles = ReadSetting("ffmpeg"), 
            tmpDir = ReadSetting("tmpDir"), 
            srcDir = ReadSetting("video_src"),
            outDir = ReadSetting("video_out"), 
            splitSize = ReadSetting("sizeSplit"), 
            format = ReadSetting("format"),
            convertTo = ReadSetting("convertTo"),
            returnWhiteSpace = ReadSetting("returnWhiteSpaces"),
            removeSplitted = ReadSetting("removeConverted");
        static List<string> allowedFormats = new List<string>() { "mp4", "avi", "flv", "wmv", "mkv" };
        static List<videoFile> videos = new List<videoFile>();
        static int videosToSplit = 0;
        static bool ignoreSkip = false;

        static void breakLine(int how = 60)
        {
            Console.Write("\n");
            for(int i = 0; i < how; i++)
            {
                Console.Write("=");
            }
            Console.Write("\n");
        }

        static void credits(string author, int year, string version, string site = "https://devcraft.club")
        {
            breakLine();
            Console.Write("Name: Video Splitter by filesize\n");
            Console.WriteLine("Author: {0}", author);
            Console.WriteLine("Year: 2020-{0}", year);
            Console.WriteLine("Version: {0}", version);
            Console.Write("Site: {0}", site);
            breakLine();
            Console.Write("Copyright: {0} (C) {1}, {2}", year, author, site);
            breakLine();
        }

        static void Main(string[] args)
        {

            credits("Maxim Harder", 2020, "1.2.0");

            Console.WriteLine("\nPath to ffmpeg bin dir. By default: ");
            Console.WriteLine(binFiles);
            Console.WriteLine("Please enter new path or let it be empty to use default:");
            string newBinPath = Console.ReadLine();

            Console.WriteLine("\nPath to video source dir. By default: ");
            Console.WriteLine(srcDir);
            Console.WriteLine("Please enter new path or let it be empty to use default:");
            string newVideoSrcPath = Console.ReadLine();

            Console.WriteLine("\nPath to video output dir. By default: ");
            Console.WriteLine(outDir);
            Console.WriteLine("Please enter new path or let it be empty to create dir in source folder.");
            Console.WriteLine("You can use . or only name for the same dir as source directory or .. for parental directory.\nExample: \n./out or out = X:/src/out\n../out = X:/out");
            string newVideoOutPath = Console.ReadLine();

            Console.WriteLine("\nPath to temp files dir. By default: ");
            Console.WriteLine(tmpDir);
            Console.WriteLine("Please enter new path or let it be empty to create dir in source folder.");
            Console.WriteLine("You can use . or only name for the same dir as source directory or .. for parental directory.\nExample: \n./tmp or tmp = X:/src/tmp\n../tmp = X:/tmp");
            string newTempPath = Console.ReadLine();

            Console.WriteLine("\nMax filesize for split video in MB. By default: ");
            Console.WriteLine(splitSize);
            Console.WriteLine("Please enter new size or let it be empty:");
            string newSizeSplit = Console.ReadLine();

            Console.WriteLine("\nVideo format looking for. By default: ");
            Console.WriteLine(format);
            Console.WriteLine("Please enter new format or let it be empty:");
            string newFormat = Console.ReadLine();

            Console.WriteLine("\nConvert video anyway? [y/n] (default: N): ");
            string ignoreSkipSetting = Console.ReadLine();

            Console.WriteLine("\nReplace _ and . with whitespaces? [y/n] (default: {0}): ", ((returnWhiteSpace.ToLower().CompareTo("true") == 0) ? "Y" : "N"));
            string newSettingsWhiteSpaces = Console.ReadLine();

            Console.WriteLine("\nRemove splitted files? [y/n] (default: {0}): ", ((removeSplitted.ToLower().CompareTo("true") == 0) ? "Y" : "N"));
            string newRemoveSplitted = Console.ReadLine();

            Console.WriteLine("\nSave as default settings? [y/n] (default: N): ");
            string newSettings = Console.ReadLine();

            breakLine();

            if (!string.IsNullOrEmpty(newBinPath) && !string.IsNullOrWhiteSpace(newBinPath)) binFiles = newBinPath;
            if (!string.IsNullOrEmpty(newVideoSrcPath) && !string.IsNullOrWhiteSpace(newVideoSrcPath)) srcDir = newVideoSrcPath;
            if (!string.IsNullOrEmpty(newTempPath) && !string.IsNullOrWhiteSpace(newTempPath)) tmpDir = newTempPath;
            if (!string.IsNullOrEmpty(newVideoOutPath) && !string.IsNullOrWhiteSpace(newVideoOutPath)) outDir = newVideoOutPath;
            if (!string.IsNullOrEmpty(newSizeSplit) && !string.IsNullOrWhiteSpace(newSizeSplit)) splitSize = newSizeSplit;
            if (!string.IsNullOrEmpty(newFormat) && !string.IsNullOrWhiteSpace(newFormat)) format = newFormat;
            if (!string.IsNullOrEmpty(newSettingsWhiteSpaces) && !string.IsNullOrWhiteSpace(newSettingsWhiteSpaces))
                if (newSettingsWhiteSpaces.ToLower().CompareTo("y") == 0) returnWhiteSpace = "True";
            if (!string.IsNullOrEmpty(newRemoveSplitted) && !string.IsNullOrWhiteSpace(newRemoveSplitted))
                if (newRemoveSplitted.ToLower().CompareTo("y") == 0) removeSplitted = "True";
            if (!string.IsNullOrEmpty(ignoreSkipSetting) && !string.IsNullOrWhiteSpace(ignoreSkipSetting))
                if (ignoreSkipSetting.ToLower().CompareTo("y") == 0) ignoreSkip = true;
            if (!string.IsNullOrEmpty(newSettings) && !string.IsNullOrWhiteSpace(newSettings))
            {
                if(string.Compare(newSettings.ToLower(), "y") == 0)
                {
                    AddUpdateAppSettings("ffmpeg", binFiles);
                    AddUpdateAppSettings("tmpDir", tmpDir);
                    AddUpdateAppSettings("video_src", srcDir);
                    AddUpdateAppSettings("video_out", outDir);
                    AddUpdateAppSettings("sizeSplit", splitSize);
                    AddUpdateAppSettings("format", format);
                    AddUpdateAppSettings("returnWhiteSpaces", returnWhiteSpace);
                    AddUpdateAppSettings("removeConverted", removeSplitted);
                }
            }

            Directory.SetCurrentDirectory(srcDir);

            if (tmpDir.Contains("./"))
            {
                string[] pathSplits = tmpDir.Split('/');
                var rootDir = Directory.GetCurrentDirectory();
                for (int i = 0, max = (pathSplits.Length - 1); i < max; i++)
                {
                    rootDir = Directory.GetParent(rootDir).FullName;
                }
                tmpDir = rootDir + "\\" + pathSplits.Last();
            }

            if (outDir.Contains("./"))
            {
                string[] pathSplits = outDir.Split('/');
                var rootDir = Directory.GetCurrentDirectory();
                for (int i = 0, max = (pathSplits.Length - 1); i < max; i++)
                {
                    rootDir = System.IO.Directory.GetParent(rootDir).FullName;
                }
                outDir = rootDir + "\\" + pathSplits.Last();
            }

            if (!Directory.Exists(tmpDir))
            {
                DirectoryInfo di = Directory.CreateDirectory(tmpDir);
                Console.WriteLine("The temp files directory was created successfully at {0}.", Directory.GetCreationTime(tmpDir));
            }

            if (!Directory.Exists(outDir))
            {
                DirectoryInfo di = Directory.CreateDirectory(outDir);
                Console.WriteLine("The output directory was created successfully at {0}.", Directory.GetCreationTime(outDir));
            }


            Console.Write("Your config: ");
            Console.WriteLine("FFmpeg bin path: " + binFiles);
            Console.WriteLine("Temp files path: " + tmpDir);
            Console.WriteLine("Video source path: " + srcDir);
            Console.WriteLine("Video output path: " + outDir);
            Console.WriteLine("Max. video filesize: " + splitSize + " MB");
            Console.WriteLine("Ignore skipping, convert anyway: " + (ignoreSkipSetting.ToLower().CompareTo("y") == 0).ToString());
            Console.WriteLine("Replace . and _ with whitespaces: " + (returnWhiteSpace.ToLower().CompareTo("y") == 0).ToString());
            Console.WriteLine("Remove splitted: " + (removeSplitted.ToLower().CompareTo("y") == 0).ToString());
            Console.Write("Convert only: " + format.ToUpper() + " files");

            breakLine();
            genVideos();
            breakLine();
            splitFiles();
            breakLine();
            cleanData();

            MessageBox.Show(string.Format("All {0} files were splitted successfully!", videosToSplit));

        }

        static void cleanData()
        {
            List<string> files = ScanFiles(tmpDir, "*.*");
            bool clean = false;
            if (files.Count > 0)
            {
                Console.WriteLine("You have {0} files in your temporary folder. Do you wish to clean them? [y/n]", files.Count);
                string cleanAgree = Console.ReadLine();

                if (cleanAgree.ToLower().CompareTo('y') == 0) clean = true;
            }
            else clean = true;

            try
            {
                try
                {
                    if (clean) Directory.Delete(tmpDir, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Temporary folder is in use and couldn't be removed!");
                    foreach (var f in files)
                    {
                        File.Delete(f);
                        Console.WriteLine("{0} has been deleted", f);
                    }
                }

                Console.WriteLine("Temporary data has been removed!");
            }
            catch (Exception e)
            {
                string errorFile = string.Format(@"{0}\error_TmpDir.txt", tmpDir);
                using (StreamWriter writer = new StreamWriter(errorFile, true))
                {
                    writer.WriteLine("-----------------------------------------------------------------------------");
                    writer.WriteLine("Date : " + DateTime.Now.ToString());
                    writer.WriteLine();

                    while (e != null)
                    {
                        writer.WriteLine(e.GetType().FullName);
                        writer.WriteLine("Message : " + e.Message);
                        writer.WriteLine("StackTrace : " + e.StackTrace);

                        e = e.InnerException;
                    }
                }
                Console.WriteLine("Temporary folder is in use and couldn't be removed!");
            }

            if(Convert.ToBoolean(removeSplitted))
            {
                foreach (var item in videos.FindAll(x => x._skip == false))
                {
                    File.Delete(item.path);
                    Console.WriteLine("{0} has been deleted", item.name);
                }
            }
        }

        static void consoleCommand(string exe, string command, bool newShell = true)
        {
            string fileName = string.Format("\"{0}\\{1}\"", binFiles, exe).Replace(@"\", @"/");
            var splitterInfo = new ProcessStartInfo
            {
                UseShellExecute = newShell,
                FileName = fileName,
                Arguments = command
            };

            using (var process = Process.Start(splitterInfo))
            {
                process.WaitForExit();
            }
        }

        static void genVideos()
        {
            List<string> files = ScanFiles(srcDir, "*." + format);
            using (var progress = new ProgressBar())
            {
                int videoNow = 0;
                Console.WriteLine("Collect data...");
                foreach (string item in files)
                {
                    FileInfo fi = new FileInfo(item);
                    if (format.Equals("*"))
                    {
                        if (!allowedFormats.Contains(fi.Extension.Replace(".", "").ToLower()))
                        {
                            continue;
                        }
                    }

                    Console.WriteLine("   " + fi.Name + "\n");

                    var ffProbe = new NReco.VideoInfo.FFProbe();
                    ffProbe.ToolPath = binFiles;
                    try
                    {
                        var videoInfo = ffProbe.GetMediaInfo(item);

                        videoFile vid = new videoFile();
                        vid.name = fi.Name.Replace(fi.Extension, "");
                        vid.duration_sec = (float)videoInfo.Duration.TotalSeconds;
                        vid.format = fi.Extension.Replace(".", "").ToLower();
                        vid.path = fi.FullName.Replace(@"\", @"/");
                        vid.size = fi.Length;
                        vid.setSplitter();
                        if (ignoreSkip) vid._skip = false;
                        videos.Add(vid);
                    }
                    catch (Exception e)
                    {
                        string errorFile = string.Format(@"{0}\{1}_error.txt", tmpDir, fi.Name);
                        Console.WriteLine("Error: {0}", fi.Name);
                        using (StreamWriter writer = new StreamWriter(errorFile, true))
                        {
                            writer.WriteLine("-----------------------------------------------------------------------------");
                            writer.WriteLine("Date : " + DateTime.Now.ToString());
                            writer.WriteLine();

                            while (e != null)
                            {
                                writer.WriteLine(e.GetType().FullName);
                                writer.WriteLine("Message : " + e.Message);
                                writer.WriteLine("StackTrace : " + e.StackTrace);

                                e = e.InnerException;
                            }
                        }

                        continue;
                    }

                    progress.Report((double)videoNow / files.Count);
                    videoNow++;
                }
            }
            breakLine();
            Console.WriteLine("Done.");
            breakLine();
            videosToSplit = videos.Count(x => x._skip == false);

            Console.WriteLine("Total {0} files counted,", files.Count);
            Console.Write("{0} will be processed.", videosToSplit);
        }

        private static List<string> ScanFiles(string path, string pattern )
        {
            return Directory.GetFiles(path, pattern).ToList();
        }

        private static void drawTextProgressBar(int progress, int total, string name)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(name + " (part " +progress.ToString() + " of " + total.ToString() + ")    "); //blanks at the end remove any excess
        }

        static void splitFiles()
        {
            string Filler(int splits)
            {
                int intLenght = (int)splits.ToString().Length - 1;
                string fill = "";
                for (int t = 0; t < intLenght; t++)
                {
                    int compareMax = 10 ^ intLenght;
                    int compareMin = 10 ^ t;
                    if (splits < 10) fill += "0";
                    if (splits >= compareMin && splits > compareMax)
                    {
                       fill += "0";
                    }
                }
                return fill + splits.ToString();
            }

            int videoNow = 0;

            Console.Write("Begin to splitting... ");
            using (var progress = new ProgressBar())
            {
                foreach (var video in videos.FindAll(x => x._skip == false))
                {
                    progress.Report((double)videoNow / videosToSplit);
                    for(int i = 0, max = video._splitter.Count; i < max; i++)
                    {
                        string tempName = video.name + "_p" + Filler((i + 1)) + "." + convertTo;
                        TimeSpan tStart = TimeSpan.FromSeconds(video._splitter[i].Start);
                        TimeSpan tEnd = TimeSpan.FromSeconds(video._splitter[i].End);
                        string outPath = string.Format("{0}\\{1}", outDir, tempName);
                        string replaceName = "";
                        if(Convert.ToBoolean(returnWhiteSpace)) replaceName = string.Format("{0}\\{1}", outDir, video.name + "_p" + Filler((i + 1)).Replace("_", " ").Replace(".", " ")) + "." + convertTo;

                        string startTime = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                                        tStart.Hours,
                                                        tStart.Minutes,
                                                        tStart.Seconds);
                        string endTime = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                                        tEnd.Hours,
                                                        tEnd.Minutes,
                                                        tEnd.Seconds);
                        string command = string.Format("-i \"{0}\" -ss {1} -to {2} -c copy \"{3}\"", video.path, startTime, endTime, outPath);

                        drawTextProgressBar((i + 1), max, video.name);

                        consoleCommand("ffmpeg.exe", command);

                    }
                    videoNow++;

                }
            }
            Console.WriteLine("Done.");
        }
    }
}
