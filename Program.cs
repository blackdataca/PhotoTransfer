// See https://aka.ms/new-console-template for more information

using System.Reflection;

ConsoleWriteLine(AppDomain.CurrentDomain.FriendlyName  + " " + Assembly.GetExecutingAssembly().GetName().Version) ;

if (args.Length == 2)
{
    string sourceRoot = args[0];
    string targetRoot = args[1];
    if (!targetRoot.EndsWith(Path.DirectorySeparatorChar))
        targetRoot+= Path.DirectorySeparatorChar;
    if (!Directory.Exists(targetRoot))
    {
        ConsoleWriteLine($"target_dir does not exist: {targetRoot}");
        return;
    }
    ConsoleWriteLine($"PhotoTransfer from {sourceRoot} to {targetRoot}");

    int n = 0;
    int total = 0;
    long sessionBytes = 0;

    foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*.*", System.IO.SearchOption.AllDirectories))
    {
        Thread.Sleep(1);
        n++;
        DateTime creation = File.GetCreationTime(sourceFile);
        DateTime lastWrite = File.GetLastWriteTime(sourceFile);
        string y = creation.ToString("yyyy");
        string m = creation.ToString("MM");
        string d = creation.ToString("yyyy-MM-dd");
        string targetFileNameOnly = Path.GetFileName(sourceFile);
        string targetDir = targetRoot + y + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + d;
        string targetFile = Path.Combine(targetDir , targetFileNameOnly);
        long sourceLen = new FileInfo(sourceFile).Length;


        ConsoleWrite($"Copying {n}: {sourceFile} ({BytesToString(sourceLen)}) to {targetFile} ");
        
        if (targetFileNameOnly.StartsWith("."))
        {
            ConsoleWriteLine("skip", true);
            continue;
        }

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        if (File.Exists(targetFile))
        {
            long targetLen = new FileInfo(targetFile).Length;
            ConsoleWrite($"({BytesToString(targetLen)}) ", true);
            if (targetLen < sourceLen)
                File.Delete(targetFile);
            else
            {
                if (File.GetCreationTime(targetFile).Date == DateTime.Now.Date)
                    File.Delete(targetFile);
            }
        }


        if (!File.Exists(targetFile))
        {
            ConsoleWrite("...", true);
            File.Copy(sourceFile, targetFile, false);

            File.SetCreationTime(targetFile, creation);
            File.SetLastWriteTime(targetFile, lastWrite);
            long len = new FileInfo(targetFile).Length;
            sessionBytes += len;
            ConsoleWriteLine($"copied {BytesToString(len)} session: {BytesToString(sessionBytes)}", true);
            total++;
        }
        else
        {
            ConsoleWriteLine("exist", true);
        }
        
    }
    ConsoleWriteLine($"Total {total:N0} files, {BytesToString(sessionBytes)} transferred");

}
else
{
    ConsoleWriteLine("Arguments: PhotoTransfer.exe source_dir target_dir");
}

static void ConsoleWriteLine(string message, bool omitDate = false)
{
    if (!omitDate)
        Console.Write(DateTime.Now + " ");
    Console.WriteLine(message);
}

static void ConsoleWrite(string message, bool omitDate = false)
{
    if (!omitDate)
        Console.Write(DateTime.Now + " ");
    Console.Write(message);
}

static String BytesToString(long byteCount)
{
    string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
    if (byteCount == 0)
        return "0" + suf[0];
    long bytes = Math.Abs(byteCount);
    int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
    double num = Math.Round(bytes / Math.Pow(1024, place), 1);
    return (Math.Sign(byteCount) * num).ToString() + suf[place];
}