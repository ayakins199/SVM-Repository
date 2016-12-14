using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    internal interface IQMatrix
    {
        float[] GetQ(int column, int len);
        float[] GetQD();
        void SwapIndex(int i, int j);
    }

    internal abstract class Kernel : IQMatrix
    {
        private Node[][] _x;
        private double[] _xSquare;

        private KernelType _kernelType;
        private int _degree;
        private double _gamma;
        private double _coef0;

        public abstract float[] GetQ(int column, int len);
        public abstract float[] GetQD();

        public virtual void SwapIndex(int i, int j)
        {
            _x.SwapIndex(i, j);

            if (_xSquare != null)
            {
                _xSquare.SwapIndex(i, j);
            }
        }

        private static double powi(double value, int times)
        {
            double tmp = value, ret = 1.0;

            for (int t = times; t > 0; t /= 2)
            {
                if (t % 2 == 1) ret *= tmp;
                tmp = tmp * tmp;
            }
            return ret;
        }

        public double KernelFunction(int i, int j)
        {
            switch (_kernelType)
            {
                case KernelType.LINEAR:
                    return dot(_x[i], _x[j]);
                case KernelType.POLY:
                    return powi(_gamma * dot(_x[i], _x[j]) + _coef0, _degree);
                case KernelType.RBF:
                    return Math.Exp(-_gamma * (_xSquare[i] + _xSquare[j] - 2 * dot(_x[i], _x[j])));
                case KernelType.SIGMOID:
                    return Math.Tanh(_gamma * dot(_x[i], _x[j]) + _coef0);
                case KernelType.PRECOMPUTED:
                    return _x[i][(int)(_x[j][0].Value)].Value;
                default:
                    return 0;
            }
        }

        public Kernel(int l, Node[][] x_, Parameter param)
        {
            _kernelType = param.KernelType;
            _degree = param.Degree;
            _gamma = param.Gamma;
            _coef0 = param.Coefficient0;

            _x = (Node[][])x_.Clone();

            if (_kernelType == KernelType.RBF)
            {
                _xSquare = new double[l];
                for (int i = 0; i < l; i++)
                    _xSquare[i] = dot(_x[i], _x[i]);
            }
            else _xSquare = null;
        }

        private static double dot(Node[] xNodes, Node[] yNodes)
        {
            double sum = 0;
            int xlen = xNodes.Length;
            int ylen = yNodes.Length;
            int i = 0;
            int j = 0;
            Node x = xNodes[0];
            Node y = yNodes[0];
            while (true)
            {
                if (x._index == y._index)
                {
                    sum += x._value * y._value;
                    i++;
                    j++;
                    if (i < xlen && j < ylen)
                    {
                        x = xNodes[i];
                        y = yNodes[j];
                    }
                    else if (i < xlen)
                    {
                        x = xNodes[i];
                        break;
                    }
                    else if (j < ylen)
                    {
                        y = yNodes[j];
                        break;
                    }
                    else break;
                }
                else
                {
                    if (x._index > y._index)
                    {
                        ++j;
                        if (j < ylen)
                            y = yNodes[j];
                        else break;
                    }
                    else
                    {
                        ++i;
                        if (i < xlen)
                            x = xNodes[i];
                        else break;
                    }
                }
            }
            return sum;
        }

        //standard deviation
        public static double StandardDeviation(List<double> valueList)
        {
            double M = 0.0;
            double S = 0.0;
            int k = 0;
            foreach (double value in valueList)
            {
                k++;
                double tmpM = M;
                M += (value - tmpM) / k;
                S += (value - tmpM) * (value - M);
            }
            return Math.Sqrt(S / (k - 1));
        }

        //Compute Weighted Euclidean Distance
        private static double computeWeightedDistance(Node[] xNodes, Node[] yNodes)
        {
            double sum = 0; List<double> values = new List<double>(); double variance, weight;

            for (int i = 0; i < xNodes.Length; i++)
                values.Add(xNodes[i]._value);
            variance = Math.Pow(StandardDeviation(values), 2);
            weight = 1 / variance;

            for (int i = 0; i < xNodes.Length; i++)
                sum += weight * Math.Pow((xNodes[i]._value - yNodes[i]._value), 2);

            return Math.Sqrt(sum);
        }

        //Compute Standardized Euclidean Distance
        private static double computeStandardizedDistance(Node[] xNodes, Node[] yNodes)
        {
            double total = 0, sumX = 0, sumY = 0, meanX, meanY, standDevX, standDevY; double[] standardValX = new double[xNodes.Length], standardValY = new double[xNodes.Length];
            List<double> valX = new List<double>(); List<double> valY = new List<double>(); 
            
            for (int i = 0; i < xNodes.Length; i++)
            {
                sumX += xNodes[i]._value; sumY += yNodes[i]._value;
                valX.Add(xNodes[i]._value); valY.Add(yNodes[i]._value);
            }
            meanX = sumX / xNodes.Length; meanY = sumY / yNodes.Length; //mean for X and Y
            standDevX = StandardDeviation(valX); standDevY = StandardDeviation(valY); //standard deviation for X and Y

            //standardize the values
            for (int j = 0; j < xNodes.Length; j++)
            {
                standardValX[j] = (xNodes[j]._value - meanX) / standDevX;
                standardValY[j] = (yNodes[j]._value - meanY) / standDevY;
            }

            for (int i = 0; i < xNodes.Length; i++)
                total += Math.Pow((standardValX[i] - standardValY[i]), 2);

            return Math.Sqrt(total);
        }

        //Compute Half Squared Euclidean Distance
        private static double computeHalfSquaredDistance(Node[] xNodes, Node[] yNodes)
        {
            double sum = 0;
            for (int i = 0; i < xNodes.Length; i++)
                sum += Math.Pow((xNodes[i]._value - yNodes[i]._value), 2);

            return sum/2;
        }

        //Compute Normal Euclidean Distance
        public static double computeNormalDistance(Node[] xNodes, Node[] yNodes)
        {
            double sum = 0;
            for (int i = 0; i < xNodes.Length; i++)
              sum += Math.Pow(Math.Abs(xNodes[i]._value - yNodes[i]._value), 2);
            
            return Math.Sqrt(sum);
        }

        //Compute Bray-Curtis Dissimilarity Distance
        private static double computeBrayCurtisDistance(Node[] xNodes, Node[] yNodes)
        {
            double sum = 0, sumx = 0, sumy = 0, distance;
            for (int i = 0; i < xNodes.Length; i++)
            {
                sum += Math.Abs(xNodes[i]._value - yNodes[i]._value);
                sumx += xNodes[i]._value; sumy += yNodes[i]._value;
                //sumx += Math.Abs(xNodes[i]._value); sumy += Math.Abs(yNodes[i]._value);

            }

            //return distance = sum / (sumx + sumy);
            return distance = sum / Math.Abs((sumx + sumy));
        }

        //calculate the k-nearest neighbour of all instances in the dataset
        private static double computeNearestNeighbour(int k, Problem trainDataset)
        {
            double sum = 0; double distance;
            int n = trainDataset.Count; //number of data instances
            Node[] xNodes = new Node[n];  
            Node[] yNodes = new Node[n];
            object[,] obj = new object[1, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (j.Equals(i))
                        continue;
                   distance = computeNormalDistance(trainDataset.X[i], trainDataset.X[j]);
                   obj[j, 0] = distance; obj[j, 1] = trainDataset.X[j];
                }
            }
                //double summ = computeNormalDistance(trainDataset.X[0], trainDataset.Y[0]);

                for (int i = 0; i < xNodes.Length; i++)
                    sum += Math.Pow(Math.Abs(xNodes[i]._value - yNodes[i]._value), 2);

            return Math.Sqrt(sum);
        }

        public static double computeSquaredDistance(Node[] xNodes, Node[] yNodes)
        {
            Node x = xNodes[0];
            Node y = yNodes[0];
            int xLength = xNodes.Length;
            int yLength = yNodes.Length;
            int xIndex = 0;
            int yIndex = 0;
            double sum = 0;

            while (true)
            {
                if (x._index == y._index)
                {
                    double d = x._value - y._value;
                    sum += d * d;
                    xIndex++;
                    yIndex++;
                    if (xIndex < xLength && yIndex < yLength)
                    {
                        x = xNodes[xIndex];
                        y = yNodes[yIndex];
                    }
                    else if(xIndex < xLength){
                        x = xNodes[xIndex];
                        break;
                    }
                    else if(yIndex < yLength){
                        y = yNodes[yIndex];
                        break;
                    }else break;
                }
                else if (x._index > y._index)
                {
                    sum += y._value * y._value;
                    if (++yIndex < yLength)
                        y = yNodes[yIndex];
                    else break;
                }
                else
                {
                    sum += x._value * x._value;
                    if (++xIndex < xLength)
                        x = xNodes[xIndex];
                    else break;
                }
            }

            for (; xIndex < xLength; xIndex++)
            {
                double d = xNodes[xIndex]._value;
                sum += d * d;
            }

            for (; yIndex < yLength; yIndex++)
            {
                double d = yNodes[yIndex]._value;
                sum += d * d;
            }

            return sum;
        }

        public static double KernelFunction(Node[] x, Node[] y, Parameter param)
        {
            switch (param.KernelType)
            {
                case KernelType.LINEAR:
                    return dot(x, y);
                case KernelType.POLY:
                    return powi(param.Degree * dot(x, y) + param.Coefficient0, param.Degree);
                case KernelType.RBF:
                    {
                        //double sum = computeBrayCurtisDistance(x, y);
                        double sum = computeSquaredDistance(x, y);
                        //double sum = computeNormalDistance(x, y);
                        //double sum = computeHalfSquaredDistance(x, y);
                        //double sum = computeStandardizedDistance(x, y);
                        //double sum = computeWeightedDistance(x, y);
                        
                        return Math.Exp(-param.Gamma * sum);
                    }
                case KernelType.SIGMOID:
                    return Math.Tanh(param.Gamma * dot(x, y) + param.Coefficient0);
                case KernelType.PRECOMPUTED:
                    return x[(int)(y[0].Value)].Value;
                default:
                    return 0;
            }
        }
    }
}
