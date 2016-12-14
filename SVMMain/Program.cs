using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SVM;
using PDS_SVM;
using System.IO;
using ClusteringKMeans;

namespace SVMMain
{
    class Program
    {
        
        static void Main(string[] args)
        {
           
            PDSClass pds = new PDSClass();
            FireFlyAlgorithm ff = new FireFlyAlgorithm();
            ParameterSelection ps = new ParameterSelection();
            //reading the phishing emails and the test Datas
            string testFileFolderName = "TrainTestData";
            string trainTestDataPath = String.Format(Environment.CurrentDirectory + "\\{0}", testFileFolderName); //filepath for the training and test dataset
            string[] trainTestFileURL = System.IO.Directory.GetFileSystemEntries(trainTestDataPath);

            string extractedVectorsFolderName = "ExtractedFeatures";
            string extractedVectorsFilePath = String.Format(Environment.CurrentDirectory + "\\{0}", extractedVectorsFolderName);
            string[] evFileURL = System.IO.Directory.GetFileSystemEntries(extractedVectorsFilePath);
            
            string outputEvaluationFilePath = String.Format(Environment.CurrentDirectory + "\\{0}", "OutputEvaluations.txt");
            //string SVAccuracyFilePath = String.Format(Environment.CurrentDirectory + "\\{0}", "SVAccuracy.txt");

            File.WriteAllText(ps.SVAccuracyFilePath, string.Empty);
            File.WriteAllText(outputEvaluationFilePath, string.Empty); //deleting the contents of the file holding the results for new results
            File.WriteAllText(ps.filePath, string.Empty); //deleting the contents of the file holding the extracted C,Gamma and CV values
            File.WriteAllText(pds.extractedFeaturesFilePathTrainDS, string.Empty);
            File.WriteAllText(ps.filePath2, string.Empty);
            File.WriteAllText(ps.filePath3, string.Empty);

            int NumofFeatures = 17;
            double DatasetEntropy = 0.0;
            int TotalMails;
            string[] features = pds.Features(); //saving the features in the string array
                             
            int[,] HamPhishCount = new int[,] { }; //count for the total number of ham and phish emails
            
            string[] testTrainFolderFiles = new string[] { };
            string folderFiles;
           
            double[] informationGain = new double[NumofFeatures];
            Dictionary<string, double> feat_infoGain = pds.Feature_InfoGain(features, informationGain); //saving the information gain for all the individual features
            
            double classficatnAccuracySum = 0.0; //cummulative classification accuracy
            double classficatnAccuracy = 0; //classification accuracy for each iteration
            DateTime start = DateTime.Now;
            double totalPhishing = 0, totalHam = 0; int mailGrandTotal = 0;
            
            //initializing all the variables that will used for evaluating the performance of the classifier
            double FP = 0.0, FN = 0.0, TP = 0.0, TN = 0.0, P = 0.0, F_M = 0.0, sumFP = 0.0, sumF_M = 0.0, sumFN = 0.0, sumTP = 0.0, sumTN = 0.0, sumP = 0.0;
            
            int N_Fold = 10;  //number of folds
            int n_Runs = 1; //number of runs
            double[,] NormalizedVector = new double[,] { }; //normalized vector values for each features
            
            Program p = new Program();
            double avgRuns = 0.0, avgFP = 0.0, avgFN = 0.0, avgR = 0.0, avgPr = 0.0, avgFM = 0.0; //avg=> average
            double globalBest = double.MinValue;
            
            //change the boolean value appropriately to choose the task you want to perform (either vector value extraction or email classification)
            //both values should not be true. This is to reduce processing time
            bool extractVectorValues = true;
            bool Emailclassification = false;

            double C = new double();
            double Gamma = new double();

            //double[] CV = new double[2]; 
            List<double> CV1 = new List<double>(); //Save the CV accuracy, C and Gamma for comparison
            List<double> CV2 = new List<double>(); //Save the CV accuracy, C and Gamma for comparison
            
            for (int aa = 0; aa < n_Runs; aa++)
            {
                
                classficatnAccuracySum = 0.0;
                sumFP = 0.0;
                sumTP = 0.0; //Recall
                sumFN = 0.0;
                sumF_M = 0.0;
                sumP = 0.0;
                for (int a = 0; a < N_Fold; a++)
                {
                    if (extractVectorValues == true) //if the value of ExtractVectorValues is true, only extract email vector values and dont perform classification
                    {
                        n_Runs = 1; // change number of runs from its default value (i.e 10) to 1 (to avoid repeating the extraction process 10 times) since we wanna do is to extract the vector values from each emails
                        string[] trainFileURLs = new string[] { }; //urls for the train emails (i.e. the emails used for training the classifier)
                        string[] testFileURLs = new string[] { }; //urls for the test emails (i.e. the emails used for testing the classifier)
                        Dictionary<string, int> trainMail_Class = new Dictionary<string, int>(); //variable containing the emails and classes of all the training emails
                        Dictionary<string, int> testMail_Class = new Dictionary<string, int>(); //variable containing the emails and classes of all the test emails
                        string[] trainMailFileNames = new string[trainMail_Class.Count]; //the file names for all the emails in the training dataset
                        string[] testMailFileNames = new string[] { }; //the file names for all the emails in the test dataset
                        string trainMailLabel; int phishCount = 0, hamCount = 0; double phishPercentage, hamPercentage;

                        //processing the training dataset for the each fold
                        for (int i = 0; i < trainTestFileURL.Length; i++)
                        {
                            if (i.Equals(a))
                                continue; //skipping one email folder, which is to be used for testing the trained classifier (i.e. the current test dataset)
                            testTrainFolderFiles = System.IO.Directory.GetFiles(trainTestFileURL[i]); //getting the filenames of all the emails in the training dataset
                            trainFileURLs = trainFileURLs.Concat(testTrainFolderFiles).ToArray(); //get all the urls for the test emails
                            trainMailFileNames = pds.getFileNames(trainFileURLs); //get the file names for all the test mails
                            for (int j = 0; j < testTrainFolderFiles.Length; j++) //processing all the emails in the current training dataset for classification
                            {
                                trainMailLabel = trainMailFileNames[j].Substring(0, 2); //getting the label for each email, HM(for Ham Mails) and PM(for Phish Mails)
                                folderFiles = File.ReadAllText(testTrainFolderFiles[j]); //extracting the content of each email in each email folder
                                trainMail_Class[pds.ProcessMails(folderFiles)] = (trainMailLabel.Equals("PM")) ? 1 : 0; //processing each email and assigning label to the emails based on the folders each emails come from.
                                if (trainMail_Class.ElementAt(j).Value == 1)
                                    phishCount++; //counting the total number of ham and phishing to get their percentage
                                else
                                    hamCount++;
                            }
                        }

                        //processing the test dataset for each fold
                        for (int i = a; i < a + 1; i++)
                        {
                            testTrainFolderFiles = System.IO.Directory.GetFiles(trainTestFileURL[i]);
                            testFileURLs = testFileURLs.Concat(testTrainFolderFiles).ToArray();
                            testMailFileNames = pds.getFileNames(testFileURLs);
                            for (int j = 0; j < testTrainFolderFiles.Length; j++)
                            {
                                trainMailLabel = testMailFileNames[j].Substring(0, 2);
                                folderFiles = File.ReadAllText(testTrainFolderFiles[j]);
                                testMail_Class[pds.ProcessMails(folderFiles)] = (trainMailLabel.Equals("PM")) ? 1 : 0; //processing each email and assigning label to the emails based on the folders each emails come from.
                                if (testMail_Class.ElementAt(j).Value == 1)
                                    phishCount++;
                                else
                                    hamCount++;
                            }
                        }

                        //calculating the percentage of phishing and ham email in the dataset
                        phishPercentage = (double)phishCount / (double)(trainMail_Class.Count + testMail_Class.Count);
                        hamPercentage = (double)hamCount / (double)(trainMail_Class.Count + testMail_Class.Count);
                        mailGrandTotal = phishCount + hamCount;
                        totalHam = hamCount; totalPhishing = phishCount;

                        //Information Gain
                        TotalMails = trainMail_Class.Count;
                        int[,] vector = new int[TotalMails, NumofFeatures];
                        pds.processVector(vector, trainMail_Class, features, trainFileURLs, NumofFeatures); //extracting the vector values of all the features
                        HamPhishCount = new int[NumofFeatures, 4];
                        pds.FeatureVectorSum(NumofFeatures, TotalMails, vector, trainMail_Class, HamPhishCount); // calculating the total number of zeros and ones for both phishing and ham emails
                        DatasetEntropy = pds.Entropy(trainMail_Class); //calculating the entropy for the entire dataset
                        pds.CalInformationGain(NumofFeatures, HamPhishCount, informationGain, TotalMails, DatasetEntropy);//calculating information gain for each feature
                        feat_infoGain = pds.Feature_InfoGain(features, informationGain); //assisgning the calculated information gain to each feature

                        //process vector for training Dataset
                        int NumofFeatures2 = NumofFeatures - 8;
                        string[] newFeatures = new string[NumofFeatures2];
                        for (int i = 0; i < NumofFeatures2; i++)
                        {
                            newFeatures[i] = feat_infoGain.ElementAt(i).Key; //copying the best 8 features with the highest information gain
                        }

                        TotalMails = trainMail_Class.Count;
                        vector = new int[TotalMails, NumofFeatures2];
                        pds.processVector(vector, trainMail_Class, newFeatures, trainFileURLs, NumofFeatures2);
                        NormalizedVector = ff.Normalize(vector); //normalize the all vector values for train data

                        //extract vectors of the training data
                        pds.extractVectors(vector, trainMail_Class, NumofFeatures2, "trainingDS", a);

                        //process vector for testing Dataset
                        TotalMails = testMail_Class.Count;
                        vector = new int[TotalMails, NumofFeatures2];
                        pds.processVector(vector, testMail_Class, newFeatures, testFileURLs, NumofFeatures2);
                        NormalizedVector = ff.Normalize(vector); //normalize the all vector values for test data

                        //extract vectors of the test data
                        pds.extractVectors(vector, testMail_Class, NumofFeatures2, "testDS", a);

                    }

                    if (Emailclassification == true) //if the value of EmailClassification is true, perform email classification and dont extract emails
                    {
                        //SVM Classfication begins here
                        //First, read in the training and test data.
                        Problem train = Problem.Read(string.Format("ExtractedFeaturesTrain{0}.{1}", (a + 1).ToString(), "txt"));
                        Problem test = Problem.Read(string.Format("ExtractedFeaturesTest{0}.{1}", (a + 1).ToString(), "txt"));

                        //scalling the data
                        GaussianTransform gt = GaussianTransform.Compute(train);
                        Problem trainScaledProblem = gt.Scale(train);
                        Problem testScaledProblem = gt.Scale(test);

                        //For this example (and indeed, many scenarios), the default parameters will suffice.
                        Parameter parameters = new Parameter();
                        //double C = new double();
                        //double Gamma = new double();

                        Console.WriteLine("\nClassification Number {0} Step: {1}...............................\n", a + 1, aa + 1);

                        //This will do a grid optimization to find the best parameters and store them in C and Gamma, outputting the entire
                        //search to params.txt.
                        /*
                         if (a == 0)
                         {
                             ParameterSelection.Grid(trainScaledProblem, parameters, "params.txt", out C, out Gamma);
                             CV1.Add(ParameterSelection.CVAccuracy);
                             CV1.Add(C);
                             CV1.Add(Gamma);
                         }
                         else if (a == 1)
                         {
                             ParameterSelection.Grid(trainScaledProblem, parameters, "params.txt", out C, out Gamma);
                             CV2.Add(ParameterSelection.CVAccuracy);
                             CV2.Add(C);
                             CV2.Add(Gamma);

                             if (CV1[0] > CV2[0]) //if the previous CV rate is greater than the present, then, discard the present and use the C and Gamma of the previous.
                             {
                                 C = CV1[1];
                                 Gamma = CV1[2];
                             }

                         }*/

                        //Bootstrap Sampling Method
                        //Training.samplingGellingPoint(trainScaledProblem, testScaledProblem);
                        //int subsetNumber = 5;
                        //int samplesPerSubset = 30;
                        //Problem subsets = new Problem();
                        //Parameter bestPara = new Parameter();
                        //subsets = Training.BootstrapSampling(trainScaledProblem, parameters, subsetNumber, samplesPerSubset, testScaledProblem, out bestPara); //select subsets using boothtrap sampling method

                        //parameters.C = C;
                        //parameters.Gamma = Gamma;


                        
                          //KNN-Based boundary instance Selection
                          int k = 50;
                          int numberOfSubset = 300; //subset to select for training
                          Problem dataSubset = Training.computeNearestNeighbour(k, trainScaledProblem, numberOfSubset);
                          ParameterSelection.Grid(dataSubset, parameters, "params.txt", out C, out Gamma);
                          parameters.C = C;
                          parameters.Gamma = Gamma;
                          //Model model = Training.buildModel(dataSubset, parameters);
                          Model model = Training.Train(dataSubset, parameters);
                        

                        //ParameterSelection.Grid(boundaryInstance, parameters, "params.txt", out C, out Gamma);
                        //ParameterSelection.Grid(trainScaledProblem, parameters, "params.txt", out C, out Gamma);

                        /* //FFA_Based Instance Selection
                         FireflyInstanceSelection fi = new FireflyInstanceSelection();
                         Problem subP = fi.firefly_simple(trainScaledProblem);
                         ParameterSelection.Grid(subP, parameters, "params.txt", out C, out Gamma);
                         parameters.C = C;
                         parameters.Gamma = Gamma;
                         Model model = Training.Train(subP, parameters);
                         */

                        /*
                        //Clustering-Based Instance Selection Algorithm
                        Problem boundaryInstance = Training.ClusteringBoundaryInstance(trainScaledProblem);
                        ParameterSelection.Grid(boundaryInstance, parameters, "params.txt", out C, out Gamma);
                        parameters.C = C;
                        parameters.Gamma = Gamma;
                        Model model = Training.Train(boundaryInstance, parameters);
                        */

                        /* //Edge Instance Selection
                         Problem edgeNN = Training.EdgeInstanceSelection(trainScaledProblem);
                         ParameterSelection.Grid(edgeNN, parameters, "params.txt", out C, out Gamma);
                         parameters.C = C;
                         parameters.Gamma = Gamma;
                         Model model = Training.Train(edgeNN, parameters);
                       */

                        /*
                        //Hybrid: KNN-based based + FFA-Based
                        //Problem boundaryInstance = Training.ClusteringBoundaryInstance(trainScaledProblem);
                        FireflyInstanceSelection fi = new FireflyInstanceSelection();
                        Problem subP = fi.firefly_simple(trainScaledProblem);
                        int k = 50;
                        int numberOfSubset = 100; //subset to select for training
                        Problem dataSubset = Training.computeNearestNeighbour(k, subP, numberOfSubset);
                        ParameterSelection.Grid(dataSubset, parameters, "params.txt", out C, out Gamma);
                        parameters.C = C;
                        parameters.Gamma = Gamma;
                        Model model = Training.Train(subP, parameters);
                       */

                        /*
                        //Hybrid: Clustering-Based + FFA-Based
                        Problem boundaryInstance = Training.ClusteringBoundaryInstance(trainScaledProblem);
                        FireflyInstanceSelection fi = new FireflyInstanceSelection();
                        Problem subP = fi.firefly_simple(boundaryInstance);
                        ParameterSelection.Grid(subP, parameters, "params.txt", out C, out Gamma);
                        parameters.C = C;
                        parameters.Gamma = Gamma;
                        Model model = Training.Train(boundaryInstance, parameters);
                        */

                        /* //Hybrid: Clustering based + FFA-Based + KNN-Based
                        Problem boundaryInstance = Training.ClusteringBoundaryInstance(trainScaledProblem);
                        FireflyInstanceSelection fi = new FireflyInstanceSelection();
                        Problem subP = fi.firefly_simple(boundaryInstance);
                        int k = 50;
                        int numberOfSubset = 100; //subset to select for training
                        Problem dataSubset = Training.computeNearestNeighbour(k, boundaryInstance, numberOfSubset);
                        ParameterSelection.Grid(dataSubset, parameters, "params.txt", out C, out Gamma);
                        parameters.C = C;
                        parameters.Gamma = Gamma;
                        Model model = Training.Train(dataSubset, parameters);
                        */
                        //Train the model using the optimal parameters.
                        //Model model = Training.Train(trainScaledProblem, parameters);
                        //removing support vectors that contributes less to the decision surface
                        //Model submod = Training.performSupportVectorReduction(model, trainScaledProblem);

                        //Perform classification on the test data, putting the results in results.txt.
                        //classficatnAccuracySum += Prediction.Predict(testScaledProblem, "ClassificationResults.txt", model, false);
                        classficatnAccuracy = Prediction.Predict(testScaledProblem, "ClassificationResults.txt", model, false); //classfication accuracy for each iteration ->for the purpose of outputting to the text file
                        classficatnAccuracySum += classficatnAccuracy;
                        Console.WriteLine("\nClassification Accuracy: {0}%", 100 * classficatnAccuracy);

                        PerformanceEvaluator pp = new PerformanceEvaluator("ClassificationResults.txt", test, out TP, out TN, out FP, out FN, out P, out F_M);

                        sumTP += TP; sumTN += TN; sumFP += FP; sumFN += FN; sumP += P; sumF_M += F_M;

                        //saving all the output to a file
                        string outpt = string.Format("Cross Validation: {0}, Run number {1}, CAccuracy: {2:0.0000} FP: {3:0.0000}, FN: {4:0.0000}, Recall: {5:0.0000}, Precision: {6:0.0000}, FMeasure: {7:0.0000}", a + 1, aa + 1, (classficatnAccuracy * 100), (FP * 100), (FN * 100), (TP * 100), (P * 100), (F_M * 100));
                        File.AppendAllText(outputEvaluationFilePath, outpt);
                        File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);

                    }
                    
                    if (classficatnAccuracy * 100 > globalBest)
                    {
                        globalBest = classficatnAccuracy * 100;
                    }
                }

                classficatnAccuracySum = (classficatnAccuracySum * 100) / N_Fold; //converting to percentage and dividing by the number of folds
                sumFP = (sumFP * 100) / N_Fold; //calculating the average cross validation for False Positive over 10 folds
                sumTP = (sumTP * 100) / N_Fold; //calculating the average cross validation for Recall over 10 folds
                sumFN = (sumFN * 100) / N_Fold; //calculating the average cross validation for False Negative over 10 folds
                sumF_M = (sumF_M * 100) / N_Fold; //calculating the average cross validation for F Measure over 10 folds
                sumP = (sumP * 100) / N_Fold; //calculating the average cross validation for Precision over 10 folds

               
                avgRuns += classficatnAccuracySum;
                avgFP += sumFP;
                avgFN += sumFN;
                avgR += sumTP;
                avgPr += sumP;
                avgFM += sumF_M;

                //saving all the outputs to a file
                File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
                File.AppendAllText(outputEvaluationFilePath, string.Format("Average Calculations....Run Number: {0}", aa + 1));
                File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
                string outpt2 = string.Format("Run number {0}, Average CAccuracy: {1:0.0000} FP: {2:0.0000}, FN: {3:0.0000}, Recall: {4:0.0000}, Precision: {5:0.0000}, FMeasure: {6:0.0000}", aa + 1, classficatnAccuracySum, sumFP, sumFN, sumTP, sumP, sumF_M);
                File.AppendAllText(outputEvaluationFilePath, outpt2);
                File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
                File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);

