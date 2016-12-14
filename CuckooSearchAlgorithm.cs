using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestSimpleRNG;

namespace SVM
{
    //Original Matlab version of this code was programmed by Xin-She Yang: https://www.mathworks.com/matlabcentral/fileexchange/29809-cuckoo-search-cs-algorithm/content/cuckoo_search.m
    public class CuckooSearchAlgorithm
    {
        FireflyInstanceSelection fi = new FireflyInstanceSelection();
        FlowerPollinationAlgorithm fp = new FlowerPollinationAlgorithm();
        BatAlgorithm bat = new BatAlgorithm();
        /// <summary>
        /// Default weight for the Selected Boundary Instances
        /// </summary>
        public const double W_SeelctedBoundaryInstances = 0.95;/// <summary>
        /// Default weight for the number of selected instances
        /// </summary>
        public const double W_Instances = 0.05;
        public Problem CuckooSearch(Problem prob, out double storagePercentage)
        {
            int nNests = 5; //number of nests, or number of solutions
            int subsetSize = 100;
            int maxGen = 5; //maximum generation
            double discoveryRate = 0.25; //discovery rate of alien eggs
            double tolerance = Math.Exp(-5);
            int lowerBound = -5;
            int upperBound = 5;
            int totalInstances = prob.X.Count(); //problem size
            double[] cuckooFitnessVal = new double[nNests];
            double[] newCuckooFitnessVal = new double[nNests];
            ObjectInstanceSelection globalBestCuckoo = null;
            double globalBest = double.MinValue;
            Random rand = new Random();
            
            FlowerPollinationAlgorithm fpa = new FlowerPollinationAlgorithm();

            //initialize population
            List<ObjectInstanceSelection> cuckoos = InitializeBinaryCuckoo(nNests, subsetSize, totalInstances, prob);
            List<ObjectInstanceSelection> newCuckoos = new List<ObjectInstanceSelection>(cuckoos.Count); //create a clone of bats
            cuckoos.ForEach((item) =>
            {
                newCuckoos.Add(new ObjectInstanceSelection(item.Attribute_Values, item.Attribute_Values_Continuous, item.Pointers, item.Fitness)); //create a clone of flowers
            });

            cuckooFitnessVal = EvaluateObjectiveFunction(cuckoos, prob); //evaluate fitness value for all the bats
            newCuckooFitnessVal = EvaluateObjectiveFunction(newCuckoos, prob); //evaluate fitness value for new bats. Note: this will be the same for this function call, since pollination has not occur
            CuckooFitness(cuckooFitnessVal, cuckoos); //fitness value for each bats
            CuckooFitness(newCuckooFitnessVal, newCuckoos); //fitness value for new bats
            globalBestCuckoo = EvaluateSolution(cuckooFitnessVal, newCuckooFitnessVal, globalBest, cuckoos, newCuckoos, globalBestCuckoo); //get the global best flower
            globalBest = globalBestCuckoo.__Fitness;

            //generate new solutions
            double beta = 3 / 2;
            double A = fp.Gamma(1 + beta) * Math.Sin(Math.PI * (beta / 2));
            double B = fp.Gamma((1 + beta) / 2) * beta;
            double C = (beta - 1) / 2;
            double D = Math.Pow(2, C);
            double E = A / (B * D);
            double sigma = Math.Pow(E, (1 / beta));

            double F;
            double G;
            double step;
            double stepSize;
            int x = 0;
            for (int i = 0; i <= maxGen; i++)
            {
                for (int j = 0; j < nNests; j++)
                {
                    for (int k = 0; k < subsetSize; k++)
                    {
                        F = SimpleRNG.GetNormal() * sigma;
                        G = SimpleRNG.GetNormal();
                        step =  F / Math.Pow(Math.Abs(G), (1 / beta));

                        //In the next equation, the difference factor (s-best) means that when the solution is the best solution, it remains unchanged.
                        //Here the factor 0.01 comes from the fact that L/100 should the typical step size of walks/flights where L is the typical lenghtscale; 
                        //otherwise, Levy flights may become too aggresive/efficient, which makes new solutions (even) jump out side of the design domain (and thus wasting evaluations).
                        stepSize = 0.01 * step * (cuckoos[j].Attribute_Values[k] - globalBestCuckoo.Attribute_Values[k]);

                        //Now the actual random walks or levyy flights
                        newCuckoos[j].Attribute_Values[k] = fi.Binarize((newCuckoos[j].Attribute_Values[k] + stepSize) * SimpleRNG.GetNormal(), rand.NextDouble());

                        if (cuckoos[j].Attribute_Values[k] == 1 && newCuckoos[j].Attribute_Values[k] == 0)
                            x++;
                    }
                }

                //discovery and randomization - replace some nest by constructing new solutions
                newCuckoos = EmptyNest(cuckoos, newCuckoos, discoveryRate, subsetSize, nNests);

                //Select best solutions from the original population and matured population for the next generation;
                fpa.SelectBestSolution(cuckoos, newCuckoos);

                //evaluate new solution
                newCuckooFitnessVal = EvaluateObjectiveFunction(newCuckoos, prob); //evaluate fitness value for all the bats
                CuckooFitness(newCuckooFitnessVal, newCuckoos); //fitness value for new bats
                globalBestCuckoo = EvaluateSolution(cuckooFitnessVal, newCuckooFitnessVal, globalBest, cuckoos, newCuckoos, globalBestCuckoo); //get the global best flower
                globalBest = globalBestCuckoo.Fitness;

                //if solution has converged to a optimal user-defined point, stop search
                int Max = 60;// maximum percentage reduction
                if (globalBest >= Max) //if the percentage reduction has approached 60%, stop search!
                    break;
            }

            //ensure that at least, N instances are selected for classification
            int min = 40; //minimum number of selected instances
            globalBestCuckoo = fpa.AddInstances(globalBestCuckoo, min);
            
            Problem subBest = fi.buildModelMultiClass(globalBestCuckoo, prob); //build model for the best Instance Mast
            storagePercentage = Training.StoragePercentage(subBest, prob); //calculate the percent of the original training set was retained by the reduction algorithm
            return subBest;
        }

