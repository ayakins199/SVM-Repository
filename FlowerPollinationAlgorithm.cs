using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestSimpleRNG;

namespace SVM
{
    //this is an implentation of bat algorithm, proposed by Yang. 
    //Reference: https://www.mathworks.com/matlabcentral/fileexchange/45112-flower-pollination-algorithm/content/fpa_demo.m
    public class FlowerPollinationAlgorithm
    {
        FireflyInstanceSelection fi = new FireflyInstanceSelection();
        //flower pollination algorithm by Yang
        public Problem FlowerPollination(Problem prob)
        {
            int nargin = 0, totalInstances = prob.X.Count(), maxGeneration = 500;
            int numOfFlower = 10; //population size
            double probabilitySwitch = 0.8; //assign probability switch
            int subsetSize = 200; //dimension for each flower
            double[] flowerFitnessVal = new double[numOfFlower];
            double[] newFlowerFitnessVal = new double[numOfFlower];
            FireflyInstanceSelection fw = new FireflyInstanceSelection();
            double globalBest = double.MinValue;
            double newBest = new double();
            ObjectInstanceSelection globalBestFlower = null;
            int lowerBound = -2; //set lower bound - lower boundary
            int upperBound = 2; //set upper bound - upper boundary
            int maxIndex;

            //inittalize flowers, and get global best
            List<ObjectInstanceSelection> flowers = InitializeFlower(numOfFlower, subsetSize, totalInstances, prob);  //initialize solution
            List<ObjectInstanceSelection> newFlowers = new List<ObjectInstanceSelection>(flowers.Count); //create a clone of flowers
            flowers.ForEach((item) =>
            {
                newFlowers.Add(new ObjectInstanceSelection(item.__Attribute_Values, item.__Attribute_Values_Continuous, item.__Pointers, item.__Fitness)); //create a clone of flowers
            });

            flowerFitnessVal = fw.EvaluateObjectiveFunction(flowers, prob); //evaluate fitness value for all the flowers
            newFlowerFitnessVal = fw.EvaluateObjectiveFunction(newFlowers, prob); //evaluate fitness value for new flowers. Note: this will be the same for this function call, since pollination has not occur
            FlowerFitness(flowerFitnessVal, flowers); //fitness value for each flower
            FlowerFitness(newFlowerFitnessVal, newFlowers); //fitness value for new flower
            globalBestFlower = EvaluateSolution(flowerFitnessVal, newFlowerFitnessVal, globalBest, flowers, newFlowers, globalBestFlower); //get the global best flower
            globalBest = flowerFitnessVal.Max();

            //start flower algorithm
            Random r = new Random();
            double[] levy = new double[subsetSize];
            for (int i = 0; i < maxGeneration; i++)
            {
                double rand = r.NextDouble();
                if (rand > probabilitySwitch) //global pollination
                {
                    //global pollination
                    for (int j = 0; j < numOfFlower; j++)
                    {
                        levy = LevyFlight(subsetSize);
                        for (int k = 0; k < subsetSize; k++)
                        {
                            double A = levy[k] * (flowers[j].__Attribute_Values_Continuous[k] - globalBestFlower.__Attribute_Values_Continuous[k]);
                            double B = flowers[j].__Attribute_Values_Continuous[k] + A;
                            A = SimpleBounds(B, lowerBound, upperBound); //ensure that value does not go beyond defined boundary
                            newFlowers[j].__Attribute_Values_Continuous[k] = A;
                            newFlowers[j].__Attribute_Values[k] = fw.Binarize(B, r.NextDouble()); //convert to binary
                        }
                    }
                }
                else //local pollination
                {
                    for (int j = 0; j < numOfFlower; j++)
                    {
                        List<int> randNum = Training.GetRandomNumbers(2, numOfFlower); //generate 2 distinct random numbers
                        double epsilon = rand;

                        //local pollination
                        for (int k = 0; k < subsetSize; k++)
                        {
                            double A = flowers[j].__Attribute_Values_Continuous[k] + epsilon * (flowers[randNum[0]].__Attribute_Values_Continuous[k] - flowers[randNum[1]].__Attribute_Values_Continuous[k]); //randomly select two flowers from neighbourhood for pollination
                            A = SimpleBounds(A, lowerBound, upperBound); //ensure that value does not exceed defined boundary
                            newFlowers[j].__Attribute_Values_Continuous[k] = A; //save computation
                            newFlowers[j].__Attribute_Values[k] = fw.Binarize(A, r.NextDouble()); //convert to binary
                        }
                    }
                }

                //evaluate new solution
                newFlowerFitnessVal = fw.EvaluateObjectiveFunction(newFlowers, prob); //evaluate fitness value for all the flowers
                FlowerFitness(newFlowerFitnessVal, newFlowers); //fitness value for new flower
                globalBestFlower = EvaluateSolution(flowerFitnessVal, newFlowerFitnessVal, globalBest, flowers, newFlowers, globalBestFlower); //Evaluate solution, update better solution and get global best flower
                globalBest = flowerFitnessVal.Max();
            }

            //ensure that at least, 40 instances is selected for classification
            int countSelected = globalBestFlower.__Attribute_Values.Count(q => q == 1); //count the total number of selected instances
            int diff, c = 0, d = 0;
            int Min = 40; //minimum number of selected instances
            if (countSelected < Min)
            {
                //if there are less than N, add N instances, where N = the number of selected instances 
                diff = Min - countSelected;
                while (c < diff)
                {
                    if (globalBestFlower.__Attribute_Values[d++] == 1)
                        continue;
                    else
                    {
                        globalBestFlower.__Attribute_Values[d++] = 1;
                        c++;
                    }
                }
            }

            Problem subBest = fw.buildModel(globalBestFlower, prob); //build model for the best Instance Mast
            return subBest;
        }