                Console.WriteLine("\nStep {0}...............................\n", aa + 1);
            }

            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;
            double time = duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
            Console.WriteLine("\nTotal processing time {0:########.00} seconds\n", time);

            File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
            File.AppendAllText(outputEvaluationFilePath, "Total processing time:\n" + time + " Seconds");
            File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);

            //sending all the outputs to the screen
            Console.WriteLine("\nOverall Average Accuracy: {0:0.00}% \nGlobal Best: {1:0.00}%", avgRuns / n_Runs, globalBest);
            Console.WriteLine("\n\nTotal False Positive: {0:0.00}%\nTotal False Negative: {1:0.00}%\nRecall: {2:0.00}%\nPrecision: {3:0.00}%\nF_Measure: {4:0.00}%", (avgFP / n_Runs), (avgFN / n_Runs), (avgR / n_Runs), (avgPr /  n_Runs), (avgFM / n_Runs));

            File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
            File.AppendAllText(outputEvaluationFilePath, "Overall Average Calculations.......");
            File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
            File.AppendAllText(outputEvaluationFilePath, Environment.NewLine);
            string outpt3 = string.Format("Overall Average CAccuracy: {0:0.0000} FP: {1:0.0000}, FN: {2:0.0000}, Recall: {3:0.0000}, Precision: {4:0.0000}, FMeasure: {5:0.0000}", avgRuns / n_Runs, avgFP / n_Runs, avgFN / n_Runs, avgR / n_Runs, avgPr / n_Runs, avgFM / n_Runs);
            File.AppendAllText(outputEvaluationFilePath, outpt3);
            
            Console.ReadKey();
               
        }

    }
}
 