        /// <summary>
        /// Evaluate Objective Function
        /// </summary>
        //public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> fireflies, List<double> accuracy, Problem prob)
        public double[] EvaluateObjectiveFunction(List<ObjectInstanceSelection> Cuckoos, Problem prob)
        {
            int NF = Cuckoos.Count; //NF -> number of fireflies
            int tNI = Cuckoos.ElementAt(0).Attribute_Values.Count(); //size of each Instance Mask
            double[] fitness = new double[NF];
            int sum;

            List<double> classes = fi.getClassLabels(prob.Y); //get the class labels
            int nClass = classes.Count;

            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();

            double C, Gamma;

            for (int i = 0; i < NF; i++)
            {
                //building model for each instance in instance mask in each firefly object
                Problem subProb = fi.buildModel(Cuckoos.ElementAt(i), prob);

                Parameter param = new Parameter();
                if (subProb != null)
                {
                    if (nClass == 2)
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
                    }

                    Problem subP = Training.ClusteringBoundaryInstance(subProb);
                    //int subProbCount = subP.Count; //number of selected boundary instances
                    int count = Cuckoos.ElementAt(i).__Attribute_Values.Count(q => q == 1); //total number of selected instances, to be used for subsetSize
                    //double percentageReduction = 100 * (tNI - count) / tNI; //calculating percentage reduction for each instance Mask
                    //double perRedBnstances = (double)(subProb.Count - subProbCount) / subProb.Count; //percentage reduction for boundary instances
                    //double perRedBInstances = (double)(subProb.Count - subP.Count) * subP.Count; //percentage reduction for boundary instances
                    double perRedBInstances = (double)(subProb.Count / subP.Count); //percentage reduction for boundary instances
                    double perRedCuckooInstances = (double)(tNI - count) / tNI; //percentage reduction for cuckoo instances

                    //fitness[i] = perRedCuckooInstances * 100;
                    fitness[i] = (100 * perRedCuckooInstances) + perRedBInstances;
                    //fitness[i] = 100 * ((double)count / (double)tNI);
                    //fitness[i] = 100 * perRedBnstances;
                    //fitness[i] = 100 * (perRedBnstances + perRedCuckooInstances);
                    //fitness[i] = (W_SeelctedBoundaryInstances * subProbCount) + (W_Instances * ((tNI - count) / tNI));
                    //fitness[i] = percentageReduction;
                }
            }

