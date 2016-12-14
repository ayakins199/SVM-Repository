using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    public class FireflyInstanceSelection
    {
        /// <summary>
        /// Default number of times to divide the data.
        /// </summary>
        public const int NFOLD = 5;
        /// <summary>
        /// Default minimum power of 2 for the C value (-5)
        /// </summary>
        public const int MIN_C = -5;
        //public const int MIN_C = -3;
        /// <summary>
        /// Default maximum power of 2 for the C value (15)
        /// </summary>
        public const int MAX_C = 15;
        //public const int MAX_C = 3;
        /// <summary>
        /// Default power iteration step for the C value (2)
        /// </summary>
        public const int C_STEP = 2;
        /// <summary>
        /// Default minimum power of 2 for the Gamma value (-15)
        /// </summary>
        public const int MIN_G = -15;
        //public const int MIN_G = -3;
        /// <summary>
        /// Default maximum power of 2 for the Gamma Value (3)
        /// </summary>
        public const int MAX_G = 3;
        //public const int MAX_G = 3;
        /// <summary>
        /// Default power iteration step for the Gamma value (2)
        /// </summary>
        public const int G_STEP = 2;
        /// <summary>
        /// Default weight for the SVM Classifier accuracy
        /// </summary>
        public const double W_SVM = 0.95;/// <summary>
        /// Default weight for the number of selected features
        /// </summary>
        public const double W_Features = 0.05;
        /// Default weight for the number of features
        /// </summary>
        public const int N_Firefly = 15;

        //get the class labels in the problem solved
        public List<double> getClassLabels(double[] labels)
        {
            List<double> lab = new List<double>();
            lab.Add(labels[0]);
            for (int i = 1; i < labels.Count(); i++)
            {
               if(!lab.Contains(labels[i]))
                   lab.Add(labels[i]);
            }

            return lab;
        }

        //build model for multi class problems
        public Problem buildModelMultiClass(ObjectInstanceSelection firefly, Problem prob)
        {
            int tNI = firefly.Attribute_Values.Count(); //size of each Instance Mask
            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();
            bool pos = false, neg = false;
            List<double> classes = getClassLabels(prob.Y); //get the class labels
            int nClass = classes.Count; //count the number of classes
            int[] classCount = new int[nClass];
            //building model for each instance in instance mask in each firefly object
            for (int j = 0; j < tNI; j++)
            {
                if (firefly.__Attribute_Values[j] == 1) //if instance is selected, use for classification
                {
                    int p = firefly.__Pointers[j];
                    x.Add(prob.X[p]);
                    y.Add(prob.Y[p]);

                    for (int i = 0; i < nClass; i++)
                    {
                        if (prob.Y[p] == classes[i])
                            classCount[i]++; //count the total number of instances in each class
                    }
                    
                }
                else
                    continue;
            }

            Node[][] X = new Node[x.Count][];
            double[] Y = new double[y.Count];

            //ensuring that the subproblem consist of both positive and negative instance
            int k = 0;
            if (classCount.Sum() == 0) //if the sum is zero, then no instance was selected
            {
                return null;
            }
            else //ensure that instance mask contains at least, one of each class instance
            {
                for (int a = 0; a < nClass; a++)
                {
                    if (classCount[a] == 0)
                    {
                        int m = 0;
                        for (int i = 0; i < prob.Count; i++) //if no instance in this class, search the whole subproblem and insert one instance in the kth position of subproblem
                        {
                            if (prob.Y[i] == classes[a])
                            {
                                x[k] = prob.X[i]; //insert negative instance in the first and second position
                                y[k] = prob.Y[i]; //insert label
                                k++; m++;
                            }
                            if (m == 2)
                                break;
                        }
                    }
                }
            }

            x.CopyTo(X); //convert from list to double[] array
            y.CopyTo(Y);
            Problem subProb = new Problem(X.Count(), Y, X, X[0].GetLength(0));
            return subProb;
        }


        //build model for binary problems
        public Problem buildModel(ObjectInstanceSelection firefly, Problem prob)
        {
            int tNI = firefly.Attribute_Values.Count(); //size of each Instance Mask
            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();
            bool pos = false, neg = false;
            
            //building model for each instance in instance mask in each firefly object
            for (int j = 0; j < tNI; j++)
            {
                if (firefly.__Attribute_Values[j] == 1) //if instance is selected, use for classification
                {
                    int p = firefly.__Pointers[j];
                    x.Add(prob.X[p]);
                    y.Add(prob.Y[p]);

                    if (prob.Y[p] == 1)
                        pos = true;
                    else if (prob.Y[p] == -1)
                        neg = true;
                }
                else
                    continue;
            }

            Node[][] X = new Node[x.Count][];
            double[] Y = new double[y.Count];

            //ensuring that the subproblem consist of both positive and negative instance
            int k = 0;
            int countP = y.Count(r => r == 1); //counting the total number of positive instance in the subpeoblem
            int countN = y.Count(r => r == -1); //counting the total number of negative instance in the subproble
            if (pos == false && neg == false) //if no instance (positive and negative) was selected, return null. Don't perform any computation
            {
                return null;
            }
            else if (pos == false || countP <= 1) //if pos == false, then no positive instance is in the subproblem
            {
                for (int i = 0; i < prob.Count; i++) //if no positive instance, search the whole subproblem and insert two postive instance in the first and second position of subproblem
                {
                    if (prob.Y[i] == 1)
                    {
                        x[k] = prob.X[i]; //insert negative instance in the first and second position
                        y[k] = prob.Y[i]; //insert label
                        k++;
                    }
                    if (k == 2)
                        break;
                }
            }
            else if (neg == false || countN <= 1) //if neg == false, then no negative instance is in the subproblem
            {
                k = 0;
                for (int i = 0; i < prob.Count; i++) //if no negative instance, search the whole subproblem and insert two negative instances in the first and second position of subproblem
                {
                    if (prob.Y[i] == -1)
                    {
                        x[k] = prob.X[i]; //insert negative instance in the first and second position
                        y[k] = prob.Y[i]; //insert label
                        k++;
                    }
                    if (k == 2)
                        break;
                }
            }

            x.CopyTo(X); //convert from list to double[] array
            y.CopyTo(Y);
            Problem subProb = new Problem(X.Count(), Y, X, X[0].GetLength(0));
            return subProb;
        }

        /// <summary>
        /// Evaluate Objective Function
        /// </summary>
        //public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> fireflies, List<double> accuracy, Problem prob)
        public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> fireflies, Problem prob)
        {
            int NF = fireflies.Count; //NF -> number of fireflies
            int tNI = fireflies.ElementAt(0).Attribute_Values.Count(); //size of each Instance Mask
            double[] fitness = new double[NF];
            int sum;
            
            
            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();
            
            double C, Gamma;

            for (int i = 0; i < NF; i++)
            {
                

                //building model for each instance in instance mask in each firefly object
                Problem subProb = buildModel(fireflies.ElementAt(i), prob);

                Parameter param = new Parameter();
                if (subProb != null)
                {
                    int countP = subProb.Y.Count(k => k == 1); //counting the total number of positive instance in the subpeoblem
                    int countN = subProb.Y.Count(k => k == -1); //counting the total number of negative instance in the subproblem

                    if (countN <= 1 || countP <= 1) //ensuring that there are at least two positive or negative instance in a subproblem
                    {
                        int m = 0;
                        if (countN <= 1)
                        {
                            for (int k = 0; k < prob.Count; k++) //if no negative instance, search the whole subproblem and insert two postive instance in the first and second position of subproblem
                            {
                                if (prob.Y[k] == -1)
                                {
                                    subProb.X[m] = prob.X[k]; //insert negative instance in the first and second position
                                    subProb.Y[m] = prob.Y[k]; //insert label
                                    m++;
                                }
                                if (m == 2)
                                    break;
                            }
                        }
                        else if (countP <= 1)
                        {
                            for (int k = 0; k < prob.Count; k++) //if no positive instance, search the whole subproblem and insert two postive instance in the first and second position of subproblem
                            {
                                if (prob.Y[k] == 1)
                                {
                                    subProb.X[m] = prob.X[k]; //insert negative instance in the first and second position
                                    subProb.Y[m] = prob.Y[k]; //insert label
                                    m++;
                                }
                                if (m == 2)
                                    break;
                            }
                        }
                    }

                    Problem subP = Training.ClusteringBoundaryInstance(subProb);

                    int c = subP.Count;

                    int count = fireflies.ElementAt(i).__Attribute_Values.Count(q => q == 1); //total number of selected instances, to be used for subsetSize
                    double percentageReduction = 100 * (tNI - count) / tNI; //calculating percentage reduction for each instance Mask
                    fitness[i] = percentageReduction;
                    
                    
                    /*
                    ParameterSelection.Grid(subProb, param, "params.txt", out C, out Gamma); //select parameters for each subset
                    param.C = C;
                    param.Gamma = Gamma;
                    Model subModel = Training.Train(subProb, param); //train each subset
                    double accr = Prediction.Predict(prob, "ClassificationResults.txt", subModel, false); //use each subset to classify train dataset
                    sum = 0;
                    for (int j = 0; j < tNI; j++)
                        sum += fireflies.ElementAt(i).Attribute_Values[j];

                    fitness[i] = W_SVM * accr + W_Features * (double)(1 - ((double)sum / (double)tNI)); //fitness evaluation for individual firefly
                    //fitness[i] = accuracy[i] + W_Features * (double)(1 - ((double)sum / (double)tNFe)); //fitness evaluation for individual firefly
                     */

                    /*
                for (int j = 0; j < tNI; j++)
                {
                    if (fireflies.ElementAt(i).__Attribute_Values[j] == 1) //if instance is selected, use for classification
                    {
                        int p = fireflies.ElementAt(i).__Pointers[j];
                        x.Add(prob.X[p]);
                        y.Add(prob.Y[p]);
                    }
                    else
                        continue;
                }

                Node[][] X = new Node[x.Count][];
                double[] Y = new double[y.Count];

                x.CopyTo(X); //convert from list to double[] array
                y.CopyTo(Y);

                
                Problem subProb = new Problem(X.Count(), Y, X, X[0].GetLength(0));
                */
                }
            }

            return fitness;
        }

        /// <summary>
        /// Main part of the Firefly Algorithm
        /// </summary>
        //public Problem firefly_simple(List<double> avgAcc, List<double> CValues, List<double> GValues, Problem prob)
        public Problem firefly_simple(Problem prob, out double storagePercentage)
        {

            //int nF = 9; //number of instances
            int nI = prob.X.Count(); //total number of instance in dataset
            int nFF = 5; //number of fireflies. Note: NFF * subsetsize must not be greater than Size of training dataset
            int subsetSize = 100; //size of each firefly Instance Mask
            int MaxGeneration = 5; //number of pseudo time steps

            int[] range = new int[4] { -5, 5, -5, 5 }; //range=[xmin xmax ymin ymax]

            double alpha = 0.2; //Randomness 0--1 (highly random)
            double gamma = 1.0; //Absorption coefficient

            int[] xn = new int[subsetSize];
            double[] xo = new double[subsetSize];
            double[] Lightn = new double[nFF];
            double[] Lighto = new double[nFF];

            double[] fitnessVal = new double[nFF];
            double globalbestIntensity;
            ObjectInstanceSelection globalBest = null;
            

            //generating the initial locations of n fireflies
            List<ObjectInstanceSelection> fireflies = init_ffa(nFF, subsetSize, nI, prob);

            ObjectInstanceSelection[] fireflyBackup = new ObjectInstanceSelection[fireflies.Count];
            ObjectInstanceSelection[] fireflyBest = new ObjectInstanceSelection[fireflies.Count];
            List<int> changedIndex = new List<int>(); //changedIndex keeps track of the index of fireflies that has been moved
            double newBestIntensity = new double();
            int maxIndex;
            bool stopSearch = false; //stopsearch is will be set to true when the a firefly with classification accuracy = 100 is found.

            globalbestIntensity = double.MinValue;

            //Iterations or pseudo time marching
            for (int i = 0; i < MaxGeneration; i++)
            {
                //Evaluate objective function
                fitnessVal = this.EvaluateObjectiveFunction(fireflies, prob); //evaluate objective function for each firefly

                //stop searching if firefly has found the best c and G value that yields 100%
                for (int t = 0; t < fitnessVal.Count(); t++)
                {
                    //double predAccr = avgAcc[changedIndex[t]] * 100;
                    double predAccr = fitnessVal[t] * 100;
                    if (predAccr == 100) //if prediction accuracy is equal to 100, stop searching and select the firefly that gives this accuracy
                    {
                        globalBest = fireflies[changedIndex[t]];
                        stopSearch = true;
                        break;
                    }
                }

                //stop searching if firefly has found the best c and G value that yields 100%
                if (stopSearch == true)
                    break;

                //fitnessVal = this.EvaluateObjectiveFunction(fireflies, avgAcc, prob); //evaluate objective function for each firefly
                newBestIntensity = fitnessVal.Max(); //get the firefly with the highest light intensity
                if (newBestIntensity > globalbestIntensity)
                {
                    globalbestIntensity = newBestIntensity;
                    maxIndex = Array.IndexOf(fitnessVal, newBestIntensity); //select the index for the global best
                    globalBest = fireflies[maxIndex]; //select the global best firefly
                    //bestC = (double)fireflies[maxIndex].cValue; //save the C value for the global best
                    //bestGamma = (double)fireflies[maxIndex].GValue; //save the Gamma for the global best
                }

                fireflies.CopyTo(fireflyBackup); fitnessVal.CopyTo(Lighto, 0); fitnessVal.CopyTo(Lightn, 0); //creating duplicates
                //Lightn.CopyTo(Lighto, 0);

                changedIndex.Clear(); 
                ffa_move(Lightn, fireflyBackup, Lighto, alpha, gamma, fireflies, prob);

                fireflies.CopyTo(fireflyBackup); //backing up the current positions of the fireflies
                Lightn.CopyTo(Lighto, 0); //backing up the current intensities of the fireflies

            }

            //ensure that at least, 40 instances is selected for classification
            int countSelected = globalBest.__Attribute_Values.Count(q => q == 1); //count the total number of selected instances
            int diff, c = 0, d = 0; 
            int Min = 15; //minimum number of selected instances
            if (countSelected < Min)
            {
                diff = Min - countSelected;
                //if there are less than 40, add N instances, where N = the number of selected instances and 40
                while (c < diff)
                {
                    if (globalBest.__Attribute_Values[d++] == 1) 
                        continue;
                    else
                    {
                        globalBest.__Attribute_Values[d++] = 1;
                        c++;
                    }
                }
            }

            Problem subBest = buildModelMultiClass(globalBest, prob); //model for the best Instance Mast
            storagePercentage = Training.StoragePercentage(subBest, prob); //calculate the percent of the original training set was retained by the reduction algorithm

            return subBest;
        }

        

        /// <summary>
        /// generating the initial locations of n fireflies
        /// </summary>
        public List<ObjectInstanceSelection> init_ffa(int nFF, int subsetSize, int probSize, Problem prob)
        {
            Random rnd = new Random();// Random rx = new Random(); Random ry = new Random();
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers

            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            //create an array of size n for x and y
            int[] xn = new int[subsetSize]; //instance mask
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            int k = 0;
            for (int i = 0; i < nFF; i++)
            {
                xn = new int[subsetSize];
                pointers = new int[subsetSize];
                cnt1 = 0; cnt2 = 0; cnt3 = 0;
                for (int j = 0; j < prob.Count; j++)
                {
                    if (cnt1 < (0.7 * subsetSize) && prob.Y[j] == 1) //select 70% positive instance of the subset
                    {
                        xn[cnt3] = rnd.Next(0, 2);
                        pointers[cnt3] = rNum[k];
                        k++; cnt1++; cnt3++;
                    }
                    else if (cnt2 < (0.3 * subsetSize) && prob.Y[j] == -1)
                    {
                        xn[cnt3] = rnd.Next(0, 2);
                        pointers[cnt3] = rNum[k];
                        k++; cnt2++; cnt3++;
                    }
                    if (cnt3 >= subsetSize)
                        break;
                }

                ObjectInstanceSelection OI = new ObjectInstanceSelection(0.0, 0.0, xn, pointers);
                attr_values.Add(OI);
            }

            return attr_values;
        }


        /// <summary>
        /// Move all fireflies toward brighter ones
        /// </summary>
        //public void ffa_move(double[] Lightn, ObjectInstanceSelection[] fireflies0, double[] Lighto, double alpha, double gamma, List<ObjectInstanceSelection> fireflies,
        //                      Problem prob, Parameter param, List<double> avgAcc, List<int> changedIndex)
        public void ffa_move(double[] Lightn, ObjectInstanceSelection[] fireflies0, double[] Lighto, double alpha, double gamma, List<ObjectInstanceSelection> fireflies,
                              Problem prob)
        {

            int nFF = fireflies.Count; //number of fireflies
            double rC, rG, rF; //rC -> distance for C value, rG-> distance for Gamma value, rF - distance for the feature mask
            double beta0;
            double beta; // beta -> attrativeness value for C and G, betaF -> attrativeness for the feature mask

            //specifying the ranges for C and Gamma
            double minC = Math.Pow(2, MIN_C); // minimum value for C
            double maxC = Math.Pow(2, MAX_C); // maximum value for C
            double minG = Math.Pow(2, MIN_G); // minimum value for G
            double maxG = Math.Pow(2, MAX_G); // maximum value for G

            int subsetSize = fireflies[0].Attribute_Values.Count(); //size of Instance Mask

            double[] CBackup = new double[fireflies.Count]; //back up array for C value
            double[] GammaBackup = new double[fireflies.Count]; ////back up array for Gamma value
            double val; 
            
            Random rnd = new Random(); 
            Random rx = new Random();
            Random ry = new Random();
            duplicateValue(fireflies, CBackup, GammaBackup);
            for (int i = 0; i < nFF; i++)
            {
                for (int j = 0; j < nFF; j++)
                {
                    if (j == i) //avoid comparism with the same element
                        continue;
                    rF = 0.0;

                    rC = Math.Pow(((double)fireflies[i].cValue - (double)fireflies0[j].cValue), 2);
                    rG = Math.Pow(((double)fireflies[i].GValue - (double)fireflies0[j].GValue), 2);
                    double r = Math.Sqrt(rC + rG); //r -> total distance for both C and Gamma

                    if (Lightn[i] < Lighto[j])
                    {
                        beta0 = 1; //setting beta to 1
                        beta = beta0 * Math.Exp(-gamma * Math.Pow(r, 2)); //The attractiveness parameter for C and Gamma -> beta=exp(-gamma*r)
                        double rand = rnd.NextDouble();

                        //changing firefly i position for the continuous values - i.e C and Gamma value respectively
                        fireflies[i].cValue = ((double)fireflies[i].cValue * (1 - beta)) + (CBackup[j] * beta) + (alpha * (rnd.NextDouble() - 0.5));
                        fireflies[i].GValue = ((double)fireflies[i].GValue * (1 - beta)) + (GammaBackup[j] * beta) + (alpha * (rnd.NextDouble() - 0.5));

                        //move the individual position of each instance mask
                        for (int k = 0; k < subsetSize; k++)
                        {
                            val = ((double)fireflies[i].__Attribute_Values[k] * (1 - beta)) + (GammaBackup[j] * beta) + (alpha * (rand - 0.5)); //moving position of firefly
                            fireflies[i].__Attribute_Values[k] = Binarize(val, rand); //convert from discrete to binary
                        }
                                                
                        findrange(fireflies[i], minC, maxC, minG, maxG); //restrict the values of C and Gamma to the specified range
                    }
                }
                //if ((double)fireflies[i].cValue != CBackup[i] || (double)fireflies[i].GValue != GammaBackup[i])
                //    changedIndex.Add(i); //saving the index of the firefly that has been moved for the purpose of accuracy calculation. This to reduce the number of computations
            }

            //calculate the new accuracy for the newly updated C and Gamma value
            //ParameterSelection.Grid(prob, param, fireflies, changedIndex, avgAcc, CBackup, GammaBackup, NFOLD);
        }

        /// <summary>
        /// Create a duplicate of C and Gamma Values
        /// </summary>
        public void duplicateValue(List<ObjectInstanceSelection> fireflies, double[] CBackup, double[] GammaBackup)
        {
            for (int i = 0; i < fireflies.Count; i++)
            {
                CBackup[i] = fireflies[i].__cValue;
                GammaBackup[i] = fireflies[i].__GValue;
            }
        }

        /// <summary>
        /// This method ensures that the C and Gamma values do not go beyond specified range
        /// </summary>
        public void findrange(ObjectInstanceSelection fireflies, double minC, double maxC, double minG, double maxG)
        {

            if ((double)fireflies.cValue <= minC)
                fireflies.cValue = minC;
            if ((double)fireflies.cValue >= maxC)
                fireflies.cValue = maxC;
            if ((double)fireflies.GValue <= minG)
                fireflies.GValue = minG;
            if ((double)fireflies.GValue >= maxG)
                fireflies.GValue = maxG;
        }

        //Convert from continuous to discrete value
        public int Binarize(double val, double rand)
        {
            double v = 2*Math.Abs(val);
            double sigmoidFunction = Math.Exp(v) - 1 / Math.Exp(v) + 1;
            sigmoidFunction = Math.Tanh(Math.Abs(sigmoidFunction));
            int retVal = new int();

            if (rand < sigmoidFunction)
                retVal = 1;
            else
                retVal = 0;

            return retVal;
        }

        public object[,] buildObject(ref int ctr, ref int cntr1, ref int cntr2, int i, int j, object[,] obj, Problem trainDataset, double propP, double propN)
        {
            
            //ctr = a; cntr1 = b; cntr2 = c;
            double distance; 
            if (cntr1 < propP && trainDataset.Y[j] == 1) //compute distance for positive class (90% of k goes for positive instances)
            {
                distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                obj[ctr, 0] = distance; obj[ctr, 1] = trainDataset.X[j]; obj[ctr, 2] = trainDataset.Y[j]; //save the instance and their corresponding distances
                ctr++;  cntr1++;
            }
            else if (trainDataset.Y[j] == -1 && cntr2 < propN) //compute distance for negative class (10% of k goes for negative instances)
            {
                distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                obj[ctr, 0] = distance; obj[ctr, 1] = trainDataset.X[j]; obj[ctr, 2] = trainDataset.Y[j]; //save the instance and their corresponding distances
                ctr++; cntr2++;
            }

            return obj;
        }

        //compute the k-nearest neighbour of all instances in the dataset
        public Problem computeNearestNeighbour(int k, Problem trainDataset, int numOfSubset)
        {
            double sum = 0; double distance;
            int n = trainDataset.Count; //number of data instances
            List<Node[]> nearestNeighbours = new List<Node[]>(); List<double> dist = new List<double>(); List<double> labels = new List<double>();
            Node[] xNodes = new Node[n];
            Node[] yNodes = new Node[n];
            object[,] obj = new object[n-1, 3]; 
            //object[,] obj = new object[k, 3];
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
                    if (countN <= 1) //come here if we have very few selected negative instance in the subproblem
                    {
                        double propP = n * 0.9, propN = n * 0.1;
                        obj = buildObject(ref ctr, ref cntr1, ref cntr2, i, j, obj, trainDataset, propP, propN); //0.9 and 0.1 are proportion of positive and negative instances to be selected
                        //ctr++; cntr1++; cntr2++;
                    }
                    else if (countP <= 1) //come here if we have very few selected positive instance
                    {
                        double propP = n * 0.1, propN = n * 0.9;
                        obj = buildObject(ref ctr, ref cntr1, ref cntr2, i, j, obj, trainDataset, propP, propN); //0.1 and 0.9 are proportion of positive and negative instances to be selected
                        
                    }
                    else if(n > trainDataset.Count) //come here of n is more than the total number of selected instances
                    {
                        double propP = countP, propN = trainDataset.Count - countP; //in this case, selected instances consist of all the positive instance and a portion of negative instance
                        obj = buildObject(ref ctr, ref cntr1, ref cntr2, i, j, obj, trainDataset, propP, propN);
                        //ctr++; cntr1++; cntr2++;
                    }
                    else if (countN < (n * 0.7) || countP < (n * 0.3)) //come here if the selected positive or negative instances is less than the defined proportion
                    {
                        if (countP < (n * 0.3))
                        {
                            double propP = countP, propN = n - countP; //in this case, selected instances consist of all the positive instance and a portion of negative instance
                            obj = buildObject(ref ctr, ref cntr1, ref cntr2, i, j, obj, trainDataset, propP, propN); 
                        }
                        else if (countN < (n * 0.7))
                        {
                            double propP = n - countN, propN = countN; //in this case, selected instances consist of all the positive instance and a portion of negative instance
                            obj = buildObject(ref ctr, ref cntr1, ref cntr2, i, j, obj, trainDataset, propP, propN); 
                        }
                    }
                    else //come here if we have fairly good distribution of positive and negative instances
                    {
                        double propP = n * 0.3, propN = n * 0.7;
                        obj = buildObject(ref ctr, ref cntr1, ref cntr2, i, j, obj, trainDataset, propP, propN); //0.3 and 0.7 are proportion of positive and negative instances to be selected
                    }
                }

                Training.sortMultiArray(obj); //sort array to select the nearest neighbour of Xi
                
                //select the k-neareast neighbours (using top K elements), their corresponding distances and class labels of Xi
                //int subK = 30; 
                int subK = k;
                int count1 = 0; int count2 = 0; int sumN = 0, sumP = 0;
                for (int z = 0; z < obj.GetLength(0); z++) //count the total number of positive and negative instances in the subproblem
                {
                    if ((double)obj[z, 2] == 1)
                        sumP++;
                    else
                        sumN++;
                }
                for (int p = 0; p < k; p++) //select k-neareast neighbours (using top K elements), their corresponding distances and class labels of Xi
                {
                    if (count1 < sumP && (double)obj[p, 2] == 1) //NN for positive class
                    {
                        dist.Add((double)obj[p, 0]); //distance
                        nearestNeighbours.Add((Node[])obj[p, 1]); //nearest neighbour i
                        labels.Add((double)obj[p, 2]); //class labels
                        count1++;
                    }
                    else if (count2 < sumN && (double)obj[p, 2] == -1) // NN for negative class
                    {
                        dist.Add((double)obj[p, 0]); //distance
                        nearestNeighbours.Add((Node[])obj[p, 1]); //nearest neighbour i
                        labels.Add((double)obj[p, 2]); //class labels
                        count2++;
                    }
                }

                //for (int z = 0; z < obj.Length; z++)

                nn[i, 0] = k; nn[i, 1] = dist; nn[i, 2] = nearestNeighbours; nn[i, 3] = trainDataset.X[i]; nn[i, 4] = labels; nn[i, 5] = trainDataset.Y[i];

                //Compute Exponential Decay
                double EDScore = 0; //Exponential decay score
                int counter = 0;
                for (int p = 0; p < subK; p++)
                {
                    //compute exponential decay for Xi and all its Nearest neighbour belonging to the opposite class
                    //if the label of the current instance in the neighbourhood list is not equal to the label of ith instance then compute its Exponential Decay Score
                    if (((List<double>)nn[i, 4])[p] != (double)nn[i, 5])//identify the nearest neighbour belonging to the opposite class
                    {
                        EDScore += ((List<double>)nn[i, 1])[p] - Math.Pow(((List<double>)nn[i, 1])[p], 2); //compute exponential decay score
                        counter++; //counting the number of contributors
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
            Training.sortMultiArray(scoreList); //sort scores to select the best N instances to be used for training

            //select data subset to be used for training. Selected subset are instances that are closest to the data boundary
            Node[][] xScoreList = new Node[numOfSubset][];
            double[] yScoreList = new double[numOfSubset];
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            int total = n - 1;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    dataSubset[i, j] = scoreList[total, j]; //select instances with the highest scores
                }
                if (cnt1 < (0.1 * numOfSubset) && (double)dataSubset[i, 2] == 1) //select 70% positive instance of the subset
                {
                    xScoreList[cnt3] = (Node[])dataSubset[i, 1];
                    yScoreList[cnt3] = (double)dataSubset[i, 2];
                    cnt1++; cnt3++;
                }
                else if (cnt2 < (0.9 * numOfSubset) && (double)dataSubset[i, 2] == -1) //select 30% negative instance of the subset
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
    }
}
