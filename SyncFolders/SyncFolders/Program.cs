internal class Program
{
    private static bool exitOnDemand = false;

    private static void Main(string[] args)
    {
        // Args should be of size 4
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: Program <source_folder_path> <replica_folder_path> <sync_interval>(seconds) <log_file_path>.");
            return;
        }

        string sourcePath = args[0]; // The path for the source folder
        if (!Directory.Exists(sourcePath)) // Check if it is a valid path
        {
            Console.WriteLine($"Error: Invalid source folder path. '{sourcePath}' does not exist.");
            return;
        }

        string replicaPath = args[1]; // The path for the replica folder
        // Create the replica folder if it does not exits already
        if (!Directory.Exists(replicaPath))
        {
            try
            {
                Directory.CreateDirectory(replicaPath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}"); // Show the message error
                return;
            }
        }

        if (!int.TryParse(args[2], out int interval)) // The interval for the synchronization
        {
            Console.WriteLine("Error: Invalid value for the synchronization interval.");
            return;
        }

        string logFile = args[3]; // The path for the log file
        // Check if the Log files exists, if not create a new one
        if (!File.Exists(logFile))
        {
            try
            {
                // Create a new log file with a 'Title'
                using StreamWriter sw = new StreamWriter(logFile);
                sw.WriteLine("Test_Task - LOG");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Problem creating the Log file. {e.Message}");
                return;
            }
        }

        Console.WriteLine("Press ESC to exit.");

        // Create a separated thread to catch user pressing 'ESC' to exit
        Thread exitThread = new Thread(CheckExitingCommand);
        exitThread.Start();

        while (!exitOnDemand)
        {
            try
            {
                Sync(sourcePath, replicaPath, logFile);
                Thread.Sleep(1000 * interval); // Since the argument is in milliseconds we convert the input interval to the correct corresponding value 
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
        }

        exitThread.Join();
        Console.WriteLine("...Exiting...");
    }

    /// <summary>
    /// Check is user wants to exit the program
    /// </summary>
    private static void CheckExitingCommand()
    {
        while (true)
        {
            // Check for 'ESC' button pressed
            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
            {
                exitOnDemand = true; // Flag to exit
                Thread.CurrentThread.Interrupt();
                break;
            }
        }
    }

    /// <summary>
    /// Sync source folder with replica folder
    /// </summary>
    /// <param name="sourcePath">Source folder path</param>
    /// <param name="replicaPath">Replica folder path</param>
    /// <param name="logFile">Log file to write the events</param>
    private static void Sync(string sourcePath, string replicaPath, string logFile)
    {
        // Thread to check for new/modified files/folders
        Thread tNew = new Thread(() => RecursiveNewModifiedSync(sourcePath, replicaPath, logFile));
        tNew.Start();

        // Thread to check for deleted files/folders
        Thread tDelete = new Thread(() => RecursiveDeletedSync(sourcePath, replicaPath, logFile));
        tDelete.Start();
    }

    /// <summary>
    /// Recursive method to check sub directories and respective files
    /// </summary>
    /// <param name="source">Source path to check</param>
    /// <param name="replica">Replica path to add/copy</param>
    /// <param name="logFile">Log file to write</param>
    private static void RecursiveNewModifiedSync(string source, string replica, string logFile)
    {
        foreach (string subSourceDir in Directory.EnumerateDirectories(source)) // Check subsequent folders/directories
        {
            string relativePath = Path.GetRelativePath(source, subSourceDir);
            string replicaSubDirectory = Path.Combine(replica, relativePath);

            if (!Directory.Exists(replicaSubDirectory)) // Check if directory already exists
            {
                try
                {
                    Directory.CreateDirectory(replicaSubDirectory); // Create the new directory
                    OperationLog(logFile, $"{DateTime.Now}: Create Folder: {relativePath}"); // Log method
                    RecursiveNewModifiedSync(subSourceDir, replicaSubDirectory, logFile); // Check again for sub directories
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}"); // Show the message error
                }
            }
        }

        foreach (string sourceFile in Directory.EnumerateFiles(source)) // Check all files in current folder
        {
            string fileName = Path.GetFileName(sourceFile); // Get source file name
            string replicaFile = Path.Combine(replica, fileName); // Get replica path + file name

            // Check if file already exists
            if (!File.Exists(replicaFile))
            {
                try
                {
                    File.Create(replicaFile); // Create replica file in the replica folder
                    OperationLog(logFile, $"{DateTime.Now}: Created: {fileName}"); // Log method
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}"); // Show the message error
                }
            }
            else
            {
                try
                {
                    if (IsFileBeingUsed(replicaFile) || IsFileBeingUsed(sourceFile))
                        continue;

                    FileInfo fileInfoSource = new FileInfo(sourceFile); // Get file info of the source file
                    FileInfo fileInfoReplica = new FileInfo(replicaFile); // Get the file info of the replica file

                    // Check if the source file was modified
                    if (fileInfoSource.LastWriteTimeUtc > fileInfoReplica.LastWriteTimeUtc)
                    {
                        File.Copy(sourceFile, replicaFile, true); // Copy the modified file, overwriting the existing one
                        OperationLog(logFile, $"{DateTime.Now}: Copied: {fileName}"); // Log method
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}"); // Show the message error
                }
            }
        }
    }

    /// <summary>
    /// Recursive method to check for deleted files/folders
    /// </summary>
    /// <param name="source">Source path to check</param>
    /// <param name="replica">Replica path to delete</param>
    /// <param name="logFile">Log file to write</param>
    private static void RecursiveDeletedSync(string source, string replica, string logFile)
    {
        foreach (string subReplicaDir in Directory.EnumerateDirectories(replica)) // Check subsequent folders/directories
        {
            string relativePath = Path.GetRelativePath(replica, subReplicaDir);
            string sourceSubDirectory = Path.Combine(source, relativePath);

            if (!Directory.Exists(sourceSubDirectory)) // Check if directory still exits
            {
                try
                {
                    Directory.Delete(subReplicaDir, true); // If not delete from replica folder
                    OperationLog(logFile, $"{DateTime.Now}: Delete Folder: {relativePath}"); // Log method
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}"); // Show the message error
                }
            }
            else
            {
                RecursiveDeletedSync(sourceSubDirectory, subReplicaDir, logFile); // Check again for sub directories
            }
        }

        foreach (string file in Directory.EnumerateFiles(replica))
        {
            try
            {
                string fileName = Path.GetFileName(file); // Get replica file name
                string sourceFile = Path.Combine(source, fileName); // Get source path + file name

                // Check if the replica file exists in source path
                if (!File.Exists(sourceFile))
                {
                    File.Delete(file); // Delete the replica file
                    OperationLog(logFile, $"{DateTime.Now}: Deleted: {fileName}"); // Log method
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}"); // Show the message error
            }
        }
    }

    /// <summary>
    /// Check for file usage, in order to skip and trying to copy it in the next interation
    /// </summary>
    /// <param name="filePath">File path to check</param>
    /// <returns>
    /// True - is being used
    /// False - is available
    /// </returns>
    private static bool IsFileBeingUsed(string filePath)
    {
        try
        {
            using FileStream s = File.Open(filePath, FileMode.Open, FileAccess.Read); // Throws exception if open, etc
        }
        catch (Exception)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Write/show the event
    /// </summary>
    /// <param name="logFile">Log file to write</param>
    /// <param name="msg">
    /// Message to show/write</param>
    private static void OperationLog(string logFile, string msg)
    {
        Console.WriteLine(msg); // Show the log in the console
        LogFileWrite(logFile, msg); // Write the log in the Log file
    }

    /// <summary>
    /// Write the event in the log file
    /// </summary>
    /// <param name="logFile">Log file to write</param>
    /// <param name="msg">Message to write</param>
    private static void LogFileWrite(string logFile, string msg)
    {
        using StreamWriter sw = new(logFile, true);
        sw.WriteLine(msg);
    }
}