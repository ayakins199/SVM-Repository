using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Expat.Bayesian;
using SVM;
using PDS_SVM;


namespace PDS_SVM
{
    class SpamDSClass
    {
        public string extractedFeaturesFilePathTrainDS = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedFeaturesTrain.txt");
        public string filenameTrainDS = "ExtractedFeaturesTrain.txt";
        public string extractedFeaturesFilePathTestDS = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedFeaturesTest.txt");
        public string filenameTestDS = "ExtractedFeaturesTest.txt";
        //Microsoft.Office.Interop.Word.Document msWordObject = CallMSWordDocumentObject(); //evoke or call ms word object
        
        PDSClass pds = new PDSClass();

        public List<string> extractedFeatures = new List<string>();

        /// <summary>
        /// This method contains all the features used for classification
        /// </summary>
        public string[] Features()
        {
            //'http' checks for IP-Based domain names, 'href=' checks for non-matching urls(e.g <a href="badsite.com"> paypal.com</a>.)
            //<a> check the url text of all the urls (that links to the modal domain, i.e, the domain most frequently linked to) 
            //of each mail to confirm whether they contain any these words: "link","click","here"
            //'content-type:' checks for HTML emails if it contains a section that is denoted with a MIME type of text/html.
            //'href' count the number of dots in each domain names that is linked to. 
            //'javascript' checks the presence the string, "javascript" in each email
            //</a> counts the number of links in an email, linkedToDomain counts the number of distinct domains that is linked to in each email
            //From_Body_MatchDomain check domain name mismatch between sender's(FROM:) field(e.g sender@paypal.com) and the email's body
            //'<form' is a feature that checks for the presence of <form> tag in the email
            //'update' checks the email for the occurence of 'update' and 'confirm', 'user' checks the email for 'user', 'customer' & 'client'
            //'suspend' checks for 'suspend', 'restrict' & 'hold', 'verify' checks for 'verify' & 'account'
            //'login' checks for 'login', 'username' & 'password', 'ssn' checks for 'ssn' and 'social security'
            //string[] featrs = new string[] { "<a>", "content-type:", "javascript", "</a>", "linkedToDomain", "From_Body_MatchDomain", "update", "suspend", "ssn", "SpamAssassin",
            //                                 "bankNames", "SpamWords", "AnchorTags", "NonAnchorTags", "TotalTags", "Alphanumeric", "StopWords", "TF_ISF",  
            //                                 "Document_Length", "FogIndex", "FleshReadingScore", "SmogIndex", "ForcastIndex", "FleschKincaidReadabilityIndex", "FogIndexSimple",
            //                                   "FogIndexInverse", "ComplexWords", "SimpleWords", "WordLength"}; //"TF_IDF" "TF_IDFSimple", "TF_IDFComplex"

            string[] featrs = new string[] { "<a>", "content-type:", "</a>", "linkedToDomain", "From_Body_MatchDomain", "SpamAssassin","TF_ISF","ComplexWords", "SimpleWords",
                                             "bankNames", "SpamWords", "NonAnchorTags", "TotalTags", "Document_Length", "StopWords"}; //, 

            return featrs;
        }

        //extract and format spam words
        public void ExtractFormatSpamWords(string spamWordsFilePath)
        {
            string inputString = File.ReadAllText(spamWordsFilePath);
            string[] split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            string formattedStr = "";
            foreach (string str in split)
            {
                formattedStr = formattedStr + String.Format("{0}, ", "\"" + str + "\"");
            }

            File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", "StopWordsOutput.txt"), formattedStr); 
        }

        //get class of Iris dataset instances for formatting
        public int GetClass(string className)
        {
            int retVal = 0;
            if (className == "Iris-setosa")
                retVal = 1;
            else if (className == "Iris-versicolor")
                retVal = 2;
            else if (className == "Iris-virginica")
                retVal = 3;

            return retVal;
        }

        //sort principal components of PCA values
        public List<int> sortPrincipalComponents(string vectorFilePath, int rowCount, int colCount)
        {
            string vectorInputString = File.ReadAllText(vectorFilePath);
            string[] vecorSplit = RemoveEmptyRow(vectorInputString.Split(new string[] { "\r\n" }, StringSplitOptions.None));

            double[,] vector = new double[rowCount, colCount - 1];

            int k = 0;
            string[] sp;
            foreach (string str in vecorSplit)
            {
                string[] splitRow = str.Split(' ');
                splitRow = splitRow.Where(x => !string.IsNullOrEmpty(x)).ToArray(); //remove empty columns in arrays
                int clas = int.Parse(splitRow.First()); //get the class of instance - this is in the last column of each row
                for (int j = 0; j < splitRow.Count() - 1; j++) //this is tot-1 because of the class. The class should not be processed
                {
                    sp = splitRow[j + 1].Split(':'); //split each column to get the vector value
                    vector[k, j] = double.Parse(sp[1]);//assign vector value to array
                }
                k++;
            }

            int[] cnt = new int[vector.GetLength(1)];
            for (int j = 0; j < vector.GetLength(0); j++)
            {
                for (int p = 0; p < vector.GetLength(1); p++)
                {
                    if (vector[j, p] > 1)
                        cnt[p]++;
                }
            }

            var sorted = cnt.Select((x, index) => new KeyValuePair<int, int>(x, index)).OrderByDescending(x => x.Key).ToList();
            List<int> Sorted = sorted.Select(x => x.Key).ToList(); //select the sorted list of values
            List<int> preservedindex = sorted.Select(x => x.Value).ToList(); //select the original index of the sorted values

            return preservedindex;
        }

        //read vector values from a text file, and calculate information gain
        public void ReadVectorFromFile(string ClassFilePath)
        {
            string[] feat = new string[] { "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10", "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20", "V21", 
                                                "V22", "V23", "V24", "V25", "V26", "V27", "V28" };
            for (int i = 0; i < 10; i++)
            {
                Dictionary<string, int> stringClass = new Dictionary<string, int>();
                string vectorFileName = "ExtractedFeaturesTest";
                string vectorFilePath = String.Format(Environment.CurrentDirectory + "\\{0}{1}.txt", vectorFileName, i + 1); //file path for the extracted vectors

                string classInputString = File.ReadAllText(ClassFilePath);
                string[] classSplit = RemoveEmptyRow(classInputString.Split(new string[] { "\r\n" }, StringSplitOptions.None));

                string vectorInputString = File.ReadAllText(vectorFilePath);
                string[] vecorSplit = RemoveEmptyRow(vectorInputString.Split(new string[] { "\r\n" }, StringSplitOptions.None));

                //Dictionary<string, int> stringClass = new Dictionary<string, int>();
                //int totalVectors = split[0].Count();
                string[] classSplitCount = classSplit[0].Split(' ').Where(x => !string.IsNullOrEmpty(x)).ToArray(); //split row and remove empty column in splitted array
                int classCount = classSplitCount.Count(); //total vectors per row. Each row represent vector output for each fold
                double[,] vector = new double[classCount, feat.Count()];

                int k = 0;
                string[] sp;
                foreach (string str in vecorSplit)
                {
                    string[] splitRow = str.Split(' ');
                    splitRow = splitRow.Where(x => !string.IsNullOrEmpty(x)).ToArray(); //remove empty columns in arrays
                    int clas = int.Parse(splitRow.First()); //get the class of instance - this is in the last column of each row
                    stringClass[k.ToString()] = clas; //assign class of each credit card
                    for (int j = 0; j < splitRow.Count() - 1; j++) //this is tot-1 because of the class. The class should not be processed
                    {
                        sp = splitRow[j + 1].Split(':'); //split each column to get the vector value
                        vector[k, j] = double.Parse(sp[1]);//assign vector value to array
                    }
                    k++;
                }

                

                int totEmail = stringClass.Count;
                Dictionary<string, double> feat_infoG = new Dictionary<string, double>(); //saving the information gain for all the individual features

                double EntropyDataset = 0.0; //calculate the entropy for the entire dataset
                int nFeat = vector.GetLength(1);
                double[,] HamPhishCnt = new double[nFeat, 4];
                pds.FeatureVectorSumCreditCard(nFeat, totEmail, vector, stringClass, HamPhishCnt); // calculating the total number of zeros and ones for credit card
                double[] informationG = new double[nFeat];
                

                EntropyDataset = pds.Entropy(stringClass); //calculating the entropy for the entire dataset
                pds.CalInformationGainCreditCard(nFeat, HamPhishCnt, informationG, totEmail, EntropyDataset);//calculating information gain for each feature
                feat_infoG = pds.Feature_InfoGain(feat, informationG); //assisgning the calculated information gain to each feature
            }
        }

        //extract and format data from UCI dataset
        public void ExtractFormatUCIDataset(string UCIDatasetFilePath, string outputFileName)
        {
            string inputString = File.ReadAllText(UCIDatasetFilePath);
            string outputFilePath = String.Format(Environment.CurrentDirectory + "\\{0}", outputFileName);
            File.WriteAllText(outputFilePath, string.Empty); //enter a new line

            string[] split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            //string[] split = inputString.Split(new string[] { "\n" }, StringSplitOptions.None);
            split = RemoveEmptyRow(split);
            string formattedStr = "";
            foreach (string str in split)
            {
                string[] splitRow = str.Split(',');
                splitRow = splitRow.Where(x => !string.IsNullOrEmpty(x)).ToArray(); //remove empty columns in arrays
                splitRow = splitRow.Skip(1).ToArray(); //remove first element from array
                //int clas = GetClass(splitRow.Last()); //get the class of instance
                //int clas = int.Parse(splitRow.Last()); //get the class of instance - this is in the last column of each row
                string clas = splitRow.Last().Trim(); //get the class of instance - this is in the last column of each row
                //string cla = clas == "2" ? "+1" : "-1";
                string cla = clas;
                //if (clas == "CYT")
                //    cla = "1";
                //else if (clas == "NUC")
                //    cla = "2";
                //else if (clas == "MIT")
                //    cla = "3";
                //else if (clas == "ME3")
                //    cla = "4";
                //else if (clas == "ME2")
                //    cla = "5";
                //else if (clas == "ME1")
                //    cla = "6";
                //else if (clas == "EXC")
                //    cla = "7";
                //else if (clas == "VAC")
                //    cla = "8";
                //else if (clas == "POX")
                //    cla = "9";
                //else if (clas == "ERL")
                //    cla = "10";
                
                formattedStr = formattedStr + String.Format("{0} ", cla);
                //formattedStr = formattedStr + String.Format("{0} ", splitRow[0]); //get the class for the row
                for (int i = 0; i < splitRow.Count()-1; i++) //loop for split.Last()
                //for (int i = 1; i < splitRow.Count(); i++) //loop for split.First()
                {
                    //if (splitRow[i] == "n")
                    //    splitRow[i] = "0";
                    //else if (splitRow[i] == "y")
                    //    splitRow[i] = "1";
                    //else if (splitRow[i] == "?")
                    //    splitRow[i] = "0";
                    //else if (splitRow[i] == "mid")
                    //    splitRow[i] = "5";
                    //else if (splitRow[i] == "stable")
                    //    splitRow[i] = "4";
                    //else if (splitRow[i] == "low")
                    //    splitRow[i] = "2";
                    //else if (splitRow[i] == "mod-stable")
                    //    splitRow[i] = "3";
                    //else if (splitRow[i] == "unstable")
                    //    splitRow[i] = "1";
                    
                    //int ret = LettersToNumber(splitRow[i]);
                    //formattedStr = formattedStr + String.Format("{0}:{1} ", i, ret); //get and format the attributes
                    //formattedStr = formattedStr + String.Format("{0}:{1} ", i, splitRow[i]); //get and format the attributes
                    formattedStr = formattedStr + String.Format("{0}:{1} ", i+1, splitRow[i]); //get and format the attributes
                }
                File.AppendAllText(String.Format(Environment.CurrentDirectory + "\\{0}", outputFileName), formattedStr); //extract row to data file
                File.AppendAllText(outputFilePath, Environment.NewLine); //enter a new line
                formattedStr = string.Empty; //clear the string for a new row entry
            }
        }

        //remove empty row from array
        public string[] RemoveEmptyRow(string[] array)
        {
            List<string> y = array.ToList<string>();
            y.RemoveAll(p => string.IsNullOrEmpty(p)); //remove empty row
            array = y.ToArray(); //convert back to string array

            return array;
        }

        //convert letter features to numeric features
        public int LettersToNumber(string letter)
        {
            int equi = 0;
            switch (letter)
            {
                case "a":
                    equi = 1;
                    break;
                case "b":
                    equi = 2;
                    break;
                case "c":
                    equi = 3;
                    break;
                case "d":
                    equi = 4;
                    break;
                case "e":
                    equi = 5;
                    break;
                case "f":
                    equi = 6;
                    break;
                case "g":
                    equi = 7;
                    break;
                case "h":
                    equi = 8;
                    break;
                case "i":
                    equi = 9;
                    break;
                case "j":
                    equi = 10;
                    break;
                case "k":
                    equi = 11;
                    break;
                case "l":
                    equi = 12;
                    break;
                case "m":
                    equi = 13;
                    break;
                case "n":
                    equi = 14;
                    break;
                case "o":
                    equi = 15;
                    break;
                case "p":
                    equi = 16;
                    break;
                case "q":
                    equi = 17;
                    break;
                case "r":
                    equi = 18;
                    break;
                case "s":
                    equi = 19;
                    break;
                case "t":
                    equi = 20;
                    break;
                case "u":
                    equi = 21;
                    break;
                case "v":
                    equi = 22;
                    break;
                case "w":
                    equi = 23;
                    break;
                case "x":
                    equi = 24;
                    break;
                case "y":
                    equi = 25;
                    break;
                case "z":
                    equi = 26;
                    break;
                default:
                    equi = 27;
                    break;
            }
            return equi;
        }

        //extract training and test vectors for UCI dataset
        public void ExtractVectorUCIDataset(string UCIDatasetFilePath, string outputFileName)
        {
            string inputString = File.ReadAllText(UCIDatasetFilePath);
            string[] split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            
            //remove empty row from array
            split = RemoveEmptyRow(split);
            
            int NFold = 10; //number of folds for cross validation
            int subsetSize = split.Count() / NFold; //number of subset 
            
            
            int i = 1, j = 1;
            string name = outputFileName.Split('.').First(); //split and select the first element, which is the file name
            string fileName = string.Format("{0}{1}.{2}", name, i.ToString(), "txt");
            File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), string.Empty); //create file

            Random ra = new Random();
            List<int> rList = Training.GetRandomNumbers(split.Count(), split.Count()); //generate N random numbers. This is to ensure that all classes are included in each dataset

            //Firstly, before obtaining test and train dataset for all the filds, split the dataset into N Folds
            SplitDataset(split, rList, name, fileName);