        //flower pollination algorithm by Yang
        public Problem BinaryFlowerPollination(Problem prob, out double storagePercentage)
        {
            int nargin = 0, totalInstances = prob.X.Count();
            int maxGeneration = 3;
            int numOfFlower = 3; //population size
            int subsetSize = 100; //dimension for each flower
            double probabilitySwitch = 0.8; //assign probability switch
            double[] flowerFitnessVal = new double[numOfFlower];
            double[] newFlowerFitnessVal = new double[numOfFlower];
            
            double globalBest = double.MinValue;
            double newBest = new double();
            ObjectInstanceSelection globalBestFlower = null;
            int lowerBound = -2; //set lower bound - lower boundary
            int upperBound = 2; //set upper bound - upper boundary
            int maxIndex;

            //inittalize flowers, and get global best
            List<ObjectInstanceSelection> flowers = InitializeBinaryFlower(numOfFlower, subsetSize, totalInstances, prob);  //initialize solution
            List<ObjectInstanceSelection> newFlowers = new List<ObjectInstanceSelection>(flowers.Count); //create a clone of flowers
            flowers.ForEach((item) =>
            {
                newFlowers.Add(new ObjectInstanceSelection(item.__Attribute_Values, item.__Attribute_Values_Continuous, item.__Pointers, item.__Fitness)); //create a clone of flowers
            });

            flowerFitnessVal = EvaluateObjectiveFunction(flowers, prob); //evaluate fitness value for all the flowers
            newFlowerFitnessVal = EvaluateObjectiveFunction(newFlowers, prob); //evaluate fitness value for new flowers. Note: this will be the same for this function call, since pollination has not occur
            FlowerFitness(flowerFitnessVal, flowers); //fitness value for each flower
            FlowerFitness(newFlowerFitnessVal, newFlowers); //fitness value for new flower
            globalBestFlower = EvaluateSolution(flowerFitnessVal, newFlowerFitnessVal, globalBest, flowers, newFlowers, globalBestFlower); //get the global best flower
            globalBest = flowerFitnessVal.Max();

            //start flower algorithm
            Random r = new Random(); int x = 0;
            double[] levy = new double[subsetSize];
            for (int i = 0; i < maxGeneration; i++)
            {
                double rand = r.NextDouble();
                if (rand > probabilitySwitch) //do global pollination, to produce new pollen solution
                {
                    levy = LevyFlight(subsetSize); 
                    for (int j = 0; j < numOfFlower; j++)
                    {
                        for (int k = 0; k < subsetSize; k++)
                        {
                            double A = levy[k] * (flowers[j].Attribute_Values[k] - globalBestFlower.Attribute_Values[k]);
                            double B = flowers[j].Attribute_Values[k] + A; //new pollen solution
                            //double A = levy[k] * (flowers[j].Attribute_Values_Continuous[k] - globalBestFlower.Attribute_Values_Continuous[k]);
                            //double B = flowers[j].Attribute_Values_Continuous[k] + A;
                            newFlowers[j].Attribute_Values[k] = ConvertToBinary(B, r.NextDouble()); //convert to binary
                            
                            //newFlowers[j].__Attribute_Values[k] = TransferFunction(B, newFlowers[j].__Attribute_Values[k]); //update flower position in the binary space
                        }
                        List<int> randNum = Training.GetRandomNumbers(2, numOfFlower); //generate 2 distinct random numbers
                        for (int k = 0; k < subsetSize; k++)
                        {
                            double A = flowers[j].Attribute_Values[k] + (r.NextDouble() * (flowers[randNum[0]].Attribute_Values[k] - flowers[randNum[1]].Attribute_Values[k])); //randomly select two flowers from neighbourhood for pollination
                            //double A = flowers[j].Attribute_Values_Continuous[k] + r.NextDouble() * (flowers[randNum[0]].Attribute_Values_Continuous[k] - flowers[randNum[1]].Attribute_Values_Continuous[k]); //randomly select two flowers from neighbourhood for pollination
                            newFlowers[j].Attribute_Values[k] = ConvertToBinary(A, r.NextDouble()); //convert to binary
                            
                            //newFlowers[j].__Attribute_Values[k] = TransferFunction(A, newFlowers[j].__Attribute_Values[k]); //update flower position in the binary space
                        }
                    }
                }
                else // //do local pollination, to produce new pollen solution
                {
                    for (int j = 0; j < numOfFlower; j++)
                    {
                        List<int> randNum = Training.GetRandomNumbers(2, numOfFlower); //generate 2 distinct random numbers
                        for (int k = 0; k < subsetSize; k++)
                        {
                            double A = flowers[j].Attribute_Values[k] + r.NextDouble() * (flowers[randNum[0]].Attribute_Values[k] - flowers[randNum[1]].Attribute_Values[k]); //randomly select two flowers from neighbourhood for pollination
                            //double A = flowers[j].Attribute_Values_Continuous[k] + r.NextDouble() * (flowers[randNum[0]].Attribute_Values_Continuous[k] - flowers[randNum[1]].Attribute_Values_Continuous[k]); //randomly select two flowers from neighbourhood for pollination
                            newFlowers[j].Attribute_Values[k] = ConvertToBinary(A, r.NextDouble()); //convert to binary

                            //newFlowers[j].__Attribute_Values[k] = TransferFunction(A, newFlowers[j].__Attribute_Values[k]); //update flower position in the binary space
                        }
                    }
                }

                //Select best solutions from the original population and matured population for the next generation;
                SelectBestSolution(flowers, newFlowers);

                //evaluate new solution
                newFlowerFitnessVal = EvaluateObjectiveFunction(newFlowers, prob); //evaluate fitness value for all the flowers
                FlowerFitness(newFlowerFitnessVal, newFlowers); //fitness value for new flower
                globalBestFlower = EvaluateSolution(flowerFitnessVal, newFlowerFitnessVal, globalBest, flowers, newFlowers, globalBestFlower); //Evaluate solution, update better solution and get global best flower
                globalBest = globalBestFlower.Fitness;

                //if solution has converged to a optimal user-defined point, stop search
                int Max = 60;// maximum percentage reduction
                if (globalBest >= Max) //if the percentage reduction has approached 60%, stop search!
                    break;
            }

            //ensure that at least, N instances are selected for classification
            int min = 15; //minimum number of selected instances
            globalBestFlower = AddInstances(globalBestFlower, min);

            Problem subBest = fi.buildModelMultiClass(globalBestFlower, prob); //build model for the best Instance Mast
            storagePercentage = Training.StoragePercentage(subBest, prob); //calculate the percent of the original training set was retained by the reduction algorithm
            return subBest;
        }

