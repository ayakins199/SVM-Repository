/*
 * SVM.NET Library
 * Copyright (C) 2008 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using ClusteringKMeans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TestSimpleRNG;

namespace SVM
{
    /// <summary>
    /// Class containing the routines to train SVM models.
    /// </summary>
    public static class Training
    {
        /// <summary>
        /// Whether the system will output information to the console during the training process.
        /// </summary>
        public static bool IsVerbose
        {
            get
            {
                return Procedures.IsVerbose;
            }
            set
            {
                Procedures.IsVerbose = value; 
            }
        }

        private static double doCrossValidation(Problem problem, Parameter parameters, int nr_fold)
        {
            int i;
            double[] target = new double[problem.Count];
            Procedures.svm_cross_validation(problem, parameters, nr_fold, target);
            int total_correct = 0;
            double total_error = 0;
            double sumv = 0, sumy = 0, sumvv = 0, sumyy = 0, sumvy = 0;
            if (parameters.SvmType == SvmType.EPSILON_SVR || parameters.SvmType == SvmType.NU_SVR)
            {
                for (i = 0; i < problem.Count; i++)
                {
                    double y = problem.Y[i];
                    double v = target[i];
                    total_error += (v - y) * (v - y);
                    sumv += v;
                    sumy += y;
                    sumvv += v * v;
                    sumyy += y * y;
                    sumvy += v * y;
                }
                return(problem.Count * sumvy - sumv * sumy) / (Math.Sqrt(problem.Count * sumvv - sumv * sumv) * Math.Sqrt(problem.Count * sumyy - sumy * sumy));
            }
            else
                for (i = 0; i < problem.Count; i++)
                    if (target[i] == problem.Y[i])
                        ++total_correct;
            return (double)total_correct / problem.Count;
        }
        /// <summary>
        /// Legacy.  Allows use as if this was svm_train.  See libsvm documentation for details on which arguments to pass.
        /// </summary>
        /// <param name="args"></param>
        [Obsolete("Provided only for legacy compatibility, use the other Train() methods")]
        public static void Train(params string[] args)
        {
            Parameter parameters;
            Problem problem;
            bool crossValidation;
            int nrfold;
            string modelFilename;
            parseCommandLine(args, out parameters, out problem, out crossValidation, out nrfold, out modelFilename);
            if (crossValidation)
                PerformCrossValidation(problem, parameters, nrfold);
            else Model.Write(modelFilename, Train(problem, parameters));
        }

        /// <summary>
        /// Performs cross validation.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="parameters">The parameters to test</param>
        /// <param name="nrfold">The number of cross validations to use</param>
        /// <returns>The cross validation score</returns>
        public static double PerformCrossValidation(Problem problem, Parameter parameters, int nrfold)
        {
            string error = Procedures.svm_check_parameter(problem, parameters);
            if (error == null)
                return doCrossValidation(problem, parameters, nrfold);
            else throw new Exception(error);
        }

        /// <summary>
        /// Trains a model using the provided training data and parameters.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="parameters">The parameters to use</param>
        /// <returns>A trained SVM Model</returns>
        public static Model Train(Problem problem, Parameter parameters)
        {
            string error = Procedures.svm_check_parameter(problem, parameters);

            if (error == null)
                return Procedures.svm_train(problem, parameters);

            else throw new Exception(error);
        }

        private static void parseCommandLine(string[] args, out Parameter parameters, out Problem problem, out bool crossValidation, out int nrfold, out string modelFilename)
        {
            int i;

            parameters = new Parameter();
            // default values

            crossValidation = false;
            nrfold = 0;

            // parse options
            for (i = 0; i < args.Length; i++)
            {
                if (args[i][0] != '-')
                    break;
                ++i;
                switch (args[i - 1][1])
                {

                    case 's':
                        parameters.SvmType = (SvmType)int.Parse(args[i]);
                        break;

                    case 't':
                        parameters.KernelType = (KernelType)int.Parse(args[i]);
                        break;

                    case 'd':
                        parameters.Degree = int.Parse(args[i]);
                        break;

                    case 'g':
                        parameters.Gamma = double.Parse(args[i]);
                        break;

                    case 'r':
                        parameters.Coefficient0 = double.Parse(args[i]);
                        break;

                    case 'n':
                        parameters.Nu = double.Parse(args[i]);
                        break;

                    case 'm':
                        parameters.CacheSize = double.Parse(args[i]);
                        break;

                    case 'c':
                        parameters.C = double.Parse(args[i]);
                        break;

                    case 'e':
                        parameters.EPS = double.Parse(args[i]);
                        break;

                    case 'p':
                        parameters.P = double.Parse(args[i]);
                        break;

                    case 'h':
                        parameters.Shrinking = int.Parse(args[i]) == 1;
                        break;

                    case 'b':
                        parameters.Probability = int.Parse(args[i]) == 1;
                        break;

                    case 'v':
                        crossValidation = true;
                        nrfold = int.Parse(args[i]);
                        if (nrfold < 2)
                        {
                            throw new ArgumentException("n-fold cross validation: n must >= 2");
                        }
                        break;

                    case 'w':
                        parameters.Weights[int.Parse(args[i - 1].Substring(2))] = double.Parse(args[1]);
                        break;

                    default:
                        throw new ArgumentException("Unknown Parameter");
                }
            }

            // determine filenames

            if (i >= args.Length)
                throw new ArgumentException("No input file specified");

            problem = Problem.Read(args[i]);

            if (parameters.Gamma == 0)
                parameters.Gamma = 1.0 / problem.MaxIndex;

            if (i < args.Length - 1)
                modelFilename = args[i + 1];
            else
            {
                int p = args[i].LastIndexOf('/') + 1;
                modelFilename = args[i].Substring(p) + ".model";
            }
        }

        //compute the k-nearest neighbour of all instances in the dataset
        public static Problem computeNearestNeighbour(int k, Problem trainDataset, int numOfSubset)
        {

            //FireflyInstanceSelection fi = new FireflyInstanceSelection();
            //Problem trainDataset = fi.firefly_simple(subP);

            double sum = 0; double distance;
            int n = trainDataset.Count; //number of data instances
            List<Node[]> nearestNeighbours = new List<Node[]>(); List<double> dist = new List<double>(); List<double> labels = new List<double>();
            Node[] xNodes = new Node[n];
            Node[] yNodes = new Node[n];
            //object[,] obj = new object[n-1, 3]; 
            object[,] obj = new object[k, 3]; 
            object[,] temp = new object[1, 3];
            List<Problem> ds = new List<Problem>();
            object[,] nn = new object[n, 6]; //data structure containing the NNs and their corresponding distances
            double score = 0; //score assigned to individual instance by the oppositiley NNs in its neighbourhood list
            object[,] scoreList = new object[n, 3]; //scores assigned to all the instances
            object[,] dataSubset = new object[n, 3]; //subset of data to return

            //compute distance between Xi and other instances
            for (int i = 0; i < n; i++)
            {
                int ctr = 0; int cntr1 = 0; int cntr2 = 0;
                int countP = trainDataset.Y.Count(q => q == 1);
                int countN = trainDataset.Y.Count(q => q == -1);
                for (int j = 0; j < n; j++)
                {
                    if (j.Equals(i))
                        continue;
                    else if (cntr1 < (k * 0.5) && trainDataset.Y[j] == 1) //compute distance for positive class (50% of k goes for positive instances)
                    {
                        distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                        obj[ctr, 0] = distance; obj[ctr, 1] = trainDataset.X[j]; obj[ctr, 2] = trainDataset.Y[j]; ctr++; //save the instance and their corresponding distances
                        cntr1++;
                    }
                    else if (trainDataset.Y[j] == -1 && cntr2 < (k * 0.5)) //compute distance for negative class (50% of k goes for negative instances)
                    {
                        distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                        obj[ctr, 0] = distance; obj[ctr, 1] = trainDataset.X[j]; obj[ctr, 2] = trainDataset.Y[j]; ctr++; //save the instance and their corresponding distances
                        cntr2++;
                    }

                }

                sortMultiArray(obj); //sort array to select the nearest neighbour of Xi

                //select the k-neareast neighbours (using top K elements), their corresponding distances and class labels of Xi
                //int subK = 30; 
                int subK = k; 
                int count1 = 0; int count2 = 0;
                for (int p = 0; p < k; p++)
                {
                    if (count1 < (subK / 2) && (double)obj[p, 2] == 1) //positive class
                    {
                        dist.Add((double)obj[p, 0]); //distance
                        nearestNeighbours.Add((Node[])obj[p, 1]); //nearest neighbour i
                        labels.Add((double)obj[p, 2]); //class labels
                        count1++;
                    }
                    else if (count2 < (subK / 2) && (double)obj[p,2] == -1)
                    {
                        dist.Add((double)obj[p, 0]); //distance
                        nearestNeighbours.Add((Node[])obj[p, 1]); //nearest neighbour i
                        labels.Add((double)obj[p, 2]); //class labels
                        count2++;
                    }
                }

                nn[i, 0] = k; nn[i, 1] = dist; nn[i, 2] = nearestNeighbours; nn[i, 3] = trainDataset.X[i]; nn[i, 4] = labels; nn[i, 5] = trainDataset.Y[i];
                
                //Compute Exponential Decay
                double EDScore = 0; //Exponential decay score
                int counter = 0; 
                for (int p = 0; p < subK; p++)
                {
                    //compute exponential decay for Xi and all its Nearest neighbour belonging to the opposite class
                    //if the label of the current instance in the neighbourhood list is not equal to the label of ith instance then compute its Exponential Decay Score
                    if (((List<double>)nn[i, 4])[p] != (double)nn[i,5])//identify the nearest neighbour belonging to the opposite class
                    {
                        EDScore += ((List<double>)nn[i, 1])[p] - Math.Pow(((List<double>)nn[i, 1])[p], 2); //compute exponential decay score
                        counter++;
                    }
                }
                EDScore = EDScore / counter;

                //determine the scores of every instance
                //int numOfContributors = k - counter; //number of NN of opposite class that contributes to Xi
                int numOfContributors = counter;
                for (int p = 0; p < subK; p++)
                {
                    //if the label of the current instance in the neighbourhood list is not equal to the label of ith instance
                    if (((List<double>)nn[i, 4])[p] != (double)nn[i, 5])//identify the nearest neighbour belonging to the opposite class
                    {
                        score += Math.Exp(-(((List<double>)nn[i, 1])[p] - Math.Pow(((List<double>)nn[i, 1])[p], 2) / EDScore)); 
                    }
                }
                score = score / numOfContributors;
                scoreList[i, 0] = score; scoreList[i, 1] = nn[i, 3]; scoreList[i, 2] = nn[i, 5];
               
                dist = new List<double>(); nearestNeighbours = new List<Node[]>(); labels = new List<double>();
                //EDScoreList.Add(EDScore);//list of Exponential Decay scores
                //Problem pp = new Problem(k, dist, nearestNeighbours, trainDataset.X[i], labels);
                //ds.Add(pp);
                
            }


            sortMultiArray(scoreList); //sort scores to select the best N instances to be used for training

            //select data subset to be used for training. Selected subset are instances that are closest to the data boundary
            Node[][] xScoreList = new Node[numOfSubset][];
            double[] yScoreList = new double[numOfSubset];
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            int total = n-1;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    dataSubset[i, j] = scoreList[total, j]; //select instances with the highest scores
                }
                if (cnt1 < (0.7 * numOfSubset) && (double)dataSubset[i, 2] == 1) //select 70% positive instance of the subset
                {
                    xScoreList[cnt3] = (Node[])dataSubset[i, 1];
                    yScoreList[cnt3] = (double)dataSubset[i, 2];
                    cnt1++; cnt3++;
                }
                else if (cnt2 < (0.3 * numOfSubset) && (double)dataSubset[i, 2] == -1) //select 30% negative instance of the subset
                {
                    xScoreList[cnt3] = (Node[])dataSubset[i, 1];
                    yScoreList[cnt3] = (double)dataSubset[i, 2];
                    cnt2++; cnt3++;
                }
                total--;
            }
            Problem subset = new Problem(numOfSubset, yScoreList, xScoreList, xScoreList[0].GetLength(0));

            return subset;
        }

        //sort multidimensional array (smallest - largest, i.e. ascending order, and descending order)
        public static void sortMultiArray(object[,] obj)
        {
            int n = obj.GetUpperBound(0);
            object[,] temp = new object[1, 3];
            for (int m = 0; m <= n; m++)
            {
                for (int p = 0; p < n; p++)
                {
                    if ((double)obj[p, 0] > (double)obj[p + 1, 0]) //sort in ascending order
                    {
                        temp[0, 0] = obj[p + 1, 0];
                        temp[0, 1] = obj[p + 1, 1];
                        temp[0, 2] = obj[p + 1, 2];
                        obj[p + 1, 0] = obj[p, 0];
                        obj[p + 1, 1] = obj[p, 1];
                        obj[p + 1, 2] = obj[p, 2];
                        obj[p, 0] = temp[0, 0];
                        obj[p, 1] = temp[0, 1];
                        obj[p, 2] = temp[0, 2];
                    }
                }
            }
        }

        /// <summary>
        /// //Removes support vectors that contributes less to the decision surface
        /// </summary>
        /// <param name="problem">The support vectors</param>
        /// <param name="model">The trained model</param>
        /// <returns>A SVM Model with Reduced Support Vectors</returns>
        public static Model performSupportVectorReduction(Model model, Problem problem)
        {
            Model submod = new Model(); ParameterSelection ps = new ParameterSelection();
            double[] classAcc = new double[model.SupportVectorCount + 1]; //this is an array of classification accuraces for the main decision function and the support vectors decision function
            classAcc[0] = Prediction.Predict(problem, "ClassificationResults.txt", model, false);
            for (int i = 0; i < model.SupportVectorCount; i++)
            {
                Problem subprob = new Problem();
                subprob.Count = model.SupportVectorCount - 1; //this is because one support vector is gonna be excluded from the dataset per iteration
                subprob.X = new Node[subprob.Count][];
                subprob.Y = new double[subprob.Count];
                int ctr = 0; //counter for sub problem

                for (int j = 0; j < model.SupportVectorCount; j++)
                {
                    if (j.Equals(i)) //skip (or remove) one support vector at a time - this is to check the effect of each suppot vector
                        continue;

                    subprob.X[ctr] = model.SupportVectors[j];
                    subprob.Y[ctr] = model.SupportVectorsClassLabels[j];
                    subprob.MaxIndex = subprob.X[ctr].Length;
                    ctr++;
                }

                //subprob.Y = (double[])model.SupportVectorsClassLabels.ToList().Where(s => s != i).ToArray();
                //subprob.X = (Node[][])model.SupportVectors.Select(x => x.Except(model.SupportVectors[i])).ToArray();

                submod = Training.Train(subprob, model.Parameter);
                classAcc[i + 1] = Prediction.Predict(problem, "ClassificationResults.txt", submod, false);
                string op = string.Format("{0} ", classAcc[i + 1].ToString());
                File.AppendAllText(ps.SVAccuracyFilePath, op);
            }
            return submod;
        }

        public static void samplingGellingPoint(Problem trainProb, Problem testProb)
        {
            Random rand = new Random();
            Problem subprob = new Problem();
            double prop1 = 0.05;
            double gellingPoint;
            Parameter param = new Parameter();
            double C, Gamma;
            int ctr1 = 0, ctr2 = 0, ctr3 = 0; 

            int subsetCount = (int)Math.Round(prop1 * trainProb.X.Length); //subset count
            subprob.Count = subsetCount;
            subprob.X = new Node[subprob.Count][];
            subprob.Y = new double[subprob.Count];
            for (int j = 0; j < subsetCount; j++)
            {
                int r = rand.Next(0, trainProb.Count);
                if (ctr1 < (int)Math.Round(0.7 * subsetCount) && trainProb.Y[r] == 1) //select 70% of positive instance
                {
                    subprob.X[ctr3] = trainProb.X[r]; //generate random subsets
                    subprob.Y[ctr3] = trainProb.Y[r];
                    ctr1++; ctr3++;
                }
                else if (ctr2 < (int)Math.Round(0.3 * subsetCount) && trainProb.Y[r] == -1) //select 70% of positive instance
                {
                    subprob.X[ctr3] = trainProb.X[r]; //generate random subsets
                    subprob.Y[ctr3] = trainProb.Y[r];
                    ctr2++; ctr3++;
                }
                if ((ctr1 + ctr2) < subsetCount && j == subsetCount - 1) //ensuring that 100% of the predefined subset number is selected
                    j = 0;
            }
            ParameterSelection.Grid(subprob, param, "params.txt", out C, out Gamma);
            param.C = C;
            param.Gamma = Gamma;
            Model model = Training.Train(subprob, param);
            double ca1 = Prediction.Predict(testProb, "ClassificationResults.txt", model, false);

            double prop2 = prop1 + 0.01;
            subsetCount = (int)Math.Round(prop2 * trainProb.X.Length); //subset count
            subprob.Count = subsetCount;
            subprob.X = new Node[subprob.Count][];
            subprob.Y = new double[subprob.Count];
            ctr1 = 0; ctr2 = 0; ctr3 = 0;
            for (int j = 0; j < subsetCount; j++)
            {
                int r = rand.Next(0, trainProb.Count);
                if (ctr1 < (int)Math.Round(0.7 * subsetCount) && trainProb.Y[r] == 1) //select 70% of positive instance
                {
                    subprob.X[ctr3] = trainProb.X[r]; //generate random subsets
                    subprob.Y[ctr3] = trainProb.Y[r];
                    ctr1++; ctr3++;
                }
                else if (ctr2 < (int)Math.Round(0.3 * subsetCount) && trainProb.Y[r] == -1) //select 70% of positive instance
                {
                    subprob.X[ctr3] = trainProb.X[r]; //generate random subsets
                    subprob.Y[ctr3] = trainProb.Y[r];
                    ctr2++; ctr3++;
                }
                if ((ctr1 + ctr2) < subsetCount && j == subsetCount - 1) //ensuring that 100% of the predefined subset number is selected
                    j = 0;
            }
            ParameterSelection.Grid(subprob, param, "params.txt", out C, out Gamma);
            param.C = C;
            param.Gamma = Gamma;
            model = Training.Train(subprob, param);
            double ca2 = Prediction.Predict(testProb, "ClassificationResults.txt", model, false);

            gellingPoint = (ca2 - ca1) / (prop2 - prop1);


            //for (double i = 0.07; i < 0.1; i += 0.01)
            //{
            //    int proportion = (int)Math.Round(i * trainProb.X.Length);
            //    subprob.Count = proportion;
            //    for (int j = 0; j < proportion; j++)
            //    {
            //        int r = rand.Next(0, trainProb.Count);
            //        subprob.X[j] = trainProb.X[r]; //generate random subsets
            //        subprob.Y[j] = trainProb.Y[r];
            //    }
            //    Model model = Training.Train(subprob, param);
            //    double ca1 = Prediction.Predict(testProb, "ClassificationResults.txt", model, false);
            //}
        }

        public static Problem BootstrapSampling(Problem prob, Parameter param, int subsetNumber, int samplesPerSubset, Problem test, out Parameter bestSubsetPara)
        {
            Problem subprob = new Problem();
            subprob.Count = samplesPerSubset;
            List<Problem> subProbList = new List<Problem>();
            List<Model> subModels = new List<Model>();
            Parameter parameters = new Parameter();
            bestSubsetPara = new Parameter();

            string[] trainResults = new string[subsetNumber];
            string[] testResults = new string[subsetNumber];
            string filePath = String.Format(Environment.CurrentDirectory + "\\{0}", "ClassificationResults.txt");
            double C = new double(); double Gamma = new double();
            //int[] index = new int[samplesPerSubset];
            List<int[]> indexList = new List<int[]>();
            List<Parameter> paraList = new List<Parameter>();

            //generate N subsets
            for (int i = 0; i < subsetNumber; i++)
            {
                Random rand = new Random();
                int[] index = new int[samplesPerSubset];
                subprob.X = new Node[subprob.Count][];
                subprob.Y = new double[subprob.Count];
                for (int j = 0; j < samplesPerSubset; j++)
                {
                    int r = rand.Next(0, prob.Count);
                    subprob.X[j] = prob.X[r]; //generate random subsets
                    subprob.Y[j] = prob.Y[r];
                    index[j] = r;
                }
                
                subProbList.Add(subprob);
                indexList.Add(index); //save the original index of the data in each subset
            }

            //use subsets to train and obtain classification accuracy of each subsets
            for (int i = 0; i < subsetNumber; i++)
            {
                ParameterSelection.Grid(subProbList[i], parameters, "params.txt", out C, out Gamma); //select parameters for each subset
                parameters.C = C;
                parameters.Gamma = Gamma;
                Model subModel = Training.Train(subProbList[i], parameters); //train each subset
                File.WriteAllText(filePath, string.Empty);
                Prediction.Predict(prob, "ClassificationResults.txt", subModel, false); //use each subset to classify train dataset
                trainResults[i] = File.ReadAllText(filePath); //save classification result for train dataset

                //File.WriteAllText(filePath, string.Empty);
                //Prediction.Predict(test, "ClassificationResults.txt", subModel, false); //use each subset to classify test dataset
                //testResults[i] = File.ReadAllText(filePath); //save classification result for train dataset

                paraList.Add(parameters); //save the parameters used by each subset for training
                subModels.Add(subModel);

            }


            int indexOfMax = InformationPatternExtration(prob, trainResults, subsetNumber, indexList); //Extract information pattern
            //Problem reducedDataList = removeDataset(prob, test, trainResults, testResults); //Remove data instances that are correctly classified
            Problem bestSubset = subProbList[indexOfMax]; //select the best subset
            bestSubsetPara = paraList[indexOfMax]; //select the parameter for the best subset

            return bestSubset;
        }

        public static int InformationPatternExtration(Problem prob, string[] predictedResults, int subsetNum, List<int[]> indexList)
        {
            string[][] predicted = new string[5][];
            int count = predictedResults[0].TrimEnd(' ').TrimStart(' ').Split(' ').Length;
            int[] totalMissclassified = new int[count];
            double[] entropy = new double[count];
            double[] entropySubset = new double[subsetNum];
            double informationEntropy;
            double sum = 0;

            //collate all the predicted results for each instance
            for (int i = 0; i < subsetNum; i++)
                predicted[i] = predictedResults[i].TrimEnd(' ').TrimStart(' ').Split(' ');

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < subsetNum; j++)
                {
                    if (Convert.ToDouble(predicted[j][i]) != prob.Y[i])
                    {
                        totalMissclassified[i]++; //sum the total number of misclassified instance for each predictor
                    }
                }
                double a = (subsetNum - totalMissclassified[i])/subsetNum;
                double b = totalMissclassified[i] / subsetNum;
                informationEntropy = -(a * Math.Log(a, 2) + (b * Math.Log(b, 2)));
                entropy[i] = (informationEntropy.Equals(double.NaN)) ? 0 : informationEntropy; //assign zero if entropy is NaN. This happens if num of misclassified data is zero.
            }

            //calculate information entropy for each subset
            for (int i = 0; i < indexList.Count; i++)
            {
                for (int j = 0; j < indexList[i].Length; j++)
                {
                    int index = indexList[i][j]; //get the original index of each data instance of each subset
                    sum += entropy[index]; //get and sum the entropy of each instance in each subset
                }
                entropySubset[i] = sum;// save the entropy of each subset
            }

            double max = entropySubset.Max();
            int indexOfMax = Array.IndexOf(entropySubset, max);

            //Problem subP = subProbList[indexOfMax];
            
            //double totalEntropy = entropy.Sum(); //information entropy for the whole dataset            

            return indexOfMax;
        }

        public static Problem removeDataset(Problem trainDataset, Problem testDataset, string[] trainResults, string[] testResults)
        {
            int totalSize = trainDataset.Y.Length + testDataset.Y.Length;
            double[] combinedTestTrainActual = new double[totalSize];
            string[] combinedTestTrainPredicted = new string[totalSize];
            Node[][] combinedDataset = new Node[totalSize][];
            List<Node[][]> reducedDataList = new List<Node[][]>();
            List<int> sub = new List<int>();
            

            //combine actual labels -> i.e. actual Test + train labels
            //Array.Copy(trainDataset.Y, combinedTestTrainActual, trainDataset.Y.Length);
            //Array.Copy(testDataset.Y, 0, combinedTestTrainActual, trainDataset.Y.Length, testDataset.Y.Length);

            //combine the train and test dataset
            //Array.Copy(trainDataset.X, combinedDataset, trainDataset.X.Length);
            //Array.Copy(testDataset.X, 0, combinedDataset, trainDataset.X.Length, testDataset.X.Length);

            for (int i = 0; i < trainResults.Length; i++)
            {
                string[] trainLabels = trainResults[i].TrimEnd(' ').TrimStart(' ').Split(' ');
                //string[] testLabels = testResults[i].TrimEnd(' ').TrimStart(' ').Split(' ');
                

                //combine predicted dataset -> i.e. predicted Test + train labels
                //Array.Copy(trainLabels, combinedTestTrainPredicted, trainLabels.Length);
                //Array.Copy(testLabels, 0, combinedTestTrainPredicted, trainLabels.Length, testLabels.Length);

                //keep track of the index of all the incorrectly classified data instances
                for (int j = 0; j < trainDataset.X.Length; j++)
                {
                    if (trainDataset.Y[j] != Convert.ToDouble(trainLabels[j]))
                    {
                        sub.Add(j); //save the index of all the incorrectly classified data instances
                    }
                }

                //for (int j = 0; j < combinedTestTrainPredicted.Length; j++)
                //{
                //    if (combinedTestTrainActual[j] != Convert.ToDouble(combinedTestTrainPredicted[j]))
                //    {
                //        sub.Add(j); //save the index of all the incorrectly classified data instances
                //    }
                //}
            }

            sub = sub.Distinct().ToList<int>();

            Node[][] reducedSet = new Node[sub.Count][];
            double[] labels = new double[sub.Count];
            for (int k = 0; k < sub.Count; k++)
            {

                reducedSet[k] = trainDataset.X[sub[k]]; //extract the incorrectly classified data instances
                labels[k] = trainDataset.Y[sub[k]]; //save the labels for the instance

                //reducedSet[k] = combinedDataset[sub[k]]; //extract the incorrectly classified data instances
                //labels[k] = combinedTestTrainActual[sub[k]]; //save the labels for the instance
            }

            Problem subProb = new Problem(reducedSet.Length, labels, reducedSet, reducedSet[0].GetLength(0));
            
            return subProb;
        }

        public static List<int> GetRandomNumbers(int count, int Max)
        {
            List<int> randomNumbers = new List<int>();
            Random random = new Random();
            for (int i = 0; i < count; i++)
            {
                int number;

                do number = random.Next(0, Max);
                while (randomNumbers.Contains(number));

                randomNumbers.Add(number);
            }

            return randomNumbers;
        }

        //returns the percent of the original training set was retained by the reduction algorithm
        public static double StoragePercentage(Problem p, Problem prob)
        {
            //double totalSelected = obj.Attribute_Values.Count(q => q == 1); //count the total number of selected instances
            double totalSelected = p.Count;
            double storagePer = (totalSelected / prob.Count) * 100; //calculate storage percentage
            return storagePer; 
        }

        //Select Clustering boundary instance to be used for training
        public static Problem ClusteringBoundaryInstance(Problem trainScaledProblem)
        {
            //double[][] clusData = new double[1][]; //variable to hold the current positive class
            //double[][] negativeInstance = new double[1][]; //variable to hold the current negative class
            //List<double[]> clusList = new List<double[]>(); //variable to hold the list of current positive class
            //List<double[]> negInstanceList = new List<double[]>(); //variable to hold the list of current negative class
            //int vecCount = trainScaledProblem.X[0].Count();
            int totTrainEmail = trainScaledProblem.Count;
            //List<double> clusLabelList = new List<double>();
            //List<Node[]> posClusNode = new List<Node[]>(); //positive cluster node
            //List<Node[]> negClusNode = new List<Node[]>(); //negative cluster node
            List<object[,]> posObjList = new List<object[,]>();
            List<object[,]> negObjList = new List<object[,]>();
            object[,] posObj = new object[1,2];
            object[,] negObj = new object[1, 2];
            int numCluster = 3;
            int numOfClasses = 2;
            double ratio = 0.5;
            int numPosInstances = Convert.ToInt16(0.1111 * totTrainEmail); //number of positive instances 

            double[][] trainEmails = new double[totTrainEmail][];
            Random rand = new Random();
            List<int> rList = new List<int>();
            bool chk = false;

            List<int> randNum = GetRandomNumbers(totTrainEmail, totTrainEmail); //generate N unique random numbers

            
            for (int i = 0; i < numPosInstances; i++)
            {
                posObj[0, 0] = trainScaledProblem.X[randNum[i]]; //randomly select positive instances
                posObj[0, 1] = trainScaledProblem.Y[randNum[i]];
                posObjList.Add(posObj);
                posObj = new object[1, 2];
            }
            for (int j = numPosInstances; j < totTrainEmail; j++) //select negative instances. Number of instances is Total minus Number of positive instances
            {
                negObj[0, 0] = trainScaledProblem.X[randNum[j]];
                negObj[0, 1] = trainScaledProblem.Y[randNum[j]];
                negObjList.Add(negObj);
                negObj = new object[1, 2];
            }

           /*
                for (int i = 0; i < totTrainEmail; i++)
                {
                    //trainEmails[i] = new double[vecCount];
                    //clusData[0] = new double[vecCount];
                    //negativeInstance[0] = new double[vecCount];
                    //for (int j = 0; j < vecCount; j++)
                    //    trainEmails[i][j] = trainScaledProblem.X[i][j].Value; //extracting vector values of training email

                    if (trainScaledProblem.Y[i] == 1) //perform clusering only on positive instances
                    {
                        //clusLabelList.Add(trainScaledProblem.Y[i]); //keep track of the label of the selected positive instance
                        //posClusNode.Add(trainScaledProblem.X[i]); //save the selected positive instance, in Node Format
                        posObj[0, 0] = trainScaledProblem.X[i];
                        posObj[0, 1] = trainScaledProblem.Y[i];
                        posObjList.Add(posObj);
                        //for (int j = 0; j < vecCount; j++)
                        //{
                        //    clusData[0][j] = trainScaledProblem.X[i][j].Value;
                        //}

                        //clusList.Add(clusData[0]); //save the selected positive instance
                    }
                    else //select negative instance for processing
                    {
                       // negClusNode.Add(trainScaledProblem.X[i]);

                        negObj[0, 0] = trainScaledProblem.X[i];
                        negObj[0, 1] = trainScaledProblem.Y[i];
                        negObjList.Add(negObj);

                        //for (int j = 0; j < vecCount; j++)
                        //{
                        //    negativeInstance[0][j] = trainScaledProblem.X[i][j].Value; //extracting vector values of training email
                        //}
                        //negInstanceList.Add(negativeInstance[0]); //save the selected negative instance for further processing
                    }
                }
          */

            //clusData = new double[clusList.Count][]; //creating a new instance based on the total number of selected positive class
            //clusList.CopyTo(clusData); //copying the selected positive instances to 'clusData' variable, for clustering. Clustering method accept only double[][]

            //negativeInstance = new double[negInstanceList.Count][]; //creating a new instance based on the total number of selected negative class
            //negInstanceList.CopyTo(negativeInstance); //copying the selected positive instances to 'clusData' variable, for clustering. Clustering method accept only double[][]

            double[][] means = KMeansDemo.Cluster(posObjList, numCluster); //apply clustering algorithm to positive instances and return means (i.e. cluster centers or centriod)
            double[] distP = new double[posObjList.Count()]; //distances for positive instances
            double[] distN = new double[negObjList.Count()]; //distances for negative instances
            List<double> clusLabelListUpdate = new List<double>(); //saves the updated list of labels

            //Select boundary instances using the obtained cluster centers
            for (int i = 0; i < means.Count(); i++)
            {
                distP = new double[posObjList.Count()];
                distN = new double[negObjList.Count()];
                double[][] clus = KMeansDemo.extractData(posObjList);
                
                for (int j = 0; j < posObjList.Count(); j++)
                {
                    distP[j] = KMeansDemo.Distance(clus[j], means[i]); //compute distance between Cluster centers (i.e. means) and all selected positive instances
                    //distP[j] = KMeansDemo.Distance(clusData[j], means[i]);
                }
                int[] indexP = sortAndRetainIndex(distP);  //Sort distance and use index of sorted array to identify instances to be removed
                //posClusNode = sortNode(posClusNode, indexP); //sort positive Node according to their corresponding distances
                posObjList = sortNode(posObjList, indexP);

                //compute distance between Cluster centers (i.e. the means) and all negative instances in the training set
                double[][] negInstance = KMeansDemo.extractData(negObjList);
                for (int j = 0; j < negObjList.Count(); j++)
                {
                    distN[j] = KMeansDemo.Distance(negInstance[j], means[i]);
                    //distN[j] = KMeansDemo.Distance(negativeInstance[j], means[i]);
                }
                int[] indexN = sortAndRetainIndex(distN); //sort distances and retain its index
                //negClusNode = sortNode(negClusNode, indexN); //sort positive Node according to their corresponding distances
                negObjList = sortNode(negObjList, indexN);

                double sel = ((ratio * totTrainEmail) / 3) + numCluster;
                int numP = 0;
                int numN = 0;

                if (posObjList.Count() >= sel) //if the total number of instances in positive class is greater than the number of selected instances of the current positive class
                {
                    double A = (ratio * totTrainEmail) / 3;
                    //numP = Convert.ToInt16((clusData.Count() - A) / numCluster); //number of positive instances to remove or delete
                    numP = (int)A;
                    //posClusNode = updateList(posClusNode, negClusNode, out clusLabelListUpdate, clusLabelList, numP, indexP, "remove");
                    posObjList = updateList(posObjList, negObjList, numP, indexP, "remove");
                    //clusData = updateList(clusData, out clusLabelListUpdate, clusLabelList, numP, idx, "remove"); //remove positive instances close to the cluster center

                    numN = Convert.ToInt16(2 * A);
                    //numN = Convert.ToInt16(((ratio * totTrainEmail) - numP) / numCluster * (numOfClasses - 1)); //number of negative instances to add
                    posObjList = updateList(posObjList, negObjList, numN, indexN, "add");
                    //clusData = updateList(clusData, out clusLabelListUpdate, clusLabelList, numN, idx, "add"); //add negative instances close to the cluster center
                }
                else
                {
                    numP = posObjList.Count(); //number of positive instances to remove or delete
                    posObjList = updateList(posObjList, negObjList, numP, indexP, "remove");

                    numN = Convert.ToInt16((ratio * totTrainEmail) - posObjList.Count());
                    posObjList = updateList(posObjList, negObjList, numN, indexN, "add");
                }

                //compute distance between Cluster centers (i.e. the means) and all negative instances in the training set
                //for (int j = 0; j < negativeInstance.Count(); j++)
                //    distN[j] = KMeansDemo.Distance(negativeInstance[j], means[i]);
                //idx = sortAndRetainIndex(distN); //sort distances and retain its index
                //int numN = Convert.ToInt16(((ratio * totTrainEmail) - numP) / numCluster * (numOfClasses - 1));//number of negative instances to add
                //posClusNode = updateList(posClusNode, negClusNode, out clusLabelListUpdate, clusLabelList, numN, idx, "add");
                //clusData = updateList(clusData, out clusLabelListUpdate, clusLabelList, numN, idx, "add"); //add negative instances close to the cluster center
            }

            //Node[][] x = posClusNode.ToArray(); //vectors
            //double[] y = clusLabelListUpdate.ToArray(); //labels
            //Problem boundaryInstance = new Problem(posClusNode.Count, y, x, x[0].Count());
            /*
            double[] Y = new double[posObjList.Count];
            Node[][] X = new Node[posObjList.Count][];
            object[,] vecObj = posObjList[0];
            Node[] vector = (Node[])vecObj[0, 0];

            for (int i = 0; i < posObjList.Count; i++)
            {
                Y[i] = (double)posObjList[i][0, 1];
            }

            int vecCount = vector.Length;
            for (int i = 0; i < posObjList.Count; i++)
            {
                X[i] = new Node[vecCount];
                for (int j = 0; j < vecCount; j++)
                {
                    Node[] v = (Node[])posObjList[i][0, 0];
                    X[i][j] = new Node();
                    X[i][j].Index = j + 1;
                    X[i][j].Value = v[j].Value;
                }
            }
                    
            Problem boundaryInstance = new Problem(posObjList.Count, Y, X, X[0].Count());
             */
            
            int tot = Convert.ToInt16(0.5 * posObjList.Count()); //selecting subset for training
            List<int> randN = GetRandomNumbers(tot, posObjList.Count()); //generate N unique random numbers
            //obtain labels of problem
            double[] Y = new double[tot];
            for (int i = 0; i < tot; i++)
            {
                Y[i] = (double)posObjList[randN[i]][0, 1];
            }

            //obtain vetor array of problem
            Node[][] X = new Node[tot][];
            object[,] vecObj = posObjList[0];
            Node[] vector = (Node[])vecObj[0, 0];
            int vecCount = vector.Length;
            for (int i = 0; i < tot; i++)
            {
                X[i] = new Node[vecCount];
                for (int j = 0; j < vecCount; j++)
                {
                    Node[] v = (Node[])posObjList[randN[i]][0, 0];
                    X[i][j] = new Node();
                    X[i][j].Index = j + 1;
                    X[i][j].Value = v[j].Value;
                }
            }

            Problem boundaryInstance = new Problem(tot, Y, X, X[0].Count());
                //return clusData; //return the final list of selected instances to be used for training
            return boundaryInstance; //return the final list of selected instances to be used for training
        }

        //sort array and retain index
        public static int[] sortAndRetainIndex(double[] dist)
        {
            var sorted = dist
                .Select((x, index) => new { sor = x, Index = index })
                .OrderBy(x => x.sor);

            dist = sorted.Select(x => x.sor).ToArray();
            int[] idx = sorted.Select(x => x.Index).ToArray();

            return idx;
        }

        //public static List<Node[]> sortNode(List<Node[]> clusNode, int[] index)
        public static List<object[,]> sortNode(List<object[,]> clusNode, int[] index)
        {
            //Node[] temp = new Node[1];
            object[,] temp = new object[1,1];
            //List<Node[]> nodeDuplicate = new List<Node[]>();
            List<object[,]> nodeDuplicate = new List<object[,]>();
            nodeDuplicate = clusNode; //create a duplicate

            //sort positive class according to their corresponding distances
            for (int j = 0; j < clusNode.Count - 1; j++)
            {
                temp = clusNode[j];
                clusNode[j] = nodeDuplicate[index[j]];
                clusNode[index[j]] = temp;
            }

            return clusNode;
        }

        //public static double[][] updateList(double[][] clusData, out List<double> clusLabelListUpdate, List<double> labelList, int num, int[] index, string action)
        //public static List<Node[]> updateList(List<Node[]> posClusNode, List<Node[]> negClusNode, out List<double> clusLabelListUpdate, List<double> labelList, int num, int[] index, string action)
        public static List<object[,]> updateList(List<object[,]> posClusNode, List<object[,]> negClusNode, int num, int[] index, string action)    
        {
            List<double[]> newClusDataList = new List<double[]>();
            List<int> indexList = new List<int>();
            //clusLabelListUpdate = new List<double>();
            //clusLabelListUpdate = labelList;
            //newClusDataList = clusData.ToList();
            indexList = index.ToList();

            //Remove or delete positive instances close to the cluster centers
            if (action == "remove")
            {
                int r = 0;
                for (int i = 0; i < num; i++)
                {
                    posClusNode.RemoveAt(r);
                    //posClusNode.RemoveAt(index[i]);
                    //newClusDataList.RemoveAt(index[i]); //remove positive instance close to the boundary. Each instance index is saved in variable 'index'. This variable is used because array is already sorted.
                    //clusLabelListUpdate.RemoveAt(r);
                    //num = posClusNode.Count; //update num, to avoid 'index out of range' error
                }
            }
            else if (action == "add")
            {
                for (int i = 0; i < num; i++)
                {
                    posClusNode.Add(negClusNode[i]);
                    //posClusNode.Add(negClusNode[index[i]]); //add negative instance close to the boundary. Each instance index is saved in variable 'index'. 
                    //clusLabelListUpdate.Add(-1); //add the corresponding label
                }
                    //newClusDataList.Add(clusData[index[i]]);
            }
            //clusData = newClusDataList.ToArray();
            return posClusNode; //return updated list of selected instances
        }

        //shuffle dataset for even distribution of positive and negative class
        public static Problem ShuffleDataset(Problem trainDS)
        {
            int n = trainDS.Count;
            Node[][] shuffle = new Node[n][];
            double[] label = new double[n];
            List<int> selected = new List<int>();
            List<int> notSelected = new List<int>();
            for (int k = 0; k < n; k++)
                selected.Add(k);

            int m = 0, j, ctr1 = 0, ctr2 = 0, p = 0, r = 0;
            int numP = 20, numN = 100; //number of positive and negative instances to be arranged next to each other
            int counter = 0;
            while (counter != 3600)
            {
                //p = ctr1 < numN ? selected[ctr1] : selected[ctr2];
                if (r >= selected.Count) //ensure that the index is not greater than the total number of elements in the list
                    r = 0;
                p = selected[r]; 
                if (ctr1 < numN && trainDS.Y[p] == -1) //select N negative instances and their corresponding labels
                {
                    shuffle[m] = trainDS.X[p];
                    label[m] = trainDS.Y[p];
                    selected.Remove(p); //remove instance from list if it has been selected
                    ctr1++; m++;
                }
                if (ctr1 == numN && ctr2 < numP && trainDS.Y[p] == 1) //select N positve instances and their corresponding labels
                {
                    shuffle[m] = trainDS.X[p];
                    label[m] = trainDS.Y[p];
                    selected.Remove(p); //remove instance from list if it has been selected
                    ctr2++; m++;
                }
                
                if (ctr1 == numN && ctr2 == numP) //come here to re-initialize r, ctr1 and ctrl2. This is done to ensure that the pre-specified shuffle order of positive and negative instance is maintained
                {
                    r = 0;
                    ctr1 = 0;
                    ctr2 = 0;
                }
                
                //count the number of 1 and -1 in the main dataset and shuffled dataset
                int countMPos = trainDS.Y.Count(a => a == 1); //count for positive class - main dataset
                int countMNeg = trainDS.Y.Count(a => a == -1); //count for negative class - main dataset
                int countSPos = label.Count(a => a == 1); //count for positive class - shuffle dataset
                int countSNeg = label.Count(a => a == -1); //count for negative class - shuffle dataset
                int remNeg = countMNeg - countSNeg; //total number of remaining negative instance
                int remPos = countMPos - countSPos; //total number of remaining positive instance
                
                if (remNeg < numN || remPos < numP) //come here if the nummber of remaining positive or negative instances in the list is less than the specified shuffle order of negative or positive instance is 
                {
                    int tPS = countSNeg + countSPos; //total number of already selected positive and negative instance
                    counter = tPS; //start updating the list from the index of the last instance on the current list
                    for (int i = 0; i < selected.Count; i++) //go through the remaining instance and update the list with the remaining positive instance
                    {
                        p = selected[i];
                        if (trainDS.Y[p] == -1) //update the list with the remaining negative instances. Start update from the index of the last instance on the current list
                        {
                            shuffle[counter] = trainDS.X[p];
                            label[counter] = trainDS.Y[p];
                            counter++;
                        }
                    }

                    for (int i = 0; i < selected.Count; i++)
                    {
                        p = selected[i];
                        if (trainDS.Y[p] == 1) //update the list with the remaining positive instances
                        {
                            shuffle[counter] = trainDS.X[p];
                            label[counter] = trainDS.Y[p];
                            counter++;
                        }
                    }
                }
                r++; //update general counter
            }
            Problem pr = new Problem(shuffle.Count(), label, shuffle, shuffle[0].GetLength(0));

            return pr;
        }
        
        //select edge instance for training
        public static Problem EdgeInstanceSelection(Problem trainDataset)
        {
            //int rN = 500;
            //trainDataset = ShuffleDataset(trainDataset); //shuffle positive and negative instances in the dataset. This is to ensure even distribution of positive and negative instances
            /*
            List<int> randNum = GetRandomNumbers(rN, subP.Count);
            Problem trainDataset = new Problem();
            trainDataset.X = new Node[rN][];
            trainDataset.Y = new double[rN];
            trainDataset.MaxIndex = subP.MaxIndex;
            trainDataset.Count = rN;
            for (int i = 0; i < rN; i++)
            {
                trainDataset.X[i] = subP.X[randNum[i]];
                trainDataset.Y[i] = subP.Y[randNum[i]];
            }
            */

            //FireflyInstanceSelection fi = new FireflyInstanceSelection();
            //Problem trainDataset = fi.firefly_simple(subP);

            int n = trainDataset.Count;
            int index = 0; int[] indexList = new int[n];
            int j;
            List<double[]> edgeInstanceList = new List<double[]>();
            List<double[]> sumIndexList = new List<double[]>();
            double[] sumIndex = new double[2];
            double[][] dist = new double[n][];
            int[] vote = new int[n];
                //compute distance between Xi and other instances
            for (int i = 0; i < n; i++)
            {
                double[] edgeInstance = new double[2];
                double bestDistance = double.MinValue;
                dist[i] = new double[n];
                for (j = 0; j < n; j++)
                {
                    if (j.Equals(i))
                        continue;
                    double distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the data
                    dist[i][j] = distance;
                    if (distance > bestDistance) //select the largest distance
                    {
                        bestDistance = distance;
                        index = j;
                    }
                }
                vote[index]++; //increase the vote of the instance with the largest distance
                // edgeInstance[0] = index; //save the index of the best distance
                // edgeInstance[1] = bestDistance; //save the best distance
                //edgeInstanceList.Add(edgeInstance); //save distance and index in a list
            }
            
            /*
            //count the total number of times each instance is selected as edge instance
            for (int i = 0; i < edgeInstanceList.Count; i++)
            {
                sumIndex = new double[2];
                sumIndex[0] = edgeInstanceList[i][0]; //select the index
                int s = 0;
                for (int k = 0; k<edgeInstanceList.Count; k++) //adding up the total number of occurences of each selected edge instance, across the dataset
                {
                    if (sumIndex[0] == edgeInstanceList[k][0])
                        sumIndex[1]++;
                }
                if (sumIndexList.Count == 0) //add the first element in the list
                    sumIndexList.Add(sumIndex);
                else //avoid adding duplicate instance; only add distinct
                {
                    for (int m = 0; m < sumIndexList.Count; m++)
                    {
                        if (sumIndexList[m][0] != sumIndex[0]) //checking if instance has been added to list before
                            s++;
                        if(s == sumIndexList.Count) //if instance is not on the list, add to list. s will be equal to count if item is not on the list
                            sumIndexList.Add(sumIndex);
                    }
                }
            }
            
            double[] temp = new double[2];
            if (sumIndexList.Count > 1)//sort list if there are more than one element (edge instance) in the list
            {
                for (int i = 0; i < sumIndexList.Count; i++)
                {
                    for (int k = 0; k < sumIndexList.Count - 1; k++)
                    {
                        if (sumIndexList[k][1] < sumIndexList[k + 1][1])
                        {
                            temp[0] = sumIndexList[k][0];
                            temp[1] = sumIndexList[k][1];
                            sumIndexList[k][0] = sumIndexList[k + 1][0];
                            sumIndexList[k][1] = sumIndexList[k + 1][1];
                            sumIndexList[k + 1][0] = temp[0];
                            sumIndexList[k + 1][1] = temp[1];
                        }
                    }
                }
            }
            */
           
            int max = vote.Max(); //select edge instance - that is, instance with the largest value
            index = Array.IndexOf(vote, max); //select index of edge instance

            //index = (int)sumIndexList[0][0]; //select the index of the first element in the array. The first index is the edge instance, that is, the index of the instance with the largest distance
            int[] NN = sortAndRetainIndex(dist[index]); // Sort distance (in ascending order) of edge instance, and retain their index.
            //select k nearest neighbours of edge instance
            int kNum = 30; //number of nearest neighbours
            //int kNum = trainDataset.Count; //number of nearest neighbours
            Node[][] edgeNN = new Node[kNum][]; //nearest neighbour to edge instances
            double[] labels = new double[kNum];
            int ctr = 0;
            
            for (int i = 0; i <= kNum; i++)
            {
                if (i == 0) //skip the first element, it is the edge instance. Its distance value is zero (the smallest or the top on the array)
                    continue;
                edgeNN[ctr] = trainDataset.X[NN[i]]; //select the nearest neighbours
                labels[ctr] = trainDataset.Y[NN[i]];
                ctr++;
            }
            
            /*
            for (int i = 0; i <= NN.Count(); i++)
            {
                if (i == 0) //skip the first element, since it is the edge instance. Its distance value is zero (the smallest or the top on the array)
                    continue;
                if (ctr2 < (int)Math.Round(0.9 * kNum) && trainDataset.Y[NN[i]] == -1) //selecting 90% ham emails
                {
                    edgeNN[ctr] = trainDataset.X[NN[i]]; //select the nearest neighbours
                    labels[ctr] = trainDataset.Y[NN[i]];
                    ctr++; ctr2++;
                }
                else if (ctr3 < (int)Math.Round(0.1 * kNum) && trainDataset.Y[NN[i]] == 1) //selecting 10% phishing emails
                {
                    edgeNN[ctr] = trainDataset.X[NN[i]]; //select the nearest neighbours
                    labels[ctr] = trainDataset.Y[NN[i]];
                    ctr++; ctr3++;
                }
                else if (ctr2 + ctr3 > kNum)
                    break;
            }
            */
            
            
            //ensure that there is at least 3 positive or negative instance in the list of selected instances
            int countP = labels.Count(q => q == 1); //count the total number of positive instances
            int countN = labels.Count(q => q == -1); //count the total number of negative instances
            ctr = 0;
            if (countP < 3 || countN < 3) //if list does not contain at least three posive or negative instances, add three to the list
            {
                if (countP < 3)
                {
                    for (int i = kNum; i < NN.Count(); i++) //Start searching from when i = KNum. Since K nearest instances are already on the list
                    {
                        if (trainDataset.Y[NN[i]] == 1 && ctr < 3) //add three closet positive instances to the list
                        {
                            edgeNN[ctr] = trainDataset.X[NN[i]];
                            labels[ctr] = trainDataset.Y[NN[i]];
                            ctr++;
                        }
                    }
                }
                else if (countN < 3)
                {
                    for (int i = kNum; i < NN.Count(); i++)
                    {
                        if (trainDataset.Y[NN[i]] == -1 && ctr < 3) //add three closest negative instances to the list
                        {
                            edgeNN[ctr] = trainDataset.X[NN[i]];
                            labels[ctr] = trainDataset.Y[NN[i]];
                            ctr++;
                        }
                    }
                }
            }
            

            //build subproblem - consisting of edge instance
            Problem subProb = new Problem(edgeNN.Length, labels, edgeNN, edgeNN[0].GetLength(0));

            return subProb;
        }

        public static Model buildModel(Problem prob, Parameter param)
        {
            Model model = new Model();
            model.Parameter = param;
            int nr_class = 2;
            int[] label = new int[nr_class];
            List<double> lab = new List<double>();
            int tot = prob.Count;
            int nPos = prob.Y.Count(a => a == 1);
            int nNeg = prob.Y.Count(a => a == -1);
            int i;

            if (nPos == 0 || nNeg == 0) //checking to ensure that there is at least one positive and negative class in the problem
            {
                if (nPos == 0) //come here if there is no positive instance in the dataset
                {
                    Console.Write("no positive instance in the dataset");
                    return null;
                }
                else
                {
                    Console.Write("no negative instance in the dataset"); //come here if there is no negative instance in the dataset
                    return null;
                }
            }
            else
            {
                //assign labels
                label[0] = 1;
                label[1] = -1;

                model.NumberOfClasses = nr_class;
                model.ClassLabels = new int[nr_class];
                for (i = 0; i < nr_class; i++)
                    model.ClassLabels[i] = label[i];

                //model.Rho = new double[nr_class * (nr_class - 1) / 2];
                //for (int i = 0; i < nr_class * (nr_class - 1) / 2; i++)
                 //   model.Rho[i] = f[i].rho;

                if (param.Probability)
                {
                    model.PairwiseProbabilityA = new double[nr_class * (nr_class - 1) / 2];
                    model.PairwiseProbabilityB = new double[nr_class * (nr_class - 1) / 2];
                    for (i = 0; i < nr_class * (nr_class - 1) / 2; i++)
                    {
                        model.PairwiseProbabilityA[i] = 0;
                        model.PairwiseProbabilityB[i] = 0;
                    }
                }
                else
                {
                    model.PairwiseProbabilityA = null;
                    model.PairwiseProbabilityB = null;
                }

                int nnz = tot; //number of support vectors
                int[] nz_count = new int[nr_class];
                model.NumberOfSVPerClass = new int[nr_class];
                for (i = 0; i < nr_class; i++)
                {
                    model.NumberOfSVPerClass[i] = tot;
                    nz_count[i] = tot;
                }

                Procedures.info("Total nSV = " + nnz + "\n");

                ParameterSelection ps = new ParameterSelection();
                File.AppendAllText(ps.filePath3, nnz.ToString());
                File.AppendAllText(ps.filePath3, Environment.NewLine);

                model.SupportVectorCount = nnz;
                model.SupportVectors = new Node[nnz][];
                model.SupportVectorsClassLabels = new double[nnz];
                int p = 0;
                for (i = 0; i < tot; i++)
                {
                    model.SupportVectors[p] = prob.X[i];
                    model.SupportVectorsClassLabels[p] = prob.Y[i];
                    p++;
                }

                int[] nz_start = new int[nr_class];
                nz_start[0] = 0;
                for (i = 1; i < nr_class; i++)
                    nz_start[i] = nz_start[i - 1] + nz_count[i - 1];

                /*
                model.SupportVectorCoefficients = new double[nr_class - 1][];
                for (i = 0; i < nr_class - 1; i++)
                    model.SupportVectorCoefficients[i] = new double[nnz];
              
                p = 0;
                for (i = 0; i < nr_class; i++)
                    for (int j = i + 1; j < nr_class; j++)
                    {
                        // classifier (i,j): coefficients with
                        // i are in sv_coef[j-1][nz_start[i]...],
                        // j are in sv_coef[i][nz_start[j]...]

                        int si = start[i];
                        int sj = start[j];
                        int ci = count[i];
                        int cj = count[j];

                        int q = nz_start[i];
                        int k;
                        for (k = 0; k < ci; k++)
                            if (nonzero[si + k])
                                model.SupportVectorCoefficients[j - 1][q++] = f[p].alpha[k];
                        q = nz_start[j];
                        for (k = 0; k < cj; k++)
                            if (nonzero[sj + k])
                                model.SupportVectorCoefficients[i][q++] = f[p].alpha[ci + k];
                        ++p;
                    }
                 */
            }
            return model;
        }

        //assign class pointers to instances. This function ensure that at least, N number of positive and negative class are selected in each instance mask
        public static int[] AssignClassPointersBinary(Problem prob, int probSize, int subsetSize)
        {
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers
            for (int i = 0; i < probSize; i++)
            {
                if (cnt1 < (0.7 * subsetSize) && prob.Y[rNum[i]] == -1) //select 70% positive instance of the subset
                {
                    pointers[cnt3] = rNum[i];
                    cnt1++; cnt3++;
                }
                else if (cnt2 < (0.3 * subsetSize) && prob.Y[rNum[i]] == 1)
                {
                    pointers[cnt3] = rNum[i];
                    cnt2++; cnt3++;
                }
                if (cnt3 >= subsetSize)
                    break;
            }

            return pointers;
        }

        //select class pointers for multiple class problem. This function ensure that each the class distribution in each instance mask is evenly distrinuted
        public static int[] AssignClassPointers_MultipleClass(Problem prob, int subsetSize, int probSize)
        {
            FireflyInstanceSelection fi = new FireflyInstanceSelection();

            Random rnd = new Random();
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers

            //xn = new int[subsetSize]; //instance mask
            //xn_Con = new double[subsetSize]; //instance mask continuous
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            
            List<double> classes = fi.getClassLabels(prob.Y); //get the class labels
            int nClass = classes.Count;
            int div = subsetSize / nClass;
            int[] classCount = new int[nClass];
            List<double> classList = new List<double>();

            int cnt = 0; //counter
            for (int b = 0; b < prob.Count; b++)
            {
                //int count = classList.Count(y => y == prob.Y[rNum[b]]); //count the number of occurence of this class
                int count = classList.Count(y => y == prob.Y[rNum[b]]); //count the number of occurence of this class
                if (count >= div)
                    continue;
                else
                {
                    //xn[cnt] = rnd.Next(0, 2);
                    pointers[cnt] = rNum[b];
                    //classList.Add(prob.Y[rNum[b]]);
                    classList.Add(prob.Y[rNum[b]]);
                    cnt++;
                }

                int totI = div * nClass; //total number of processed instances
                int rem = subsetSize % nClass; //total number of remaining instances to be processed
                if (cnt >= totI && rem != 0) //this is to ensure that the total number of processed instances is equal to subset size
                {
                    for (int i = 0; i < rem; i++)
                    {
                        //xn[cnt] = rnd.Next(0, 2);
                        pointers[cnt] = rNum[b + 1]; //add the next instance, since the b-th instance would have been added already
                        //classList.Add(prob.Y[rNum[b + 1]]);
                        classList.Add(prob.Y[rNum[b + 1]]);
                        cnt++;
                    }
                    break;
                }
                else if (cnt >= totI)
                    break;
            }

            return pointers;
        }
    }
}