            return fitness;
        }


        //Replace some nests by constructing new solutions/nests
        // A fraction of worse nests are discovered with a probability of "discoveryRate"
        public List<ObjectInstanceSelection> EmptyNest(List<ObjectInstanceSelection> cuckoos, List<ObjectInstanceSelection> newCuckoos, double discoveryRate, int subsetSize, int nNests)
        {
            Random r = new Random();
            int[] arr1 = new int[cuckoos.Count];
            int[] arr2 = new int[cuckoos.Count];
            arr1 = Shuffle(arr1); //perform random permutation
            arr2 = Shuffle(arr2); //perform random permutation
            
            // In the real world, if a cuckoo's egg is very similar to a host's eggs, then this cuckoo's egg is less likely to be discovered, thus the fitness should 
            //be related to the difference in solutions.  Therefore, it is a good idea to do a random walk in a biased way with some random step sizes.  
            for (int i = 0; i < nNests; i++)
            {
                for (int j = 0; j < subsetSize; j++)
                {
                    double rand = r.NextDouble();
                    double K = rand > discoveryRate ? 1 : 0;
                    double stepWise = r.NextDouble() * (cuckoos[arr1[i]].Attribute_Values[j] - cuckoos[arr2[i]].Attribute_Values[j]);
                    double A = cuckoos[i].Attribute_Values[j] + stepWise * K;
                    double B = fi.Binarize(A, r.NextDouble());
                    
                    //change, only when better solution is found; that is, when 1 is changed to 0, and not vice-versa
                    if (newCuckoos[i].Attribute_Values[j] == 1 && A == 0)
                        newCuckoos[i].Attribute_Values[j] = fi.Binarize(A, r.NextDouble());
                }
            }

            return newCuckoos;
        }

        //Shuffle Array, i.e. perform random permutation of array. Reference: http://stackoverflow.com/questions/14535274/randomly-permutation-of-n-consecutive-integer-number
        public int[] Shuffle(int[] arr)
        {
            //create an integer array with increasing consecutive numbers
            for (int i = 0; i < arr.Count(); i++)
            {
                arr[i] = i;
            }

            //Shuffle, using Knuth / Fisher–Yates shuffle. 
            Random random = new Random();
            int n = arr.Count();
            while (n > 1)
            {
                n--;
                int i = random.Next(n + 1);
                int temp = arr[i];
                arr[i] = arr[n];
                arr[n] = temp;
            }

            return arr;
        }

        