        //Add N instances, if selected instances is less than the user-defined minimum
        public ObjectInstanceSelection AddInstances(ObjectInstanceSelection globalBestFlower, int Min)
        {
            int countSelected = globalBestFlower.Attribute_Values.Count(q => q == 1); //count the total number of selected instances
            int diff, c = 0, d = 0;
            
            if (countSelected < Min)
            {
                //if there are less than N, add N instances, where N = the number of selected instances 
                diff = Min - countSelected;
                while (c < diff)
                {
                    if (globalBestFlower.Attribute_Values[d] == 1) //skip the already selected solutions
                    {
                        d++;
                        continue;
                    }
                    else //add instances to positions that are not selected; i.e. where instance mask is equal to 0
                    {
                        globalBestFlower.Attribute_Values[d] = 1;
                        c++; d++;
                    }
                }
            }

            diff = globalBestFlower.Attribute_Values.Count(a => a == 1);

            return globalBestFlower;
        }

        //Select best solutions from the original population and matured population for the next generation
        public void SelectBestSolution(List<ObjectInstanceSelection> flowers, List<ObjectInstanceSelection> newFlowers)
        {
            int subsetSize = flowers[0].Attribute_Values.Count();
            int numOfFlower = flowers.Count;

            for (int a = 0; a < numOfFlower; a++)
            {
                for (int b = 0; b < subsetSize; b++)
                {
                    if (newFlowers[a].Attribute_Values[b] == 1 && flowers[a].Attribute_Values[b] == 0)
                        newFlowers[a].Attribute_Values[b] = 0;
                }
            }

        }

