using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    //This idea was gotten from the paper: "An Ant Colony Optimization Algorithm For Image Edge Detection". Paper applied it to image detection
    //Most of the constant values used was gotten from the same paper paper
    //Code was adapted from: https://msdn.microsoft.com/en-us/magazine/hh781027.aspx
    public class AntColonyOptimizationBoundarySelection
    {
        private static Random random = new Random(0);

        private static int alpha = 1; // influence of pheromone on direction; the weighting factor of the pheromone information
        private static int beta = 2; // influence of adjacent node distance; the weighting factor of the heuristic information
        private static double rho = 0.1;  // pheromone decrease factor or evaporation rate
        private static double Q = 2.0; // pheromone increase factor
        private static double DC = 0.05;  // pheromone decay coefficient
        private static double initialPheVal = 0.01; //initial pheromone value

        public Problem ACOBoundarySelection(Problem trainDataset, int subsetSize, int kNum)
        {
            int n = trainDataset.Count;
            double[] heuristicInformation = new double[subsetSize];
            int probSize = trainDataset.X.Count(); //problem size
            List<int> rNum = Training.GetRandomNumbers(probSize, probSize); //generate N distinct random numbers
            int[] nodePointers = new int[subsetSize];
            Random rnd = new Random();
            List<ObjectInstanceSelection> attr_values = new List<ObjectInstanceSelection>();
            int numNodes = subsetSize; //pheromone size
            int k = 8; //connectivity neighborhood
            int steps = 40; //total number of ant’s movement-steps within each construction-step
            double decayCoeficient = 0.05; //the pheromone decay coefficient
            int nRuns = 10;
            int nAnts = 5;
            
            //select the pointers to the node subsets
            int cnt1 = 0, cnt2 = 0, cnt3 = 0;
            for (int i = 0; i < trainDataset.Count; i++)
            {
                if (cnt1 < (0.7 * subsetSize) && trainDataset.Y[rNum[i]] == -1) //select 70% negative instance of the subset
                {
                    nodePointers[cnt3] = rNum[i]; //get pointer for node i
                    cnt1++; cnt3++;
                }
                else if (cnt2 < (0.3 * subsetSize) && trainDataset.Y[rNum[i]] == 1) //select 30% positive instance of the subset
                {
                    nodePointers[cnt3] = rNum[i]; //get pointer for node i
                    cnt2++; cnt3++;
                }

                if (cnt3 >= subsetSize) //break from the loop, immediately specified number of pointers to instances has been selected from the dataset
                    break;
            }
            
            //compute K-nearest neighbours for all the instances in the subset; also, compute the initial pheromone value for each node
            List<double> heuristicValList = new List<double>(); //list contains summed distance of NNs belonging to the opposite class - this is used for pheromone value
            List<List<double>> listDistance = new List<List<double>>(); //Data structure holding the list of all the distances;
            List<int[]> kNNList = ComputeKNearestNeighbour(trainDataset, nodePointers, k, out heuristicValList);

            int[][] ants = InitAnts(nAnts, subsetSize);
            double[][] pheromones = InitPheromones(subsetSize, k);
            int[][] heuristic = InitHeuristicVal(subsetSize, k, heuristicValList, kNNList, nodePointers);

            Dictionary<int, int[]> pointer_kNN = new Dictionary<int, int[]>();
            for (int i = 0; i < subsetSize; i++) //this is to preserve the original structure of nodePointers and corresponding kNN, for easy reference
                pointer_kNN[nodePointers[i]] = kNNList[i];

            for (int i = 0; i < nRuns; i++)
            {
                    MoveAnts(ants, pheromones, heuristic, steps); //move ants for N number of steps, where N = steps
                    UpdateAllAntPheromones(pheromones, initialPheVal, decayCoeficient); //perform second update, after all ants has been moved
            }

            //select the boundary node - that is, node with the highest pheromone value
            double highest = double.MinValue;
            int boundaryInstancePointer = 0; //pointer to boundary instances
            for (int i = 0; i < pheromones.Length; i++)
            {
                for (int j = 0; j < pheromones[0].Length; j++)
                {
                    if (highest < pheromones[i][j])
                    {
                        highest = pheromones[i][j];
                        boundaryInstancePointer = pointer_kNN.ElementAt(i).Value[j]; //get the node pointer of the current highest
                    }
                }
            }

            int[] boundaryInstanceIndexes = BoundaryInstancePointers(trainDataset, boundaryInstancePointer, kNum, nodePointers); //select pointers to NNs
            Problem boundaryInstances = buildModel(trainDataset, boundaryInstanceIndexes.ToList()); //use pointers to select boundary instances from training dataset

            return boundaryInstances;


            //ACO_TravelingSalesManProblem(trainDataset, nodePointers, k, out heuristicValList, out listDistance);

            /***
            
            //initialize ant position, pheromone value and heuristic information
            double AntPosition = 0;
            double initialPheVal = 0.01; //initial pheromone value
            for (int j = 0; j < subsetSize; j++)
            {
                //pheromoneValue[j] = initialPheromoneValue; //set initial value for each node
                //heuristicInformation[j] = totFeatVal[j] / grandSum; //set heuristic information for each node
                //get the object for each node. Each node is a collection of: Node pointer, pheromone value, heristic information and pointers to NN of node
                ObjectInstanceSelection oi = new ObjectInstanceSelection(nodePointers[j], initialPheVal, initialPheVal, heuristicValList[j], AntPosition, kNNList[j]);
                //ObjectInstanceSelection oi = new ObjectInstanceSelection(nodePointers[j], heuristicValList[j], heuristicValList[j], heuristicValue, AntPosition, kNNList[j]);
                //ObjectInstanceSelection oi = new ObjectInstanceSelection(nodePointers[j], pheromoneValue[j], heuristicInformation[j], AntPosition, kNNList[j]);
                attr_values.Add(oi);
            }
            
            //get initial pheromone pheromone value for all the instances in the subset
            //double[] initialPheromoneValue = 

            //get the pointers to each nodes - members of node subset are randomly selected from dataset
            //also, assign initial value to each node, and heuristic information
            //double[] totFeatVal = GetTotalFeatureValue(trainDataset, nodePointers); //add up the vector values features for each selected instances - this is for heuristic information
             
            //perform ACO
            //randomly select k-th ant and move it for L steps, L is user-defined.
            List<int> r = Training.GetRandomNumbers(subsetSize, subsetSize); //Gnerate N distinct random numbers. Ants are randomly selected
            for (int i = 0; i < nRuns; i++)
            {
                for (int j = 0; j < numNodes; j++)
                {

                    int nodePo = r[j]; //select pointer of the randomly selected ant 
                    for (int a = 0; a < L; a++)
                    {
                        int r2 = rnd.Next(0, k); //randomly select neighboring node that ant should move to - ant can only move within its neighbourhood list, hence generated random number must be between 0 and k
                        int NN_NodePointer = attr_values[nodePo].Pointers[r2]; //select pointer of the current location of ant, i.e. the neighbouring node ant moved to
                        int NNindex = Array.IndexOf(nodePointers, NN_NodePointer); //select the index of the pointer from nodePointers Array. Pointer index in nodePointers array is equivalent to object index in attr_values
                        double pheromoneVal = attr_values[NNindex].PheromoneValue; //get pheromone value of the current location of ant - the node ant moved to
                        double heuristicInfo = attr_values[NNindex].HeuristicInformation; //get heuristic information of the current location of ant - the node ant moved to

                        //perform addition of the resultant value of pv * hi, for all the neighbouring nodes of the current ant
                        double sum = 0;
                        for (int b = 0; b < k; b++)
                        {
                            int NN_NodeP = attr_values[nodePo].Pointers[b];
                            int NNind = Array.IndexOf(nodePointers, NN_NodeP); //select the index of the pointer from the array of pointers, that is, from node pointers
                            double A = attr_values[NNind].PheromoneValue;
                            double B = attr_values[NNind].HeuristicInformation;
                            sum += Math.Pow(A, alpha) * Math.Pow(B, beta);
                        }
                        attr_values[nodePo].Position = (Math.Pow(pheromoneVal, alpha) * Math.Pow(heuristicInfo, beta)) / sum; //transition probability indicating movement

                        //update the pheromone value of the current location of ant, after each step movement
                        double C = (1 - evaporationRate) * pheromoneVal;
                        double D = evaporationRate * heuristicInfo;
                        attr_values[NNindex].PheromoneValue = C + D;
                    }

                    //perform second update after movements of all ants
                    //double E = (1 - decayCoeficient) * attr_values[nodePo].PheromoneValue;
                    //double F = decayCoeficient * attr_values[nodePo].InitialPheromoneValue;
                    //attr_values[nodePo].PheromoneValue = E + F; //update pheromone value
                }

                //perform second update after movements of all ants
                for (int a = 0; a < numNodes; a++)
                {
                    double E = (1 - decayCoeficient) * attr_values[a].PheromoneValue;
                    double F = decayCoeficient * attr_values[a].InitialPheromoneValue;
                    attr_values[a].PheromoneValue = E + F; //update pheromone value
                }
            }

            //select the node with the highest pheromone value, this will be the boundary instace
            double[] phV = new double[subsetSize];
            for (int i = 0; i < subsetSize; i++)
            {
                phV[i] = attr_values[i].PheromoneValue;
            }
            double maxPheromoneValue = phV.Max(); //select maximum pheromoneValue
            int boundaryInstanceIndex = Array.IndexOf(phV, maxPheromoneValue); //select index of max value
            //int nodePointerMax = attr_values[index].NodePointer; //select node pointer with maximum value, that is, the boundary node or instance
            ***/


            //select boundary instances, using boundary node as reference
           
            
            /***
            //calculate threshold for decision making
            double threshold = CalculateThreshold(attr_values, toleranceLevel);
            
            //select pointers for boundary nodes, and apply threshold on all nodes to detect boundary nodes
            List<int> boundaryNodePointers = new List<int>();
            for (int i = 0; i < numNodes; i++)
            {
                if (attr_values[i].PheromoneValue >= threshold)
                    boundaryNodePointers.Add(attr_values[i].NodePointer); //select pointer to boundary nodes
            }

            int cp = 0, cn = 0;
            for (int i = 0; i < boundaryNodePointers.Count; i++)
            {
                if (trainDataset.Y[boundaryNodePointers[i]] == -1)
                    cn++;
                else if (trainDataset.Y[boundaryNodePointers[i]] == 1)
                    cp++;
            }

            //select pointer to boundary instances
            List<int> boundaryInstancePointers = SelectBoundaryInstancePointers(trainDataset, boundaryNodePointers, subsetSize, nodePointers); 
            
            //use pointers to select boundary instances from training dataset
            Problem boundaryInstances = buildModel(trainDataset, boundaryInstancePointers);
             **/
        }

        public void ACO_TravelingSalesManProblem(Problem trainDataset, int[] nodePointers, int k, out List<double> heuristicValList, out List<List<double>> listDistance)
        {
            //pair the pointer and index together, so that we can get the value, after shuffling
            Dictionary<int, int> index_pointer = new Dictionary<int, int>();
            int nAnts = 5;
            listDistance = new List<List<double>>(); //Data structure holding the list of all the distances;
            heuristicValList = new List<double>();
            for (int i = 0; i < nodePointers.Count(); i++)
            {
                index_pointer[i] = nodePointers[i];
            }


            Console.WriteLine("\nBegin Ant Colony Optimization demo\n");
            Console.WriteLine("\nInitialing ants to random trails\n");
            int nodeSize = listDistance[0].Count;
            int[][] ants = InitAnts(nAnts, nodeSize);//initialize ants

            //convert list to int[][] array for easy simulation of ready-made code. Check Reference above
            int[][] dists = new int[listDistance.Count][];
            for (int i = 0; i < listDistance.Count; i++)
            {
                dists[i] = new int[nodeSize];
                for (int j = 0; j < nodeSize; j++)
                {
                    dists[i][j] = (int)listDistance[i][j];
                }
            }

            //int[][] heuristicVals = MakeHeuristicValues(subsetSize, ants, heuristicValList, index_pointer, nodePointers); //get the heuristic value for each ant

            ShowAnts(ants, dists);

            int bestIndex; //index of ant with the best trail
            int[] bestTrail = BestTrail(ants, dists, out bestIndex); //get the best trails
            double bestLength = Length(bestTrail, dists); // the length of the best trail
            Console.Write("\nBest initial trail length: " + bestLength.ToString("F1") + "\n");

            Console.WriteLine("\nInitializing pheromones on trails");
            //double[][] pheromones = InitPheromones(nodeSize); //initialize pheromones
            double[][] pheromones = InitPheromones(nodeSize, 8); //initialize pheromones

            Console.WriteLine("\nEntering UpdateAnts - UpdatePheromones loop\n");
            int time = 0;
            int maxTime = 50;
            while (time < maxTime)
            {
                UpdateAnts(ants, pheromones, dists);
                UpdatePheromones(pheromones, ants, dists);

                int[] currBestTrail = BestTrail(ants, dists, out bestIndex);
                double currBestLength = Length(currBestTrail, dists);
                if (currBestLength < bestLength)
                {
                    bestLength = currBestLength;
                    bestTrail = currBestTrail;
                    Console.WriteLine("New best length of " + bestLength.ToString("F1") + " found at time " + time);
                }
                time += 1;
            }

            Console.WriteLine("\nTime complete");

            Console.WriteLine("\nBest trail found:");
            Display(bestTrail);
            Console.WriteLine("\nLength of best trail found: " + bestLength.ToString("F1"));

            Console.WriteLine("\nEnd Ant Colony Optimization demo\n");

            //select node with the best pheromone, from the best trail
            double[] bestPheromoneTrail = pheromones[bestIndex]; //select the pheromone associated with the best trail - best pheromone trail
            double best = bestPheromoneTrail[0];
            int idxBest = bestTrail[0];
            for (int i = 0; i < bestPheromoneTrail.Length; i++)
            {
                if (best < bestPheromoneTrail[i])
                {
                    best = bestPheromoneTrail[i];
                    idxBest = bestTrail[i];
                }
            }

            int boundaryNodePointer = index_pointer[idxBest]; //select the exact pointer of the best pheromone - this is the pointer to the boundary instance
            int boundaryInstanceIndex = Array.IndexOf(nodePointers, boundaryNodePointer); //select index of max value
            //Console.ReadLine();
        }

        private static void Display(int[] trail)
        {
            for (int i = 0; i <= trail.Length - 1; i++)
            {
                Console.Write(trail[i] + " ");
                if (i > 0 && i % 20 == 0)
                {
                    Console.WriteLine("");
                }
            }
            Console.WriteLine("");
        }

        //private static void ShowAnts(int[][] ants, int[][] heuristicVals)
        private static void ShowAnts(int[][] ants, int[][] distances)
        {
            for (int i = 0; i <= ants.Length - 1; i++)
            {
                Console.Write(i + ": [ ");

                for (int j = 0; j <= 3; j++)
                {
                    Console.Write(ants[i][j] + " ");
                }

                Console.Write(". . . ");

                for (int j = ants[i].Length - 4; j <= ants[i].Length - 1; j++)
                {
                    Console.Write(ants[i][j] + " ");
                }

                Console.Write("] len = ");
                double len = Length(ants[i], distances);
                Console.Write(len.ToString("F1"));
                Console.WriteLine("");
            }
        }


        //get the best trails for each ant
        private static int[] BestTrail(int[][] ants, int[][] heuristicVals, out int idxBestLength)
        {
            // best trail has shortest total length
            double bestLength = Length(ants[0], heuristicVals);
            idxBestLength = 0; //index of ant with the best trail
            for (int k = 1; k <= ants.Length - 1; k++)
            {
                double len = Length(ants[k], heuristicVals);
                if (len < bestLength)
                {
                    bestLength = len;
                    idxBestLength = k;
                }
            }
            int numCities = ants[0].Length;
            //INSTANT VB NOTE: The local variable bestTrail was renamed since Visual Basic will not allow local variables with the same name as their enclosing function or property:
            int[] bestTrail_Renamed = new int[numCities];
            ants[idxBestLength].CopyTo(bestTrail_Renamed, 0);
            return bestTrail_Renamed;
        }


        //assign heuristic values to all ants
        static int[][] MakeHeuristicValues(int subsetSize, int[][] Ants, List<double> heuristicValList, Dictionary<int, int> index_pointer, int[] nodePointers)
        {
            Random r = new Random();
            int[][] heuristicVals = new int[subsetSize][];

            for (int i = 0; i < heuristicVals.Length; i++)
                heuristicVals[i] = new int[subsetSize];

            for (int i = 0; i < subsetSize; ++i)
            {
                for (int j = i+1; j < subsetSize; j++)
                {
                    int idx = Ants[i][j]; //get the index of the current instanceindex
                    int InstanceIndex = index_pointer[idx];
                    int hPointer = Array.IndexOf(nodePointers, InstanceIndex); //select the index of the instance in the current ant trail, that is, its index in nodePointer. 
                    int hVal = (int)heuristicValList[hPointer]; //get the heuristic value for the instance in the current ant trail, using the selected index - since 'heuristicValList' and 'nodePointer' are arranged similarly
                    heuristicVals[i][j] = hVal; //assign heuristic value
                }
            }
            
            return heuristicVals;
        }

        //data structure for assigning heuristic values
        //static double SetHeuristicValue(int nodeX, int nodeY, int[][] heuristicVals)
        static double SetHeuristicValue(int nodeX, int nodeY, int[][] heuristicVals)
        {
            return heuristicVals[nodeX][nodeY];
        }

        //initialize ants with random trails
        static int[][] InitAnts(int numAnts, int subsetSize)
        {
            int[][] ants = new int[numAnts][];
            for (int k = 0; k < numAnts; ++k)
            {
                int start = random.Next(0, subsetSize);
                ants[k] = RandomTrail(start, subsetSize);
            }
            return ants;
        }

        //set trail for all ants
        static int[] RandomTrail(int start, int subsetSize)
        {
            int[] trail = new int[subsetSize];

            //assign instance index to each trail
            for (int i = 0; i < subsetSize; ++i) 
            { 
                trail[i] = i; 
            }

            //Shuffle trail using Fisher-Yates shuffle algorithm to randomize the order of the cities in the trail
            for (int i = 0; i <= subsetSize - 1; i++)
            {
                int r = random.Next(i, subsetSize);
                int tmp = trail[r]; trail[r] = trail[i]; trail[i] = tmp;
                //int t = nodePointers[r]; nodePointers[r] = nodePointers[i]; nodePointers[i] = t; 
            }

            // put start at [0]
            int idx = IndexOfTarget(trail, start);
            int temp = trail[0]; trail[0] = trail[idx]; trail[idx] = temp;
            //int t2 = nodePointers[0]; nodePointers[0] = nodePointers[idx]; nodePointers[idx] = t2; 

            return trail;
        }

        //get the target index or index of start node
        private static int IndexOfTarget(int[] trail, int target)
        {
            // helper for RandomTrail
            for (int i = 0; i <= trail.Length - 1; i++)
            {
                if (trail[i] == target)
                {
                    return i;
                }
            }
            throw new Exception("Target not found in IndexOfTarget");
        }

        //initialize pheromones for all ants
        //static double[][] InitPheromones(int subsetSize)
        static double[][] InitPheromones(int subsetSize, int k)
        {
            //double[][] pheromones = new double[subsetSize][];
            double[][] pheromones = new double[subsetSize][];
            for (int i = 0; i < subsetSize; ++i)
                pheromones[i] = new double[k];
            for (int i = 0; i < pheromones.Length; ++i)
                for (int j = 0; j < pheromones[i].Length; ++j)
                    pheromones[i][j] = 0.01;
            return pheromones;
        }

        //initialize heuristic values
        static int[][] InitHeuristicVal(int subsetSize, int k, List<double> hValList, List<int[]> kNNList, int[] nodePointer)
        {
            int[][] hVal = new int[subsetSize][];
            for (int i = 0; i < subsetSize; ++i)
                hVal[i] = new int[k];
            for (int i = 0; i < subsetSize; ++i)
            {
                for (int j = 0; j < k; ++j)
                {
                    int A = kNNList[i][j]; //get the pointer to the current neigboring node
                    int idx = Array.IndexOf(nodePointer, A); //get the position of the pointer in nodePointer, since nodePointer and hValList have similar arrangement. This is to get the actual pheromone value 
                    hVal[i][j] = (int)hValList[idx]; //assign heuristic value to neghbring ant
                }
            }

            return hVal;
        }

        //update ants. This is the core of ACO algorithm. This updates, and tries to construct a new trail-maybe better trail based on the pheromone and distance information
        static void MoveAnts(int[][] ants, double[][] pheromones, int[][] heuristicVals, int steps)
        {
            int subsetsize = pheromones.Length;
            for (int k = 0; k < ants.Length; ++k)
            {
                for (int m = 0; m < steps; m++)
                {
                    int currentNode = random.Next(0, subsetsize); //randomly select node position, that ant should move
                    int[] newTrail = BuildAntTrail(k, currentNode, pheromones, heuristicVals); //trail of neighborhood nodes of current node
                    UpdateAntPheromones(pheromones, ants, heuristicVals, currentNode, newTrail); //update pheromone, after each step
                }
            }
        }

        //build ant trail
        static int[] BuildAntTrail(int k, int currentNode, double[][] pheromones, int[][] heuristicVals)
        {
            int subsetSize = pheromones[0].Length;
            int[] trail_NN = new int[subsetSize]; //neighourhood nodes trail
            //trail[0] = start;
            for (int i = 0; i < subsetSize; ++i)
            {
                //int nodeX = trail[i];
                int nodeX = currentNode;
                int next = MoveToNextNode(k, nodeX, pheromones, heuristicVals); //move ant to neighboring nodes, of curremt node. Current node is stored in nodeX
                trail_NN[i] = next;
            }
            return trail_NN;
        }

        //move ant to next node
        static int MoveToNextNode(int k, int nodeX, double[][] pheromones, int[][] heuristicVals)
        {
            double[] probs = AntMovementProbs(k, nodeX, pheromones, heuristicVals);

            double[] cumul = new double[probs.Length + 1];
            for (int i = 0; i < probs.Length; ++i)
                cumul[i + 1] = cumul[i] + probs[i];

            double p = random.NextDouble();

            for (int i = 0; i < cumul.Length - 1; ++i)
                if (p >= cumul[i] && p < cumul[i + 1])
                    return i;
            throw new Exception("Failure to return valid city in NextCity");
        }

        //calculating transition probability for each ant movement
        static double[] AntMovementProbs(int k, int nodeX, double[][] pheromones, int[][] heuristicVals)
        {
            int NNSize = pheromones[0].Length; //size of neighboring node
            double[] taueta = new double[NNSize];
            double sum = 0.0;
            for (int i = 0; i < taueta.Length; ++i)
            {
                if (i == nodeX)
                    taueta[i] = 0.0; // Prob of moving to self is zero
                else
                {
                    taueta[i] = Math.Pow(pheromones[nodeX][i], alpha) * Math.Pow((SetHeuristicValue(nodeX, i, heuristicVals)), beta);
                    //taueta[i] = Math.Pow(pheromones[nodeX][i], alpha) * Math.Pow((1.0 / SetHeuristicValue(nodeX, i, heuristicVals)), beta);
                    if (taueta[i] < 0.0001)
                        taueta[i] = 0.0001; //impose arbitrary min value, to avoid too small tauta value
                    else if (taueta[i] > (double.MaxValue / (NNSize * 100)))
                        taueta[i] = double.MaxValue / (NNSize * 100); //impose arbitrary max value, to avoid too large tauta value
                }
                sum += taueta[i];
            }

            double[] probs = new double[NNSize];
            for (int i = 0; i < probs.Length; ++i)
                probs[i] = taueta[i] / sum;
            return probs;
        }

        //update ant pheromone
        private static void UpdateAntPheromones(double[][] pheromones, int[][] ants, int[][] heuristicVals, int nodeIndex, int[] trail)
        {
            for (int j = 0; j < pheromones[0].Length; j++)
            {
                int indexVisited = trail[j]; //index of neighboring node that was visited by current ant
                double decrease = (1.0 - rho) * pheromones[nodeIndex][indexVisited];
                double increase = rho * heuristicVals[nodeIndex][indexVisited];
                pheromones[nodeIndex][indexVisited] = decrease + increase;

                if (pheromones[nodeIndex][indexVisited] < 0.0001)
                {
                    pheromones[nodeIndex][indexVisited] = 0.0001; //enforce minimum
                }
                else if (pheromones[nodeIndex][indexVisited] > 100000.0)
                {
                    pheromones[nodeIndex][indexVisited] = 100000.0; //enforce maximum
                }

                pheromones[nodeIndex][indexVisited] = pheromones[nodeIndex][indexVisited];
            }
        }

        //update all pheromones. This is for the second update
        private static void UpdateAllAntPheromones(double[][] pheromones, double initialPheromone, double decayCoef)
        {
            for (int i = 0; i < pheromones.Length; i++)
            {
                for (int j = 0; j < pheromones[0].Length; j++)
                {
                    double decrease = (1.0 - decayCoef) * pheromones[i][j];
                    double increase = decayCoef * initialPheromone;
                    pheromones[i][j] = decrease + increase;

                    if (pheromones[i][j] < 0.0001)
                    {
                        pheromones[i][j] = 0.0001; //enforce minimum
                    }
                    else if (pheromones[i][j] > 100000.0)
                    {
                        pheromones[i][j] = 100000.0; //enforce maximum
                    }

                    pheromones[i][j] = pheromones[i][j];
                }
            }
        }

        //update ants. This is the core of ACO algorithm. This updates, and tries to construct a new trail-maybe better trail based on the pheromone and distance information
        static void UpdateAnts(int[][] ants, double[][] pheromones, int[][] heuristicVals)
        {
            int subsetsize = pheromones.Length;
            for (int k = 0; k < ants.Length; ++k)
            {
                int start = random.Next(0, subsetsize);
                int[] newTrail = BuildTrail(k, start, pheromones, heuristicVals);
                ants[k] = newTrail;
            }
        }

        //build ant trail
        static int[] BuildTrail(int k, int start, double[][] pheromones, int[][] heuristicVals)
        {
            int subsetSize = pheromones[0].Length;
            int[] trail = new int[subsetSize];
            bool[] visited = new bool[subsetSize];
            trail[0] = start;
            visited[start] = true;
            for (int i = 0; i < subsetSize - 1; ++i)
            {
                int nodeX = trail[i];
                int next = MoveToNextNode(k, nodeX, pheromones, heuristicVals); //move ant to next node
                trail[i + 1] = next;
                visited[next] = true;
            }
            return trail;
        }
        
        //move ant to next node
        static int NextNode(int k, int nodeX, bool[] visited, double[][] pheromones, int[][] heuristicVals)
        {
            double[] probs = MoveProbs(k, nodeX, visited, pheromones, heuristicVals);

            double[] cumul = new double[probs.Length + 1];
            for (int i = 0; i < probs.Length; ++i)
                cumul[i + 1] = cumul[i] + probs[i];

            double p = random.NextDouble();

            for (int i = 0; i < cumul.Length - 1; ++i)
                if (p >= cumul[i] && p < cumul[i + 1])
                    return i;
            throw new Exception("Failure to return valid city in NextCity");
        }

        //calculating transition probability
        static double[] MoveProbs(int k, int nodeX, bool[] visited, double[][] pheromones, int[][] heuristicVals)
        {
            int subsetSize = pheromones.Length;
            double[] taueta = new double[subsetSize];
            double sum = 0.0;
            for (int i = 0; i < taueta.Length; ++i)
            {
                if (i == nodeX)
                    taueta[i] = 0.0; // Prob of moving to self is zero
                else if (visited[i] == true)
                    taueta[i] = 0.0; // Prob of moving to a visited node is zero
                else
                {
                    taueta[i] = Math.Pow(pheromones[nodeX][i], alpha) * Math.Pow((SetHeuristicValue(nodeX, i, heuristicVals)), beta);
                    //taueta[i] = Math.Pow(pheromones[nodeX][i], alpha) * Math.Pow((1.0 / SetHeuristicValue(nodeX, i, heuristicVals)), beta);
                    if (taueta[i] < 0.0001)
                        taueta[i] = 0.0001; //impose arbitrary min value, to avoid too small tauta value
                    else if (taueta[i] > (double.MaxValue / (subsetSize * 100)))
                        taueta[i] = double.MaxValue / (subsetSize * 100); //impose arbitrary max value, to avoid too large tauta value
                }
                sum += taueta[i];
            }

            double[] probs = new double[subsetSize];
            for (int i = 0; i < probs.Length; ++i)
                probs[i] = taueta[i] / sum;
            return probs;
        }

        //update pheromone
        private static void UpdatePheromones(double[][] pheromones, int[][] ants, int[][] heuristicVals)
        {
            for (int i = 0; i <= pheromones.Length - 1; i++)
            {
                for (int j = i + 1; j <= pheromones[i].Length - 1; j++)
                {
                    for (int k = 0; k <= ants.Length - 1; k++)
                    {
                        //double length = Length(ants[k], heuristicVals);
                        // length of ant k trail
                        double decrease = (1.0 - rho) * pheromones[i][j];
                        double increase = 0.0;
                        if (EdgeInTrail(i, j, ants[k]) == true)
                        {
                            //increase = (Q / length);
                            increase = rho * heuristicVals[i][j];
                        }
                        
                        pheromones[i][j] = decrease + increase;

                        if (pheromones[i][j] < 0.0001)
                        {
                            pheromones[i][j] = 0.0001; //enforce minimum
                        }
                        else if (pheromones[i][j] > 100000.0)
                        {
                            pheromones[i][j] = 100000.0; //enforce maximum
                        }

                        pheromones[j][i] = pheromones[i][j];
                    }

                    //for (int k = 0; k <= ants.Length - 1; k++)
                    //{
                    //    //double length = Length(ants[k], heuristicVals);
                        //// length of ant k trail
                        //double decrease = (1.0 - rho) * pheromones[i][j];
                        //double increase = 0.0;
                        //if (EdgeInTrail(i, j, ants[k]) == true)
                        //{
                        //    //increase = (Q / length);
                        //    increase = rho * heuristicVals[k][j];
                        //}

                        //pheromones[i][j] = decrease + increase;

                        //if (pheromones[i][j] < 0.0001)
                        //{
                        //    pheromones[i][j] = 0.0001;
                        //}
                        //else if (pheromones[i][j] > 100000.0)
                        //{
                        //    pheromones[i][j] = 100000.0;
                        //}

                        //pheromones[j][i] = pheromones[i][j];
                    //}
                }
            }
        }

        //determines if a segment between two cities is on the ant’s current trail
        private static bool EdgeInTrail(int nodeX, int nodeY, int[] trail)
        {
            // are cityX and cityY adjacent to each other in trail[]?
            int lastIndex = trail.Length - 1;
            int idx = IndexOfTarget(trail, nodeX);

            if (idx == 0 && trail[1] == nodeY)
            {
                return true;
            }
            else if (idx == 0 && trail[lastIndex] == nodeY)
            {
                return true;
            }
            else if (idx == 0)
            {
                return false;
            }
            else if (idx == lastIndex && trail[lastIndex - 1] == nodeY)
            {
                return true;
            }
            else if (idx == lastIndex && trail[0] == nodeY)
            {
                return true;
            }
            else if (idx == lastIndex)
            {
                return false;
            }
            else if (trail[idx - 1] == nodeY)
            {
                return true;
            }
            else if (trail[idx + 1] == nodeY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //get the total value (or length) for each trail
        //private static double Length(int[] trail, int[][] heuristicVals)
        private static double Length(int[] trail, int[][] distances)
        {
            // total length of a trail
            double result = 0.0;
            for (int i = 0; i <= trail.Length - 2; i++)
            {
                result += SetHeuristicValue(trail[i], trail[i + 1], distances);
            }
            return result;
        }

        /// <summary>
        /// generating the initial locations of n spiders
        /// </summary>
        public List<ObjectInstanceSelection> InitializeAnt(int nAnts, int subsetSize, int probSize, Problem prob)
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
            for (int i = 0; i < nAnts; i++)
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

        //sort array and retain index
        public int[] sortAndRetainIndex(double[] dist)
        {
            var sorted = dist
                .Select((x, index) => new { sor = x, Index = index })
                .OrderBy(x => x.sor);

            dist = sorted.Select(x => x.sor).ToArray();
            int[] idx = sorted.Select(x => x.Index).ToArray();

            return idx;
        }

        //calculate the threshold for making decision, decision to determine the boundary instance to select
        public double CalculateThreshold(List<ObjectInstanceSelection> nodes, double toleranceLevel)
        {
            //calculate the initial threshold
            double sum = 0;
            List<double> class1 = new List<double>();
            List<double> class2 = new List<double>();
            for (int i = 0; i < nodes.Count; i++)
            {
                sum += nodes[i].PheromoneValue;
            }
            double InitialThreshold = sum / nodes.Count;

            //calculate final threshold to be used for decision
            int j = 0;
            double currentThreshold = InitialThreshold;
            double newThreshold = 0;
            while (j == 0)
            {
                //divide nodes into two, according to their pheromone value. Split, using initial threshold as crieterion
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].PheromoneValue > currentThreshold)
                        class1.Add(nodes[i].PheromoneValue);
                    else
                        class2.Add(nodes[i].PheromoneValue);
                }
                newThreshold = (class1.Average() + class2.Average()) / 2;
                if ( (newThreshold - currentThreshold) > toleranceLevel)
                {
                    currentThreshold = newThreshold; //update threshold
                    continue;
                }
                else
                    break;
            }

            return newThreshold;
        }

        //Sum the feature vectors for each selected instances
        public double[] GetTotalFeatureValue(Problem prob, int[] nodePointers)
        {
            int nodeSize = prob.X[0].Count();

            //calculate total feature value for all the instances in the pheromone matrix

            double totFeatureValue = 0;
            double[] sum = new double[nodePointers.Count()];
            for (int i = 0; i < nodePointers.Count(); i++)
            {
                totFeatureValue = 0;
                for (int j = 0; j < nodeSize; j++)
                {
                    totFeatureValue += prob.X[nodePointers[i]][j].Value;
                }
                sum[i] = totFeatureValue;
            }

            return sum;
        }

        //compute K-Nearest Neighbours for each instance; also, compute the initial pheromone value for each node, this is returned as an 'out' parameter
        public List<int[]> ComputeKNearestNeighbour(Problem prob, int[] nodePointers, int k, out List<double> sumDistList)
        {
            int subsetSize = nodePointers.Count();
            List<int[]> listNN = new List<int[]>();
            //List<List<double>> listDistance = new List<List<double>>(); //Data structure holding the list of all the distances;
            //listDistance = new List<List<double>>(); //Data structure holding the list of all the distances;

            sumDistList = new List<double>();

            for (int i = 0; i < subsetSize; i++)
            {
                int a = 0;
                List<double> distList = new List<double>();
                List<int> listPointers = new List<int>();
                double distance;
                List<Dictionary<int, double>> distPointerList = new List<Dictionary<int, double>>();
                //Dictionary<int, double> pointer_NodeLabel = new Dictionary<int,double>();
                //List<Node[]> listNode = new List<Node[]>();
                List<double> labelList = new List<double>();
                double label_i = prob.Y[nodePointers[i]];
                List<double> allLabels = new List<double>();
                for (int j = 0; j < subsetSize; j++)
                {
                    if (j.Equals(i) /*|| label_i == lab_j*/) //dont compute instance i and instances that doesent belong to opposite class nearest neighbours. Get the k-NN of instances belonging to opposite class 
                        continue;
                    
                    distance = Kernel.computeSquaredDistance(prob.X[nodePointers[i]], prob.X[nodePointers[j]]);
                    distList.Add(distance); //compute the distance between Xi and all other instances in the subset
                    listPointers.Add(nodePointers[j]); //save the pointer
                    //listNode.Add(prob.X[nodePointers[j]]);
                    labelList.Add(prob.Y[nodePointers[j]]); //save label

                    //pointer_NodeLabel[nodePointers[j]] = prob.Y[nodePointers[j]]; //save distance and pointer - key=pointer, and value=Node
                }
                
                //sort to select NN, retain index to select the actual NN from pointers array
                var sorted = distList.Select((x, b) => new KeyValuePair<double, int>(x, b)).OrderBy(x => x.Key).ToList();
                List<int> index = sorted.Select(x => x.Value).ToList(); //select pointers to nearest neighbours for instance i
                List<double> dist = sorted.Select(y => y.Key).ToList(); //select the sorted distances

                //select pointer to K-nearest neighbours for instance i
                int[] NN = new int[k];
                                
                //select pointer to i-th NN of the current instance
                for (int j = 0; j < k; j++)
                {
                    NN[j] = listPointers[index[j]]; //save pointer to NN i of instance i
                }

                //sum distance of the nearest neighbour belonging to the opposite class
                double sumDist = 0; int ctr = 0;
                for (int j = 0; j < distList.Count; j++)
                {
                    double label_j = labelList[index[j]];
                    
                    if (label_i != label_j && ctr < k)
                    {
                        sumDist += distList[index[j]];
                        ctr++;
                    }

                    if (ctr == k)
                        break;
                }

                listNN.Add(NN);
                sumDistList.Add(sumDist);
                //listDistance.Add(distList);
            }

            return listNN;
        }

        //select pointers to boundary instances from one boundary instance
        public int[] BoundaryInstancePointers(Problem trainDataset, int boundaryNodePointer, int k, int[] nodePointers)
        {
            List<double> dist = new List<double>();
            List<int> listPointers = new List<int>();
            int idx = Array.IndexOf(nodePointers, boundaryNodePointer); //index of boundary node pointer
            for (int j = 0; j < nodePointers.Count(); j++)
            {
                if (idx == j)
                    continue;
                dist.Add(Kernel.computeSquaredDistance(trainDataset.X[nodePointers[idx]], trainDataset.X[nodePointers[j]])); // compute NN for each boundary instance
                listPointers.Add(nodePointers[j]); //save the pointer to instance
            }

            //sort to select NN, retain index to select the actual NN from pointers array
            var sorted = dist.Select((x, b) => new KeyValuePair<double, int>(x, b)).OrderBy(x => x.Key).ToList();
            List<int> index = sorted.Select(x => x.Value).ToList(); //select pointers to nearest neighbours for instance i
            List<double> sortedDist = sorted.Select(x => x.Key).ToList(); //select pointers to nearest neighbours for instance i
            
            /***
            //select pointer to K-nearest neighbours for instance i
            int count = sortedDist.Count(i => i == 0); //count the number of zeros
            k = count > k ? k : (k - count); //change the value of k if the the number of zero distances, is less than the user-defined value for k.
            int[] NN = new int[k]; int m = 0;
            for (int j = 0; j < sortedDist.Count; j++)
            {
                if (sortedDist[j] == 0) //don't consider instances having their distance to be zero. It is assumed that instance in this category is on the same position with the compared distance
                    continue;
                else
                {
                    NN[m++] = listPointers[index[j]]; //save pointer to NN i of instance i
                    if (m == k)
                        break;
                }
                    
            }**/

            int[] NN = new int[k]; 
            for (int j = 0; j < k; j++)
            {
                    NN[j] = listPointers[index[j]]; //save pointer to NN i of instance i
            }

            return NN;
        }


        //select pointers to boundary instances from a list of boundary instances
        public List<int> SelectBoundaryInstancePointers(Problem trainDataset, List<int> boundaryNodePointers, int subsetSize, int[] nodePointers)
        {
            List<int[]> NNPointerList = new List<int[]>(); 
            int kNum = 5; //k distances for each nearest neighbour for all the selected boundary instances
            for (int i = 0; i < boundaryNodePointers.Count; i++)
            {
                List<double> dist = new List<double>();
                List<int> listPointers = new List<int>();
                for (int j = 0; j < subsetSize; j++)
                {
                    if (boundaryNodePointers[i] == nodePointers[j])
                        continue;
                    dist.Add(Kernel.computeSquaredDistance(trainDataset.X[boundaryNodePointers[i]], trainDataset.X[nodePointers[j]])); // compute NN for each boundary instance
                    listPointers.Add(nodePointers[j]); //save the pointer to instance
                }

                //sort to select NN, retain index to select the actual NN from pointers array
                var sorted = dist.Select((x, b) => new KeyValuePair<double, int>(x, b)).OrderBy(x => x.Key).ToList();
                List<int> index = sorted.Select(x => x.Value).ToList(); //select pointers to nearest neighbours for instance i

                //select pointer to K-nearest neighbours for instance i
                int[] NN = new int[kNum];
                for (int j = 0; j < kNum; j++)
                    NN[j] = listPointers[index[j]]; //save pointer to NN i of instance i

                NNPointerList.Add(NN); //save nearest neighbours for each boundary instance
            }

            //collate arrays in NNPointerList to one array
            List<int> collList = new List<int>();
            for (int i = 0; i < NNPointerList.Count; i++)
            {
                for (int j = 0; j < kNum; j++)
                    collList.Add(NNPointerList[i][j]);
            }

            int[] distinct = collList.Distinct().ToArray(); //select distinct
            int vote;
            Dictionary<int, int> pointer_vote = new Dictionary<int, int>();
            for (int i = 0; i < distinct.Count(); i++)
            {
                vote = collList.Count(a => a == distinct[i]); //count the number of times each pointer appear in list;
                pointer_vote[distinct[i]] = vote;
            }

            //sort dictionary to select instances with highest votes
            var sortedNodes = from pair in pointer_vote
                              orderby pair.Value descending
                              select pair;

            //select instances with high votes
            List<int> boundaryPointers = new List<int>();
            int threshold = 3;
            for (int i = 0; i < distinct.Count(); i++)
            {
                if (sortedNodes.ElementAt(i).Key >= threshold)
                    boundaryPointers.Add(sortedNodes.ElementAt(i).Key); //select the pointers to boundary instances
            }

            return boundaryPointers;
        }

        // select model for boundary instances from dataset. This is to be used for training
        public Problem buildModel(Problem prob, List<int> boundaryInstancePointers)
        {
            int tNI = boundaryInstancePointers.Count; //size of each Instance Mask
            List<double> y = new List<double>();
            List<Node[]> x = new List<Node[]>();
            bool pos = false, neg = false;

            //building model for each instance in instance mask in each firefly object
            for (int j = 0; j < tNI; j++)
            {
                    int p = boundaryInstancePointers[j];
                    x.Add(prob.X[p]);
                    y.Add(prob.Y[p]);

                    if (prob.Y[p] == 1)
                        pos = true;
                    else if (prob.Y[p] == -1)
                        neg = true;
            }

            Node[][] X = new Node[x.Count][];
            double[] Y = new double[y.Count];

            //ensuring that the subproblem consist of both positive and negative instance
            int k = 0;
            int countP = y.Count(r => r == 1); //counting the total number of positive instance in the subproblem
            int countN = y.Count(r => r == -1); //counting the total number of negative instance in the subproblem
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

    }
}
