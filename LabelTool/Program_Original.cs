

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SplitLabelFiles
{
    class Program
    {
        // Required Inputs
        //const string currentLabelFilePath = @"C:\AOSService\PackagesLocalDirectory\BssiSubscriptionBilling\BssiSubscriptionBilling\AxLabelFile\BssiSb_en-US.xml";
        //const string newLabelFilePath = @"C:\AOSService\PackagesLocalDirectory\BssiSubscriptionBilling\BssiSubscriptionBilling\AxLabelFile\BssiSb2_en-US.xml";
        //const string actualNewLableFilePath = @"C:\AOSService\PackagesLocalDirectory\BssiSubscriptionBilling\BssiSubscriptionBilling\AxLabelFile\LabelResources\en-US\BssiSb2.en-US.label.txt";
        //const string AOTpath = @"C:\AOSService\PackagesLocalDirectory\BssiSubscriptionBilling\";

        //const string currentLabelFilePath = @"C:\AOSService\PackagesLocalDirectory\CreditManagement\CreditManagement\AxLabelFile\CreditManagement_en-US.xml";
        //const string newLabelFilePath = @"C:\AOSService\PackagesLocalDirectory\CreditManagement\CreditManagement\AxLabelFile\CreditManagement2_en-US.xml";
        //const string actualNewLableFilePath = @"C:\AOSService\PackagesLocalDirectory\CreditManagement\CreditManagement\AxLabelFile\LabelResources\en-US\CreditManagement2.en-US.label.txt";
        //const string AOTpath = @"C:\AOSService\PackagesLocalDirectory\CreditManagement\";

        const string currentLabelFilePath = @"E:\git\ApplicationSuite\Source\Metadata\CreditManagement\CreditManagement\AxLabelFile\CreditManagement_en-US.xml";
        const string newLabelFilePath = @"E:\git\ApplicationSuite\Source\Metadata\CreditManagement\CreditManagement\AxLabelFile\CreditManagement2_en-US.xml";
        const string actualNewLableFilePath = @"E:\git\ApplicationSuite\Source\Metadata\CreditManagement\CreditManagement\AxLabelFile\LabelResources\en-US\CreditManagement2.en-US.label.txt";
        const string AOTpath = @"E:\git\ApplicationSuite\Source\Metadata\CreditManagement\";

        // Optional input: provide a team name if it is different from the owner of the new label file
        const string TeamName = "AR-Nano";

        // Internal program variables
        const char DELIM = ',';
        const int MAX_LABEL_ID_LENGTH = 60;
        static string currentLabelPrefix = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Program Start");

            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            SplitLabelFileForTeam(newLabelFilePath);

            File.WriteAllText(@"C:\ConsoleOutput.txt", consoleOut.ToString());
        }

        public static void SplitLabelFileForTeam(string newLabelFilePath)
        {
            DateTime start = DateTime.Now;

            // Determine team 
            string team = TeamName;
            //if (String.IsNullOrEmpty(team))
            //{
               // team = GetOwnership(new FileInfo(newLabelFilePath));
            //}
            Console.WriteLine("Owner determined: " + team);

            // Parse current label file
            string throwaway;
            List<Label> labelFileLabels = ParseLabelFile(currentLabelFilePath, out throwaway, out currentLabelPrefix);
            Console.WriteLine("Current label file parsed");

            // Parse new label file
           /* string newLabelPrefix = "";
            string actualNewLabelFilePath = "";
            List<Label> labelsCurrentlyInNewLabelFile = ParseLabelFile(newLabelFilePath, out actualNewLabelFilePath, out newLabelPrefix);
            Console.WriteLine("New label file parsed");*/

            // Get files that use the labels
            FileInfo[] files = getFilesWithUsage(team, @"C:\teamFilesWithLabelUsage.csv", true);
            Console.WriteLine(files.Length + " team files use labels");

            // Get a list of all the labels used
            List<Label> labels = getLabelsWithOwners(files, labelFileLabels);
            Console.WriteLine(labels.Count + " Labels used");

            // Short-circuit
            /*  if (labels.Count == 0)
              {
                  Console.WriteLine("No labels to process");
                  return;
              }*/

            // Find unusaed labels
            /* List<Label> unusedLabel = new List<Label>();
             foreach (Label l in labelFileLabels)
             {
                 Label usedLabel = findLabel(labels, l.labelId);

                 if (usedLabel == null)
                 {
                     unusedLabel.Add(l);
                 }
             }
         */

            // Update description of labels.  Create a list of labels to add to new label file
            List<Label> labelFileLabelsWithNewDescription = new List<Label>();
            foreach (Label l in labelFileLabels)
            {
                Label usedLabel = findLabel(labels, l.labelId);
                if (usedLabel == null)
                {
                    l.labelDescription = "Label is not used.";
                    //labelFileLabelsWithNewDescription.Add(l); // Uncomment this line to re-tain unused labels
                }
                else if (l.labelDescription != "No description provided" && !l.labelDescription.StartsWith("Used in Ax"))
                {
                    // description stay the same
                    labelFileLabelsWithNewDescription.Add(l);
                }
                else
                {
                    StringBuilder descriptionUsage = new StringBuilder();
                    Boolean isFirst = true;

                    // Parse new description based on on usage
                    foreach (Label currentUsedLabel in labels)
                    {
                        if (l.labelId.ToUpper().Equals(currentUsedLabel.labelId.ToUpper()))
                        {
                            if (isFirst)
                            {
                                isFirst = false;
                            }
                            else
                            {
                                descriptionUsage.Append(", ");
                            }

                            //parse location
                            string name = currentUsedLabel.fileUsedIn.Replace(@"E:\AppMU\Source\AppIL\Metadata\CreditManagementStaging\CreditManagementStaging\", "");
                            string name2 = name.Replace(".xml", "");

                            descriptionUsage.Append(name2);
                        }
                    }

                    l.labelDescription = "Used in " + descriptionUsage.ToString();

                    labelFileLabelsWithNewDescription.Add(l);
                }
                
            }

            // Parse all the new label file entries
            StringBuilder sbAddToLabelFile = new StringBuilder();
            foreach (Label l in labelFileLabelsWithNewDescription)
            {
                sbAddToLabelFile.Append(l.getNewLabelFileEntry());
                //label.updateReferencesToLabel(files, team, newLabelPrefix);
            }
            Console.WriteLine("Label changes processed");

            // Write labels to new label file
            try
            {
                System.IO.File.AppendAllText(actualNewLableFilePath, sbAddToLabelFile.ToString());
                Console.WriteLine("Labels added to label file");
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unauthorized Access Exception to label file: " + actualNewLableFilePath);
                Console.ForegroundColor = ConsoleColor.Gray;
            }


            /*            // Checkout files to update
                        Checkout(files);
                        Console.WriteLine("Files checked out");

                        // Checkout the new label file
                        EditFile(new FileInfo(actualNewLabelFilePath));
                        Console.WriteLine("Label file checked out");

                        //
                        // 1. Retrieve label content and description
                        // 2. Create a new label id
                        // 3. Create a new entry for the new label file
                        // 4. Update references to label
                        //
                        List<Label> newLabels = new List<Label>();
                        StringBuilder sbAddToLabelFile = new StringBuilder();
                        foreach (Label label in labels)
                        {
                            label.processLabelFromOriginalLabelFile(labelFileLabels, team);
                            string newLabelId = label.createNewLabelId(labelsCurrentlyInNewLabelFile, newLabels);
                            newLabels.Add(new Label(newLabelId));
                            sbAddToLabelFile.Append(label.getNewLabelFileEntry());
                            label.updateReferencesToLabel(files, team, newLabelPrefix);
                        }
                        Console.WriteLine("Label changes processed");

                        // Write labels to new label file
                        try
                        {
                            System.IO.File.AppendAllText(actualNewLabelFilePath, sbAddToLabelFile.ToString());
                            Console.WriteLine("Labels added to label file");
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Unauthorized Access Exception to label file: " + actualNewLabelFilePath);
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }

                        // Calculate processing times
                        DateTime end = DateTime.Now;
                        TimeSpan duration = end - start;
                        Console.WriteLine("Process took: " + duration.ToString());*/

            // Print out moved labels for logging purposes
            printLabels(labels);
        }

        public class Label
        {
            public string labelId { get; set; }
            public string newLabelId { get; set; }
            public string fileUsedIn { get; set; }
            public int countOfLabel { get; set; }

            public string labelContent;
            public string labelDescription;
            private bool labelDoesNotExist;

            public Label(string _labelId)
            {
                labelId = _labelId;
                labelDoesNotExist = false;
            }

            public Label(string _labelId, string _labelText, string _labelDescription)
            {
                labelId = _labelId;
                labelContent = _labelText;
                labelDescription = _labelDescription;
                labelDoesNotExist = false;
            }

            public void processLabelFromOriginalLabelFile(List<Label> labelFileLabels, string team)
            {
                Label foundLabel = findLabel(labelFileLabels, labelId);

                // Label does not exist in label file
                if (foundLabel == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Ignoring label that does not exist in label file but is referenced in code owned by " + team + ": " + labelId);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    labelDoesNotExist = true;
                    return;
                }

                labelContent = foundLabel.labelContent;
                labelDescription = foundLabel.labelDescription + " (Previously was " + labelId + ")";
            }


            public string createNewLabelId(List<Label> currentLabelsInNewLabelFile, List<Label> newLabelsInNewLabelFile)
            {
                // Short circuit if label already has been assigned a new  id
                if (!String.IsNullOrEmpty(newLabelId))
                {
                    return newLabelId;
                }

                // Generate a valid id based on label content
                string potentialNewLabelId = LabelIdConversation.GenerateValidLabelId(labelContent);

                // Label has no content
                if (String.IsNullOrEmpty(potentialNewLabelId))
                {
                    // Use original label minus the '@' symbol
                    return labelId.Substring(1);                    
                }

                bool duplicateFound = false;
                int i = 1;

                // Label id already exists in label file we're copying to
                string interimLabel = potentialNewLabelId;
                if (currentLabelsInNewLabelFile != null)
                {
                    while (findLabel(currentLabelsInNewLabelFile, interimLabel) != null)
                    {
                        duplicateFound = true;
                        interimLabel = potentialNewLabelId + i;
                        i++;
                    }
                }

                // Label id already exists in the labels that are currently being added to the label file
                if (newLabelsInNewLabelFile != null)
                {
                    while (findLabel(newLabelsInNewLabelFile, interimLabel) != null)
                    {
                        duplicateFound = true;
                        interimLabel = potentialNewLabelId + i;
                        i++;
                    }
                }

                newLabelId = interimLabel;

                if (duplicateFound)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Detected duplicate label text for: " + labelId + ". New label id is: " + newLabelId);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (newLabelId.Length >= MAX_LABEL_ID_LENGTH)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("New label Id for: " + labelId + " is long. New label id is: " + newLabelId);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                return newLabelId;
            }

            public string getNewLabelFileEntry()
            {
                return labelId + "=" + labelContent + Environment.NewLine + " ;" + labelDescription + Environment.NewLine;
            }

            public void updateReferencesToLabel(FileInfo[] files, string team, string prefix)
            {
                if (labelDoesNotExist)
                {
                    return;
                }
                foreach (FileInfo file in files)
                {
                    string text = readFile(file.FullName);
                    string newLabelReference = String.Concat(prefix + ":" + newLabelId);
                    text = text.Replace(labelId, newLabelReference);

                    try
                    {
                        File.WriteAllText(file.FullName, text);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Skipping update of reference to " + labelId + ": Cannot write to file: " + file.FullName);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }

            public StringBuilder printToSB()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(countOfLabel.ToString() + DELIM);
                sb.Append(labelId + DELIM);
                sb.Append(newLabelId + DELIM);
                sb.Append(labelContent + DELIM);
                sb.Append(labelDescription + DELIM);
                sb.Append(fileUsedIn + DELIM);

                sb.Append(Environment.NewLine);

                return sb;
            }
        }

        public static void printLabels(List<Label> labels)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Label label in labels)
            {
                sb.Append(label.printToSB());
            }

            File.WriteAllText(@"C:\labelsUsed.csv", sb.ToString());
        }

        public static List<Label> ParseLabelFile(string filepath, out string actualLabelFilePath, out string prefix)
        {
            // Read XML
            using (XmlReader reader = XmlReader.Create(new StringReader(readFile(filepath))))
            {
                reader.ReadToFollowing("LabelContentFileName");
                string filename = reader.ReadElementContentAsString();
                actualLabelFilePath = (new FileInfo(filepath)).DirectoryName + @"\LabelResources\en-US\" + filename;

                reader.ReadToFollowing("LabelFileId");
                prefix = "@" + reader.ReadElementContentAsString();
            }

            // Parse actual label file
            List<Label> labelsInLabelFile = new List<Label>();
            string[] text = System.IO.File.ReadAllLines(actualLabelFilePath);

            string labelId = "";
            int count = 1;
            for (int i = 0; i < text.Length; i = i + 2)
            {
                int endOfLabel = text[i].IndexOf('=');

                try
                {
                    labelId = text[i].Substring(0, endOfLabel);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("parsing error: " + text[i]);
                    Console.WriteLine("parsing error: " + i);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    i--;
                    continue;
                }
                string labelContent = text[i].Substring(endOfLabel + 1);

                string labelDescription = "";

                // Check for no description provided on the label
                int beginningOfDescription = text[i + 1].IndexOf(';');
                if (beginningOfDescription != 1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Following label had no description: " + labelId);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    labelDescription = "No description provided";
                    i--;
                }
                else
                {
                    labelDescription = text[i + 1].Substring(beginningOfDescription + 1);
                }

                Label newLabel = new Label(labelId, labelContent, labelDescription);
                newLabel.countOfLabel = count;
                count++;
                labelsInLabelFile.Add(newLabel);
            }

            return labelsInLabelFile;
        }



        static Label findLabel(List<Label> labels, string label)
        {
            foreach (Label l in labels)
            {
                if (label.ToUpper().Equals(l.labelId.ToUpper()))
                {
                    return l;
                }
            }

            return null;
        }

        static List<Label> getLabelsWithOwners(FileInfo[] files, List<Label> labelFileLabels)
        {
            List<Label> labels = new List<Label>();

            foreach (FileInfo file in files)
            {
                string text = readFile(file.FullName);

                for (int i = 0; i >= 0 && i < text.Length; i++)
                {
                    // Find next usage of a label
                    i = text.IndexOf(currentLabelPrefix, i);

                    // Break if there are no more usages
                    if (i == -1)
                    {
                        break;
                    }

                    // Parse label
                    string labelUsed = parseLabel(text, currentLabelPrefix, i);


                    Label labelFileLabel = findLabel(labelFileLabels, labelUsed);
                    if (labelFileLabel != null)
                    {
                        Label newLable = new Label(labelUsed, labelFileLabel.labelContent, labelFileLabel.labelDescription);
                        newLable.fileUsedIn = file.FullName;
                        newLable.countOfLabel = labelFileLabel.countOfLabel;
                        labels.Add(newLable);
                    }
                }
            }

            return labels;
        }

        static void recordFilesWithUsage(string team, string filePathForCache)
        {
            StringBuilder sb = new StringBuilder();
            FileInfo[] files = getFiles();

            foreach (FileInfo file in files)
            {
                if (file.FullName.Contains("AxLabelFile"))
                {
                    continue;
                }

                //if (!isOwnedByTeam(file, team))
                //{
                //    continue;
                //}

                string text = readFile(file.FullName);

                int i = text.IndexOf(currentLabelPrefix);

                // Break if there are no usages
                if (i == -1)
                {
                    continue;
                }

                // Document usage
                sb.Append(file.FullName + DELIM);
            }

            File.WriteAllText(filePathForCache, sb.ToString());
        }

        public static FileInfo[] getFilesWithUsage(string team, string filePathForCache, bool record = false)
        {
            if (record)
            {
                recordFilesWithUsage(team, filePathForCache);
            }

            List<FileInfo> files = new List<FileInfo>();
            string text = readFile(filePathForCache);
            string[] rawFiles = text.Split(DELIM);

            for (int i = 0; i < rawFiles.Length; i++)
            {
                string fileName = rawFiles[i];

                if (String.IsNullOrWhiteSpace(fileName))
                {
                    continue;
                }

                FileInfo file = new FileInfo(fileName);

                //if (!isOwnedByTeam(file, team))
                //{
                //    continue;
                //}

                if (!files.Contains(file))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }

        static string parseLabel(string text, string labelPrefix, int labelLocation)
        {
            //int j = 0;
            //int num = 0;
            //while (int.TryParse(text[labelLocation + labelPrefix.Length + j].ToString(), out num))
            //{
            //    j++;
            //}

            bool endOfLabelFound = false;

            // + 1 slock for the : in the label
            int j = 1;

            while (!endOfLabelFound)
            {
                char characterAtLocation = text[labelLocation + labelPrefix.Length + j];

                if (characterAtLocation == '"' || characterAtLocation == '<')
                {
                    endOfLabelFound = true;
                }
                else
                {
                    j++;
                }
            }



            // Parse label
            string result = text.Substring(labelLocation + labelPrefix.Length + 1, j - 1);
            return result;
        }

        static SortedSet<string> getUsedLabels()
        {
            FileInfo[] files = getFiles();

            SortedSet<string> usedLabels = new SortedSet<string>();

            foreach (FileInfo file in files)
            {
                string text = System.IO.File.ReadAllText(file.FullName);

                for (int i = 0; i >= 0 && i < text.Length; i++)
                {
                    // Find next usage of a label
                    i = text.IndexOf(currentLabelPrefix, i);

                    // Break if there are no more usages
                    if (i == -1)
                    {
                        break;
                    }

                    // Determine length of the label
                    int j = 0;
                    while (!char.IsNumber(text[i + currentLabelPrefix.Length + j]))
                    {
                        j++;
                    }

                    // Parse label
                    string usedLabel = text.Substring(i, currentLabelPrefix.Length + j);

                    // Add label to list of labels used
                    if (!usedLabels.Contains(usedLabel))
                    {
                        usedLabels.Add(usedLabel);
                    }
                }
            }

            return usedLabels;
        }

        public static void Checkout(FileInfo[] files)
        {
            foreach (FileInfo file in files)
            {
                EditFile(file);
            }
        }
        static void EditFile(FileInfo file)
        {
            string strCmdText = "/C sd edit " + file.FullName;
            System.Diagnostics.Process.Start("CMD.exe", strCmdText).WaitForExit(100);
        }
        static void SyncFile(FileInfo file)
        {
            string strCmdText = "/C sd sync " + file.FullName;
            System.Diagnostics.Process.Start("CMD.exe", strCmdText).WaitForExit(100);
        }
        static void RevertFile(FileInfo file)
        {
            string strCmdText = "/C sd revert " + file.FullName;
            System.Diagnostics.Process.Start("CMD.exe", strCmdText).WaitForExit(100);
        }

        static string GetOwnership(FileInfo file)
        {
            /*List<XppOwnerIntegration.PrefixLine> prefixLines = XppOwnerIntegration.XppOwner.GetOwnerInfo(file.Name);

            string team;

            if (prefixLines.Count > 0)
            {
                team = prefixLines[0].Team;
            }
            else
            {
                team = "NoOwner";
            }

            return team;*/
            return TeamName;
        }
        static bool isOwnedByTeam(FileInfo file, string team)
        {
            return true;
            //return GetOwnership(file).Equals(team);
        }

        static FileInfo[] getFiles()
        {
            DirectoryInfo directory = new DirectoryInfo(AOTpath);

            return directory.GetFiles("*", SearchOption.AllDirectories);
        }
        static string readFile(string fileFullName)
        {
            return System.IO.File.ReadAllText(fileFullName);
        }

        public static class LabelIdConversation
        {
            public static string GenerateValidLabelId(string strIn)
            {
                strIn = ConvertToCamelCase(strIn);
                strIn = RemoveInvalidChars(strIn);
                strIn = HandleLeadingNumeral(strIn);
                strIn = HandleLeadingUnderscore(strIn);
                strIn = HandleNoLetters(strIn);
                strIn = Truncate(strIn);
                return strIn;
            }

            private static string Truncate(string strIn, int maxLength = MAX_LABEL_ID_LENGTH)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    return strIn;
                }
                if (strIn.Length <= maxLength)
                {
                    return strIn;
                }
                return strIn.Substring(0, maxLength);
            }

            private static string ConvertToCamelCase(string strIn)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    return strIn;
                }
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(strIn);
            }

            private static string HandleNoLetters(string strIn)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    return strIn;
                }
                if (!Regex.IsMatch(strIn, @"[a-zA-Z]"))
                {
                    return "Label_" + strIn;
                }

                return strIn;
            }

            private static string HandleLeadingUnderscore(string strIn)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    return strIn;
                }

                if (strIn.StartsWith("_"))
                {
                    return "Label" + strIn;
                }

                return strIn;
            }

            private static string HandleLeadingNumeral(string strIn)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    return strIn;
                }

                if (Regex.IsMatch(strIn, @"^\d"))
                {
                    return "Num_" + strIn;
                }

                return strIn;
            }

            private static string RemoveInvalidChars(string strIn)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    return strIn;
                }
                try
                {
                    return Regex.Replace(strIn, @"[^\w]", "", RegexOptions.None);
                    //return Regex.Replace(strIn, @"[^\w]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
                }
                catch (TimeoutException)
                {
                    return String.Empty;
                }
            }
        }
    }
}
