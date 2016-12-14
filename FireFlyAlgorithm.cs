using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    /// <summary>
    /// Selects the best features using fire fly algorithm
    /// </summary>
    public class FireFlyAlgorithm
    {
        /// <summary>
        /// Default number of times to divide the data.
        /// </summary>
        public const int NFOLD = 5;
        /// <summary>
        /// Default minimum power of 2 for the C value (-5)
        /// </summary>
        public const int MIN_C = -5;
        /// <summary>
        /// Default maximum power of 2 for the C value (15)
        /// </summary>
        public const int MAX_C = 15;
        /// <summary>
        /// Default power iteration step for the C value (2)
        /// </summary>
        public const int C_STEP = 2;
        /// <summary>
        /// Default minimum power of 2 for the Gamma value (-15)
        /// </summary>
        public const int MIN_G = -15;
        /// <summary>
        /// Default maximum power of 2 for the Gamma Value (3)
        /// </summary>
        public const int MAX_G = 3;
        /// <summary>
        /// Default power iteration step for the Gamma value (2)
        /// </summary>
        public const int G_STEP = 2;
        /// <summary>
        /// Default weight for the SVM classification accuracy
        /// </summary>
        public const double W_SVM = 0.95;
        /// <summary>
        /// Default weight for the number of selected features
        /// </summary>
        public const double W_Features = 0.05;
        /// <summary>
        /// Total number of features
        /// </summary>
        public const int NumFeatures = 15;
        
        //calculate the objective function
        public double[] EvaluateObjectiveFunction(double[] accuracy, double[] a)
        {
            double[] fitness = new double[accuracy.Length];

            for (int i = 0; i < accuracy.Length; i++)
            {
                fitness[i] = W_SVM * accuracy[i] + W_Features * (1 - (double)(8 / NumFeatures));
            }

                //for (int i = 0; i < xn.Length; i++)
                //{
                //    funstr[i] = Math.Exp(-Math.Pow((xn[i] - 4), 2) - Math.Pow((yn[i] - 4), 2)) + Math.Exp(-Math.Pow((xn[i] + 4), 2) - Math.Pow((yn[i] - 4), 2)) +
                //    2 * Math.Exp(-Math.Pow(xn[i], 2) - Math.Pow((yn[i] + 4), 2)) + 2 * Math.Exp(-Math.Pow(xn[i], 2) - Math.Pow(yn[i], 2));
                //}

                return fitness;
        }

        //Main Fire fly function
        public void firefly_simple(int[] instr, out double[] xnBest, out double[] ynBest, out double[] lightnBest)
        {

            int n; //number of fire flies

            int MaxGeneration; //number of pseudo time steps
            int nargin = 0;
            //double str1, str2, funstr;
            int[] range = new int[4] { -5, 5, -5, 5 }; //range=[xmin xmax ymin ymax]

            double alpha = 0.2; //Randomness 0--1 (highly random)
            double gamma = 1.0; //Absorption coefficient

            instr[0] = 15;
            instr[1] = 50;
            
            n = instr[0];
            MaxGeneration = instr[1];

            double[] xn = new double[n]; double[] xo = new double[n];
            double[] yn = new double[n]; double[] yo = new double[n];
            double[] Lightn = new double[n]; double[] Lighto = new double[n];

            double[] zn = new double[n];
            double[] an = new double[n];

            //generating the initial locations of n fireflies
            init_ffa(n, range, out xn, out yn, out Lightn);

            //Iterations or pseudo time marching
            for (int i = 0; i < MaxGeneration; i++)
            {
                //Evaluate new solutions
                zn = this.EvaluateObjectiveFunction(xn, yn);

                zn.ToList<double>();

                //Ranking the fireflies by their light intensity - this sort nethod also preserves the original index of the sorted array values for future reference
                var sorted = zn.Select((x, index) => new KeyValuePair<double, int>(x, index)).OrderBy(x => x.Key).ToList();

                List<double> znSorted = sorted.Select(x => x.Key).ToList(); //select the sorted list of zn values
                Lightn = znSorted.ToArray(); //saving the sorted light intensities
                List<int> preservedindex = sorted.Select(x => x.Value).ToList(); //select the original index of the zn values

                for (int j = 0; j < xn.Length; j++)
                {
                    for (int k = 0; k < xn.Length; k++)
                    {
                        if (preservedindex[k] == j)
                        {
                            xn[k] = xn[preservedindex[k]];
                            yn[k] = yn[preservedindex[k]];
                            continue;
                        }
                    }
                }

                xn.CopyTo(xo, 0); yn.CopyTo(yo, 0); Lightn.CopyTo(Lighto, 0);

                //Move all fireflies to the better locations
                ffa_move(xn, yn, Lightn, xo, yo, Lighto, alpha, gamma, range);
            }
            xnBest = xo; ynBest = yo; lightnBest = Lighto;
        }

        //generating the initial locations of n fireflies
        public void init_ffa(int n, int[] range, out double[] xn, out double[] yn, out double[] Lightn)
        {
            Random rnd = new Random(); Random rx = new Random(); Random ry = new Random();

            List<double> cValues = GetList(MIN_C, MAX_C, C_STEP);
            List<double> GValues = GetList(MIN_G, MAX_G, G_STEP);

            int xrange = range[1] - range[0];
            int yrange = range[3] - range[2];

            //create an array of size n for x and y
            xn = new double[n];
            yn = new double[n];

            //initize the array with random decimal values between 0.0 and 1.0
            //for (int i = 0; i < n; i++)
            //{
            //    xn[i] = rx.NextDouble() * xrange + range[0];
            //    yn[i] = ry.NextDouble() * yrange + range[2];
            //}

            for (int i = 0; i < n; i++)
            {
                xn[i] = cValues[i];
                yn[i] = GValues[i];
            }

            Lightn = new double[n]; //initialize an array of n,n matrix

        }

        //Move all fireflies toward brighter ones
        public void ffa_move(double[] xn, double[] yn, double[] Lightn, double[] xo, double[] yo, double[] Lighto, double alpha, double gamma, int[] range)
        {
            int ni = yn.Length;
            int nj = yo.Length;
            double r, beta0, beta;
            Random rnd = new Random(); Random rx = new Random(); Random ry = new Random();

            for (int i = 0; i < ni; i++)
            {
                for (int j = 0; j < nj; j++)
                {
                    r = Math.Sqrt(Math.Pow((xn[i] - xo[j]), 2) + Math.Pow((yn[i] - yo[j]), 2));
                    if (Lightn[i] < Lighto[j])
                    {
                        beta0 = 1; //setting beta to 1
                        beta = beta0 * Math.Exp(-gamma * Math.Pow(r, 2)); //The attractiveness parameter beta=exp(-gamma*r)
                        //double x = xn[i]; x = (x * (1 - beta)) + (xo[j] * beta) + alpha * (rnd.NextDouble() - 0.5); xn[i] = x;
                        xn[i] = (xn[i] * (1 - beta)) + (xo[j] * beta) + alpha * (rx.NextDouble() - 0.5);
                        yn[i] = (yn[i] * (1 - beta)) + (yo[j] * beta) + alpha * (ry.NextDouble() - 0.5);
                    }
                }
            }
            findrange(xn, yn, range);
        }

        //Make sure the fireflies are within the range
        public void findrange(double[] xn, double[] yn, int[] range)
        {
            for (int i = 0; i < yn.Length; i++)
            {
                if (xn[i] <= range[0])
                    xn[i] = range[0];
                if (xn[i] >= range[1])
                    xn[i] = range[1];
                if (yn[i] <= range[2])
                    yn[i] = range[2];
                if (yn[i] >= range[3])
                    yn[i] = range[3];
            }
        }

        /// <summary>
        /// Returns a logarithmic list of values from minimum power of 2 to the maximum power of 2 using the provided iteration size.
        /// </summary>
        /// <param name="minPower">The minimum power of 2</param>
        /// <param name="maxPower">The maximum power of 2</param>
        /// <param name="iteration">The iteration size to use in powers</param>
        /// <returns></returns>
        public static List<double> GetList(double minPower, double maxPower, double iteration)
        {
            List<double> list = new List<double>();
            for (double d = minPower; d <= maxPower; d += iteration)
                list.Add(Math.Pow(2, d));
            return list;
        }

        //Normalize the vector values
        public double[,] Normalize(int[,] vector)
        {
            int rCount = vector.GetLength(0); //row count
            int cCount = vector.GetLength(1);// column count
            int[] cols = new int[rCount];

            double[,] Normalized = new double[rCount, cCount];
            int min = 0, max = 0;
            for (int i = 0; i < cCount; i++)
            {
                for (int j = 0; j < rCount; j++)
                    cols[j] = vector[j, i];

                //getting the maximum and minimum in each colums
                max = cols.Max();
                min = cols.Min();

                //normalizing the values in each columns
                for (int j = 0; j < rCount; j++)
                    Normalized[j, i] = max.Equals(0) ? 0 : Math.Round((double)(cols[j] - min) / (double)(max - min), 2); //avoiding division by zero.. also round the values to two decimal places
            }

            return Normalized;
        }

    }
}