            //Secondly, after the above extraction, proceed with the extraction of test and train dataset
            string filePath;
            for (int k = 1; k <= NFold; k++)
            {
                string fileNameTrain = string.Format("{0}{1}.{2}", "ExtractedFeaturesTrain", k.ToString(), "txt");
                File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileNameTrain), string.Empty); //create file

                for (int m = 1; m <= NFold; m++)
                {
                    if (m.Equals(k)) //process test dataset for this fold, and continue; proceed to process train dataset for this fold
                    {
                        fileName = string.Format("{0}{1}.{2}", name, k.ToString(), "txt");
                        filePath = String.Format(Environment.CurrentDirectory + "\\{0}", fileName);
                        inputString = File.ReadAllText(filePath); //extract vectors from file
                        split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        split = RemoveEmptyRow(split);

                        fileName = string.Format("{0}{1}.{2}", "ExtractedFeaturesTest", k.ToString(), "txt");
                        File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), string.Empty); //create file

                        //process test dataset
                        int a = 0;
                        foreach (string str in split)
                        {
                            File.AppendAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), str); //extract row to data file
                            if (!(a++ == split.Count())) //don't insert a new line for the last instance
                                File.AppendAllText(fileName, Environment.NewLine); //enter a new line
                        }

                        continue;
                    }

                    fileName = string.Format("{0}{1}.{2}", name, m.ToString(), "txt"); //get filename for this current fold
                    filePath = String.Format(Environment.CurrentDirectory + "\\{0}", fileName); //get file path
                    inputString = File.ReadAllText(filePath); //read from file
                    split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    
                    //process training dataset
                    int z = 1;
                    foreach (string str in split)
                    {
                        File.AppendAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileNameTrain), str); //extract row to data file
                        if (!(z++ == split.Count())) //don't insert a new line for the last instance
                            File.AppendAllText(fileNameTrain, Environment.NewLine); //enter a new line
                    }
                }
            }
        }

        

        //Split the dataset into N Folds, and write splitted data to file
        public void SplitDataset(string[] split, List<int> rList, string name, string fileName)
        {
            int NFold = 10; //number of folds for cross validation
            int subsetSize = split.Count() / NFold; //number of subset 
            int j = 1, i = 1;
            for (int p = 0; p < split.Count(); p++)
            {
                //int rand = ra.Next(0, split.Count()); 

                string[] splitRow = split[rList[p]].Split(' '); //randomly select rows
                splitRow = RemoveEmptyRow(splitRow); //remove empty cells from array

                //extract data to sub-dataset. 
                for (int r = 0; r < splitRow.Count(); r++)
                {
                    File.AppendAllText(fileName, splitRow[r] + " ");
                }
                File.AppendAllText(fileName, Environment.NewLine);

                if (j % subsetSize == 0) //insert N instances per file, where N = subsetSize
                {
                    i++; //go to a new file number if N instances has been inserted to file
                    fileName = string.Format("{0}{1}.{2}", name, i.ToString(), "txt");
                    File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), string.Empty); //create file
                    j = 0;
                }
                j++;

            }
        }

        //extract training and test vectors for credit card kaggle dataset
        public void ExtractVectorCreditCard(string UCIDatasetFilePath, string outputFileName)
        {
            string inputString = File.ReadAllText(UCIDatasetFilePath);
            string[] split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            //remove empty row from array
            split = RemoveEmptyRow(split);

            int NFold = 10; //number of folds for cross validation
            int subsetSize = split.Count() / NFold; //number of subset 

            int i = 1, j = 1;
            string name = outputFileName.Split('.').First(); //split and select the first element, which is the file name
            string fileName = string.Format("{0}{1}.{2}", name, i.ToString(), "txt");
            
            //Random ra = new Random();
            List<int> rList = Training.GetRandomNumbers(split.Count(), split.Count()); //generate N random numbers. This is to ensure that all classes are included in each dataset

            //Firstly, before obtaining test and train dataset for all the filds, split the dataset into N Folds
            //File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), string.Empty); //create file
            Split_ReduceDatasetFeatures(split, rList, name);

            //Secondly, sequel to the above extraction, proceed with the extraction of test and train dataset
            string filePath;
            for (int k = 1; k <= NFold; k++)
            {
                string fileNameTrain = string.Format("{0}{1}.{2}", "ExtractedFeaturesTrain", k.ToString(), "txt");
                File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileNameTrain), string.Empty); //create file

                for (int m = 1; m <= NFold; m++)
                {
                    if (m.Equals(k)) //process test dataset for this fold, and continue; proceed to process train dataset for this fold
                    {
                        fileName = string.Format("{0}{1}.{2}", name, k.ToString(), "txt");
                        filePath = String.Format(Environment.CurrentDirectory + "\\{0}", fileName);
                        inputString = File.ReadAllText(filePath); //extract vectors from file
                        split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        split = RemoveEmptyRow(split);

                        fileName = string.Format("{0}{1}.{2}", "ExtractedFeaturesTest", k.ToString(), "txt");
                        File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), string.Empty); //create file

                        //process test dataset
                        int a = 0;
                        foreach (string str in split)
                        {
                            File.AppendAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName), str); //extract row to data file
                            if (!(a++ == split.Count())) //don't insert a new line for the last instance
                                File.AppendAllText(fileName, Environment.NewLine); //enter a new line
                        }

                        continue;
                    }

                    fileName = string.Format("{0}{1}.{2}", name, m.ToString(), "txt"); //get filename for this current fold
                    filePath = String.Format(Environment.CurrentDirectory + "\\{0}", fileName); //get file path
                    inputString = File.ReadAllText(filePath); //read from file
                    split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    //process training dataset
                    int z = 1;
                    foreach (string str in split)
                    {
                        File.AppendAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileNameTrain), str); //extract row to data file
                        if (!(z++ == split.Count())) //don't insert a new line for the last instance
                            File.AppendAllText(fileNameTrain, Environment.NewLine); //enter a new line
                    }
                }
            }
        }

        //Split the dataset into N Folds, and reduce number of features, and write reduced splitted data to file
        public void Split_ReduceDatasetFeatures(string[] split, List<int> rList, string name)
        {
            int NFold = 10; //number of folds for cross validation
            int subsetSize = split.Count() / NFold; //number of subset 
            int j = 1, i = 1;
            string fileName1 = string.Format("{0}.{1}", name, "txt");
            string fileName2 = string.Format("{0}{1}.{2}", name, i.ToString(), "txt");
            File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName2), string.Empty); //create file

            int nFeat = 10;

            string[] splitR = split[0].Split(' '); 
            splitR = RemoveEmptyRow(splitR); //remove empty cells from row
            //List<int> rNum = Training.GetRandomNumbers(nFeat, splitR.Count() - 1); //generate N random numbers. 

            List<int> sortedIndex = sortPrincipalComponents(fileName1, split.Count(), splitR.Count());
            for (int p = 0; p < split.Count(); p++)
            {
                string[] splitRow = split[rList[p]].Split(' '); //randomly select rows
                splitRow = RemoveEmptyRow(splitRow); //remove empty cells from array
                
                //extract data to sub-dataset
                int k = 0;
                for (int r = 0; r < nFeat + 1; r++)
                {
                    if (r == 0) //select first column, since it is the class
                        File.AppendAllText(fileName2, splitRow[r] + " "); //write first column to file, since it is the class
                    else
                    {
                        int row = sortedIndex[k++] + 1; //this is +1, because, the first index is the class
                        string[] sCol = splitRow[row].Split(':'); //select column, and split
                        string vec = sCol[1]; //select vector, which is the second element
                        string formatVec = string.Format("{0}:{1}", r, vec); //format vector to be written to file
                        File.AppendAllText(fileName2, formatVec + " "); //randomly select N number of features (i.e columns) and write to file
                    }
                }
                File.AppendAllText(fileName2, Environment.NewLine);

                if (j % subsetSize == 0) //insert N instances per file, where N = subsetSize
                {
                    i++; //go to a new file number if N instances has been inserted to file
                    fileName2 = string.Format("{0}{1}.{2}", name, i.ToString(), "txt");
                    File.WriteAllText(String.Format(Environment.CurrentDirectory + "\\{0}", fileName2), string.Empty); //create file
                    j = 0;
                }
                j++;

            }
        }

        /// <summary>
        /// Process each emails for vector value extraction
        /// </summary>
        //public void processVector(int[,] vector, Dictionary<string, int> mails, string[] features, string[] mailURLs, int NumOfFeatures)
        public int[,] processTrainVector(Dictionary<string, int> mails, ref string[] features)
        {
            //PDSClass pds = new PDSClass();
            SpamFilter _filter;
            string[] URLFeatures = { "login", "update", "click", "here" };
            //assign vector to phishing emails
            int i; int j;
            string[] tokenizedCleanEmail = new string[] { };
            string[] tokenizedEmail = new string[] { };
            string[] collatedTokenizedEmail = new string[] { };
            Dictionary<string, int> wordCount = new Dictionary<string, int>();
            string[] add = new string[] {"!","$","?" };
            List<string[]> tokenizedEmailList = new List<string[]>();
            List<string[]> spamtokenizedEmailList = new List<string[]>();
            List<string[]> hamtokenizedEmailList = new List<string[]>();
            List<Corpus> goodTokensList = new List<Corpus>(); //list of good tokens
            List<Corpus> badTokensList = new List<Corpus>(); //list of bad tokens

            //Dictionary<Corpus, int> GoodBadTokens = new Dictionary<Corpus, int>();
            Corpus hamTokens = new Corpus();
            Corpus spamTokens = new Corpus();
            for (i = 0; i < mails.Count; i++)
            {
                if (mails.ElementAt(i).Value == 1)
                    spamTokens.LoadFromFile(mails.ElementAt(i).Key); //load each email from file using their corresponding url, and split to tokens
                else
                    hamTokens.LoadFromFile(mails.ElementAt(i).Key); //load each email from file using their corresponding url, and split to tokens

                //GoodBadTokens[tokens] = mails.ElementAt(i).Value; //save each tokenized email (and its corresponding class) in a dictionary
                //List<string> mail = mails.ElementAt(i).Key.Split(' ').ToList();   
                //tokenizedEmail = TokenizeEmail(mails.ElementAt(i).Key); //tokenize emails
                //tokenizedCleanEmail = cleanEmail(tokenizedEmail); //clean tokenized emails - remove stop words and other unwanted words and characters
                //tokenizedEmailList.Add(tokenizedCleanEmail); //add clean tokenized email to list
                //collatedTokenizedEmail = collatedTokenizedEmail.Concat(tokenizedEmailList[i]).ToArray(); //collate all clean tokenized emails in the dataset

                //tokenizedEmail = tokenizedEmail.Concat(TokenizeEmail(mails.ElementAt(i).Key)).ToArray();
                //TokenizeEmail(mails.ElementAt(i).Key); //tokenize each email

                //testFileURLs = testFileURLs.Concat(testTrainFolderFiles).ToArray();
                //for (j = 0; j < NumOfFeatures; j++)
                //{
                //    //checking the phishing emails for each features
                //    vector[i, j] = 0; //asigning zero to vector[] if the currently checked feature dosen't exist in the current mail
                //    pds.AssignVector(vector, mails, mailURLs[i], features[j], mail, i, j); //start assigning vector to each phish mail
                //}
            }

            _filter = new SpamFilter();
            _filter.Load(hamTokens, spamTokens);

            //Dictionary<string, double> filteredFeatures = new Dictionary<string,double>();
            List<string> filteredFeaturesList = new List<string>();
            //extracting features with high probabilities - i.e. important features
            foreach (KeyValuePair<string, double> filteredTokens in _filter.Prob)
            {
                if (filteredTokens.Value >= 0.9999)
                {
                    //filteredFeatures[filteredTokens.Key] = filteredTokens.Value;
                    filteredFeaturesList.Add(filteredTokens.Key);
                }
            }

            List<string> additionalFeature = Features().ToList(); //get the features from Function
            filteredFeaturesList.AddRange(additionalFeature); //update list of features with additional features
            int totalFeatures = filteredFeaturesList.Count;
            int[,] vector = new int[mails.Count, totalFeatures];
            for (int m = 0; m < mails.Count; m++)
            {
                Corpus emailDict = new Corpus();
                emailDict.LoadFromFile(mails.ElementAt(m).Key); //extract email from file
                SortedDictionary<string, int> sortedMail = emailDict.Tokens;
                List<string> tokenizedEmails = cleanEmail(emailDict.Tokens.Keys.ToArray()).ToList(); //get tokenized email from dictionary
                int count = sortedMail.Count;
                List<string> remov = new List<string>();

                //clean the dictionary by removing element that is not appear in the cleaned tokenized email
                foreach (KeyValuePair<string, int> word in sortedMail)
                {
                    if (!tokenizedEmails.Contains(word.Key))
                        remov.Add(word.Key);
                }

                for (int mm = 0; mm < remov.Count; mm++)
                {
                    sortedMail.Remove(remov[mm]);
                }

                //List<string> tokenizedEmails = tokenizedEmailList[m].ToList(); //sake tokens in a list
                vector = AssignVector(vector, mails, sortedMail, filteredFeaturesList, tokenizedEmails, m, totalFeatures); //assign vector to each email - 1 if feature is contained in email and 0 otherwise
            }

            int[,] binarizedVec = ConvertToBinary(vector); //binarize vector values

            features = new string[totalFeatures];
            filteredFeaturesList.ToArray().CopyTo(features, 0); //returing the selected features
            return binarizedVec;

            /***
           collatedTokenizedEmail.Concat(add); //add three special charactes to email list - "!", "$" and "?"
           wordCount = countWordOccurence(collatedTokenizedEmail); //count number of times words appear in the dataset
           //wordCount = wordCount.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value); //sort the dictionary in ascending order
           
           //save N words with highest occurence
           List<string> importantWords = new List<string>();
           //for (int k = n-1; k >=0; k--)
           //{
           //    importantWords.Add(wordCount.ElementAt(k).Key);
           //}

           //extracting words that appears more than 50 times in entire dataset
           for (int k = 0; k < wordCount.Count; k++)
           {
               if (wordCount.ElementAt(k).Value >= 50)
                   importantWords.Add(wordCount.ElementAt(k).Key);
           }

           //vector = new int[mails.Count, importantWords.Count];
           Dictionary<string, int> feature_Count = new Dictionary<string, int>(); //count the number of times each feature appear in an email
           List<Dictionary<string, int>> featureCountList = new List<Dictionary<string, int>>();
           int[] acrossCount = new int[importantWords.Count];

           //counting the number of times each feature appears across all the emails in dataset
           for (i = 0; i < mails.Count; i++)
           {
               //List<string> mail = TokenizeEmail(mails.ElementAt(i).Key).ToList();
               string[] mail = tokenizedEmailList[i];
               //mail = cleanEmail(mail.ToArray());

               int k = 0;
               foreach (string word in importantWords)
               {
                   int count = mail.Count(a => a == word); //count the number of time each feature appear in each email
                   feature_Count[word] = count; //save feature and its corresponding count
                   if (count > 0)
                       acrossCount[k]++; //for each feature, count the total number of emails they appear
                   k++;
               }

               featureCountList.Add(feature_Count); //save list of feature and count for email i
               feature_Count = new Dictionary<string, int>();
               //for (j = 0; j < importantWords.Count; j++)
               //{
               //    //checking the phishing emails for each features
               //    vector[i, j] = 0; //asigning zero to vector[] if the currently checked feature dosen't exist in the current mail
               //    pds.AssignVector(vector, mails, mailURLs[i], importantWords[j], mail, i, j); //start assigning vector to each phish mail
               //}
           }

           //extract new features - words that appears more often across all the emails in the dataset
           List<string> newFeat = new List<string>();
           for (int m = 0; m < acrossCount.Count(); m++)
           {
               if (acrossCount[m] > 0.2 * mails.Count) //get words that appears in at least 20% of the dataset emails
                   newFeat.Add(importantWords[m]);
           }

           int[,] vector = new int[mails.Count, newFeat.Count];
           for (int m = 0; m < mails.Count; m++)
           {
               List<string> mail = tokenizedEmailList[m].ToList();
               vector = AssignVector(vector, newFeat, mail, m, newFeat.Count); //assign vector to each email - 1 if feature is contained in email and 0 otherwise
                
               //for (n = 0; n < newFeat.Count; n++)
               //{
               //    //checking the phishing emails for each features
               //    vector[m, n] = 0; //asigning zero to vector[] if the currently checked feature dosen't exist in the current mail
               //    //pds.AssignVector(vector, mails, mailURLs[i], newFeat[n], mail, m, n); //start assigning vector to each phish mail
               //}
           }
           ***/

            
        }

        //binarize vector
        public int[,] ConvertToBinary(int[,] vector)
        {
            int[,] binarizedVector = new int[vector.GetLength(0), vector.GetLength(1)];
            
            int[] temp = new int[vector.GetLength(1)];

            for (int i = 0; i < vector.GetLength(0); i++)
            {
                for (int j = 0; j < vector.GetLength(1); j++)
                {
                    temp[j] = vector[i, j]; //select row, and save in temp[] array
                }
                for (int j = 0; j < vector.GetLength(1); j++)
                {
                    int countZeros = temp.Count(a => a == 0); //count the total number of zeros in temp, to check whether all elements in temp are zero
                    int min = countZeros == temp.Count() ? 0 : temp.Where(val => val > 0).Min(); //select the minimum non zero vector. Assign 0, if all elements in temp is 0
                    if (vector[i, j] == 0 || vector[i, j] == 1) //don't binirize vector if it is already in its binary form (i.e. 0 or 1)
                        binarizedVector[i, j] = vector[i, j];
                    else if(vector[i, j] > min)
                        binarizedVector[i, j] = 1; //assign 1, if value is greater than the minimum
                    else
                        binarizedVector[i, j] = 0;
                }
            }

            return binarizedVector;
        }

        //process vectors for Spam Words
        public int processSpamWordVectors(Dictionary<string,int> mails, int m)
        {
             //extract the vector values for the additional features
            string TokenPattern = @"([a-zA-Z]\w+)\W*";
            Regex re = new Regex(TokenPattern, RegexOptions.Compiled);
            List<string> combinedTokens = new List<string>();
            List<string> spamList = SpamWords(); //get all the spam words from function SpamWords()

            
                int TotalOccurence = 0;
                string read = File.ReadAllText(mails.ElementAt(m).Key);
                string[] tokenized = TokenizeEmail(read);
                string combinedString = string.Join(" ", tokenized); // convert List of words to single string
                foreach (string spam in spamList)
                {
                    string[] split = spam.Split(' ');
                    //var spamWords = split.Select(w => @"\b" + w + @"\b"); //select substring (i.e. words to be searched)
                    //var spamWords = split.Select(w => @"(\W|^)" + w + @"(\W|$)"); //select substring (i.e. words to be searched)
                    //var spamPattern = new Regex("(" + string.Join(")|(", spamWords) + ")"); //form a RegEx pattern to match substring 
                    //var spamPattern = new Regex("^(" + string.Join("", split) + ")$"); //form a RegEx pattern to match substring 
                    var spamPattern = new Regex("(" + @"\b" + string.Join(" ", split) + @"\b" + ")"); //form a RegEx pattern to match substring 

                    //Check whether the regular expression has '#' after b (i.e. \b#\b), the '#' symbol requires a different regular expression
                    string str = spamPattern.ToString();
                    int index = str.IndexOf("b"); //get the first occurence of 'b'. 
                    string substr = spamPattern.ToString().Substring(index + 1, 1); //get the character after 'b'

                    if (substr == "#") //come here if the RegExpr has '#' symbol. The '#' symbol requires a different regular expression
                    {
                        StringBuilder sb = new StringBuilder(str);
                        sb[index] = 'B'; //replace 'b' with 'B' (i.e \b => \B). This is necessary for the Reular expression engine to match '#'.
                        str = sb.ToString();
                        TotalOccurence += Regex.Matches(combinedString, str).Count; //count the number of occurence of word
                    }
                    else
                        TotalOccurence += Regex.Matches(combinedString, spamPattern.ToString()).Count; //count the number of occurence of each word or sentence in SpamList
                    //vector[p, m] = TotalOccurence > 1 ? 1 : 0; //assign 1 if at least one occurence is found, else, assign 0
                }
                return TotalOccurence;
        }

        public int[,] processVector(Dictionary<string, int> mails, string[] features, int numOfFeat)
        {
            string[] tokenizedCleanEmail = new string[] { };
            string[] tokenizedEmail = new string[] { };
            string[] collatedTokenizedEmail = new string[] { };
            Dictionary<string, int> wordCount = new Dictionary<string, int>();
            List<string[]> tokenizedEmailList = new List<string[]>();
            int[,] vector = new int[mails.Count, numOfFeat];

            for (int m = 0; m < mails.Count; m++)
            {
                Corpus emailDict = new Corpus();
                emailDict.LoadFromFile(mails.ElementAt(m).Key); //extract email from file
                List<string> tokenizedEmails = emailDict.Tokens.Keys.ToList(); //get tokenized email from dictionary
                //List<string> tokenizedEmails = tokenizedEmailList[m].ToList(); //sake tokens in a list
                vector = AssignVector(vector, mails, emailDict.Tokens, features.ToList(), tokenizedEmails, m, numOfFeat); //assign vector to each email - 1 if feature is contained in email and 0 otherwise
            }

            int[,] binirizedVector = ConvertToBinary(vector); //convert vector to binary

            /***
            for (int i = 0; i < mails.Count; i++)
            {
                tokenizedEmail = TokenizeEmail(mails.ElementAt(i).Key); //tokenize emails
                tokenizedCleanEmail = cleanEmail(tokenizedEmail); //clean tokenized emails - remove stop words and other unwanted words and characters
                tokenizedEmailList.Add(tokenizedCleanEmail); //add clean tokenized email to list
            }
           
            for (int m = 0; m < mails.Count; m++)
            {
                List<string> mail = tokenizedEmailList[m].ToList();
                vector = AssignVector(vector, features.ToList(), mail, m, numOfFeat); //assign vector to each email - 1 if feature is contained in email and 0 otherwise
            }
            ***/

            return binirizedVector;
        }

        //stop words
        public List<string> stopWords()
        {
            List<string> list = new List<string>()
            {
"a", "able", "about", "above", "according", "accordingly", "across", "actually", "after", "afterwards", "again", "against", "ain't", "all", "allow", "allows", "almost",
"alone", "along", "already", "also", "although", "always", "am", "among", "amongst", "an", "and", "another", "any", "anybody", "anyhow", "anyone", "anything", "anyway",
"anyways", "anywhere", "apart", "appear", "appreciate", "appropriate", "are", "aren't", "around", "as", "a's", "aside", "ask", "asking", "associated", "at", "available",
"away", "awfully", "be", "became", "because", "become", "becomes", "becoming", "been", "before", "beforehand", "behind", "being", "believe", "below", "beside", "besides",
"best", "better", "between", "beyond", "both", "brief", "but", "by", "came", "can", "cannot", "cant", "can't", "cause", "causes", "certain", "certainly", "changes",
"clearly", "c'mon", "co", "com", "come", "comes", "concerning", "consequently", "consider", "considering", "contain", "containing", "contains", "corresponding",
"could", "couldn't", "course", "c's", "currently", "definitely", "described", "despite", "did", "didn't", "different", "do", "does", "doesn't", "doing", "done", "don't",
"down", "downwards", "during", "each", "edu", "eg", "eight", "either", "else", "elsewhere", "enough", "entirely", "especially", "et", "etc", "even", "ever", "every","everybody",
"everyone", "everything", "everywhere", "ex", "exactly", "example", "except", "far", "few", "fifth", "first", "five", "followed", "following", "follows", "for", "former",
"formerly", "forth", "four", "from", "further", "furthermore", "get", "gets", "getting", "given", "gives", "go", "goes", "going", "gone", "got", "gotten", "greetings", "had",
"hadn't", "happens", "hardly", "has", "hasn't", "have", "haven't", "having", "he", "he'd", "he'll", "hello", "help", "hence", "her", "here", "hereafter", "hereby","herein",
"here's", "hereupon", "hers", "herself", "he's", "hi", "him", "himself", "his", "hither", "hopefully", "how", "howbeit", "however", "how's", "i", "i'd", "ie", "if", "ignored",
"i'll", "i'm", "immediate", "in", "inasmuch", "inc", "indeed", "indicate", "indicated", "indicates", "inner", "insofar", "instead", "into", "inward", "is", "isn't", "it", "it'd",
"it'll", "its", "it's", "itself", "i've", "just", "keep", "keeps", "kept", "know", "known", "knows", "last", "lately", "later", "latter", "latterly", "least", "less", "lest",
"let", "let's", "like", "liked", "likely", "little", "look", "looking", "looks", "ltd", "mainly", "many", "may", "maybe", "me", "mean", "meanwhile", "merely", "might", "more",
"moreover", "most", "mostly", "much", "must", "mustn't", "my", "myself", "name", "namely", "nd", "near", "nearly", "necessary", "need", "needs", "neither", "never", 
"nevertheless", "new", "next", "nine", "no", "nobody", "non", "none", "noone", "nor", "normally", "not", "nothing", "novel", "now", "nowhere", "obviously", "of", "off",
"often", "oh", "ok", "okay", "old", "on", "once", "one", "ones", "only", "onto", "or", "other", "others", "otherwise", "ought", "our", "ours", "ourselves", "out", "outside",
"over", "overall", "own", "particular", "particularly", "per", "perhaps", "placed", "please", "plus", "possible", "presumably", "probably", "provides", "que", "quite",
"qv", "rather", "rd", "re", "really", "reasonably", "regarding", "regardless", "regards", "relatively", "respectively", "right", "said", "same", "saw", "say", "saying", "says",
"second", "secondly", "see", "seeing", "seem", "seemed", "seeming", "seems", "seen", "self", "selves", "sensible", "sent", "serious", "seriously", "seven", "several", "shall",
"shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "since", "six", "so", "some", "somebody", "somehow", "someone", "something", "sometime", "sometimes",
"somewhat", "somewhere", "soon", "sorry", "specified", "specify", "specifying", "still", "sub", "such", "sup", "sure", "take", "taken", "tell", "tends", "th", "than", "thank",
"thanks", "thanx", "that", "thats", "that's", "the", "their", "theirs", "them", "themselves", "then", "thence", "there", "thereafter", "thereby", "therefore", "therein",
"theres", "there's", "thereupon", "these", "they", "they'd", "they'll", "they're", "they've", "think", "third", "this", "thorough", "thoroughly", "those", "though", "three",
"through", "throughout", "thru", "thus", "to", "together", "too", "took", "toward", "towards", "tried", "tries", "truly", "try", "trying", "t's", "twice", "two", "un",
"under", "unfortunately", "unless", "unlikely", "until", "unto", "up", "upon", "us", "use", "used", "useful", "uses", "using", "usually",  "value", "various", "very",
"via", "viz", "vs", "want", "wants", "was", "wasn't", "way", "we", "we'd", "welcome", "well", "we'll", "went",  "were", "we're", "weren't", "we've", "what", "whatever",
"what's", "when", "whence", "whenever", "when's", "where", "whereafter", "whereas", "whereby", "wherein", "where's", "whereupon", "wherever", "whether", "which",
"while","whither", "who", "whoever", "whole", "whom", "who's", "whose", "why", "why's", "will", "willing", "wish", "with", "within", "without", "wonder", "won't", "would",
"wouldn't", "yes", "yet", "you", "you'd", "you'll", "your", "you're", "yours", "yourself", "yourselves", "you've", "zero"
            };

            return list;
        }

        //dictionary of spam words - courtesy of "http://www.site.uottawa.ca/~nat/Courses/csi5387_Winter2014/paper13.pdf"
        public List<string> SpamWords()
        {
            List<string> spamList = new List<string>() 
            {
                "#1", "$$$", "100% free", "100% Satisfied", "4U", "50% off", "Accept Credit Cards", "Acceptance", "Access", "Accordingly", "Ad", "Additional Income", 
                "Addresses on CD", "Affordable", "All natural", "All new", "Amazing", "Apply now", "Apply Online", "As seen on", "Auto email removal", "Avoid", 
                "Avoid bankruptcy", "Bargain", "Be your own boss", "Being a member", "Beneficiary", "Best price", "Beverage", "Big bucks", "Billing address", "Billion dollars", 
                "Bonus", "Brand new pager", "Bulk email", "Buy", "Buying judgments", "Cable converter", "Call", "Call free", "Call now", "Calling creditors", 
                "Cancel at any time", "Cannot be combined with any other offer", "Can't live without", "Cards accepted", "Cash", "Cash bonus", "Cashcashcash", "Casino", 
                "Celebrity", "Cents on the dollar", "Certified", "Chance", "Cheap", "Check", "Check or money order", "Claims", "Clearance", "Click", "Click below", "Click here",
                "Click to remove", "Collect", "Collect child support", "Compare", "Compare rates", "Compete for your business", "Confidentially on all orders", "Congratulations",
                "Consolidate debt and credit", "Consolidate your debt", "Copy DVDs", "Cost", "Credit", "Credit bureaus", "Credit card offers", "Cures baldness", "Deal", 
                "Diagnostics", "Dig up dirt on friends", "Direct email", "Direct marketing", "Discount", "Do it today", "Don't hesitate", "Dormant", "Double your", 
                "Drastically reduced", "Earn", "Earn extra cash", "Earn per week", "Easy terms", "Eliminate bad credit", "Eliminate debt", "Email harvest", "Email marketing", 
                "Expect to earn", "Explode your business", "Extra income", "F r e e", "Fantastic deal", "Fast cash", "Fast Viagra delivery", "Financial freedom", 
                "Financially independent", "For free", "For instant access", "For just $XXX", "For Only", "For you", "Form", "Free", "Free access", "Free cell phone", 
                "Free consultation", "Free DVD", "Free gift", "Free grant money", "Free hosting", "Free installation", "Free Instant", "Free investment", "Free leads", 
                "Free membership", "Free money", "Free offer", "Free preview", "Free priority mail", "Free quote", "Free sample", "Free trial", "Free website", "Freedom", 
                "Friend", "Full refund", "Get", "Get out of debt", "Get paid", "Get started now", "Gift certificate", "Giving away", "Great offer", "Guarantee", "Guaranteed", 
                "Have you been turned down?", "Here", "Hidden", "Hidden assets", "hidden charges", "Home", "Home based", "Home employment", "Homebased business", 
                "Human growth hormone", "If only it were that easy", "Important information regarding", "In accordance with laws", "Income", "Income from home", "Increase sales",
                "Increase traffic", "Increase your sales", "Incredible deal", "Info you requested", "Information you requested", "Instant", "Insurance", "Internet market", 
                "Internet marketing", "Internet Traffic", "Investment", "Investment decision", "It�s effective", "Join millions", "Join millions of Americans", "Laser printer", 
                "Leave", "Legal", "Life Insurance", "Lifetime", "Loans", "Long distance phone offer", "Lose", "Lose weight", "Lose weight", "Lower interest rate", 
                "Lower monthly payment", "Lower your mortgage rate", "Lowest insurance rates", "Lowest price", "Luxury car", "Mail in order form", "Maintained", "Make money", 
                "Marketing", "Marketing solutions", "Mass email", "Medicine", "Medium", "Meet singles", "Member", "Message contains", "Million", "Million dollars", "Miracle", 
                "Money", "Money back", "Money making", "Month trial offer", "More", "Mortgage", "Mortgage rates", "Multi level marketing", "Name brand", "Never", 
                "New customers only", "New domain extensions", "Nigerian", "No age restrictions", "No catch", "No claim forms", "No cost", "No credit check", "No disappointment",
                "No experience", "No fees", "No gimmick", "No hidden Costs", "No inventory", "No investment", "No medical exams", "No middleman", "No obligation", 
                "No purchase necessary", "No questions asked", "No selling", "No strings attached", "No-obligation", "Not intended", "Notspam", "Now", "Obligation", "Off shore",
                "Offer", "Offer expires", "Once in lifetime", "One hundred percent free", "One hundred percent guaranteed", "One time", "One time mailing", 
                "Online biz opportunity", "Online degree", "Online marketing", "Online pharmacy", "Only", "Only $", "Open", "Opportunity", "Opt in", "Order", "Order now", 
                "Order status", "Order today", "Orders shipped by", "Outstanding values", "Passwords", "Pennies a day", "Per day", "Per week", "Performance", "Phone", 
                "Please read", "Potential earnings", "Pre-approved", "Price", "Print form signature", "Print out and fax", "Priority mail", "Prize", "Prizes", "Problem", 
                "Produced and sent out", "Profits", "Promise you", "Pure profit", "Quote", "Real thing", "Refinance", "Refinance home", "Removal instructions", "Remove", 
                "Removes wrinkles", "Requires initial investment", "Reserves the right", "Reverses", "Reverses aging", "Risk free", "Rolex", "Sale", "Sales", "Sample", 
                "Satisfaction", "Satisfaction guaranteed", "Save $", "Save big money", "Save up to", "Score with babes", "Search engine listings", "Search engines", 
                "See for yourself", "Sent in compliance", "Serious cash", "shopper", "Shopping spree", "Sign up free today", "Social security number", "Solution", 
                "Special promotion", "Stainless steel", "Stock alert", "Stock disclaimer statement", "Stock pick", "Stop", "Stop snoring", "Stuff on sale", "Subject to credit", 
                "Subscribe", "Success", "Supplies are limited", "Take action now", "Teen", "Terms and conditions", "The best rates", "The following form", 
                "They keep your money -- no refund!", "They�re just giving it away", "This isn't junk", "This isn't spam", "Thousands", "Time limited", "Trial", 
                "Undisclosed recipient", "University diplomas", "unlimited", "Unsecured credit", "Unsecured debt", "Unsolicited", "Unsubscribe", "Urgent", "US dollars", 
                "Vacation", "Vacation offers", "Valium", "Viagra", "Vicodin", "Visit our website", "Warranty", "We hate spam", "We honor all", "Web traffic", "Weekend getaway", 
                "Weight loss", "What are you waiting for?", "While supplies last", "While you sleep", "Who really wins?", "Why pay more?", "Wife", "Will not believe your eyes", 
                "Win", "Winner", "Winning", "won", "Work at home", "Work from home", "Xanax", "You are a winner!", "You have been selected", "You�re a Winner!", "Your income"
            };

            return spamList;
        }

        //list of stop words as listed in 
        public List<string> StopWords()
        {
            List<string> stopWordList = new List<string>() 
            {
                "a", "about", "above", "according", "across", "actually", "adj", "after", "afterwards", "again", "against", "all", "almost", "alone", "along", "already", "also",
                "although", "always", "am", "among", "amongst", "amoungst", "amount", "an", "and", "another", "any", "anybody", "anyhow", "anyone", "anything", "anyway", 
                "anywhere", "are", "area", "areas", "aren't", "around", "as", "ask", "asked", "asking", "asks", "at", "away", "b", "back", "backed", "backing", "backs", "be", 
                "became", "because", "become", "becomes", "becoming", "been", "before", "beforehand", "began", "begin", "beginning", "behind", "being", "beings", "below", 
                "beside", "besides", "best", "better", "between", "beyond", "big", "bill", "billion", "both", "bottom", "but", "by", "c", "call", "came", "can", "can't", 
                "cannot", "cant", "caption", "case", "cases", "certain", "certainly", "clear", "clearly", "co", "com", "come", "computer", "con", "could", "couldn't", "couldnt",
                "cry", "d", "de", "describe", "detail", "did", "didn't", "differ", "different", "differently", "do", "does", "doesn't", "don't", "done", "down", "downed", 
                "downing", "downs", "due", "during", "e", "each", "early", "eg", "eight", "eighty", "either", "eleven", "else", "elsewhere", "empty", "en", "end", "ended", 
                "ending", "ends", "enough", "etc", "even", "evenly", "ever", "every", "everybody", "everyone", "everything", "everywhere", "except", "f", "face", "faces", 
                "fact", "facts", "far", "felt", "few", "fifteen", "fifty", "fify", "fill", "find", "finds", "fire", "first", "five", "for", "former", "formerly", "forty", 
                "found", "four", "from", "front", "full", "fully", "further", "furthered", "furthering", "furthers", "g", "gave", "general", "generally", "get", "gets", 
                "give", "given", "gives", "go", "going", "good", "goods", "got", "great", "greater", "greatest", "group", "grouped", "grouping", "groups", "h", "had", "has", 
                "hasn't", "hasnt", "have", "haven't", "having", "he", "he'd", "he'll", "he's", "hence", "her", "here", "here's", "hereafter", "hereby", "herein", "hereupon", 
                "hers", "herself", "high", "higher", "highest", "him", "himself", "his", "how", "however", "hundred", "i", "i'd", "i'll", "i'm", "i've", "ie", "if", "important",
                "in", "inc", "indeed", "instead", "interest", "interested", "interesting", "interests", "into", "is", "isn't", "it", "it's", "its", "itself", "j", "just", "k", 
                "keep", "keeps", "kind", "knew", "know", "known", "knows", "l", "la", "large", "largely", "last", "later", "latest", "latter", "latterly", "least", "less", 
                "let", "let's", "lets", "like", "likely", "long", "longer", "longest", "ltd", "m", "made", "make", "makes", "making", "man", "many", "may", "maybe", "me", 
                "meantime", "meanwhile", "member", "members", "men", "might", "mill", "million", "mine", "miss", "more", "moreover", "most", "mostly", "move", "mr", "mrs", 
                "much", "must", "my", "myself", "n", "name", "namely", "necessary", "need", "needed", "needing", "needs", "neither", "never", "nevertheless", "new", "newer", 
                "newest", "next", "nine", "ninety", "no", "nobody", "non", "none", "nonetheless", "noone", "nor", "not", "nothing", "now", "nowhere", "number", "numbers", "o", 
                "of", "off", "often", "old", "older", "oldest", "on", "once", "one", "one's", "only", "onto", "open", "opened", "opening", "opens", "or", "order", "ordered", 
                "ordering", "orders", "other", "others", "otherwise", "our", "ours", "ourselves", "out", "over", "overall", "own", "p", "part", "parted", "parting", "parts", 
                "per", "perhaps", "place", "places", "please", "point", "pointed", "pointing", "points", "possible", "present", "presented", "presenting", "presents", "problem",
                "problems", "put", "puts", "q", "quite", "r", "rather", "re", "really", "recent", "recently", "right", "room", "rooms", "s", "said", "same", "saw", "say", 
                "says", "second", "seconds", "see", "seem", "seemed", "seeming", "seems", "sees", "serious", "seven", "seventy", "several", "shall", "she", "she'd", "she'll", 
                "she's", "should", "shouldn't", "show", "showed", "showing", "shows", "side", "sides", "since", "sincere", "six", "sixty", "small", "smaller", "smallest", "so", 
                "some", "somebody", "somehow", "someone", "something", "sometime", "sometimes", "somewhere", "state", "states", "still", "stop", "such", "sure", "system", "t", 
                "take", "taken", "taking", "ten", "than", "that", "that'll", "that's", "that've", "the", "their", "them", "themselves", "then", "thence", "there", "there'd", 
                "there'll", "there're", "there's", "there've", "thereafter", "thereby", "therefore", "therein", "thereupon", "these", "they", "they'd", "they'll", "they're", 
                "they've", "thick", "thin", "thing", "things", "think", "thinks", "third", "thirty", "this", "those", "though", "thought", "thoughts", "thousand", "three", 
                "through", "throughout", "thru", "thus", "to", "today", "together", "too", "took", "top", "toward", "towards", "trillion", "turn", "turned", "turning", "turns", 
                "twelve", "twenty", "two", "u", "un", "und", "under", "unless", "unlike", "unlikely", "until", "up", "upon", "us", "use", "used", "uses", "using", "v", "very", 
                "vfor", "via", "w", "want", "wanted", "wanting", "wants", "was", "wasn't", "way", "ways", "we", "we'd", "we'll", "we're", "we've", "well", "wells", "went", 
                "were", "weren't", "what", "what'll", "what's", "what've", "whatever", "when", "whence", "whenever", "where", "where's", "whereafter", "whereas", "whereby", 
                "wherein", "whereupon", "wherever", "whether", "which", "while", "whither", "who", "who'd", "who'll", "who's", "whoever", "whole", "whom", "whomever", "whose", 
                "why", "will", "with", "within", "without", "won't", "work", "worked", "working", "works", "would", "wouldn't", "www", "x", "y", "year", "years", "yes", "yet", 
                "you", "you'd", "you'll", "you're", "you've", "young", "younger", "youngest", "your", "yours", "yourself", "yourselves", "z"
            };

            return stopWordList;
        }

        //count words in email and assign the count to the corresponding email
        public Dictionary<string, int> countWordOccurence(string[] tokenizedEmail)
        {
            List<string> stopwords = stopWords();
            Dictionary<string, int> wordCount = new Dictionary<string, int>();
            //Dictionary<string, int> wordCount = new Dictionary<string,int>();
            //Regex re = new Regex(@"^(?=[^\s]*?[0-9])(?=[^\s]*?[a-zA-Z])[a-zA-Z0-9]*$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);

            //remove stop words
            //foreach (string word in stopwords)
            //{
            //    tokenizedEmail = removeArrayElement(tokenizedEmail, word);
            //}

            //clean email and count the number of occurence of each words in the dataset
            //tokenizedEmail = cleanEmail(tokenizedEmail); //clean email
            foreach (string word in tokenizedEmail)
            {
                int count = countOccurence(tokenizedEmail, word); //count the number of times each word appear in the dataset
                
                //if (count == 1)
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that appear only once from dataset
                //else if(word.Count() <=2)
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that is less than three characters
                //else if (word.Count() > 20)
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that is longer than 20 from tokenized dataset
                //else if(AreAllCharactersSame(word))
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words having the same character - e.g. ssss, nnn, oo, etc
                //else if(re.IsMatch(word))
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words having combination of characters and words - e.g 12rd2, 23dfs
                //else if(IsDigitsOnly(word))
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words containing only numbers words - e.g 12rd2, 23dfs
               wordCount[word] = count; //assigning key-word pair. That is, counting each word and their corresponsing word count
            }

            return wordCount;
        }

        public int countOccurence(string[] tokenizedEmail, string word)
        {
            return tokenizedEmail.Count(a => a == word);
        }

        //remove stop words and other thrash from email
        public string[] cleanEmail(string[] tokenizedEmail)
        {
            Regex re = new Regex(@"^(?=[^\s]*?[0-9])(?=[^\s]*?[a-zA-Z])[a-zA-Z0-9]*$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);
            List<string> stopwords = stopWords();

            //remove stop words
            foreach (string word in stopwords)
                tokenizedEmail = removeArrayElement(tokenizedEmail, word);

            foreach (string word in tokenizedEmail)
            {
                int count = countOccurence(tokenizedEmail, word); //count the number of times each word appear in the dataset
                //if (count == 1)
                //    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that appear only once from dataset
                if (word.Count() <= 2)
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that is less than three characters
                else if (word.Count() > 20)
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that is longer than 20 from tokenized dataset
                else if (AreAllCharactersSame(word))
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words having the same character - e.g. ssss, nnn, oo, etc
                else if (re.IsMatch(word))
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words having combination of characters and words - e.g 12rd2, 23dfs
                else if (IsDigitsOnly(word))
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words containing only numbers words - e.g 1122, 234221, 21
            }

            return tokenizedEmail;
        }

        //check whether all characters in word is the same
        public static bool AreAllCharactersSame(string s)
        {
            return s.Length == 0 || s.All(ch => ch == s[0]); //if string length is 0, then return true, bcos, all characters are the same, OR if the first character is equal to All the other characters
        }

        //check whether word contain only digits
        public bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        //tokenize emails
        public string[] TokenizeEmail(string email)
        {
            char[] delimiters = new char[] { '�', '<', '>', '\"', '(', ')', '[', ']', '{', '}', '/', '\\', '|', '-', '_', '^', '&', '*', ',', '.', ':', ';', '@', '~', '\'', '+', '=', '\n', '\r', '!', '?', ' ' };
            string[] split = email.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            return split;
        }

        //select top features that appears more frequently across all the folds - i.e. cross validation folds
        public string[] getTopFeatures(List<string[]> featList, int NumofFeatures)
        {
            List<string> collfeat = new List<string>(); //collated features
            Dictionary<string, int> feat_Count = new Dictionary<string, int>();

            foreach (string[] feat in featList)
            {
                foreach (string fe in feat)
                {
                    collfeat.Add(fe); //collate all the features obtained from all the folds into one list
                }
            }

            //count the total number of times each feature appear in the list
            foreach (string fe in collfeat)
            {
                int count = collfeat.Count(w => w == fe); //count the total number of times each feature appear in the list
                feat_Count[fe] = count;
            }

            feat_Count = feat_Count.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value); //sort the dictionary in descending order

            //select the features that appears frequently across all the folds - i.e. top N features
            string[] topFeatures = new string[NumofFeatures];
            for (int b = 0; b < NumofFeatures; b++)
            {
                topFeatures[b] = feat_Count.ElementAt(b).Key; //select top N features, where N = NumberofFeatures
            }

            return topFeatures;
        }

        /*
        //count and save the total number of times each word in the dataset occurs
        public Dictionary<string, int> wordCountOccurence(string[] tokenizedEmail)
        {
            Dictionary<string, int> wordCount = new Dictionary<string,int>();
            foreach (string word in tokenizedEmail)
            {
                int count = tokenizedEmail.Count(a => a == word); //count the number of times each word appear in the dataset
                if (count == 1)
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that appear only once from dataset
                else if (word.Count() > 20)
                    tokenizedEmail = removeArrayElement(tokenizedEmail, word); //remove words that is longer than 20 from tokenized dataset
                else
                    wordCount[word] = count; //assigning key-word pair. That is, counting each word and their corresponsing word count
            }

            return wordCount;
        }*/

        //remove certain words from dataset
        public string[] removeArrayElement(string[] tokenizedEmail, string remove)
        {
            tokenizedEmail = tokenizedEmail.Where(w => w != remove).ToArray();
            return tokenizedEmail;
        }

        /// <summary>
        /// Extract the vector values of each email
        /// </summary>
        //public void AssignVector(int[,] vector, Dictionary<string, int> mails, string url, string Feature, List<string> mail, int i, int j)
        public int[,] AssignVector(int[,] vector, Dictionary<string,int> mailDict, SortedDictionary<string, int> mailToken_count, List<string> newFeat, List<string> mail, int i, int numOfFeat)
        {
            int count = 0;
            string url = mailDict.ElementAt(i).Key; //url of current email
            string email = ProcessMails(File.ReadAllText(url)); //extract and process email
            List<string> mailSplit = email.Split(' ').ToList(); //split email
            Dictionary<string, double[]> featVal = new Dictionary<string, double[]>();
            double[] featCount = new double[numOfFeat];

            for (int j = 0; j < numOfFeat; j++)
            {
                string feature = newFeat[j];
                if (feature == "SpamWords" || feature == "StopWords")
                {
                    if (feature == "SpamWords")
                    {
                        count = processSpamWordVectors(mailDict, i);
                        //vector[i, j] = count; //assign 1 if there is more than one occurence of SpamWords, else, assign 0
                        vector[i, j] = count > 1 ? 1 : 0; //assign 1 if there is more than one occurence of SpamWords, else, assign 0
                    }
                    else if (feature == "StopWords")
                    {
                        count = CountStopWords(mailDict, i);
                        //featCount[j] = count;
                        //vector[i, j] = count; //assign 1 if there is more than 100 occurence of stopWords, else, assign 0
                        vector[i, j] = count > 100 ? 1 : 0; //assign 1 if there is more than 100 occurence of stopWords, else, assign 0
                    }
                }
                else if (feature == "<a>" || feature == "content-type:" || feature == "javascript" || feature == "</a>" || feature == "linkedToDomain" || feature == "From_Body_MatchDomain"
                    || feature == "update" || feature == "suspend" || feature == "ssn" || feature == "SpamAssassin" || feature == "bankNames")
                {
                    pds.AssignVector(vector, mailDict, url, feature, mailSplit, i, j);
                }
                else if (feature == "AnchorTags" || feature == "NonAnchorTags" || feature == "TotalTags")
                {
                    if (feature == "AnchorTags")
                        count = CountAnchorTags(email);
                    else if (feature == "NonAnchorTags")
                        count = CountNonAnchorTags(email);
                    else if (feature == "TotalTags")
                        count = CountTotalTags(email);

                    //featCount[j] = count;
                    //vector[i, j] = count; //assign 1 if there is more than one occurence of tags, else, assign 0
                    vector[i, j] = count > 1 ? 1 : 0; //assign 1 if there is more than one occurence of tags, else, assign 0
                }
                else if (feature == "Alphanumeric" || feature == "TF_ISF" || feature == "TF_IDF" || feature == "Document_Length")
                {
                    if (feature == "Alphanumeric")
                    {
                        count = CountAlphanumericWords(mailSplit);
                        vector[i, j] = count;
                        //featCount[j] = count;
                        //vector[i, j] = count > 10 ? 1 : 0; //assign 1 if there is more than 10 occurence of alphanumeric, else, assign 0
                    }
                    else if (feature == "TF_ISF")
                    {
                        //vector[i, j] = (int)TermFrequency_InverseSentenceFrequency(mailToken_count, url);
                        vector[i, j] = TermFrequency_InverseSentenceFrequency(mailToken_count, url) > 100 ? 1 : 0; //assign 1 if TF_ISF is greater than 100
                    }
                    else if (feature == "Document_Length")
                    {
                        //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
                        //int sentenceCount = msWordObjectEmail.Sentences.Count;
                        string[] sentenceSplit = SplitToSentence(File.ReadAllText(url));
                        int sentenceCount = sentenceSplit.Count();
                        //vector[i, j] = sentenceCount;
                        vector[i, j] = sentenceCount > 5 ? 1 : 0; //assign 1 if sentence count is greater than 5
                    }
                    else if (feature == "TF_IDF")
                    {
                        //count = (int)TermFrequency_InverseDocumentFrequency(mailDict, mailToken_count);
                        //vector[i, j] = (int)TermFrequency_InverseDocumentFrequency(mailDict, mailToken_count);
                        vector[i, j] = TermFrequency_InverseDocumentFrequency(mailDict, mailToken_count) > 100 ? 1 : 0; //assign 1 if TF_IDF is greater than 100
                    }
                }
                else if (feature == "FogIndex" || feature == "FleshReadingScore" || feature == "SmogIndex" || feature == "ForcastIndex" || feature == "FleschKincaidReadabilityIndex")
                {
                    if (feature == "FogIndex")
                        vector[i, j] = (int)FogIndex(mailToken_count, url);
                    //vector[i, j] = FogIndex(mailToken_count, url) > 4 ? 1 : 0;
                    else if (feature == "FleshReadingScore")
                    //    vector[i, j] = (int)FleshReadingScore(mailToken_count, url);
                    vector[i, j] = FleshReadingScore(mailToken_count, url) > 100 ? 1 : 0;
                    else if (feature == "SmogIndex")
                        vector[i, j] = (int)FleshReadingScore(mailToken_count, url);
                    //vector[i, j] = SmogIndex(mailToken_count, url) > 8 ? 1 : 0;
                    else if (feature == "ForcastIndex")
                        vector[i, j] = (int)ForcastIndex(mailToken_count);
                        //vector[i, j] = ForcastIndex(mailToken_count) > 7 ? 1 : 0;
                    else if (feature == "FleschKincaidReadabilityIndex")
                        vector[i, j] = (int)FleschKincaidReadabilityIndex(mailToken_count, url);
                        //vector[i, j] = FleschKincaidReadabilityIndex(mailToken_count, url) > 3 ? 1 : 0;
                }
                else if (feature == "FogIndexSimple" || feature == "FogIndexInverse" || feature == "ComplexWords" || feature == "SimpleWords")
                {
                    if (feature == "FogIndexSimple")
                        vector[i, j] = (int)FogIndexSimple(mailToken_count, url);
                        //vector[i, j] = FogIndexSimple(mailToken_count, url) > 4 ? 1 : 0;
                    else if (feature == "FogIndexInverse")
                        vector[i, j] = (int)FogIndexInverse(mailToken_count, url);
                        //vector[i, j] = FogIndexInverse(mailToken_count, url) > 0.05 ? 1 : 0;
                    else if (feature == "ComplexWords")
                    {
                        int numComplexWords = 0;
                        foreach (string word in mailToken_count.Keys) //calculate the number of complex word for each word in email
                        {
                            int numSyllable = SyllableCount(word); //count the number of syllable in word
                            numComplexWords += numSyllable >= 3 ? 1 : 0; //add 1 if word is complex (i.e. if  it contains more than 3 sylabbles), otherwise dont increment (it is a simple word)
                        }
                        //vector[i, j] = numComplexWords;
                        vector[i, j] = numComplexWords > 15 ? 1 : 0;
                    }
                    else if (feature == "SimpleWords")
                    {
                        int numOfSimpleWords = 0;
                        foreach (string word in mailToken_count.Keys)  //calculate the number of complex word for each word in email
                        {
                            int numSyllable = SyllableCount(word); //count the number of syllable in word
                            numOfSimpleWords += numSyllable <= 2 ? 1 : 0; //add 1 if word is complex (i.e. if  it contains more than 3 sylabbles), otherwise dont increment (it is a simple word)
                        }
                        vector[i, j] = numOfSimpleWords > 50 ? 1 : 0;
                        //vector[i, j] = numOfSimpleWords;
                    }
                }
                else if (feature == "WordLength" || feature == "TF_IDFSimple" || feature == "TF_IDFComplex")
                {
                    if (feature == "WordLength")
                        //vector[i, j] = (int)EmailWordLengh(mailToken_count);
                        vector[i, j] = EmailWordLengh(mailToken_count) > 0 ? 1 : 0;
                    else if (feature == "TF_IDFSimple")
                        //vector[i, j] = (int)TermFrequency_InverseDocumentFrequencySimple(mailDict, mailToken_count);
                        vector[i, j] = TermFrequency_InverseDocumentFrequencySimple(mailDict, mailToken_count) > 10 ? 1 : 0;
                    else if (feature == "TF_IDFComplex")
                        //vector[i, j] = (int)TermFrequency_InverseDocumentFrequencyComplex(mailDict, mailToken_count);
                        vector[i, j] = TermFrequency_InverseDocumentFrequencyComplex(mailDict, mailToken_count) > 0 ? 1 : 0;
                }
                else
                {
                    count = mail.Count(a => a == feature);
                    if (count > 0)
                        vector[i, j] = 1; //assign 1 if current feature appears more than twice in the current email
                    else
                        vector[i, j] = 0; //assign 0 if current feature appears more than twice in the current email
                }
                //j++;
            }

            return vector;
        }
            /*
            StreamReader reader = new StreamReader(url);
            string htmlContent = reader.ReadToEnd();
            int ctr = 0; string join;
            htmlContent = htmlContent.ToLower(); //convert email to lower case for easy matching


            //string[] fetures = Features();
            string[] fetures = Features();
            if (fetures.Contains(Feature))
            {

                if (Feature.Equals("content-type:") || Feature.Equals("verify"))
                {
                    ctr = 0;
                    foreach (string m in mail)
                    {
                        if (m.Equals("content-type:") && Feature.Equals("content-type:"))
                        {
                            join = mail[ctr] + " " + mail[++ctr];
                            vector[i, j] = (join.Equals("content-type: text/html") || join.Equals("content-type: multipart/alternative")) ? 1 : 0;
                            if (vector[i, j] == 1) break;
                        }
                        else if (Feature.Equals("verify") && m.Equals("verify") && (join = string.Format("{0} {1} {2}", mail[ctr], mail[++ctr], mail[++ctr])) == "verify your account")
                        {
                            vector[i, j] = 1;
                            break;
                        }
                        ctr++;
                    }

                }
                else if (Feature.Equals("href"))
                {
                    vector[i, j] = DomainNameDotCount(htmlContent); //assigning the number of dots in the domain of each email
                }
                else if (Feature.Equals("href="))
                {
                    if (urlTarDifLinks(htmlContent))
                    {
                        vector[i, j] = 1;
                        // break;
                    }
                }
                else if (Feature.Equals("http"))
                {
                    if (urlDetection(htmlContent))
                    {
                        vector[i, j] = 1;
                        //break;
                    }
                }
                else if (Feature.Equals("<img"))
                {
                    if (imgLinkDetection(htmlContent))
                    {
                        vector[i, j] = 1;
                        //break;
                    }
                }
                else if (Feature.Equals("<a>"))
                {
                    int numOfDomain; //note, this variable is a dummy variable for dis feature. it is only useful when Feature == "linkedToDomain"
                    if (getURLText(htmlContent, vector, Feature, i, j, out numOfDomain))
                        vector[i, j] = 1;
                    //break;
                }
                else if (Feature.Equals("javascript"))
                {
                    if (containsJavaScript(htmlContent))
                        vector[i, j] = 1;
                    //break;
                }
                else if (Feature.Equals("</a>"))
                {
                    vector[i, j] = countNumberOfLinks(htmlContent); //assigning the total number of links in each emails
                    //break;
                }
                else if (Feature.Equals("linkedToDomain"))
                {
                    int numOfDomain;
                    getURLText(htmlContent, vector, Feature, i, j, out numOfDomain);
                    vector[i, j] = numOfDomain;//this variable is used to get the total number of domains in the urls of each email
                    //break; 
                }
                else if (Feature.Equals("From_Body_MatchDomain"))
                {
                    if (checkFrom_Body_DomainNameMismatch(url, htmlContent))
                        vector[i, j] = 1;
                }
                //processing the Word Features
                else if (Feature.Equals("update"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("user"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("suspend"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("verify"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("login"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("ssn"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent); 
                }
                else if (Feature.Equals("bankNames"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                } 
                else if (Feature.Equals("spamassassin"))
                {
                    vector[i, j] = spamassassin(url, mails, i);
                }
                else
                {
                    if (htmlContent.Contains(Feature))
                    {
                        vector[i, j] = 1;
                    }
                }
            }
           */
   

        /// <summary>
        /// Calculate the dataset entropy
        /// </summary>
        public double Entropy(Dictionary<string, int> mails, int FNum)
        {
            int TotalMails; Double DivH = 0.0, DivP, DatasetEntropy = 0.0;
            int TotalSEmail = 0, TotalHEmail = 0;

            for (int i = 0; i < mails.Count; i++)
            {
                if (mails.ElementAt(i).Value.Equals(1))
                    TotalSEmail++;
                else
                    TotalHEmail++;
            }
            

            TotalMails = TotalSEmail + TotalHEmail;
            DivH = (double)TotalHEmail / (double)TotalMails;
            DivP = (double)TotalSEmail / (double)TotalMails;
            DatasetEntropy = -(DivP * Math.Log(DivP, 2)) + -(DivH * Math.Log(DivH, 2));

            return DatasetEntropy;
        }

        /// <summary>
        /// Calculating the information gain
        /// </summary>
        public void CalInformationGain(int FNum, int[,] HPCount, double[] informationGain, int TMails, double DSEntropy)
        {
            int l = 0;
            for (int i = 0; i < FNum; i++)
            {
                double DivHam, DivPhish, hamEntropy, phishEntropy;
                for (int j = 0; j < 1; j++)
                {
                    //calculating the entropy for each feature
                    DivHam = (HPCount[i, j] + HPCount[i, j + 1]) / (double)TMails;
                    hamEntropy = CalculateEntropy(HPCount[i, j + 1], HPCount[i, j]);//calculating the ham entropy for this feature
                    DivPhish = (HPCount[i, j + 2] + HPCount[i, j + 3]) / (double)TMails;
                    phishEntropy = CalculateEntropy(HPCount[i, j + 3], HPCount[i, j + 2]);//calculating the phishing entropy for this feature

                    informationGain[l++] = DSEntropy - (DivHam * hamEntropy) - (DivPhish * phishEntropy);
                }
            }
        }
        
        /// <summary>
        /// Calculate Entropy
        /// </summary>
        public double CalculateEntropy(int TotalPEmail, int TotalHEmail)
        {
            int TotalMails; Double DivH, DivP, Entropy;
            TotalMails = TotalPEmail + TotalHEmail;
            DivH = (double)TotalHEmail / (double)TotalMails;
            DivP = (double)TotalPEmail / (double)TotalMails;
            Entropy = -(DivH * Math.Log(DivH, 2)) + -(DivP * Math.Log(DivP, 2));
            Entropy = Entropy.Equals(double.NaN) ? 0.0 : Entropy;
            return Entropy;
        }

        /// <summary>
        /// Calculating the total number of domain names in a url
        /// </summary>
        public int DomainNameDotCount(string Email)
        {
            //bool retVal = false;
            string[] domSplit = { }; int dotCount = 0;
            int Count = CountWordOccurences(Email, "href"); int m = 0;
            //getting the domain name in this href (i.e in the url)
            while (Count > 0)
            {
                int idx = Email.IndexOf("href");
                if (idx != -1)
                {
                    idx = Email.IndexOf("href", ++m);
                    if (idx != -1)
                    {
                        int idx2 = Email.IndexOf(">", idx);
                        if (idx2 != -1)
                        {
                            string url = Email.Substring(idx, idx2 - idx);

                            int idx3 = url.IndexOf("http");
                            if (idx3 != -1)
                            {
                                int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                                if (idx4 != -1)
                                {
                                    //string domainName = url.Substring(idx3, idx4 - idx3); // get the domain name
                                    string domainName = getDomainNameFromURL(url); // get the domain name
                                    domSplit = domainName.Split('.'); //split the domain name to count the number of dots in the url
                                    dotCount += domSplit.Length - 1;
                                   
                                }
                            }
                        }
                    }
                }
                m = idx;
                Count--;
            }
            return dotCount;
        }

        /// <summary>
        /// This method gets the index of the Nth occurence of a character in the string
        /// </summary>
        public int IndexOfNth(string str, char c, int n, int StartIndex)
        {
            int s = 0;

            for (int i = 0; i < n; i++)
            {
                s = str.IndexOf(c, StartIndex + 1);

                if (s == -1) break;

                StartIndex = s;
            }

            return s;
        }

        /// <summary>
        /// Get domain name from url
        /// </summary>
        public string getDomainNameFromURL(string url)
        {
            int idDomain2 = -1, idDomain1;
            string urlDomain = ""; string domainName = ""; Uri uri;
            url = url.Trim(); int index; int idx2 = 0;
            idDomain1 = url.IndexOf("http"); //get the index of the string "http" in the url
            if (idDomain1 != -1) //if the string exist then proceed (if the string does not exist then index will return "-1")
                idDomain2 = url.IndexOf(">", idDomain1);
            if (idDomain2 != -1)
            {
                urlDomain = url.Substring(idDomain1, idDomain2 - idDomain1 + 1); //get the substring within the specified index
                index = (urlDomain.IndexOf("\">") == -1) ? urlDomain.IndexOf(">") : urlDomain.IndexOf("\">"); //get the index of "">" or ">" in url 
                urlDomain = urlDomain.Remove(index);//remove all the other characters starting from indexOf(">") bcos they are useless(or they are not part of the domain)

                if (Uri.TryCreate(urlDomain, UriKind.Absolute, out uri))
                {
                    uri = new Uri(urlDomain);
                    domainName = uri.Host; //get the domain name from the url
                }
                else //if the url cannot be parsed by URi, then strip domain name from url manually
                {
                    int idx1 = url.IndexOf("http");
                    if (idx1 != -1)
                    {
                        idx2 = (url.IndexOf("\"", idx1) == -1) ? url.IndexOf("<", idx1) : url.IndexOf("\"", idx1); //get the domain name either within a tag(<a href="www.yah.com") or in between a tag(<font>www.yah.com</font>)
                        domainName = url.Substring(idx1, idx2 - idx1).Split(new string[] { "//" }, StringSplitOptions.None).Last();
                    }

                }
            }

            return domainName;
        }

        /// <summary>
        /// Processing the spam assassin feature - i.e. assigning the classification result of SpamAssassin to each email
        /// Note that, each emails has already been classified by spamAssassin. We are only assigning the classification result here
        /// </summary>
        public int spamassassin(string mailURLs, Dictionary<string, int> mails, int i)
        {
            string[] split = mailURLs.Split('\\');
            int retVal = mails.ElementAt(i).Value;
      
            return retVal;
        }

        /// <summary>
        /// This method checks emails for the presence of URLs whose target domain name is different from the domain name displayed in the url text
        /// </summary>
        public bool urlTarDifLinks(string Email)
        {
            bool retVal = false;
            string domainName2 = "";
            int Count = CountWordOccurences(Email, "href=");

            //getting the domain name in this href (i.e in the url)
            int idx = Email.IndexOf("href="); int m = 0;
            while (Count > 0)
            {
                if (idx != -1)
                {
                    idx = Email.IndexOf("href=", ++m);
                    int idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName1 = getDomainNameFromURL(url); // get the domain name
                            int idx5 = url.IndexOf(">"); int idx6 = url.IndexOf("</a>");
                            string reslt = url.Substring(idx5, idx6 - idx5);
                            int idx7 = reslt.IndexOf("http");
                            int idx8 = IndexOfNth(reslt, '/', 3, idx7);
                            if (idx8 != -1)
                            {
                                string domainName = getDomainNameFromURL(reslt); // get the domain name

                                if (!domainName1.Equals(domainName2) && domainName2.Split('.').Length == 3) //comparing the two domain names in the present url
                                {
                                    retVal = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                Count--;
                m = idx;
            }

            return retVal;
        }

        /// <summary>
        /// using regualar expression to count the occurence of certain words in an email
        /// </summary>
        public int CountWordOccurences(string Email, string pattern)
        {
            int TotalOccurence = Regex.Matches(Email, pattern).Count;

            return TotalOccurence;
        }

       /***
        public bool checkFileForDuplicate(string feature)
        {
            bool retVal = false;
            string inputString = File.ReadAllText(filenameTrainDS);
            string[] split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            if (split.Contains(feature))
                retVal = true;

            return retVal;
        }
        ****/

        /// <summary>
        /// This method checks each email to see whether the domain name in each urls contain IP address
        /// </summary>
        public bool urlDetection(string Email)
        {
            bool retVal = false;
            string IPAddressPattern = "^([01]?\\d\\d?|2[0-4]\\d|25[0-5])\\." + "([01]?\\d\\d?|2[0-4]\\d|25[0-5])\\." +
                                      "([01]?\\d\\d?|2[0-4]\\d|25[0-5])\\." + "([01]?\\d\\d?|2[0-4]\\d|25[0-5])$";

            int Count = CountWordOccurences(Email, "href=");

            //getting the domain name in this url (i.e in the href)
            int idx = Email.IndexOf("href="); int m = 0;
            while (Count > 0)
            {
                if (idx != -1)
                {
                    idx = Email.IndexOf("href=", ++m);
                    int idx2 = Email.IndexOf(">", idx);
                    string url = Email.Substring(idx, idx2 - idx);
                    int idx3 = url.IndexOf("http");
                    int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                    if (idx4 != -1)
                    {
                        string domainName = getDomainNameFromURL(url); // get the domain name
                        if (Regex.IsMatch(domainName, IPAddressPattern)) // check whether the domain name is an ip address
                        {
                            retVal = true;
                            break;
                        }
                    }
                }
                Count--;
                m = idx;
            }
            return retVal;
        }

        /// <summary>
        /// Checking emails for the presence of image tags that appears between anchor tags
        /// </summary>
        public bool imgLinkDetection(string Email)
        {
            bool retVal = false;

            int idx = Email.IndexOf("<img");

            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx); //get the index of the first occurence of </a> tag after idx
                string str = Email.Substring(idx, idx2 - idx + 4); //get the substring between the '<img' tag and '<a/>' tag
                string str2 = str.Substring(str.Length - 4, str.Length - (str.Length - 4)); //checking whether the last four characters of str is equal to "</a>" - this will always be the case if the <img tag occurs between an anchor tag(i.e btw <a> & </a>)
                if (str2.Equals("</a>"))
                    retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// This method checks for the presence of hexadecimal values in url. 
        /// Hexadecimal values such as '%' and '@' are used for redirecting users to a different website
        /// </summary>
        public bool HexAt_URLDetection(string Email)
        {
            bool retVal = false;

            //getting the domain name in this href (i.e in the url)
            int idx = Email.IndexOf("href=");

            if (idx != -1)
            {
                int idx2 = Email.IndexOf(">", idx);
                string url = Email.Substring(idx, idx2 - idx);
                int idx3 = url.IndexOf("http");
                int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
               
                string domainName = getDomainNameFromURL(url); // get the domain name
                if (domainName.Split('%').Length > 0 || domainName.Split('@').Length > 0)
                    retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// This method gets the text of each URL in each email and checks for the presence of this four words: 'click', 'login', 'update' or 'here'
        /// Also, this method, checks the url text of all the urls(that links to the modal domain) to confirm whether they contain the words indicated above
        /// Futhermore, this method checks the number of domains that is linked to, in each email
        /// Note that this method is performing three different primary tasks
        /// </summary>
        public bool getURLText(string Email, int[,] vector, string Feature, int i, int j, out int numOfDomain)
        {
            bool retVal = false; numOfDomain = 0;

            int Count = CountWordOccurences(Email, "href="); //counting the number of times "href=" occured in the mail; i.e, counting the numba of urls in the email
            int Count2 = Count;
            string[] domainNames = new string[Count2];
            int[] sum = new int[Count];

            int idx = Email.IndexOf("href=");
            int m = 0, ctr = 0;
            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx);
                while (Count > 0)
                {
                    idx = Email.IndexOf("href=", ++m);
                    idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            domainNames[ctr++] = getDomainNameFromURL(url); //saving the domain names to get the Modal domain

                            int idx5 = url.IndexOf(">"); int idx6 = url.IndexOf("</a>");
                            string reslt = url.Substring(idx5, idx6 - idx5);
                            int idx7 = reslt.IndexOf("http");
                            int idx8 = IndexOfNth(reslt, '/', 3, idx7);
                            if (idx8 == -1)
                            {
                                reslt = ProcessMails(reslt); //strip the text part of the url from erronoeus characters
                                if (checkURLBagWord(reslt, Feature))//checking whether the text part of the url contains either of 'click', 'login', 'update' or 'here'
                                {
                                    vector[i, j] = 1;

                                }
                            }
                        }
                    }

                    Count--;
                    m = idx;
                }

                IEnumerable<string> distint = domainNames.Distinct().Where(x => !string.IsNullOrEmpty(x)); //select the distinct urls in d email, excluding null values
                int[] add = new int[distint.Count()];
                if (Feature == "<a>" && (Count2 == 1 || Count2 == 2) && compareURLWithModalDomain(Email, "")) // check the url text of all the urls(that links to the modal domain) to confirm whether they contain these words: "link","click","here"
                {
                    retVal = true;
                }
                if (Feature == "<a>" && Count2 > 2) //check for "link", "click" and "here" in d modal domain (i.e the domain name that has d total numba of occurence in d email)
                {
                    Array.Sort(domainNames);
                    int b = 0;
                    foreach (string w in distint)
                    {
                        int cnt = 0;
                        //counting the total number of times each domain name occurs in the email
                        for (int a = 0; a < domainNames.Length; a++)
                        {
                            if (w.Equals(domainNames[a]))
                                cnt++;
                        }
                        add[b++] = cnt;
                    }
                    int max = add.Max(); //get the highest value in the array
                    if (!add.Contains(max)) //ensuring that the values in the array are not equal (bcos if they are equal, then the modal domain cannot be determined)
                    {
                        int index = Array.IndexOf(add, max);
                        string ModalDomain = distint.ToArray()[index]; //get the domain name that has the highest number of occurence
                        if (compareURLWithModalDomain(Email, ModalDomain)) // check the url text of all the urls(that links to the modal domain) to confirm whether they contain these words: "link","click","here"
                        {
                            retVal = true;
                        }
                    }
                    else if (compareURLWithModalDomain(Email, "")) //if there is no modal domain, just check the email for the occurnce of 'link', 'click', 'here'
                        retVal = true;


                }
                else if (Feature == "linkedToDomain") //checking for the number of domains linked to in the email
                {
                    numOfDomain = distint.Count(); //assigning the total number of domains in each email
                }
            }
            return retVal;
        }

        /// <summary>
        /// This method remove erroneous characters from email
        /// </summary>
        public string[] stripChar()
        {
            string[] strpChr = new string[] { "\n", "\r", ";", "&nbsp", "<BR>", "<br>" };

            return strpChr;
        }

        /// <summary>
        /// Process email by removing any extra whitespace from email
        /// </summary>
        public string ProcessMails(string inputString)
        {
            // Convert our input to lowercase
            inputString = inputString.ToLower();
            string[] stripChars = stripChar();

            foreach (string character in stripChars)
                inputString = inputString.Replace(character, " ");

            inputString = Regex.Replace(inputString, @"\s+", " "); //Remove any extra whitespace from string
            return inputString;

        }

        /// <summary>
        /// This method performs comparism between the text part of a url and the identified ModalDomain
        /// This method is called by 'getURLText'
        /// </summary>
        public bool compareURLWithModalDomain(string Email, string ModalDomain)
        {
            bool retVal = false;
            int count = CountWordOccurences(Email, "href=");
            string[] domainNames = new string[count];
            int idx = Email.IndexOf("href=");

            int m = 0;
            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx);
                while (count > 0)
                {
                    idx = Email.IndexOf("href=", ++m);
                    idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName1 = getDomainNameFromURL(url); //get the domain name

                            int idx5 = url.IndexOf(">"); int idx6 = url.IndexOf("</a>");
                            string reslt = url.Substring(idx5 + 1, idx6 - idx5 - 1);
                            if (reslt.Length > 0)
                            {
                                string Feature = "link click here";
                                reslt = ProcessMails(reslt); //strip the text part of the url from erronoeus characters
                                if ((count == 1 || count == 2) && domainName1.Split('.').Length > 2 && checkURLBagWordsModalDomain(reslt, Feature))
                                {
                                    retVal = true;
                                    break;
                                }
                                else if (!domainName1.Equals(ModalDomain) && checkURLBagWordsModalDomain(reslt, Feature))//checking whether the text part of the url contains either of 'click', 'link','here'
                                {
                                    retVal = true;
                                    break;
                                }
                            }
                        }
                    }

                    count--;
                    m = idx;
                }
            }

            return retVal;
        }

        /// <summary>
        /// This method performs checks whether the url text contains 'click', 'link', or 'here'
        /// This method is called by 'getURLText'
        /// </summary>
        public bool checkURLBagWordsModalDomain(string urlText, string Feature)
        {
            bool retVal = false;
            List<string> feat = Feature.Split(' ').ToList();

            foreach (string s in feat)
            {
                if (urlText.Contains(s)) //checking whether the text part of the url contains either of 'click', 'link', or 'here'
                {
                    retVal = true;
                    break;
                }
            }
            return retVal;
        }

        /// <summary>
        /// This method checks whether an email contains the word "javascript"
        /// This word usually indicates that an email contains javascript code
        /// </summary>
        public bool containsJavaScript(string Email)
        {
            bool retVal = false;
            int idx = Email.ToLower().IndexOf("javascript"); //checking whether the email contains javascript codes in it

            if (idx != -1)
            {
                retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// This method counts the number of links in each email
        /// </summary>
        public int countNumberOfLinks(string email)
        {
            int count = CountWordOccurences(email, "href="); //counting the number of times "href=" occured in the mail; i.e, counting the numba of urls in the email
            return count;
        }

        /// <summary>
        /// checking  domain name mismatch between the sender's address and the domain names in the email address
        /// </summary>
        public bool checkFrom_Body_DomainNameMismatch(string mailURL, string email)
        {
            int idDomain1 = new int(), idDomain2 = new int(), idDomain3 = new int(), idDomain4 = new int(), idDomain5 = new int();
            string senderDomainName2 = "";
            string senderDomainName = getSenderAdress(email).Split('@').Last(); //getting the domain part of the sender's address for comparism with other domain names in the email

            if (senderDomainName.Equals(""))
            {
                idDomain1 = email.IndexOf("from:");
                if (idDomain1 != -1)
                {
                    idDomain2 = email.IndexOf("<", idDomain1);
                    idDomain3 = email.IndexOf(">", idDomain1);
                }
                if (idDomain2 != -1 && idDomain3 != -1)
                {
                    if ((idDomain3 - idDomain2) < 0)
                    {
                        idDomain4 = email.IndexOf("@", idDomain1);
                        if (idDomain4 != -1)
                            idDomain5 = email.IndexOf(" ", idDomain4);
                        senderDomainName2 = email.Substring(idDomain4 + 1, (idDomain5 - idDomain4 + 1) - 2); //getting the sender's address if the email's sender's address is not in the right format
                    }
                    else
                        senderDomainName2 = email.Substring(idDomain2 + 1, (idDomain3 - idDomain2 + 1) - 2); //getting the sender's address if the email's sender's address is not in the right format
                }
            }

            senderDomainName = (senderDomainName.Length > 0) ? senderDomainName : senderDomainName2.Split('@').Last();
            bool retVal = false;
            string[] domainNames = getDomainNames(email);
            foreach (string s in domainNames)
            {
                if (!s.Contains(senderDomainName))
                {
                    retVal = true;
                    break;
                }
            }

            return retVal;
        }

        /// <summary>
        /// This method is used to get the sender's address from an email
        /// </summary>
        public string getSenderAdress(string email)
        {
            Regex toline = new Regex(@"(?im-:^from\s*:\s*(?<from>.*)$)");
            string to = toline.Match(email).Groups["from"].Value; string address2 = "";

            int from = 0;
            int pos = 0;
            int found;
            string test = "", address = "";

            while (from < to.Length)
            {
                found = (found = to.IndexOf(',', from)) > 0 ? found : to.Length;
                from = found + 1;
                test = to.Substring(pos, found - pos);

                try
                {
                    System.Net.Mail.MailAddress addy = new System.Net.Mail.MailAddress(test.Trim());
                    address = addy.Address;
                    pos = found + 1;
                }
                catch (FormatException)
                {
                }
            }

            if (address.Equals(""))
            {
                int idDomain1 = test.IndexOf("<");
                int idDomain2 = test.IndexOf(">");
                if (idDomain1 != -1 && idDomain2 != -1)
                {
                    address2 = test.Trim().Substring(idDomain1 + 1, (idDomain2 - idDomain1 + 1) - 2); //getting the sender's address if the email's sender's address is not in the right format
                }
                address = (address.Length > 0) ? address : address2;

            }
            return address;
        }

        /// <summary>
        /// This method extract the domain names from each email for Modal Domain name processing
        /// </summary>
        public string[] getDomainNames(string Email)
        {
            IEnumerable<string> distint = new string[] { };

            int Count = CountWordOccurences(Email, "href="); //counting the number of times "href=" occured in the mail; i.e, counting the numba of urls in the email
            int Count2 = Count;
            string[] domainNames = new string[Count];
            int[] sum = new int[Count];

            int idx = Email.IndexOf("href=");
            int m = 0; int ctr = 0;
            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx);
                while (Count > 0)
                {
                    idx = Email.IndexOf("href=", ++m);
                    idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName1 = getDomainNameFromURL(url); // get the domain name
                            domainNames[ctr++] = domainName1; //saving the domain names to get the Modal domain
                        }
                    }

                    Count--;
                    m = idx;
                }

                distint = domainNames.Distinct().Where(x => !string.IsNullOrEmpty(x)); //select the distinct urls in d email
            }

            return distint.ToArray();
        }

        /// <summary>
        /// This method is used to count the number of times each word feature occur in each email
        /// Note that, the count for each word feature is normalized before the result is returned
        /// </summary>
        public int checkKeyWords(string url, string feature, string email)
        {
            double normalizedEmailVal;
            List<string> word = new List<string>(); int ctr = 0; double sqrt;

            if (feature == "update")
            {
                word.Add("update"); word.Add("confirm");
            }
            else if (feature == "user")
            {
                word.Add("user"); word.Add("customer"); word.Add("client");
            }
            else if (feature == "suspend")
            {
                word.Add("suspend"); word.Add("restrict"); word.Add("hold");
            }
            else if (feature == "verify")
            {
                word.Add("verify"); word.Add("account");
            }
            else if (feature == "login")
            {
                word.Add("login"); word.Add("username"); word.Add("password");
            }
            else if (feature == "ssn")
            {
                word.Add("ssn"); word.Add("social security");
            }
            else if (feature == "bankNames")
            {
                word.Add("papal"); word.Add("visa"); word.Add("ebay");
            }

            foreach (string w in word)
            {
                ctr += CountWordOccurences(email.ToLower(), w.ToLower()); //counting the total number of times each of the words occured in the email
            }

            sqrt = Math.Sqrt(ctr);
            normalizedEmailVal = (ctr.Equals(0)) ? 0 : (double)ctr / sqrt;

            return (int)Math.Round(normalizedEmailVal);
        }

        /// <summary>
        /// This method checks url for the presence of 'click', 'login', 'update' or 'here'
        /// Note that, this method is used by 'getURLText'
        /// </summary>
        public bool checkURLBagWord(string domainName, string Feature)
        {
            bool retVal = false;
            if (domainName.Contains(Feature)) //checking whether the text part of the url contains either of 'click', 'login', 'update' or 'here'
                retVal = true;

            return retVal;
        }

        /// <summary>
        /// Calculate information gain for each feature
        /// </summary>
        public Dictionary<string, double> Feature_InfoGain(string[] feature, double[] infoGain, int NumOfFeat)
        {
            Dictionary<string, double> feat_InfoGain = new Dictionary<string, double>();
            for (int i = 0; i < NumOfFeat; i++)
            {
                feat_InfoGain[feature[i]] = infoGain[i];
            }

            //Create a dictionary sorted by value (i.e. how many times a word occurs)
            Dictionary<string, double> sortedDict = (from entry in feat_InfoGain orderby entry.Value descending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);

            return sortedDict;
        }

        /// <summary>
        /// Extract the file name (label) of each email
        /// </summary>
        public string[] getFileNames(string[] mailURLs)
        {
            string[] testMailFileNames = new string[mailURLs.Length];
            for (int j = 0; j < mailURLs.Length; j++)
            {
                testMailFileNames[j] = mailURLs[j].Split('\\').Last(); //save the file names for all the test mails
            }

            return testMailFileNames;
        }

        //this method counts the number of close-ended anchor tags (<a></a>) in emails, and the number of tags that are not anchors (e.g <p> or <br>)
        //this feature was suggested by paper titled: "Classifying Spam Emails using Text and Readability Features" 
        public int CountAnchorTags(string Email)
        {
            int count1 = pds.CountWordOccurences(Email, "<a>");
            int count2 = pds.CountWordOccurences(Email, "</a>");
            int result = 0;

            if (count1 == 0 || count2 == 0)
            {
                result = 0; //return 0 if the number of open or close anchor tags is 0; implying that it is not in pairs
                return result;
            }
            else
            {
                if (count1 == count2)
                    result = count1; //return either count1 or count2, because they are equal
                else if (count1 > count2)
                    result = count2; //return the lesser count, because the tags should be in pairs. Additional tags does not have pairs, hence should not counted.
                else if (count2 > count1)
                    result = count1;
            }

            //int sentenceCount = countSentences(Email); //count the number of sentences in email
            //result = result / sentenceCount;  //normalize by the number of sentence in email - as indicated in paper

            return result;
        }

        //this method evokes the MSWord Document objects
        public static Microsoft.Office.Interop.Word.Document CallMSWordDocumentObject()
        {
            object oMissing = System.Reflection.Missing.Value;
            var oWord = new Microsoft.Office.Interop.Word.Application(); //call microsoft office to count sentences in email
            oWord.Visible = false;
            var oDoc = oWord.Documents.Add(ref oMissing, ref oMissing, ref oMissing, ref oMissing);

            return oDoc;
        }

        //load each email in the ms word document object
        public Microsoft.Office.Interop.Word.Document LoadEmail(string Email, Microsoft.Office.Interop.Word.Document msWordObject)
        {
            msWordObject.Content.Text = Email;
            return msWordObject;
        }

        //count the total number of sentences containing word
        public int SentenceFrequency(string email, string word)
        {
            int count = 0; 
            //int sentenceCount = msWordObjectEmail.Sentences.Count; //count number of sentence
            string[] sentenceSplit = SplitToSentence(email); //split email to sentence
            int sentenceCount = sentenceSplit.Count();
            for (int i = 0; i < sentenceCount; i++)
            {
                //int j = pds.CountWordOccurences(msWordObjectEmail.Sentences[i].Text, word);
                int j = pds.CountWordOccurences(sentenceSplit[i], word);
                count += j > 0 ? 1 : 0; //add 1 if word is present in sentence
            }

            return count;
        }

        //count the total number of document or email containing word
        public int DocumentFrequency(Dictionary<string, int> emailDict, string word)
        {
            int count = 0;
            int emailCount = emailDict.Count; //count number of sentence
            //string email = ProcessMails(File.ReadAllText(emailDict.ElementAt(i).Key)); //extract and process email
            //string[] cleanedEmail = cleanEmail(email.Split(' '));
            //string combinedCleanedEmail = string.Join(" ", cleanedEmail); // convert List of words to single string for processing

            for (int i = 0; i < emailCount; i++)
            {
                string email = ProcessMails(File.ReadAllText(emailDict.ElementAt(i).Key)); //extract and process email
                int j = pds.CountWordOccurences(email, word); //check the number of emails that contains the current word
                count += j > 0 ? 1 : 0; //add 1 if word is present in sentence
            }

            return count;
        }

        //this method counts the number of tags that are not anchors (e.g <p> or <br>)
        //this feature was gotten from paper titled: "Classifying Spam Emails using Text and Readability Features" 
        public int CountNonAnchorTags(string Email)
        {

            List<string> htmlTags = HTMLTags();
            int count = 0;

            foreach (string tag in htmlTags)
            {
                count += pds.CountWordOccurences(Email, tag);
            }

            //count = count / countSentences(Email); //normalize count by the total number of sentences in email

            return count;
        }

        //count the total number of tags in email
        //this feature was suggested by paper titled: "Classifying Spam Emails using Text and Readability Features" 
        public int CountTotalTags(string Email)
        {
            return (CountAnchorTags(Email) + CountNonAnchorTags(Email));
        }

        //list of HTML tags to transverse
        public List<string> HTMLTags()
        {
            List<string> htmlTags = new List<string>() 
            {
                "<p>","</br>","<br>","<title>","<hr>","h1","h2","h3","h4","h5","h6","<acronym>","<!--","-->","<acronym>","<abbr>","<address>","<b>","<bdi>","<bdo>","<center>",
                "<blockquote>","<big>","<cite>","<code>","<del>","<em>","<font>","<i>","<q>","<pre>","<s>","<samp>","<small>","<strike>","<strong>","<sub>","<sup>","<time>",
                "<u>","<wbr>","<form>","<input>","<textarea>","<button>","<select>","<optgroup>","<option>","<label>","<fieldset>","<legend>","<datalist>","<keygen>","<output>",
                "<frame>","<frameset>","<noframes>","<iframe>","<img>","<map>","<area>","<canvas>","<figcaption>","<figure>","<link>","<nav>","<ul>","<ol>","<li>","<dir>","<dl>",
                "<menu>","<menuitem>","<dt>","<dd>","<table>","<style>","<div>","<span>","<section>","<script>","<noscript>","<applet>","<embed>","<object>","<param>"
            };

            return htmlTags;
        }

        //count the total number of alphanumeric words in email
        public int CountAlphanumericWords(List<string> mailSplit)
        {
            string str = "";
            int count = 0;
            foreach (string word in mailSplit)
            {
                Regex r = new Regex("[a-zA-Z]");

                if (r.IsMatch(word))
                {
                    Regex r1 = new Regex("[0-9]");
                    if (r1.IsMatch(word))
                        count++;
                }
            }

            return count;
        }

        //split email into sentences
        public string[] SplitToSentence(string email)
        {
            string emailProcessed = ProcessMails(email);
            string[] sentences = Regex.Split(emailProcessed, @"(?<=[\.!\?])\s+");
            //string[] ssentences = Regex.Split(emailProcessed, @"(?<=[.!?])\s+(?=[A-Z])");

            return sentences;
        }

        //count the number of stop words in each email
        public int CountStopWords(Dictionary<string,int> mails, int m)
        {
            //extract the vector values for the additional features
            string TokenPattern = @"([a-zA-Z]\w+)\W*";
            Regex re = new Regex(TokenPattern, RegexOptions.Compiled);
            List<string> combinedTokens = new List<string>();
            List<string> stopList = StopWords(); //get all the spam words from function SpamWords()

            int TotalOccurence = 0;
            string read = File.ReadAllText(mails.ElementAt(m).Key);
            string[] tokenized = TokenizeEmail(read);
            string combinedString = string.Join(" ", tokenized); // convert List of words to single string for processing
            foreach (string spam in stopList)
            {
                string[] split = spam.Split(' ');
                var spamPattern = new Regex("(" + @"\b" + string.Join(" ", split) + @"\b" + ")"); //form a RegEx pattern to match substring 
                
                TotalOccurence += Regex.Matches(combinedString, spamPattern.ToString()).Count; //count the number of occurence of each word or sentence in SpamList
            }

            return TotalOccurence;
        }

        //calculate term frequency for each email as specified in paper: "Classifying Spam Emails using Text and Readability Features"
        public Dictionary<string, double> TermFrequency(SortedDictionary<string, int> tokenizedMail_count)
        {
            Dictionary<string, int> words = new Dictionary<string,int>();
            Dictionary<string, double> termFrequency = new Dictionary<string,double>();

            foreach (KeyValuePair<string, int> term in tokenizedMail_count)
            {
                termFrequency[term.Key] = term.Value > 0 ? (1 + Math.Log(term.Value)) : 0; //calculate term frequency for each word if word count is greater than zero
            }

            return termFrequency;
        }

        //calculate TF_ISF for each email as specified in paper: "Classifying Spam Emails using Text and Readability Features"
        public double TermFrequency_InverseSentenceFrequency(SortedDictionary<string, int> tokenizedMail_count, string mailURL)
        {
            string[] cleanedEmail = cleanEmail(tokenizedMail_count.Keys.ToList().ToArray()); //remove erroneous words from email
            Dictionary<string, double> inverseDocumentFrequency = new Dictionary<string, double>();
            
            //remove words that occurs just once in the email
            foreach (var s in tokenizedMail_count.Where(kv => kv.Value == 1).ToList())
            {
                tokenizedMail_count.Remove(s.Key);
            }
            int totalEmail = tokenizedMail_count.Count();

            //remove erroneous words from email dictionary
            for (int i = 0; i < tokenizedMail_count.Count(); i++)
            {
                if (!cleanedEmail.Contains(tokenizedMail_count.ElementAt(i).Key))
                {
                    tokenizedMail_count.Remove(tokenizedMail_count.ElementAt(i).Key);
                    i = 0; //start from the beginning again if element is removed; this is to ensure that no element is omitted during search
                }
            }

            Dictionary<string, double> termFreq = TermFrequency(tokenizedMail_count); //get term frequency of words in email
            Dictionary<string, double> inverseSentenceFrequency = new Dictionary<string, double>();
            //string combinedString = string.Join(" ", tokenizedMail_count.Keys.ToList()); // convert List of words to single string
            string email = File.ReadAllText(mailURL); //extract and process email
            //string email = ProcessMails(File.ReadAllText(mailURL)); //extract and process email
            double TF_ISF = 0;

            foreach (KeyValuePair<string, double> term in termFreq)
            {
                //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(email, msWordObject); //load email to msword object
                //int sentenceCount = msWordObjectEmail.Sentences.Count; //count number of sentences in each email
                int sentenceCount = SplitToSentence(email).Count(); //split email and count number of occurences
                int sentenceFreq = SentenceFrequency(email, term.Key);
                inverseSentenceFrequency[term.Key] = sentenceFreq == 0 ? 0 : Math.Log((sentenceCount / sentenceFreq)); //calculate inverse sentence frequency for email
            }

            //calculate TF_IDF
            for (int i = 0; i < termFreq.Count; i++)
            {
                TF_ISF += termFreq.ElementAt(i).Value * inverseSentenceFrequency.ElementAt(i).Value; //sum TF_ISF for each term in email - this gives TF_ISF for the current email
            }

            return TF_ISF;
        }

        //calculate the TF_IDF for each email
        public double TermFrequency_InverseDocumentFrequency(Dictionary<string, int> emailDict, SortedDictionary<string, int> tokenizedMail_count)
        {
            
            string[] cleanedEmail =  cleanEmail(tokenizedMail_count.Keys.ToList().ToArray()); //remove erroneous words from email
            Dictionary<string, double> inverseDocumentFrequency = new Dictionary<string, double>();
            //int totalEmail = cleanedEmail.Count();

            //remove words that occurs just once in the email
            foreach (var s in tokenizedMail_count.Where(kv => kv.Value == 1).ToList())
            {
                tokenizedMail_count.Remove(s.Key);
            }
            int totalEmail = emailDict.Count();

            //remove erroneous words from email dictionary
            for (int i = 0; i < tokenizedMail_count.Count(); i++)
            {
                if (!cleanedEmail.Contains(tokenizedMail_count.ElementAt(i).Key))
                {
                    tokenizedMail_count.Remove(tokenizedMail_count.ElementAt(i).Key);
                }
            }

            Dictionary<string, double> termFreq = TermFrequency(tokenizedMail_count); //get term frequency of words in email

            double TF_IDF = 0;
            double[] IDF = new double[termFreq.Count];

            int j = 0;
            foreach (KeyValuePair<string, double> term in termFreq)
            {
                int docFreq = DocumentFrequency(emailDict, term.Key);
                IDF[j++] = docFreq == 0 ? 0 : Math.Log((totalEmail / docFreq)); //calculate ISF for email, assign zero if document frequency is 0
                //inverseDocumentFrequency[term.Key] = docFreq == 0 ? 0 : Math.Log((totalEmail / docFreq)); //calculate ISF for email, assign zero if document frequency is 0
                //inverseDocumentFrequency[term.Key] = Math.Log((totalEmail / DocumentFrequency(emailDict, term.Key))); //calculate inverse sentence frequency for email
            }

            //calculate TF_IDF
            for (int i = 0; i < termFreq.Count; i++)
            {
                TF_IDF += termFreq.ElementAt(i).Value * IDF[i]; //sum TF_ISF for each term in email - this gives TF_ISF for the current email
                //TF_IDF += termFreq.ElementAt(i).Value * inverseDocumentFrequency.ElementAt(i).Value; //sum TF_ISF for each term in email - this gives TF_ISF for the current email
            }

            return TF_IDF;
        }

        //count the number of syllabus in a word
        private static int SyllableCount(string word)
        {
            word = word.ToLower().Trim();
            int count = System.Text.RegularExpressions.Regex.Matches(word, "[aeiouy]+").Count;
            if ((word.EndsWith("e") || (word.EndsWith("es") || word.EndsWith("ed"))) && !word.EndsWith("le"))
                count--;

            return count;
        }

        //calculate fog index for complex words in each email, as defined in paper "Classifying Spam Emails using Text and Readability Features"
        public double FogIndex(SortedDictionary<string, int> tokenizedMail_count, string url)
        {
            //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
            int numOfWords = tokenizedMail_count.Count;
            //int sentenceCount = msWordObjectEmail.Sentences.Count;
            int sentenceCount = SplitToSentence(File.ReadAllText(url)).Count(); //split and count number of sentences
            int numComplexWords = 0;
            double fogIndex;

            //calculate the number of complex word for each word in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                int numSyllable = SyllableCount(word); //count the number of syllable in word
                numComplexWords += numSyllable >= 3 ? 1 : 0; //add 1 if word is complex (i.e. if  it contains more than 3 sylabbles), otherwise dont increment (it is a simple word)
            }

            fogIndex = 0.4 * ((numOfWords / sentenceCount) + 100 * (numComplexWords / numOfWords));

            return fogIndex;
        }

        //calculate the flesch reading score of email, as explained in paper "Classifying Spam Emails using Text and Readability Features"
        public double FleshReadingScore(SortedDictionary<string, int> tokenizedMail_count, string url)
        {
            int numOfWords = tokenizedMail_count.Count;
            //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
            //int sentenceCount = msWordObjectEmail.Sentences.Count;
            int sentenceCount = SplitToSentence(File.ReadAllText(url)).Count(); //process mail, split and count number of sentences
            int numSyllable = 0;
            double fleshRScore;

            //sum the number of sylabus for all the words in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                numSyllable += SyllableCount(word); //count the number of syllable for each word
            }

            double a = sentenceCount == 0 ? 0 : numOfWords / sentenceCount; //dont divide if sentenceCount is 0 - this is to avoid division by zero
            double b = numOfWords == 0 ? 0 : numSyllable / numOfWords;

            fleshRScore = 206.835 - (1.015 * a) - (84.6 * b);

            return fleshRScore;
        }

        //calculate the smog index for each email, as explained in paper: "Classifying Spam Emails using Text and Readability Features"
        public double SmogIndex(SortedDictionary<string, int> tokenizedMail_count, string url)
        {
            int numComplexWords = 0;
            //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
            //int sentenceCount = msWordObjectEmail.Sentences.Count;
            int sentenceCount = SplitToSentence(File.ReadAllText(url)).Count(); //process mail, split and count number of sentences
            //calculate the number of complex word for each word in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                int numSyllable = SyllableCount(word); //count the number of syllable in word
                numComplexWords += numSyllable >= 3 ? 1 : 0; //add 1 if word is complex (i.e. if  it contains more than 3 sylabbles), otherwise dont increment (it is a simple word)
            }

            double smogInd = 1.043 * Math.Sqrt(30 * (numComplexWords / sentenceCount)) + 3.1219;

            return smogInd;
        }

        //calculate the forcast index of email as explained in paper: "Classifying Spam Emails using Text and Readability Features"
        public double ForcastIndex(SortedDictionary<string, int> tokenizedMail_count)
        {
            int numSimpleWords = 0;
            int numOfWords = tokenizedMail_count.Count;
            int i = 1;
            if (numOfWords >= 150)
            {
                //calculate the number of simple word in a 150-word sample of text
                foreach (string word in tokenizedMail_count.Keys)
                {
                    int numSyllable = SyllableCount(word); //count the number of syllable in word
                    numSimpleWords += numSyllable <= 2 ? 1 : 0; //add 1 if word is simple (i.e. if  it contains 1 or 2 sylabbles), otherwise dont increment (it is a simple word)
                    i++;

                    if (i > 150)
                        break;
                }
            }

            double forcastInd = 20 - (numSimpleWords / 10);

            return forcastInd;
        }

        //calculate Flesch-Kincaid Readability Index for each email as explained in paper: "Classifying Spam Emails using Text and Readability Features"
        public double FleschKincaidReadabilityIndex(SortedDictionary<string, int> tokenizedMail_count, string url)
        {
            int numOfWords = tokenizedMail_count.Count;
            //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
            //int sentenceCount = msWordObjectEmail.Sentences.Count;
            int sentenceCount = SplitToSentence(File.ReadAllText(url)).Count(); //process mail, split and count number of sentences
            int numOfSyllable = 0;

            //sum the number of sylabus for all the words in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                numOfSyllable += SyllableCount(word); //count the number of syllable for each word
            }

            double FKRI = 0.39 * (numOfWords / sentenceCount) + 11.8 * (numOfSyllable / numOfWords) - 15.59;

            return FKRI;
        }

        //calculate fog index for simple words in each email, as defined in paper "Classifying Spam Emails using Text and Readability Features"
        public double FogIndexSimple(SortedDictionary<string, int> tokenizedMail_count, string url)
        {
            //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
            //int sentenceCount = msWordObjectEmail.Sentences.Count;
            int numOfWords = tokenizedMail_count.Count;
            int sentenceCount = SplitToSentence(File.ReadAllText(url)).Count(); //process mail, split and count number of sentences
            int numSimpleWords = 0;
            double fogIndex;

            //calculate the number of simple word for each word in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                int numSyllable = SyllableCount(word); //count the number of syllable in word
                numSimpleWords += numSyllable <= 2 ? 1 : 0; //add 1 if word is simple (i.e. if  it contains 1 or 2 sylabbles), otherwise dont increment (it is a simple word)
            }

            fogIndex = 0.4 * ((numOfWords / sentenceCount) + 100 * (numSimpleWords / numOfWords));

            return fogIndex;
        }

        //calculate fog index inverse for each email, as defined in paper "Classifying Spam Emails using Text and Readability Features"
        public double FogIndexInverse(SortedDictionary<string, int> tokenizedMail_count, string url)
        {
            //Microsoft.Office.Interop.Word.Document msWordObjectEmail = LoadEmail(File.ReadAllText(url), msWordObject); //load email to msword object
            //int sentenceCount = msWordObjectEmail.Sentences.Count;
            int numOfWords = tokenizedMail_count.Count;
            int sentenceCount = SplitToSentence(File.ReadAllText(url)).Count(); //process mail, split and count number of sentences
            int numComplexWords = 0;
            double fogIndex;

            //calculate the number of complex word for each word in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                int numSyllable = SyllableCount(word); //count the number of syllable in word
                numComplexWords += numSyllable >= 3 ? 1 : 0; //add 1 if word is complex (i.e. if  it contains more than 3 sylabbles), otherwise dont increment (it is a simple word)
            }

            fogIndex = 0.4 * ((numOfWords / sentenceCount) + 100 * (numComplexWords / numOfWords));

            return 1 / fogIndex; //return inverse of fog index
        }

        //calculate the word length of each email as explained in paper: "Classifying Spam Emails using Text and Readability Features"
        public double EmailWordLengh(SortedDictionary<string, int> tokenizedMail_count)
        {
            int numOfSyllable = 0;
            int numOfWord = tokenizedMail_count.Count;

            //sum the number of sylabus for all the words in email
            foreach (string word in tokenizedMail_count.Keys)
            {
                numOfSyllable += SyllableCount(word); //count the number of syllable for each word
            }

            double wordLength = numOfSyllable / numOfWord;

            return wordLength;
        }

        //calculate the TF_IDF for simple words in email
        public double TermFrequency_InverseDocumentFrequencySimple(Dictionary<string, int> emailDict, SortedDictionary<string, int> tokenizedMail_count)
        {
            Dictionary<string, double> inverseDocumentFrequency = new Dictionary<string, double>();
            int totalEmail = emailDict.Count;
            double TF_IDF = 0;
            Dictionary<string, double> termFreq = new Dictionary<string, double>();
            List<string> elementToRemove = new List<string>();

             //remove every simple word from mail dictionary - i.e. words with syllablus greater than 2
            foreach (string word in tokenizedMail_count.Keys)
            {
                int numOfSyllable = SyllableCount(word); //count the number of syllable in word

                if (numOfSyllable <= 2)
                    elementToRemove.Add(word); //add simple word to remove
            }

            //remove complex word from dictionary
            foreach(string word in elementToRemove)
                tokenizedMail_count.Remove(word);

            termFreq = TermFrequency(tokenizedMail_count); //calculate the document frequency for all the simple words in the email

            foreach (KeyValuePair<string, double> term in termFreq)
            {
                int docFreq = DocumentFrequency(emailDict, term.Key);
                inverseDocumentFrequency[term.Key] = docFreq == 0 ? 0 : Math.Log((totalEmail / docFreq)); //calculate IDF for email, assign zero if document frequency is 0
            }

            //calculate TF_IDF for simple words
            for (int i = 0; i < termFreq.Count; i++)
            {
                TF_IDF += termFreq.ElementAt(i).Value * inverseDocumentFrequency.ElementAt(i).Value; //sum TF_ISF for each term in email - this gives TF_ISF for the current email
            }

            return TF_IDF;
        }

        //calculate the TF_IDF for complex words in email
        public double TermFrequency_InverseDocumentFrequencyComplex(Dictionary<string, int> emailDict, SortedDictionary<string, int> tokenizedMail_count)
        {
            Dictionary<string, double> inverseDocumentFrequency = new Dictionary<string, double>();
            int totalEmail = emailDict.Count;
            double TF_IDF = 0;
            Dictionary<string, double> termFreq = new Dictionary<string, double>();
            List<string> elementToRemove = new List<string>();

            //remove every complex word from mail dictionary - i.e. words with syllablus less than 2
            //first add word to remove to list
            foreach (string word in tokenizedMail_count.Keys)
            {
                int numOfSyllable = SyllableCount(word); //count the number of syllable in word

                if (numOfSyllable > 2)
                    elementToRemove.Add(word); //add complex word to remove
            }

            //remove complex word from dictionary
            foreach (string word in elementToRemove)
                tokenizedMail_count.Remove(word); //remove word from dictionary

            termFreq = TermFrequency(tokenizedMail_count); //calculate the document frequency for all the complex words in the email

            foreach (KeyValuePair<string, double> term in termFreq)
            {
                int docFreq = DocumentFrequency(emailDict, term.Key);
                inverseDocumentFrequency[term.Key] = docFreq == 0 ? 0 : Math.Log((totalEmail / docFreq)); //calculate IDF for email, assign zero if document frequency is 0
            }

            //calculate TF_IDF for complex words
            for (int i = 0; i < termFreq.Count; i++)
            {
                TF_IDF += termFreq.ElementAt(i).Value * inverseDocumentFrequency.ElementAt(i).Value; //sum TF_ISF for each term in email - this gives TF_ISF for the current email
            }

            return TF_IDF;
        }

        /// <summary>
        /// Calculate the vector sum of each phish and ham email
        /// The vector sum is needed to calculate information gain
        /// </summary>
        public void FeatureVectorSum(int FNum, int EmailCount, int[,] vector, Dictionary<string, int> emails, int[,] HamPhishCount)
        {
            for (int i = 0; i < FNum; i++)
            {
                int sumZeroHam = 0, sumOneHam = 0, sumZeroPhish = 0, sumOnePhish = 0; int k = 0;
                for (int j = 0; j < EmailCount; j++)
                {
                    if (vector[j, i].Equals(0) && emails.ElementAt(j).Value.Equals(0))
                        sumZeroHam++;
                    if (vector[j, i].Equals(0) && emails.ElementAt(j).Value.Equals(1))
                        sumZeroPhish++;
                    if (vector[j, i] > 0 && emails.ElementAt(j).Value.Equals(0))
                        sumOneHam += vector[j, i];
                    if (vector[j, i] > 0 && emails.ElementAt(j).Value.Equals(1))
                        sumOnePhish += vector[j, i];
                }

                HamPhishCount[i, k] = sumZeroHam; //total number of zeros for ham mails - zero indicate that a feature is absent in a mail and one indicate otherwise
                HamPhishCount[i, k + 1] = sumZeroPhish; //total number of zeros for Phishing emails
                HamPhishCount[i, k + 2] = sumOneHam; //total number of ones for ham mails
                HamPhishCount[i, k + 3] = sumOnePhish; //total number of ones for Phishing mails
            }
        }

        /// <summary>
        /// This method is used to extract vector value, format the vector value and send the formatted vector value to file
        /// </summary>
        public void extractVectors(int[,] vector, Dictionary<string, int> Mail_Class, int NumofFeatures, string extractedFeaturePath, int fileNum)
        {
            string vectorSVM = "";  string formattedVector;
            extractedFeaturePath = (extractedFeaturePath.Equals("trainingDS")) ? extractedFeaturesFilePathTrainDS : extractedFeaturesFilePathTestDS;
            extractedFeaturePath = string.Format("{0}{1}.{2}", extractedFeaturePath.Split('.').First(),(fileNum + 1).ToString(), "txt");
           
            File.WriteAllText(extractedFeaturePath, string.Empty); //clearing the file
            
            //extract vectors for training data
            for (int i = 0; i < vector.GetLength(0); i++)
            {
                int index = 0;
                vectorSVM = Mail_Class.ElementAt(i).Value.Equals(0) ? "-1" : "+1"; //assigning labels to the feature vectors
                File.AppendAllText(extractedFeaturePath, vectorSVM + " ");
                for (int j = 0; j < NumofFeatures; j++)
                {
                    formattedVector = string.Format("{0}:{1}", ++index, vector[i, j]);
                    File.AppendAllText(extractedFeaturePath, formattedVector + " ");
                }

                File.AppendAllText(extractedFeaturePath, Environment.NewLine);
            }

        }
    }
}