        /// <summary>
        /// Evaluate Objective Function
        /// </summary>
        public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> Flowers, Problem prob)
        {
            int NB = Flowers.Count; //NF -> number of fireflies
            int tNI = Flowers.ElementAt(0).Attribute_Values.Count(); //size of each Instance Mask
            double[] fitness = new double[NB];
            int sum;


            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();

            double C, Gamma;

            for (int i = 0; i < NB; i++)
            {
                //building model for each instance in instance mask in each firefly object
                Problem subProb = fi.buildModel(Flowers.ElementAt(i), prob);

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
                    int count = Flowers.ElementAt(i).Attribute_Values.Count(q => q == 1); //total number of selected instances, to be used for subsetSize
                    double perRedBInstances = ((double)subProb.Count / (double)subP.Count); //percentage reduction for boundary instances
                    double perRedFlowerInstances = (double)(tNI - count) / tNI; //percentage reduction for flower instances
                    //fitness[i] = (100 * perRedFlowerInstances);
                    fitness[i] = (100 * perRedFlowerInstances) + perRedBInstances;
                    //fitness[i] = 100 * ((double)count / (double)tNI);
                }
            }

            return fitness;
        }


        //transfer function for movement of bats in binary space. Proposed by authors in: http://link.springer.com/article/10.1007%2Fs00521-013-1525-5
        public int TransferFunction(double newPosition, int batPosition)
        {
            Random rand = new Random();
            double A = 2 / Math.PI;
            double B = Math.Atan(A * newPosition);
            newPosition = Math.Abs(A * B);
            double rNum = rand.NextDouble();

            if (rNum < newPosition)
            {
                if (batPosition == 0)
                    batPosition = 1;
                else if (batPosition == 1)
                    batPosition = 0;
            }

            return batPosition;
        }


        //get fitness value for each flower
        public static void FlowerFitness(double[] fitVal, List<ObjectInstanceSelection> Flowers)
        {
            for (int i = 0; i < fitVal.Count(); i++)
                Flowers[i].__Fitness = fitVal[i];
        }

        //evaluate new flower solution, update better solution (if found), and get global best flower
        public ObjectInstanceSelection EvaluateSolution(double[] flowerFitnessVal, double[] newflowerFitnessVal, double globalBest, List<ObjectInstanceSelection> flowers, List<ObjectInstanceSelection> newFlowers, ObjectInstanceSelection globalBestFlower)
        {
            double newBest = new double();
            int maxIndex;

            //evaluate solution and update, if better solution is found
            for (int i = 0; i < flowerFitnessVal.Count(); i++)
            {
                if (newFlowers[i].Fitness > flowers[i].Fitness)
                {
                    flowers[i] = new ObjectInstanceSelection(newFlowers[i].Attribute_Values, newFlowers[i].Attribute_Values_Continuous, newFlowers[i].Pointers, newFlowers[i].Fitness); //create a clone of flowers
                    flowerFitnessVal[i] = newFlowers[i].Fitness;
                }
            }

            //get blobal best flower
            newBest = newflowerFitnessVal.Max(); //get the flower with the highest fitness
            if (newBest > globalBest)
            {
                globalBest = newBest;
                maxIndex = Array.IndexOf(newflowerFitnessVal, newBest); //select the index for the global best
                globalBestFlower = new ObjectInstanceSelection(newFlowers[maxIndex].Attribute_Values, newFlowers[maxIndex].Attribute_Values_Continuous, newFlowers[maxIndex].Pointers, newFlowers[maxIndex].Fitness); //create a clone of flowers; //select the global best flower
            }

            return globalBestFlower;
        }

        //Convert from continuous to binary value, using function proposed in: https://www.researchgate.net/publication/282305118_Binary_Flower_Pollination_Algorithm_and_Its_Application_to_Feature_Selection 
        public int ConvertToBinary(double val, double rand)
        {
            double A = 1 + Math.Exp(-val);
            double func = 1 / A;
           
            int retVal = new int();

            if (func > rand)
                retVal = 1;
            else
                retVal = 0;

            return retVal;
        }

        /// <summary>
        /// generating the initial locations of n flower
        /// </summary>
        public List<ObjectInstanceSelection> InitializeBinaryFlower(int nFlower, int subsetSize, int probSize, Problem prob)
        {
            Random rnd = new Random();
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers
            FireflyInstanceSelection fpa = new FireflyInstanceSelection();

            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            //create an array of size n for x and y
            int[] xn = new int[subsetSize]; //instance mask
            double[] xn_Con = new double[subsetSize]; //instance mask continuous
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            int k = 0;
            for (int i = 0; i < nFlower; i++)
            {
                xn = new int[subsetSize];
                xn_Con = new double[subsetSize];
                pointers = new int[subsetSize];
                cnt1 = 0; cnt2 = 0; cnt3 = 0;
                for (int j = 0; j < prob.Count; j++)
                {
                    if (cnt1 < (0.7 * subsetSize) && prob.Y[rNum[j]] == -1) //select 70% positive instance of the subset
                    {
                       //xn[cnt3] = rnd.NextDouble() <= 0.5 ? 0 : 1;
                        xn[cnt3] = rnd.Next(0, 2);
                        //xn_Con[cnt3] = rnd.NextDouble();
                        //xn[cnt3] = fi.Binarize(xn_Con[cnt3], rnd.NextDouble());
                        pointers[cnt3] = rNum[j];
                        k++; cnt1++; cnt3++;
                    }
                    else if (cnt2 < (0.3 * subsetSize) && prob.Y[rNum[j]] == 1)
                    {
                       //xn[cnt3] = rnd.NextDouble() <= 0.5 ? 0 : 1;
                        xn[cnt3] = rnd.Next(0, 2);
                        //xn_Con[cnt3] = rnd.NextDouble();
                        //xn[cnt3] = fi.Binarize(xn_Con[cnt3], rnd.NextDouble());
                        pointers[cnt3] = rNum[j];
                        k++; cnt2++; cnt3++;
                    }
                    if (cnt3 >= subsetSize)
                        break;
                }

                ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, pointers, 0.0);
                attr_values.Add(OI);
            }

            return attr_values;
        }


        /// <summary>
        /// generating the initial locations of n flower
        /// </summary>
        public List<ObjectInstanceSelection> InitializeFlower(int nFlower, int subsetSize, int probSize, Problem prob)
        {
            Random rnd = new Random();
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers
            FireflyInstanceSelection fpa = new FireflyInstanceSelection();

            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            //create an array of size n for x and y
            int[] xn = new int[subsetSize]; //instance mask
            double[] xn_Con = new double[subsetSize]; //instance mask continuous
            int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            int k = 0;
            for (int i = 0; i < nFlower; i++)
            {
                xn = new int[subsetSize];
                xn_Con = new double[subsetSize];
                pointers = new int[subsetSize];
                cnt1 = 0; cnt2 = 0; cnt3 = 0;
                for (int j = 0; j < prob.Count; j++)
                {
                    if (cnt1 < (0.7 * subsetSize) && prob.Y[rNum[j]] == 1) //select 70% positive instance of the subset
                    {
                        xn_Con[cnt3] = rnd.NextDouble();
                        xn[cnt3] = fpa.Binarize(xn_Con[cnt3], rnd.NextDouble()); //convert generated random number to binary
                        pointers[cnt3] = rNum[j];
                        k++; cnt1++; cnt3++;
                    }
                    else if (cnt2 < (0.3 * subsetSize) && prob.Y[rNum[j]] == -1)
                    {
                        xn_Con[cnt3] = rnd.NextDouble();
                        xn[cnt3] = fpa.Binarize(xn_Con[cnt3], rnd.NextDouble()); //convert generated random number to binary
                        pointers[cnt3] = rNum[j];
                        k++; cnt2++; cnt3++;
                    }
                    if (cnt3 >= subsetSize)
                        break;
                }

                ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, pointers, 0.0);
                attr_values.Add(OI);
            }

            return attr_values;
        }

        //levy flight - refer to url: https://www.mathworks.com/matlabcentral/fileexchange/45112-flower-pollination-algorithm/content/fpa_demo.m
        public double[] LevyFlight(int subsetSize)
        {
            //levy exponent and coefficient. For more details see Chapter 11 of the following book:
            // Xin-She Yang, Nature-Inspired Optimization Algorithms, Elsevier, (2014).

            double beta = 3 / 2;
            double A = Gamma(1 + beta);
            double B = Math.Sin(Math.PI * beta / 2);
            double C = Gamma((1 + beta) / 2);
            double D = (beta - 1) / 2;
            double E = beta * Math.Pow(2, D);
            double F = 1 / beta;
            double sigma = Math.Pow((A * B / (C * E)), F);

            double G, H, I, step;
            double[] levyF = new double[subsetSize];

            for (int i = 0; i < subsetSize; i++)
            {
                double randNum = SimpleRNG.GetNormal();//generate random number with normal distribution
                G = sigma * randNum;
                H = SimpleRNG.GetNormal();
                I = Math.Abs(H);
                step = G / Math.Pow(I, F);
                levyF[i] = 0.01 * step;
            }

            return levyF;
        }

        //function to define the boundary of search variables
        public double SimpleBounds(double flower, double lowerBound, double upperBound)
        {
            //set lower bound
            if (flower < lowerBound)
                flower = lowerBound;
            else if (flower > upperBound)
                flower = upperBound;

            return flower;
        }

        //Gamma function (Use polynomial approximation). Code from: https://www.experts-exchange.com/questions/27178124/Gamma-function-in-C.html
        public double Gamma(double alpha)
        {
            try
            {
                double gamma = 0;
                if (alpha > 0)
                {
                    if (alpha > 0 && alpha < 1)
                    {
                        gamma = Gamma(alpha + 1) / alpha;
                    }
                    else if (alpha >= 1 && alpha <= 2)
                    {
                        gamma = 1 - 0.577191652 * Math.Pow(alpha - 1, 1) + 0.988205891 * Math.Pow(alpha - 1, 2) -
                                0.897056937 * Math.Pow(alpha - 1, 3) + 0.918206857 * Math.Pow(alpha - 1, 4) -
                                0.756704078 * Math.Pow(alpha - 1, 5) + 0.482199394 * Math.Pow(alpha - 1, 6) -
                                0.193527818 * Math.Pow(alpha - 1, 7) + 0.03586843 * Math.Pow(alpha - 1, 8);
                    }
                    else
                    {
                        gamma = (alpha - 1) * Gamma(alpha - 1);
                    }
                }
                if (alpha > 171)
                {
                    gamma = Math.Pow(10, 307);
                }
                return gamma;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
