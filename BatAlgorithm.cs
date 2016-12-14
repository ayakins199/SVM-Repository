using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestSimpleRNG;

namespace SVM
{
    //this is an implentation of bat algorithm, proposed by Yang. 
    //Reference 1: https://www.mathworks.com/matlabcentral/fileexchange/37582-bat-algorithm--demo-/content/bat_algorithm.m
    //Reference 2: https://www.mathworks.com/matlabcentral/fileexchange/44707-binary-bat-algorithm/content/BBA/BBA.m
    public class BatAlgorithm
    {
        FireflyInstanceSelection fi = new FireflyInstanceSelection();
        public Problem Bat(Problem prob)
        {
            //default parameters
            int populationSize = 5; //number of bats in the population
            int maxGeneration = 100;
            int subsetSize = 200;
            double loudness = 0.5;
            double pulseRate = 0.5;
            int totalInstances = prob.X.Count(); //problem size
            double frequencyMin = 0; //minimum frequency. Frequency range determine the scalings
            double frequencyMax = 2; //maximum frequency. 
            int lowerBound = -2; //set lower bound - lower boundary
            int upperBound = 2; //set upper bound - upper boundary
            double[] batFitnessVal = new double[populationSize];
            double[] newbatFitnessVal = new double[populationSize];
            double globalBest = double.MinValue;
            ObjectInstanceSelection globalBestBat = null;
            Random r = new Random();

            //initialize population
            List<ObjectInstanceSelection> bats = InitializeBat(populationSize, subsetSize, totalInstances, prob);
            List<ObjectInstanceSelection> newBats = new List<ObjectInstanceSelection>(bats.Count); //create a clone of bats
            bats.ForEach((item) =>
            {
                newBats.Add(new ObjectInstanceSelection(item.__Attribute_Values, item.__Attribute_Values_Continuous, item.__Frequency, item.__Velocity, item.__Pointers, item.__Fitness)); //create a clone of flowers
            });

            batFitnessVal = fi.EvaluateObjectiveFunction(bats, prob); //evaluate fitness value for all the bats
            newbatFitnessVal = fi.EvaluateObjectiveFunction(newBats, prob); //evaluate fitness value for new bats. Note: this will be the same for this function call, since pollination has not occur
            BatFitness(batFitnessVal, bats); //fitness value for each bats
            BatFitness(newbatFitnessVal, newBats); //fitness value for new bats
            globalBestBat = EvaluateSolution(batFitnessVal, newbatFitnessVal, globalBest, bats, newBats, globalBestBat, loudness); //get the global best flower
            globalBest = globalBestBat.__Fitness;

            //start bat algorithm
            double rand = r.NextDouble(); //generate random number
            for (int i = 0; i < maxGeneration; i++)
            {
                //loop over all bats or solutions
                for (int j = 0; j < populationSize; j++)
                {
                    bats[j].__Frequency = frequencyMin + (frequencyMin - frequencyMax) * rand; //adjust frequency
                    for (int k = 0; k < subsetSize; k++)
                    {
                        double randNum = SimpleRNG.GetNormal();//generate random number with normal distribution
                        newBats[j].__Velocity[k] = bats[j].__Velocity[k] + (bats[j].__Attribute_Values_Continuous[k] - globalBestBat.Attribute_Values_Continuous[k]) * bats[j].__Frequency; //update velocity
                        newBats[j].__Attribute_Values_Continuous[k] = bats[j].__Attribute_Values_Continuous[k] + bats[j].__Velocity[k]; //update bat position in continuous space
                        newBats[j].__Attribute_Values_Continuous[k] = SimpleBounds(newBats[j].__Attribute_Values_Continuous[k], lowerBound, upperBound); //ensure that value does not go beyond defined boundary

                        if (rand > pulseRate) //The factor 0.001 limits the step sizes of random walks 
                            newBats[j].__Attribute_Values_Continuous[k] = globalBestBat.Attribute_Values_Continuous[k] + 0.001 * randNum;

                        newBats[j].__Attribute_Values[k] = fi.Binarize(newBats[j].__Attribute_Values_Continuous[k], r.NextDouble()); //convert to binary
                    }

                }

                //evaluate new solution
                newbatFitnessVal = fi.EvaluateObjectiveFunction(newBats, prob); //evaluate fitness value for all the bats
                BatFitness(newbatFitnessVal, newBats); //fitness value for new bats
                globalBestBat = EvaluateSolution(batFitnessVal, newbatFitnessVal, globalBest, bats, newBats, globalBestBat, loudness); //get the global best flower
                globalBest = globalBestBat.__Fitness;
            }

            //ensure that at least, 40 instances is selected for classification
            int countSelected = globalBestBat.__Attribute_Values.Count(q => q == 1); //count the total number of selected instances
            int diff, c = 0, d = 0;
            int Min = 40; //minimum number of selected instances
            if (countSelected < Min)
            {
                //if there are less than N, add N instances, where N = the number of selected instances 
                diff = Min - countSelected;
                while (c < diff)
                {
                    if (globalBestBat.__Attribute_Values[d++] == 1)
                        continue;
                    else
                    {
                        globalBestBat.__Attribute_Values[d++] = 1;
                        c++;
                    }
                }
            }

            Problem subBest = fi.buildModel(globalBestBat, prob); //build model for the best Instance Mast
            return subBest;
        }

