// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using System.Globalization;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.CodeDom.Compiler;

ConsoleWriteLine(AppDomain.CurrentDomain.FriendlyName + " " + Assembly.GetExecutingAssembly().GetName().Version);
string[] imageFileExtensions = { ".jpg", ".heic", ".jpeg", ".png" };
string[] videoFileExtensions = { ".mp4", ".mov", ".m4v", ".flv", ".mts", ".avi" };

if (args.Length < 3)
{
    ConsoleWriteLine("Arguments: PhotoTransfer.exe source_dir target_dir -move true|false [-imageSize >n<N] [-videoSize >n<N]");
    return;
}



string sourceRoot = args[0];
if (!sourceRoot.EndsWith(Path.DirectorySeparatorChar))
    sourceRoot += Path.DirectorySeparatorChar;
string targetRoot = args[1];
if (!targetRoot.EndsWith(Path.DirectorySeparatorChar))
    targetRoot += Path.DirectorySeparatorChar;

bool deleteSource = true;
if (args.Contains("-move"))
    deleteSource = bool.Parse(args[Array.IndexOf(args, "-move") + 1]);

long maxImageSize = long.MaxValue;
long minImageSize = long.MinValue;

GetSizes(args, "-imageSize", ref maxImageSize, ref minImageSize);

long maxVideoSize = long.MaxValue;
long minVideoSize = long.MinValue;

GetSizes(args, "-videoSize", ref maxVideoSize, ref minVideoSize);

if (!Directory.Exists(targetRoot))
{
    ConsoleWriteLine($"target_dir does not exist: {targetRoot}");
    return;
}
ConsoleWriteLine($"PhotoTransfer {(deleteSource?"move":"copy")} from {sourceRoot} to {targetRoot}");
Console.TreatControlCAsInput = true;

