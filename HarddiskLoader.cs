using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    public class HarddiskLoader
    {

        public static void SaveHarddisk(Harddisk disk)
        {
            // Check our directories to see if this one exists
            if (Directory.Exists(disk.Directory))
            {
                // If it does we incinerate it
                Directory.Delete(disk.Directory, true);
            }

            Directory.CreateDirectory(disk.Directory);
            // Recreate the directory, and save the files
            foreach (File file in disk.Files)
            {
                System.IO.File.WriteAllLines(disk.Directory + "/" + file.Filename + ".kos", file.ToArray());
            }
        }

        // For loading the whole disk, useful on startup to set up each disk
        public static Harddisk LoadHarddisk(String diskName)
        {
            String directory = HighLogic.fetch.GameSaveFolder + "/" + diskName;

            // Look for the disk we want to load
            if (Directory.Exists(directory))
            {
                // Create a new disk, I think I saw 10000 being used everywhere else, then name it properly
                Harddisk hd = new Harddisk(10000);
                hd.Name = diskName;

                // Load the file names within the directory
                String[] files = Directory.GetFiles(directory);

                foreach (String file in files)
                {
                    // Create an array for each file, create the new file with the proper name,
                    // add each string to the File, and finally add the file to the volume
                    String[] content = System.IO.File.ReadAllLines(file);
                    File hdFile = new File(file);

                    foreach (String s in content)
                    {
                        hdFile.Add(s);
                    }

                    hd.Files.Add(hdFile);
                }

                return hd;
            }

            // If not found, let them know
            else throw new kOSException("Disk '" + diskName + "' could not be found");
        }

        // Useful for reloading the files in a disk
        public static List<File> LoadFiles(String diskName)
        {
            String directory = HighLogic.fetch.GameSaveFolder + "/" + diskName;

            List<File> allFiles = new List<File>();

            // Again, make sure the disk exists
            if (Directory.Exists(directory))
            {
                // Load up the names of each file
                String[] files = Directory.GetFiles(directory);

                foreach (String file in files)
                {
                    // Read all the lines and create new Files, then add them to the list
                    String[] content = System.IO.File.ReadAllLines(file);
                    File newFile = new File(file);

                    foreach (String s in content)
                    {
                        newFile.Add(s);
                    }

                    allFiles.Add(newFile);
                }

                return allFiles;
            }

            // If not found, let them know
            else throw new kOSException("Disk '" + diskName + "' could not be found");
        }

        // Useful for startup, used in conjuction with 'LoadHarddisk' to load up all disks
        public static String[] GetAllHarddiskNames()
        {
            return Directory.GetDirectories(HighLogic.fetch.GameSaveFolder);
        }
    }
}