        //Binary Bat
        public Problem BinaryBat(Problem prob, out double storagePercentage)
        {
            //default parameters
            int populationSize = 3; //number of bats in the population
            int subsetSize = 100;
            int maxGeneration = 3;
            double loudness = 0.5;
            double pulseRate = 0.5;
            int totalInstances = prob.X.Count(); //problem size
            double frequencyMin = 0; //minimum frequency. Frequency range determine the scalings
            double frequencyMax = 2; //maximum frequency. 
            int lowerBound = -2; //set lower bound - lower boundary
            int upperBound = 2; //set upper bound - upper boundary
            double[] batFitnessVal = new double[populationSize];
            double[] newbatFitnessVal = new double[populationSize];
            double globalBest = double.MinValue;
            ObjectInstanceSelection globalBestBat = null;
            Random r = new Random();
            FlowerPollinationAlgorithm fpa = new FlowerPollinationAlgorithm();

            //initialize population
            List<ObjectInstanceSelection> bats = InitializeBinaryBat(populationSize, subsetSize, totalInstances, prob);
            List<ObjectInstanceSelection> newBats = new List<ObjectInstanceSelection>(bats.Count); //create a clone of bats
            bats.ForEach((item) =>
            {
                newBats.Add(new ObjectInstanceSelection(item.Attribute_Values, item.Attribute_Values_Continuous, item.Frequency, item.Velocity, item.Pointers, item.Fitness)); //create a clone of flowers
            });

            batFitnessVal = EvaluateObjectiveFunction(bats, prob); //evaluate fitness value for all the bats
            newbatFitnessVal = EvaluateObjectiveFunction(newBats, prob); //evaluate fitness value for new bats. Note: this will be the same for this function call, since pollination has not occur
            BatFitness(batFitnessVal, bats); //fitness value for each bats
            BatFitness(newbatFitnessVal, newBats); //fitness value for new bats
            globalBestBat = EvaluateSolution(batFitnessVal, newbatFitnessVal, globalBest, bats, newBats, globalBestBat, loudness); //get the global best flower
            globalBest = globalBestBat.Fitness;

            //start bat algorithm
            double rand = r.NextDouble(); //generate random number
            for (int i = 0; i < maxGeneration; i++)
            {
                //loop over all bats or solutions
                for (int j = 0; j < populationSize; j++)
                {
                    for (int k = 0; k < subsetSize; k++)
                    {
                        bats[j].Frequency = frequencyMin + (frequencyMin - frequencyMax) * r.NextDouble(); //Adjust frequency
                        double randNum = SimpleRNG.GetNormal();//generate random number with normal distribution
                        newBats[j].Velocity[k] = newBats[j].Velocity[k] + (bats[j].Attribute_Values[k] - globalBestBat.Attribute_Values[k]) * bats[j].Frequency; //update velocity
                        //newBats[j].Attribute_Values[k] = fpa.ConvertToBinary(newBats[j].Velocity[k], newBats[j].Attribute_Values[k]); //update bat position in the binary space
                        newBats[j].Attribute_Values[k] = TransferFunction(newBats[j].Velocity[k], newBats[j].Attribute_Values[k]); //update bat position in the binary space

                        if (rand > pulseRate)
                            newBats[j].Attribute_Values[k] = globalBestBat.Attribute_Values[k]; //change some of the dimensions of the position vector with some dimension of global best. Refer to reference for more explaination
                    }
                }

                //Select best solutions from the original population and matured population for the next generation;
                fpa.SelectBestSolution(bats, newBats);

                //evaluate new solution
                newbatFitnessVal = EvaluateObjectiveFunction(newBats, prob); //evaluate fitness value for all the bats
                BatFitness(newbatFitnessVal, newBats); //fitness value for new bats
                globalBestBat = EvaluateSolution(batFitnessVal, newbatFitnessVal, globalBest, bats, newBats, globalBestBat, loudness); //get the global best flower
                globalBest = globalBestBat.Fitness;

                //if solution has converged to a optimal user-defined point, stop search
                int Max = 60;// maximum percentage reduction
                if (globalBest >= Max) //if the percentage reduction has approached 60%, stop search!
                    break;
            }

            //ensure that at least, N instances are selected for classification
            int min = 15; //minimum number of selected instances
            globalBestBat = fpa.AddInstances(globalBestBat, min);

            Problem subBest = fi.buildModelMultiClass(globalBestBat, prob); //build model for the best Instance Mast
            storagePercentage = Training.StoragePercentage(subBest, prob); //calculate the percent of the original training set was retained by the reduction algorithm
            return subBest;
        }