int n = 0;
int total = 0;
int skipped = 0;
long sessionBytes = 0;
string[] allFiles = Directory.GetFiles(sourceRoot, "*.*", System.IO.SearchOption.AllDirectories);
foreach (var sourceFile in allFiles)
{
    n++;
    Thread.Sleep(1);
    if (Console.KeyAvailable)
    {
        var keyPressed = Console.ReadKey();
        if (keyPressed.Key == ConsoleKey.Escape || keyPressed.Modifiers == ConsoleModifiers.Control && keyPressed.Key == ConsoleKey.C)
        {
            break;
        }
    }
    string? p = Path.GetDirectoryName(sourceFile);
    if (p == null || p.EndsWith("Trash") || p.IndexOf("\\.")>0)
    {
        skipped++;
        continue;
    }
    string ext = Path.GetExtension(sourceFile).ToLower();
    if (imageFileExtensions.Contains(ext) || videoFileExtensions.Contains(ext)) 
    {
        if (deleteSource)
            ConsoleWrite("Moving");
        else
            ConsoleWrite("Copying");
        ConsoleWrite($" {n:N0}/{allFiles.Length:N0}: {sourceFile} ", true);
        if (!File.Exists(sourceFile))
        {
            ConsoleWriteLine(" not found", true, ConsoleColor.Red);
            continue;
        }

        bool isArchive = (File.GetAttributes(sourceFile) & FileAttributes.Archive) == FileAttributes.Archive;
        if (!isArchive)
        {
            ConsoleWriteLine(" no archive bit", true, ConsoleColor.Yellow);
            continue;
        }

        long sourceLen = new FileInfo(sourceFile).Length;
        ConsoleWrite($"({BytesToHuman(sourceLen)}) ", true);
        if (imageFileExtensions.Contains(ext) && ( sourceLen < minImageSize || sourceLen>maxImageSize) ||
             videoFileExtensions.Contains(ext) && (sourceLen < minVideoSize || sourceLen > maxVideoSize))
        {
            ConsoleWriteLine("wrong size", true, ConsoleColor.Yellow);
            continue;
        }

        DateTime creation = GetDateTakenFromImage(sourceFile); // File.GetCreationTime(sourceFile);
        if (creation == DateTime.MinValue || creation.Year<2000)
        {
            if (sourceRoot != targetRoot)
            {
                ConsoleWriteLine("no timestamp found. skip", true, ConsoleColor.Red);
                skipped++;
                File.SetAttributes(sourceFile, File.GetAttributes(sourceFile) & ~FileAttributes.Archive);
                continue;
            }
        }
        DateTime lastWrite = File.GetLastWriteTime(sourceFile);
        if (lastWrite < creation)
            lastWrite = creation;
        string targetFileNameOnly = Path.GetFileName(sourceFile);
        string? targetDir = Path.GetDirectoryName(sourceFile);
        if (targetDir == null)
            targetDir = sourceRoot;
        targetDir = targetDir.Replace(sourceRoot, targetRoot);
        if (creation != DateTime.MinValue)
        {
            string y = creation.ToString("yyyy");
            string m = creation.ToString("MM");
            string d = creation.ToString("yyyy_MM_dd");
            targetDir = targetRoot + y + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + d;
        }
        string targetFile = Path.Combine(targetDir, targetFileNameOnly);



        ConsoleWrite($"to {targetFile} ", true);

        if (targetFileNameOnly.StartsWith("."))
        {
            ConsoleWriteLine("skip", true, ConsoleColor.Yellow);
            skipped++;
            File.SetAttributes(sourceFile, File.GetAttributes(sourceFile) & ~FileAttributes.Archive);
            continue;
        }

        //if (!Directory.Exists(targetDir))
        //    Directory.CreateDirectory(targetDir);

        if (File.Exists(targetFile) && (sourceFile != targetFile))
        {
            long targetLen = new FileInfo(targetFile).Length;
            ConsoleWrite($"({BytesToHuman(targetLen)}) ", true);
            if (targetLen < sourceLen)
                File.Delete(targetFile);
            else
            {
                if (File.GetCreationTime(targetFile).Date == DateTime.Now.Date)
                    File.Delete(targetFile);
            }
        }


        if (!File.Exists(targetFile) || (sourceFile == targetFile))
        {
            ConsoleWrite("...", true);

            string result = FfMpeg("ffprobe", $"-v error -select_streams v:0 -show_entries stream \"{sourceFile}\"");
            if (result.IndexOf("side_data_type=Display Matrix") >0)
            {
                //rotate 180
                ConsoleWrite("removing rotation ", true, ConsoleColor.Green);
                string tempFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileNameWithoutExtension(sourceFile) + "fix" + Path.GetExtension(sourceFile));
                FfMpeg("ffmpeg",$"-i \"{sourceFile}\" -metadata:s:v:0 rotate=0 \"{tempFile}\" -y");
                File.Delete(sourceFile);
                File.Move(tempFile, sourceFile, false);
                //ConsoleWrite("removed rotation, ", true, ConsoleColor.Green);
            }
            if (sourceRoot == targetRoot)
            {
                ConsoleWriteLine("skip", true, ConsoleColor.Yellow);
                skipped++;
                File.SetAttributes(sourceFile, File.GetAttributes(sourceFile) & ~FileAttributes.Archive);
                continue;
            }
            
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            if (deleteSource && sourceFile.Substring(0, 2).Equals(targetFile.Substring(0, 2), StringComparison.OrdinalIgnoreCase))
                File.Move(sourceFile, targetFile, false);
            else
            {
                byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                using (FileStream source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    //long fileLength = source.Length;
                    using (FileStream dest = new FileStream(targetFile, FileMode.CreateNew, FileAccess.Write))
                    {
                        long totalBytes = 0;
                        int currentBlockSize = 0;
                        //int lastPer = 0;
                        var start = DateTime.Now;
                        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            dest.Write(buffer, 0, currentBlockSize);

                            totalBytes += currentBlockSize;
                            int percentage = (int)(totalBytes * 100 / sourceLen);

                            //if (percentage != lastPer)
                            //{
                            //lastPer = percentage;
                            var totalSeconds = DateTime.Now - start;
                            int eta = 0; //seconds
                            if (totalSeconds.TotalSeconds != 0)
                            {
                                var speed = totalBytes / totalSeconds.TotalSeconds; // B/s
                                var remaind = sourceLen - totalBytes;
                                eta = (int)(remaind / speed);

                            }
                            string per = $"{percentage:N0}% -{HumanTime(eta)}   ";
                            StringBuilder bs = new StringBuilder();
                            for (int i = 0; i < per.Length; i++)
                                bs.Append("\b");
                            ConsoleWrite($"{per}{bs}", true);
                            Thread.Sleep(1);
                            //}


                        }
                    }
                }

                if (deleteSource)
                    File.Delete(sourceFile);
            }
            if (creation != DateTime.MinValue)
                File.SetCreationTime(targetFile, creation);
            if (lastWrite != DateTime.MinValue)
               File.SetLastWriteTime(targetFile, lastWrite);

            long len = new FileInfo(targetFile).Length;
            sessionBytes += len;
            if (deleteSource)
                ConsoleWrite("moved", true, ConsoleColor.Green);
            else
                ConsoleWrite("copied", true, ConsoleColor.Green);
            ConsoleWriteLine($" {BytesToHuman(len)} session {BytesToHuman(sessionBytes)}", true, ConsoleColor.Green);

            if (File.Exists(sourceFile))
            {
                File.SetAttributes(sourceFile, File.GetAttributes(sourceFile) & ~FileAttributes.Archive);
            }
            total++;
        }
        else
        {
            ConsoleWriteLine("exist", true, ConsoleColor.Yellow);
            if (deleteSource && (sourceFile != targetFile))
            {
                File.Delete(sourceFile);
            }
            skipped++;
        }
    }
    else
    {
        //ConsoleWriteLine($"Skip {n}/{allFiles.Length}: {sourceFile}", false, ConsoleColor.Yellow);
        skipped++;
    }
}
ConsoleWriteLine($"Copied {total:N0} files, skipped {skipped:N0} files, {BytesToHuman(sessionBytes)} transferred", false, ConsoleColor.White);




