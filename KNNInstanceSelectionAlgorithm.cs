using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    public class KNNInstanceSelectionAlgorithm
    {
        //compute the k-nearest neighbour of all instances in the dataset
        public Problem computeNearestNeighbour(int k, Problem trainDataset, int numOfSubset)
        {
            double sum = 0;
            double distance;
            int n = trainDataset.Count; //number of data instances
            int subN = 400;
            //int n = 800; //number of data instances
            List<Node[]> nearestNeighbours = new List<Node[]>();
            List<double> dist = new List<double>();
            List<double> labels = new List<double>();
            List<int> index = new List<int>();
            Node[] xNodes = new Node[n];
            Node[] yNodes = new Node[n];
            object[,] obj = new object[subN, 4];
            List<object[,]> objList = new List<object[,]>();
            object[,] temp = new object[1, 3];
            List<Problem> ds = new List<Problem>();
            object[,] nn = new object[n, 7]; //data structure containing the NNs and their corresponding distances
            double score = 0; //score assigned to individual instance by the oppositiley NNs in its neighbourhood list
            object[,] scoreList = new object[n, 4]; //scores assigned to all the instances
            object[,] dataSubset = new object[n, 3]; //subset of data to return

            //Get the neighbourhood list of all the instances in the dataset. That is, compute distance between Xi and other instances in the dataset.
            List<object[,]> objSortedList = new List<object[,]>();
            for (int i = 0; i < n; i++)
            {
                int ctr = 0; int cntr1 = 0; int cntr2 = 0; int a = 0; int b = 0;
                int countP = trainDataset.Y.Count(q => q == 1);
                int countN = trainDataset.Y.Count(q => q == -1);
                
                //generate unique random number
                //List<int> rNum = Training.GetRandomNumbers(2000, n);
                
                for (int j = 0; j < n; j++)
                {

                    if (j.Equals(i))
                        continue;
                    //randomly select N instances from dataset
                    else if (cntr1 < (subN * 0.5) && trainDataset.Y[j] == 1) //compute distance for positive class (50% of k goes for positive instances)
                    {
                        distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                        obj[ctr, 0] = distance;
                        obj[ctr, 1] = trainDataset.X[j];
                        obj[ctr, 2] = trainDataset.Y[j];
                        obj[ctr, 3] = ctr; //save the index
                        ctr++; //save the instance and their corresponding distances
                        cntr1++; 
                        
                    }
                    else if (cntr2 < (subN * 0.5) && trainDataset.Y[j] == -1) //compute distance for negative class (50% of k goes for negative instances)
                    {
                        distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                        obj[ctr, 0] = distance;
                        obj[ctr, 1] = trainDataset.X[j];
                        obj[ctr, 2] = trainDataset.Y[j];
                        obj[ctr, 3] = ctr; //save the index
                        ctr++; //save the instance and their corresponding distances
                        cntr2++;
                    }

                    //distance = Kernel.computeSquaredDistance(trainDataset.X[i], trainDataset.X[j]); //compute the distance between Xi and all other instances in the dataset
                    ////save the instance and their corresponding distances
                    //obj[a, 0] = distance;
                    //obj[a, 1] = trainDataset.X[j];
                    //obj[a, 2] = trainDataset.Y[j];
                    //obj[a, 3] = a; //save the index
                    //a++;
                }

                objList.Add(obj); //Data structure (or List), containing the instances and distances of K nearest neighbours of every instance in the dataset
                obj = new object[subN, 4];
            }

            //sort the data structure. That, sort(and retain index) the neighbourhood list of each instance in the dataset
            for (int i = 0; i < subN; i++)
            {
                object[,] objSort = sortMultiArray(objList[i]); //sort array to select the nearest neighbour of Xi
                objSortedList.Add(objSort); //add to list
            }

            //select boundary instances
            for (int i = 0; i < n; i++)
            {
                //select the k-neareast neighbours (using top K elements), their corresponding distances and class labels of Xi
                int subK = k;
                int count1 = 0; int count2 = 0;
                for (int p = 0; p < subN; p++)
                {
                    object[,] objSorted = objSortedList[p];
                    if (count1 < (subK / 2) && (double)objSorted[p, 2] == 1) //50% of k goes to positive class. This is to ensure that there is a balance in the training subset 
                    {
                        dist.Add((double)objSorted[p, 0]); //distance
                        nearestNeighbours.Add((Node[])objSorted[p, 1]); //nearest neighbour i
                        labels.Add((double)objSorted[p, 2]); //class labels
                        index.Add((int)objSorted[p, 3]); //add index for each nearest neighbour
                        count1++;
                    }
                    else if (count2 < (subK / 2) && (double)objSorted[p, 2] == -1) //50% of K goes to negative class
                    {
                        dist.Add((double)objSorted[p, 0]); //distance
                        nearestNeighbours.Add((Node[])objSorted[p, 1]); //nearest neighbour i
                        labels.Add((double)objSorted[p, 2]); //class labels
                        index.Add((int)objSorted[p, 3]); //add index for each nearest neighbour
                        count2++;
                    }
                }

                nn[i, 0] = k;
                nn[i, 1] = dist;
                nn[i, 2] = nearestNeighbours;
                nn[i, 3] = trainDataset.X[i];
                nn[i, 4] = labels;
                nn[i, 5] = trainDataset.Y[i];
                nn[i, 6] = index; //save the index

                //Compute Exponential Decay
                double EDScore = 0; //Exponential decay score
                int counter = 0;
                double distNN = 0;
                List<double> distNNList = new List<double>();
                for (int p = 0; p < subK; p++)
                {
                    //compute exponential decay for Xi and all its Nearest neighbour belonging to the opposite class
                    //if the label of the current instance in the neighbourhood list is not equal to the label of ith instance then compute its Exponential Decay Score
                    if (((List<double>)nn[i, 4])[p] != (double)nn[i, 5])//identify the nearest neighbour belonging to the opposite class
                    {
                        int indx = ((List<int>)nn[i, 6])[p]; //get the index of the current nearest neighbour 
                        object[,] objNN = objSortedList[indx]; //get the current nearest neighbour from list
                        //using the index, select the distance of the closest instance of the opposite class on its neighborhood list

                        for (int a = 0; a < subN; a++)
                        {
                            double label1 = (double)objNN[a, 2]; //label of the current instance in the neighbourhood list of the current nearest neigbour
                            double label2 = ((List<double>)nn[i, 4])[p]; //label of the current instance in the neighbourhood list
                            //if the statement below is true (that is, if the labels are not equal), then select the closest instance of the opposite class on its neighborhood list 
                            //List is ordered already, hence the topmost instance of the opposite class in the neighbourhood list, is the closest instance
                            if (label1 != label2) 
                            {
                                distNN = (double)objNN[a, 0]; //get the distance and break. We only need the distance of the closest instance.
                                distNNList.Add(distNN);
                                break;
                            }
                        }

                        EDScore += ((List<double>)nn[i, 1])[p] - Math.Pow(distNN, 2); //compute exponential decay score
                        //EDScore += ((List<double>)nn[i, 1])[p] - Math.Pow(((List<double>)nn[i, 1])[p], 2); //compute exponential decay score
                        counter++;
                    }
                }

                EDScore = EDScore / counter;

                //determine the scores of every instance
                int numOfContributors = counter; int b = 0;
                for (int p = 0; p < subK; p++)
                {
                    //if the label of the current instance in the neighbourhood list is not equal to the label of ith instance
                    if (((List<double>)nn[i, 4])[p] != (double)nn[i, 5])//identify the nearest neighbour belonging to the opposite class
                    {
                        //score += Math.Exp(-(((List<double>)nn[i, 1])[p] - Math.Pow(((List<double>)nn[i, 1])[p], 2) / EDScore));
                        score += Math.Exp(-(((List<double>)nn[i, 1])[p] - Math.Pow(distNNList[b++], 2) / EDScore));
                    }
                }
                score = score / numOfContributors;
                scoreList[i, 0] = score; scoreList[i, 1] = nn[i, 3]; scoreList[i, 2] = nn[i, 5]; scoreList[i, 3] = nn[i, 6];

                dist = new List<double>(); nearestNeighbours = new List<Node[]>(); labels = new List<double>();
            }


            sortMultiArray(scoreList); //sort scores to select the best N instances to be used for training

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

        //sort (and retain index) of multidimensional array (smallest - largest, i.e. ascending order, and descending order)
        public static object[,] sortMultiArray(object[,] obj)
        {
            int n = obj.GetUpperBound(0);
            object[,] temp = new object[1, 4];
            for (int m = 0; m <= n; m++)
            {
                for (int p = 0; p < n; p++)
                {
                    if ((double)obj[p, 0] > (double)obj[p + 1, 0]) //sort in ascending order
                    {
                        temp[0, 0] = obj[p + 1, 0];
                        temp[0, 1] = obj[p + 1, 1];
                        temp[0, 2] = obj[p + 1, 2];
                        temp[0, 3] = obj[p + 1, 3];
                        obj[p + 1, 0] = obj[p, 0];
                        obj[p + 1, 1] = obj[p, 1];
                        obj[p + 1, 2] = obj[p, 2];
                        obj[p + 1, 3] = obj[p, 3];
                        obj[p, 0] = temp[0, 0];
                        obj[p, 1] = temp[0, 1];
                        obj[p, 2] = temp[0, 2];
                        obj[p, 3] = temp[0, 3];
                    }
                    temp = new object[1, 4];
                }
            }

            return obj;
        }
    }
}