        /// <summary>
        /// Evaluate Objective Function
        /// </summary>
        public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> Bats, Problem prob)
        {
            int NB = Bats.Count; //NF -> number of fireflies
            int tNI = Bats.ElementAt(0).Attribute_Values.Count(); //size of each Instance Mask
            double[] fitness = new double[NB];
            int sum;


            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();

            double C, Gamma;

            for (int i = 0; i < NB; i++)
            {
                //building model for each instance in instance mask in each firefly object
                Problem subProb = fi.buildModel(Bats.ElementAt(i), prob);

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
                    int count = Bats.ElementAt(i).__Attribute_Values.Count(q => q == 1); //total number of selected instances, to be used for subsetSize
                    double perRedBInstances = (double)(subProb.Count / subP.Count); //percentage reduction for boundary instances
                    double perRedCuckooInstances = (double)(tNI - count) / tNI; //percentage reduction for cuckoo instances
                    //fitness[i] = (100 * perRedCuckooInstances);
                    fitness[i] = (100 * perRedCuckooInstances) + perRedBInstances;
                }
            }

            return fitness;
        }

        //transfer function for movement of bats in binary space. Proposed by authors in: http://link.springer.com/article/10.1007%2Fs00521-013-1525-5
        public int TransferFunction(double velocity, int batPosition)
        {
            Random rand = new Random();
            double A = 2 / Math.PI;
            double B = Math.PI / 2;
            double C = Math.Atan(B * velocity);
            double D = Math.Abs(A * C);

            if (rand.NextDouble() < D)
            {
                if (batPosition == 0)
                    batPosition = 1;
                else if (batPosition == 1)
                    batPosition = 0;
            }

            //if (rNum < velocity)
            //    batPosition = ~batPosition; //~ denotes the compliment operator

            return batPosition;
        }

        /// <summary>
        /// generating the initial locations of n flower
        /// </summary>
        public List<ObjectInstanceSelection> InitializeBat(int nBats, int subsetSize, int probSize, Problem prob)
        {
            Random rnd = new Random();
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers
            FireflyInstanceSelection fpa = new FireflyInstanceSelection();

            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            //create an array of size n for x and y
            int[] xn = new int[subsetSize]; //instance mask
            double[] xn_Con = new double[subsetSize]; //instance mask continuous
            double freq = new double(); //initialize the frequency of all the bats to zero
            double[] vel = new double[subsetSize]; //initialize the velocity of all the bats to zero
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            for (int i = 0; i < nBats; i++)
            {
                xn = new int[subsetSize];
                xn_Con = new double[subsetSize];
                pointers = new int[subsetSize];
                cnt1 = 0; cnt2 = 0; cnt3 = 0;
                for (int j = 0; j < prob.Count; j++)
                {
                    if (cnt1 < (0.7 * subsetSize) && prob.Y[rNum[j]] == -1) //select 70% positive instance of the subset
                    {
                        xn_Con[cnt3] = rnd.NextDouble();
                        xn[cnt3] = fpa.Binarize(xn_Con[cnt3], rnd.NextDouble()); //convert generated random number to binary
                        pointers[cnt3] = rNum[j];
                        cnt1++; cnt3++;
                    }
                    else if (cnt2 < (0.3 * subsetSize) && prob.Y[rNum[j]] == 1)
                    {
                        xn_Con[cnt3] = rnd.NextDouble();
                        xn[cnt3] = fpa.Binarize(xn_Con[cnt3], rnd.NextDouble()); //convert generated random number to binary
                        pointers[cnt3] = rNum[j];
                        cnt2++; cnt3++;
                    }
                    if (cnt3 >= subsetSize)
                        break;
                }

                ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, freq, vel, pointers, 0.0);
                attr_values.Add(OI);
            }

            return attr_values;
        }

        /// <summary>
        /// generating the initial locations of n bats
        /// </summary>
        public List<ObjectInstanceSelection> InitializeBinaryBat(int nBats, int subsetSize, int probSize, Problem prob)
        {
            Random rnd = new Random();
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers
            FireflyInstanceSelection fpa = new FireflyInstanceSelection();

            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            //create an array of size n for x and y
            int[] xn = new int[subsetSize]; //instance mask
            double[] xn_Con = new double[subsetSize]; //instance mask continuous
            double freq = new double(); //initialize the frequency of all the bats to zero
            double[] vel = new double[subsetSize]; //initialize the velocity of all the bats to zero
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            int k = 0;
            for (int i = 0; i < nBats; i++)
            {
                xn = new int[subsetSize];
                xn_Con = new double[subsetSize];
                pointers = new int[subsetSize];
                cnt1 = 0; cnt2 = 0; cnt3 = 0;
                for (int j = 0; j < prob.Count; j++)
                {
                    if (cnt1 < (0.7 * subsetSize) && prob.Y[j] == -1) //select 70% negative instance (i.e. ham) of the subset
                    {
                        xn[cnt3] = rnd.Next(0, 2);
                        //xn[cnt3] = 0;
                        pointers[cnt3] = rNum[j];
                        k++; cnt1++; cnt3++;
                    }
                    else if (cnt2 < (0.3 * subsetSize) && prob.Y[j] == 1)
                    {
                        xn[cnt3] = rnd.Next(0, 2);
                        //xn[cnt3] = 0;
                        pointers[cnt3] = rNum[j];
                        k++; cnt2++; cnt3++;
                    }
                    if (cnt3 >= subsetSize)
                        break;
                }

                ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, freq, vel, pointers, 0.0);
                attr_values.Add(OI);
            }

            return attr_values;
        }

        //get fitness value for each bat
        public static void BatFitness(double[] fitVal, List<ObjectInstanceSelection> Bats)
        {
            for (int i = 0; i < fitVal.Count(); i++)
                Bats[i].__Fitness = fitVal[i];
        }

        //evaluate new bat solution, update better solution (if found), and get global best bat
        public ObjectInstanceSelection EvaluateSolution(double[] batFitnessVal, double[] newBatFitnessVal, double globalBest, List<ObjectInstanceSelection> bats, List<ObjectInstanceSelection> newBats, ObjectInstanceSelection globalBestBat, double loudness)
        {
            double newBest = new double();
            int maxIndex;
            Random r = new Random();

            //evaluate solution and update, if better solution is found
            for (int i = 0; i < batFitnessVal.Count(); i++)
            {
                if (newBats[i].Fitness > bats[i].Fitness && r.NextDouble() < loudness)
                {
                    bats[i] = new ObjectInstanceSelection(newBats[i].Attribute_Values, newBats[i].Attribute_Values_Continuous, newBats[i].Frequency, newBats[i].Velocity, newBats[i].Pointers, newBats[i].Fitness); //create a clone of flowers
                    batFitnessVal[i] = newBats[i].Fitness;
                    //bats[i] = newBats[i]; //update solution
                }
            }

            //get blobal best flower
            newBest = newBatFitnessVal.Max(); //get the flower with the highest fitness
            if (newBest > globalBest)
            {
                globalBest = newBest;
                maxIndex = Array.IndexOf(newBatFitnessVal, newBest); //select the index for the global best
                globalBestBat = new ObjectInstanceSelection(newBats[maxIndex].Attribute_Values, newBats[maxIndex].Attribute_Values_Continuous, newBats[maxIndex].Frequency, newBats[maxIndex].Velocity, newBats[maxIndex].Pointers, newBats[maxIndex].Fitness); //create a clone of flowers; //select the global best flower
                //globalBestBat = newBats[maxIndex]; //select the global best flower
            }

            return globalBestBat;
        }

        //function to define the boundary of search variables
        public double SimpleBounds(double bat, double lowerBound, double upperBound)
        {
            //set lower bound
            if (bat < lowerBound)
                bat = lowerBound;
            else if (bat > upperBound)
                bat = upperBound;

            return bat;
        }
    }
}