        /// <summary>
        /// generating the initial locations of n Cuckoo
        /// </summary>
        public List<ObjectInstanceSelection> InitializeBinaryCuckoo(int nNests, int subsetSize, int probSize, Problem prob)
        {
            //Random rnd = new Random();
            //List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers

            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            //int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            //create an array of size n for x and y
            Random rnd = new Random();
            //List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N random numbers
            int[] xn = new int[subsetSize]; //instance mask
            double[] xn_Con = new double[subsetSize]; //instance mask continuous
            
            //int[] pointers = new int[subsetSize]; //array contain pointer to actual individual instance represented in the instance mask
            List<double> classes = fi.getClassLabels(prob.Y); //get the class labels
            int nClass = classes.Count;
            int div = subsetSize / nClass;

            //double freq = new double(); //initialize the frequency of all the bats to zero
            //double[] vel = new double[subsetSize]; //initialize the velocity of all the bats to zero
            
            //select pointers to instances for all the particles
            

            //int k = 0;
            if (nClass > 2) //do this for multi-class problems
            {
                int[] pointers = Training.AssignClassPointers_MultipleClass(prob, subsetSize, probSize); //array contain pointer to actual individual instance represented in the instance mask
                for (int a = 0; a < nNests; a++)
                {
                    xn = new int[subsetSize]; //instance mask
                    xn_Con = new double[subsetSize]; //instance mask continuous

                    for (int j = 0; j < subsetSize; j++)
                    {
                        xn[j] = rnd.Next(0, 2);
                    }

                    //Training.InstanceMask_MultipleClass(prob, subsetSize, probSize, out xn); //initialize instance mask
                    ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, pointers, 0.0);
                    attr_values.Add(OI);
                }
            }
            else //do this for binary class problem
            {
                int[] pointers = Training.AssignClassPointersBinary(prob, probSize, subsetSize); //array contain pointer to actual individual instance represented in the instance mask
                for (int i = 0; i < nNests; i++)
                {
                    xn = new int[subsetSize];
                    xn_Con = new double[subsetSize];
                    //pointers = new int[subsetSize];
                    //cnt1 = 0; cnt2 = 0; cnt3 = 0;
                    
                    for (int j = 0; j < subsetSize; j++)
                    {
                        xn[j] = rnd.Next(0, 2);
                    }

                    //Training.InstanceMask_Binary(prob, subsetSize, pointers, out xn);
                    ObjectInstanceSelection OI = new ObjectInstanceSelection(xn, xn_Con, pointers, 0.0);
                    attr_values.Add(OI);
                    
                    //for (int j = 0; j < prob.Count; j++)
                    //{
                    //    if (cnt1 < (0.7 * subsetSize) && prob.Y[rNum[j]] == -1) //select 70% positive instance of the subset
                    //    {
                    //        xn[cnt3] = rnd.Next(0, 2);
                    //        pointers[cnt3] = rNum[j];
                    //        k++; cnt1++; cnt3++;
                    //    }
                    //    else if (cnt2 < (0.3 * subsetSize) && prob.Y[rNum[j]] == 1)
                    //    {
                    //        xn[cnt3] = rnd.Next(0, 2);
                    //        pointers[cnt3] = rNum[j];
                    //        k++; cnt2++; cnt3++;
                    //    }
                    //    if (cnt3 >= subsetSize)
                    //        break;
                    //}

                    
                }
            }

            return attr_values;
        }

        //get fitness value for each bat
        public static void CuckooFitness(double[] fitVal, List<ObjectInstanceSelection> Cuckoos)
        {
            for (int i = 0; i < fitVal.Count(); i++)
                Cuckoos[i].Fitness = fitVal[i];
        }

        //evaluate new cuckoo solution, update better solution (if found), and get global cuckoo
        public ObjectInstanceSelection EvaluateSolution(double[] cuckooFitnessVal, double[] newCuckooFitnessVal, double globalBest, List<ObjectInstanceSelection> Cuckoos, List<ObjectInstanceSelection> newCuckoos, ObjectInstanceSelection globalBestCuckoo)
        {
            double newBest = new double();
            int maxIndex;
            Random r = new Random();

            //evaluate solution and update, if better solution is found
            for (int i = 0; i < cuckooFitnessVal.Count(); i++)
            {
                if (newCuckoos[i].Fitness > Cuckoos[i].Fitness)
                {
                    Cuckoos[i] = new ObjectInstanceSelection(newCuckoos[i].Attribute_Values, newCuckoos[i].Attribute_Values_Continuous, newCuckoos[i].Pointers, newCuckoos[i].Fitness); //create a clone of flowers
                    cuckooFitnessVal[i] = newCuckoos[i].Fitness;
                    //bats[i] = newBats[i]; //update solution
                }
            }

            //get blobal best flower
            newBest = newCuckooFitnessVal.Max(); //get the flower with the highest fitness
            if (newBest > globalBest)
            {
                globalBest = newBest;
                maxIndex = Array.IndexOf(newCuckooFitnessVal, newBest); //select the index for the global best
                globalBestCuckoo = new ObjectInstanceSelection(newCuckoos[maxIndex].Attribute_Values, newCuckoos[maxIndex].Attribute_Values_Continuous, newCuckoos[maxIndex].Pointers, newCuckoos[maxIndex].Fitness); //create a clone of flowers; //select the global best flower
                //globalBestBat = newBats[maxIndex]; //select the global best flower
            }

            return globalBestCuckoo;
        }

    }
}
