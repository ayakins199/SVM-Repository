using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    //This class is a direct implementation of the matlab code provided by original author of Spider Algorithm
    //Reference: https://github.com/James-Yu/SocialSpiderAlgorithm/blob/master/MATLAB/SSA.m
    public class SocialSpiderAlgorithm
    {
        FireflyInstanceSelection fi = new FireflyInstanceSelection();
        public Problem SocialSpider(Problem prob, out double storagePercentage)
        {
            int nSpiders = 5; //population size of spiders
            int subsetSize = 100;
            int totalInstances = prob.X.Count(); ;
            int bound = 100, maxGen = 5;
            double r_a = 1; //This parameter controls the attenuation rate of the vibration intensity over distance
            double p_c = 0.7; // p_c describes the probability of changing mask of spider 
            double p_m = 0.1; // This is also a user-controlled parameter defined in (0, 1). It controls the probability of assigning a one or zero to each bit of a mask
            bool info = true;
            double[][] globalBestPosition = new double[1][];
            double[] targetIntensity = new double[nSpiders]; //best vibration for each spider
            //double[] targetPosition = new double[nSpiders]; //target position for each spider
            double[,] mask = new double[nSpiders, subsetSize];
            double[,] newMask = new double[nSpiders, subsetSize];
            double[,] movement = new double[nSpiders, subsetSize];
            double[] inactive = new double[nSpiders];
            double[] spiderFitnessVal = new double[nSpiders];
            double[] newSpiderFitnessVal = new double[nSpiders];
            ObjectInstanceSelection globalBestSpider = null;
            double globalBest = double.MinValue;
            Random rand = new Random();
            FlowerPollinationAlgorithm fpa = new FlowerPollinationAlgorithm();

            //initialize population
            List<ObjectInstanceSelection> spiders = InitializeBinarySpider(nSpiders, subsetSize, totalInstances, prob);
            List<ObjectInstanceSelection> newSpiders = new List<ObjectInstanceSelection>(spiders.Count); //create a clone of bats
            spiders.ForEach((item) =>
            {
                newSpiders.Add(new ObjectInstanceSelection(item.Attribute_Values, item.Attribute_Values_Continuous, item.Pointers, item.Fitness, item.Position)); //create a clone of flowers
            });

            spiderFitnessVal = EvaluateObjectiveFunction(spiders, prob); //evaluate fitness value for all the bats
            newSpiderFitnessVal = EvaluateObjectiveFunction(newSpiders, prob); //evaluate fitness value for new spiders. Note: this will be the same for this function call, since pollination has not occur
            SpiderFitness(spiderFitnessVal, spiders); //fitness value for each spiders
            SpiderFitness(newSpiderFitnessVal, newSpiders); //fitness value for new spider
            globalBestSpider = EvaluateSolution(spiderFitnessVal, newSpiderFitnessVal, globalBest, spiders, newSpiders, globalBestSpider); //get the global best spider
            globalBest = globalBestSpider.Fitness;

            double[] standDev = new double[subsetSize];
            List<double> listPositions = new List<double>();
            List<double[]> spiderPositions = new List<double[]>();
            //calculate the standard deviation of all spider positions
            for (int a = 0; a < subsetSize; a++)
            {
                double[] sPositions = new double[nSpiders];
                for (int b = 0; b < nSpiders; b++)
                {
                    sPositions[b] = spiders[b].Attribute_Values[a]; //get all spider positions column wise
                    //sPositions[b] = spiders[b].Attribute_Values_Continuous[a]; //get all spider positions column wise
                }
                spiderPositions.Add(sPositions); //save positions in list
            }

            for (int a = 0; a < subsetSize; a++)
                standDev[a] = getStandardDeviation(spiderPositions[a].ToList()); //calculate standard deviation for each spider solution
            double baseDistance = standDev.Average(); //calculate the mean of standev

            //compute paired euclidean distances of all vectors in spider; similar to pdist function in matlab. Reference: http://www.mathworks.com/help/stats/pdist.html
            int n = (nSpiders * (nSpiders - 1)) / 2; //total number of elements array dist. 
            double[] euclidenDist = new double[n]; //Note that, this is array for paired eucliden distance, similar to pdist() function in matlab.
            int kk = 0;
            for (int i = 0; i < nSpiders; i++)
            {
                for (int j = 1 + i; j < nSpiders; j++)
                {
                    //this distance is in pairs -> 1,0; 2,0; 3,0,...n,0; 2,1; 3,1; 4,1,...n,1;.... It is similar to pdist function in matlab
                    //euclidenDist[kk++] = computeEuclideanDistance(spiders[j].Attribute_Values_Continuous, spiders[i].Attribute_Values_Continuous); //generate a vibration for each spider position
                    euclidenDist[kk++] = computeEuclideanDistance(spiders[j].Attribute_Values, spiders[i].Attribute_Values); //generate a vibration for each spider position
                    //distance[i][j] = computeEuclideanDistance(spiders[i].Attribute_Values, spiders[j].Attribute_Values);
                }
            }

            double[,] distance = SquareForm(euclidenDist, nSpiders); //Convert vibration to square matix, using SquareForm() function in matlab. Reference: see Squareform function in google
            //double[,] intensityReceive = new double[nSpiders, nSpiders];
            double[][] intensityReceive = new double[nSpiders][];

            for (int a = 0; a < maxGen; a++)
            {
                for (int j = 0; j < nSpiders; j++)
                {
                    //calculate the intensity for all the generated vibrations
                    intensityReceive[j] = new double[nSpiders];
                    double A = (spiders[j].Fitness + Math.Exp(-100)) + 1;
                    double intensitySource = Math.Log(1 / A);
                    for (int k = 0; k < nSpiders; k++)
                    {
                        double intensityAttenuation = Math.Exp(-distance[j, k] / (baseDistance * r_a));
                        //intensityReceive[j, k] = intensitySource * intensityAttenuation; //intensity for each spider vibration   
                        intensityReceive[j][k] = intensitySource * intensityAttenuation; //intensity for each spider vibration   
                    }
                }

                //select strongest vibration from intensity
                int row = intensityReceive.GetLength(0);
                int column = intensityReceive[0].Count();
                //IEnumerable<double> bestReceive = Enumerable.Range(0, row).Select(i => Enumerable.Range(0, column).Select(j => intensityReceive[i, j]).Max()); //get the max value in each row
                IEnumerable<double> bestReceive = Enumerable.Range(0, row).Select(i => Enumerable.Range(0, column).Select(j => intensityReceive[i][j]).Max()); //get the max value in each row

                //IEnumerable<int> bestReceiveIndex = Enumerable.Range(0, row).Select(i => Enumerable.Range(0, column).Select(j => intensityReceive[i, j]).Max()); //get the max value in each row

                //get the index of the strongest vibration
                int[] maxIndex = new int[nSpiders];
                for (int i = 0; i < nSpiders; i++)
                    maxIndex[i] = Array.IndexOf(intensityReceive[i], bestReceive.ElementAt(i));

                //Store the current best vibration
                int[] keepTarget = new int[nSpiders];
                int[] keepMask = new int[nSpiders];
                double[,] targetPosition = new double[nSpiders, subsetSize];
                for (int i = 0; i < nSpiders; i++)
                {
                    if (bestReceive.ElementAt(i) <= targetIntensity[i])
                        keepTarget[i] = 1;

                    inactive[i] = inactive[i] * keepTarget[i] + keepTarget[i];
                    targetIntensity[i] = (targetIntensity[i] * keepTarget[i]) + bestReceive.ElementAt(i) * (1 - keepTarget[i]);
                    

                    if (rand.NextDouble() < Math.Pow(p_c, inactive[i]))
                        keepMask[i] = 1;
                    inactive[i] = inactive[i] * keepMask[i];

                    for (int j = 0; j < subsetSize; j++)
                    {
                        //newSpiders[i].Attribute_Values[j] = fi.Binarize(newSpiders[i].Attribute_Values[j] * spiders[maxIndex[i]].Attribute_Values[j] * (1 - keepTarget[i]), rand.NextDouble()); //update solution
                        targetPosition[i, j] = targetPosition[i, j] * keepTarget[i] + spiders[maxIndex[i]].Attribute_Values[j] * (1 - keepTarget[i]);
                        //targetPosition[i, j] = targetPosition[i, j] * keepTarget[i] + spiders[maxIndex[i]].Attribute_Values_Continuous[j] * (1 - keepTarget[i]);
                        newMask[i, j] = Math.Ceiling(rand.NextDouble() + rand.NextDouble() * p_m - 1);
                        mask[i, j] = keepMask[i] * mask[i, j] + (1 - keepMask[i]) * newMask[i, j]; //update dimension mask of spider
                    }
                }

                //Reshuffule the Spider solution
                //Method: randomly generated positions pointing to rows and columns in the solution space. With the pointers, we can acess indivdual indices(or positions) in the solution
                double[,] randPosition = GenerateRandomSpiderPosition(nSpiders, subsetSize, spiders);

                //generate psfo, and perform random walk
                double[,] followPosition = new double[nSpiders, subsetSize];
                for (int i = 0; i < nSpiders; i++)
                {
                    for (int j = 0; j < subsetSize; j++)
                    {
                        followPosition[i, j] = mask[i, j] * randPosition[i, j] + (1 - mask[i, j]) * targetPosition[i, j];
                        movement[i, j] = rand.NextDouble() * movement[i, j] + (followPosition[i, j] - spiders[i].Attribute_Values[j]) * rand.NextDouble(); //perform random movement
                        //movement[i, j] = rand.NextDouble() * movement[i, j] + (followPosition[i, j] - spiders[i].Attribute_Values_Continuous[j]) * rand.NextDouble(); //perform random movement
                        //newSpiders[i].Attribute_Values[j] = fi.Binarize(newSpiders[i].Attribute_Values_Continuous[j] + movement[i, j], rand.NextDouble()); //actual random walk
                        newSpiders[i].Attribute_Values[j] = fi.Binarize(newSpiders[i].Attribute_Values[j] + movement[i, j], rand.NextDouble()); //actual random walk
                    }
                }

                //Select best solutions from the original population and matured population for the next generation;
                fpa.SelectBestSolution(spiders, newSpiders);

                //evaluate new solution
                newSpiderFitnessVal = EvaluateObjectiveFunction(newSpiders, prob); //evaluate fitness value for all the bats
                SpiderFitness(newSpiderFitnessVal, newSpiders); //fitness value for new bats
                globalBestSpider = EvaluateSolution(spiderFitnessVal, newSpiderFitnessVal, globalBest, spiders, newSpiders, globalBestSpider); //get the global best flower
                globalBest = globalBestSpider.Fitness;

                //if solution has converged to a optimal user-defined point, stop search
                int Max = 60;// maximum percentage reduction
                if (globalBest >= Max) //if the percentage reduction has approached 60%, stop search!
                    break;
            }

            //ensure that at least, N instances are selected for classification
            int Min = 15; //minimum number of selected instances
            globalBestSpider = fpa.AddInstances(globalBestSpider, Min);

            Problem subBest = fi.buildModelMultiClass(globalBestSpider, prob); //build model for the best Instance Mast
            storagePercentage = Training.StoragePercentage(subBest, prob); //calculate the percent of the original training set was retained by the reduction algorithm
            return subBest;
        }

        /// <summary>
        /// Evaluate Objective Function
        /// </summary>
        public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> Spiders, Problem prob)
        {
            int NB = Spiders.Count; //NF -> number of spiders
            int tNI = Spiders.ElementAt(0).Attribute_Values.Count(); //size of each Instance Mask
            double[] fitness = new double[NB];
            int sum;


            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();

            double C, Gamma;

            for (int i = 0; i < NB; i++)
            {
                //building model for each instance in instance mask in each spider object
                Problem subProb = fi.buildModel(Spiders.ElementAt(i), prob);

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
                    int count = Spiders.ElementAt(i).__Attribute_Values.Count(q => q == 1); //total number of selected instances, to be used for subsetSize
                    double perRedBInstances = (double)(subProb.Count / subP.Count); //percentage reduction for boundary instances
                    double perRedSpiderInstances = (double)(tNI - count) / tNI; //percentage reduction for flower instances
                    fitness[i] = (100 * perRedSpiderInstances) + perRedBInstances;
                }
            }

            return fitness;
        }


        //Reshuffule the Spider solution. by generating random spider solution
        //Method: randomly generated positions pointing to rows and columns in the solution. With the pointers, we can acess indivdual indices(or positions) in the solution
        public double[,] GenerateRandomSpiderPosition(int nSpiders, int subsetSize, List<ObjectInstanceSelection> spiders)
        {
            double[,] randPosition = new double[nSpiders, subsetSize];
            Random rand = new Random();
            for (int i = 0; i < nSpiders; i++)
            {
                for (int j = 0; j < subsetSize; j++)
                {
                    int randi = rand.Next(0, nSpiders); //generate random ith position
                    int randj = rand.Next(0, subsetSize); //genertae random jth position
                    randPosition[i, j] = spiders[randi].Attribute_Values[randj];
                }
            }

            return randPosition;
        }

        //calculate standard deviation
        private double getStandardDeviation(List<double> intList)
        {
            double average = intList.Average();
            double sumOfDerivation = 0;
            foreach (double value in intList)
            {
                sumOfDerivation += (value) * (value);
            }
            double sumOfDerivationAverage = sumOfDerivation / (intList.Count - 1);
            return Math.Sqrt(sumOfDerivationAverage - (average * average));
        }

        //squareform() function in matlab
        public double[,] SquareForm(double[] array, int popSize)
        {
            //int n = array.Count();
            //int nZeros = 0;
            //bool isPerfect = true;
            //int A = n * 2;
            //while (isPerfect)
            //{
            //    nZeros++;
            //    bool res = IsPerfectSquare(A + nZeros);

            //    if (res == true) //if this is true, then perfect square is found
            //        isPerfect = false;
            //}

            double[,] sMatrix = new double[popSize, popSize]; //Square matrix
            int b = 0;
            for (int i = 0; i < popSize; i++)
            {
                for (int j = 0; j < popSize; j++)
                {
                    if (i == j) //assign zero to the diagonals. Diagonals occur, when i == j
                        sMatrix[i, j] = 0; 
                    else 
                    {
                        if (sMatrix[i, j] == 0) //assign like squareform() in matlab. E.g: 0,1=>1,0; 0,2=>2,0; 0,3=>3,0; 0,4=>4,0; 1,2=>2,1; 1,3=>3,1
                        {
                            sMatrix[i, j] = array[b];
                            sMatrix[j, i] = array[b];
                            b++;
                        }
                    }
                }
            }

            return sMatrix;
        }

        //check for perfect square
        public static bool IsPerfectSquare(long target)
        {
            return Math.Sqrt((double)target) % 1d == 0d;
        }

        //Compute eucliden distance
        public static double computeEuclideanDistance(int[] arrA, int[] arrB)
        {
            double sum = 0;
            for (int i = 0; i < arrA.Length; i++)
                sum += Math.Pow(Math.Abs(arrA[i] - arrB[i]), 2);

            return Math.Sqrt(sum);
        }

        /// <summary>
        /// generating the initial locations of n spiders
        /// </summary>
        public List<ObjectInstanceSelection> InitializeBinarySpider(int nSpiders, int subsetSize, int probSize, Problem prob)
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
            double spiderPosition = 0;
            int k = 0;
            int bound = 100;
            for (int i = 0; i < nSpiders; i++)
            {
                xn = new int[subsetSize];
                xn_Con = new double[subsetSize];
                pointers = new int[subsetSize];
                cnt1 = 0; cnt2 = 0; cnt3 = 0;
                for (int j = 0; j < prob.Count; j++)
                {
                    if (cnt1 < (0.7 * subsetSize) && prob.Y[rNum[j]] == -1) //select 70% positive instance of the subset
                    {
                        //xn_Con[cnt3] = rnd.NextDouble();
                        //xn[cnt3] = fi.Binarize(xn_Con[cnt3], rnd.NextDouble());
                        xn[cnt3] = rnd.Next(0, 2); //initialize each spider position. 
                        pointers[cnt3] = rNum[j];
                        spiderPosition = rnd.NextDouble() * 2 * bound - bound; //generate position of spider
                        k++; cnt1++; cnt3++;
                    }
                    else if (cnt2 < (0.3 * subsetSize) && prob.Y[rNum[j]] == 1)
                    {
                        //xn_Con[cnt3] = rnd.NextDouble();
                        //xn[cnt3] = fi.Binarize(xn_Con[cnt3], rnd.NextDouble());
                        xn[cnt3] = rnd.Next(0, 2); //initialize each spider position. 
                        pointers[cnt3] = rNum[j];
                        spiderPosition = rnd.NextDouble() * 2 * bound - bound; //generate position of spider
                        k++; cnt2++; cnt3++;
                    }
                    if (cnt3 >= subsetSize)
                        break;
                }

                ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, pointers, 0.0, spiderPosition);
                attr_values.Add(OI);
            }

            return attr_values;
        }

        //get fitness value for each bat
        public static void SpiderFitness(double[] fitVal, List<ObjectInstanceSelection> Spiders)
        {
            for (int i = 0; i < fitVal.Count(); i++)
                Spiders[i].__Fitness = fitVal[i];
        }

        //evaluate new spider solution, update better solution (if found), and get global spider
        public ObjectInstanceSelection EvaluateSolution(double[] spiderFitnessVal, double[] newSpiderFitnessVal, double globalBest, List<ObjectInstanceSelection> Spiders, List<ObjectInstanceSelection> newSpiders, ObjectInstanceSelection globalBestSpider)
        {
            double newBest = new double();
            int maxIndex;
            Random r = new Random();

            //evaluate solution and update, if better solution is found
            for (int i = 0; i < spiderFitnessVal.Count(); i++)
            {
                if (newSpiders[i].Fitness > Spiders[i].Fitness)
                {
                    Spiders[i] = new ObjectInstanceSelection(newSpiders[i].Attribute_Values, newSpiders[i].Attribute_Values_Continuous, newSpiders[i].Pointers, newSpiders[i].Fitness); //create a clone of flowers
                    spiderFitnessVal[i] = newSpiders[i].Fitness;
                    //bats[i] = newBats[i]; //update solution
                }
            }

            //get blobal best flower
            newBest = newSpiderFitnessVal.Max(); //get the flower with the highest fitness
            if (newBest > globalBest)
            {
                globalBest = newBest;
                maxIndex = Array.IndexOf(newSpiderFitnessVal, newBest); //select the index for the global best
                globalBestSpider = new ObjectInstanceSelection(newSpiders[maxIndex].Attribute_Values, newSpiders[maxIndex].Attribute_Values_Continuous, newSpiders[maxIndex].Pointers, newSpiders[maxIndex].Fitness); //create a clone of flowers; //select the global best flower
                //globalBestBat = newBats[maxIndex]; //select the global best flower
            }

            return globalBestSpider;
        }
    }
}