static void ConsoleWriteLine(string message, bool omitDate = false, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    if (!omitDate)
        Console.Write(DateTime.Now + " ");
    Console.WriteLine(message);
}

static void ConsoleWrite(string message, bool omitDate = false, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    if (!omitDate)
        Console.Write(DateTime.Now + " ");
    Console.Write(message);
}

static string HumanTime(double seconds)
{
    TimeSpan t = TimeSpan.FromSeconds(seconds);
    var sb = new StringBuilder();
    if (t.Hours > 0)
        sb.Append(t.Hours).Append("h");
    if (t.Minutes > 0)
        sb.Append(t.Minutes).Append("m");
    sb.Append(t.Seconds).Append("s");
    return sb.ToString();
}
static String BytesToHuman(long byteCount)
{
    string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
    if (byteCount == 0)
        return "0" + suf[0];
    long bytes = Math.Abs(byteCount);
    int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
    double num = Math.Round(bytes / Math.Pow(1024, place), 1);
    return (Math.Sign(byteCount) * num).ToString() + suf[place];
}

static long HumanToBytes(string sizeString)
{
    string pattern = @"^(\d+(\.\d+)?)\s*([KMGTP]B)$";

    Match match = Regex.Match(sizeString, pattern, RegexOptions.IgnoreCase);

    if (match.Success)
    {
        double value = double.Parse(match.Groups[1].Value);
        string unit = match.Groups[3].Value.ToUpper();

        long bytes = (long)(value * GetMultiplier(unit));
        return bytes;
    }

    throw new ArgumentException("Invalid size format.");
}

static long GetMultiplier(string unit)
{
    switch (unit.ToUpper())
    {
        case "KB":
            return 1024;
        case "MB":
            return 1024 * 1024;
        case "GB":
            return 1024 * 1024 * 1024;
        case "TB":
            return 1024L * 1024 * 1024 * 1024;
        case "PB":
            return 1024L * 1024 * 1024 * 1024 * 1024;
        default:
            throw new ArgumentException("Invalid size unit.");
    }
}

static DateTime GetDateTakenFromImage(string path)
{
    try
    {
        if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase)
             || path.EndsWith(".avi", StringComparison.OrdinalIgnoreCase)
            )
        {
            using (ShellObject shell = ShellObject.FromParsingName(path))
            {
                var v = shell.Properties.System.Media.DateEncoded.Value;
                if (v == null)
                {
                    //return DateFromJson(path);
                    //ConsoleWriteLine("Error reading date", true, ConsoleColor.Red);
                    return DateTime.MinValue;
                }
                else
                {
                    DateTime datetime = v.Value;
                    return datetime;
                }
            }

        }
        else
        {

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var myImage = System.Drawing.Image.FromStream(fs, false, false))
                {
                    PropertyItem? propItem = myImage.GetPropertyItem(36867);
                    if (propItem == null || propItem.Value == null)
                        return DateFromJson(path);
                    string dateTaken = Encoding.ASCII.GetString(propItem.Value);
                    dateTaken = dateTaken.Replace("?", "");
                    dateTaken = dateTaken.Trim('\0').Trim();

                    if (Regex.Match(dateTaken, @"^\d{4}:\d{2}:\d{2} \d{2}:\d{2}:\d{2}").Success)
                    {
                        var regex = new Regex(Regex.Escape(":"));
                        dateTaken = regex.Replace(dateTaken, "-", 2);
                        return DateTime.Parse(dateTaken, CultureInfo.InvariantCulture);
                        //return DateTime.ParseExact(dateTaken, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    else if (Regex.Match(dateTaken, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$").Success)
                        return DateTime.ParseExact(dateTaken, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    else
                        return DateTime.Parse(dateTaken, CultureInfo.InvariantCulture);
                }
            }

        }



    }
    catch (System.ArgumentException ex)
    {
        if (ex.Message == "Property cannot be found." || ex.Message == "Parameter is not valid.")
        {
            return DateFromJson(path);
        }
        else
            throw ex;
    }


}

