# FoldersSync
Test Task

Usage: SyncFolder.exe "<source_folder_path>" "<replica_folder_path>" <interval> "<log_file_path>"

This is a simple example on how to create a syncronization between two folders, source and replica.

The program will not work if:
- Number of arguments isn't 4
- Source folder path isn't valid
- Replica folder does not exists, and can't create it
- Invalid value (ex: string) for the interval number
- Log file does not exists, and can't create it

How it works:
- The program works in the command prompt
- Is it possible for the user to exit the program by pressing 'ESC'
- Given the interval, the program will check for modifications in the file collection on the source folder, granting the synchronization between the two folders
- Events on the replica folder are displayed in as console output and registered in the log file
- New folders in the source folder are added to the replica folder, and logged as 'Create folder'
- New files in the source folder are added to the replica folder, and logged as 'Created'
- Modified files in the source folder are copied to the replica folder, and logged as 'Copied'
- Deleted folder in the source folder are deleted from the replica folder, and logged as 'Delete folder'
- Deleted files in the source folder are deleted from the replica folder, and logged as 'Deleted'

Made by:
Daniel Silva 26-05-2024

Updaded: 27-05-2024

Program made using:
Microsoft Visual Studio Community 2022 (64 bits) - Current
Versão 17.9.2