static DateTime DateFromImage(string file)
{
    try
    {
        using (var myImage = System.Drawing.Image.FromFile(file))
        {
            PropertyItem propItem = myImage.GetPropertyItem(306);
            DateTime dtaken;

            //Convert date taken metadata to a DateTime object
            string sdate = Encoding.UTF8.GetString(propItem.Value).Trim();
            string secondhalf = sdate.Substring(sdate.IndexOf(" "), (sdate.Length - sdate.IndexOf(" ")));
            string firsthalf = sdate.Substring(0, 10);
            firsthalf = firsthalf.Replace(":", "-");
            sdate = firsthalf + secondhalf;
            dtaken = DateTime.Parse(sdate);
            return dtaken;
        }
    }
    catch (System.OutOfMemoryException)
    {
        return DateTime.MinValue;
    }
    catch (System.ArgumentException)
    {
        return DateTime.MinValue;
    }
    catch (Exception ex)
    {
        ConsoleWriteLine(ex.ToString(), true, ConsoleColor.Yellow);
        return DateTime.MinValue;
    }
}

static DateTime DateFromJson(string file)
{

    DateTime dt = DateFromImage(file);
    if (dt != DateTime.MinValue)
        return dt;

    string jsonFile = file + ".json";
    if (File.Exists(jsonFile))
    {
        string json = File.ReadAllText(jsonFile);

        dynamic data = JObject.Parse(json);
        double timeStamp = data.photoTakenTime.timestamp;
        return UnixTimeStampToDateTime(timeStamp);
    }
    string? spath = Path.GetDirectoryName(file);
    if (spath == null)
        return DateTime.MinValue;

    if (Path.GetFileNameWithoutExtension(file).EndsWith("(1)"))
    {
        jsonFile = Path.Combine(spath, Path.GetFileNameWithoutExtension(file).Replace("(1)", "") + Path.GetExtension(file) + "(1)" + ".json");
        if (File.Exists(jsonFile))
        {
            string json = File.ReadAllText(jsonFile);

            dynamic data = JObject.Parse(json);
            double timeStamp = data.photoTakenTime.timestamp;
            return UnixTimeStampToDateTime(timeStamp);
        }
    }
    else if (Path.GetFileNameWithoutExtension(file).EndsWith("(2)"))
    {
        jsonFile = Path.Combine(spath, Path.GetFileNameWithoutExtension(file).Replace("(2)", "") + Path.GetExtension(file) + "(1)" + ".json");
        if (File.Exists(jsonFile))
        {
            string json = File.ReadAllText(jsonFile);

            dynamic data = JObject.Parse(json);
            double timeStamp = data.photoTakenTime.timestamp;
            return UnixTimeStampToDateTime(timeStamp);
        }
    }
    else
    {
        jsonFile = Path.Combine(spath, Path.GetFileName(file) + "(1)" + ".json");
        if (File.Exists(jsonFile))
        {
            string json = File.ReadAllText(jsonFile);

            dynamic data = JObject.Parse(json);
            double timeStamp = data.photoTakenTime.timestamp;
            return UnixTimeStampToDateTime(timeStamp);
        }
        else
        {
            jsonFile = Path.Combine(spath, Path.GetFileNameWithoutExtension(file) + ".JPG" + ".json");
            if (File.Exists(jsonFile))
            {
                string json = File.ReadAllText(jsonFile);

                dynamic data = JObject.Parse(json);
                double timeStamp = data.photoTakenTime.timestamp;
                return UnixTimeStampToDateTime(timeStamp);
            }
        }
    }

    jsonFile = Path.Combine(spath, "metadata.json");
    if (File.Exists(jsonFile))
    {

        string json = File.ReadAllText(jsonFile);

        dynamic data = JObject.Parse(json);
        double timeStamp = data.date.timestamp;
        return UnixTimeStampToDateTime(timeStamp);
    }

    dt = DateTime.MinValue;
    DateTime.TryParse(Path.GetFileNameWithoutExtension(file).Replace(".", ":"), out dt);

    return dt;
}

static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
{
    // Unix timestamp is seconds past epoch
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
    return dateTime;
}

static string FfMpeg(string app, string parameters)
{
    string result = String.Empty;

    using (Process p = new Process())
    {
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = $"C:\\Program Files\\ffmpeg\\bin\\{app}.exe";
        p.StartInfo.Arguments = parameters;
        p.Start();
        p.WaitForExit();

        result = p.StandardOutput.ReadToEnd();
    }

    return result;
}

static void GetSizes(string[] args, string para, ref long maxSize, ref long minSize)
{
    if (args.Contains(para))
    {
        string customSize = args[Array.IndexOf(args, para) + 1];
        string pattern = @">(.*)<(.*)";

        Match match = Regex.Match(customSize, pattern);

        if (match.Success)
        {
            string min = match.Groups[1].Value;
            minSize = HumanToBytes(min);
            string max = match.Groups[2].Value;
            maxSize = HumanToBytes(max);
            Console.WriteLine($">{min}:{minSize:N0} <{max}:{maxSize:N0}");
        }
    }
}