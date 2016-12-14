using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM
{
    class FireFly
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

        /// <summary>
        /// Evaluate Objective Function
        /// </summary>
        public double[] EvaluateObjectiveFunction(List<Object> fireflies, List<double> accuracy)
        {
            int NF = fireflies.Count; //NF -> number of fireflies
            int tNFe = fireflies.ElementAt(0).Attribute_Values.Count(); //number of features or attributes
            //int tNFe = 8; //total number of features or attributes
            double[] fitness = new double[NF];
            int sum;

            for (int i = 0; i < NF; i++)
            {
                sum = 0;
                for (int j = 0; j < tNFe; j++)
                    sum += fireflies.ElementAt(i).Attribute_Values[j];

                fitness[i] = W_SVM * accuracy[i] + W_Features * (double)(1 - ((double)sum / (double)tNFe)); //fitness evaluation for individual firefly
                //fitness[i] = accuracy[i] + W_Features * (double)(1 - ((double)sum / (double)tNFe)); //fitness evaluation for individual firefly
            }

            return fitness;
        }

        /// <summary>
        /// Main part of the Firefly Algorithm
        /// </summary>
        public Object firefly_simple(List<double> avgAcc, List<double> CValues, List<double> GValues, Problem prob, Parameter param)
        {

            int nF = 15; //number of features
            int nFF = avgAcc.Count; //number of fireflies

            int MaxGeneration = 10; //number of pseudo time steps
            
            int[] range = new int[4] { -5, 5, -5, 5 }; //range=[xmin xmax ymin ymax]

            double alpha = 0.2; //Randomness 0--1 (highly random)
            double gamma = 1.0; //Absorption coefficient
            
            int[] xn = new int[nF];
            double[] xo = new double[nF];
            double[] Lightn = new double[nFF]; 
            double[] Lighto = new double[nFF];

            double[] zn = new double[nFF]; 
            double globalbestIntensity;
            Object globalBest = null;

            //generating the initial locations of n fireflies
            List<Object> fireflies = init_ffa(nF, CValues, GValues);
            
            Object[] fireflyBackup = new Object[fireflies.Count];
            Object[] fireflyBest = new Object[fireflies.Count]; 
            List<int> changedIndex = new List<int>(); //changedIndex keeps track of the index of the firefly that has been moved
            double newBestIntensity = new double();
            int maxIndex;
            bool stopSearch = false; //stopsearch is will be set to true when the a firefly with classification accuracy = 100 is found.
            
            globalbestIntensity = double.MinValue;

            //Iterations or pseudo time marching
            for (int i = 0; i < MaxGeneration; i++)
            {
                //Evaluate new solutions
                //zn = this.EvaluateObjectiveFunction(xn);

                //stop searching if firefly has found the best c and G value that yields 100%
                for (int t = 0; t < changedIndex.Count; t++)
                {
                    double predAccr = avgAcc[changedIndex[t]] * 100;
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

                zn = this.EvaluateObjectiveFunction(fireflies, avgAcc); //evaluate objective function for each firefly
                newBestIntensity = zn.Max(); //get the firefly with the highest light intensity
                if (newBestIntensity > globalbestIntensity)
                {
                    globalbestIntensity = newBestIntensity;
                    maxIndex = Array.IndexOf(zn, newBestIntensity); //select the index for the global best
                    globalBest = fireflies[maxIndex]; //select the global best firefly
                    //bestC = (double)fireflies[maxIndex].cValue; //save the C value for the global best
                    //bestGamma = (double)fireflies[maxIndex].GValue; //save the Gamma for the global best
                }

                fireflies.CopyTo(fireflyBackup); zn.CopyTo(Lighto, 0); zn.CopyTo(Lightn, 0); //creating duplicates
                //Lightn.CopyTo(Lighto, 0);

                changedIndex.Clear();
                ffa_move(Lightn, fireflyBackup, Lighto, alpha, gamma, fireflies, prob, param, avgAcc, changedIndex);

                
                fireflies.CopyTo(fireflyBackup); //backing up the current positions of the fireflies
                Lightn.CopyTo(Lighto, 0); //backing up the current intensities of the fireflies

            }
            
           return globalBest;
        }

        /// <summary>
        /// generating the initial locations of n fireflies
        /// </summary>
        public List<Object> init_ffa(int n, List<double> CVal, List<double> GVal)
        {

            Random rnd = new Random();// Random rx = new Random(); Random ry = new Random();
            int nF = n; //nF -> number of features

            List<Object> attr_values = new List<Object>();

            //create an array of size n for x and y
            int[] xn = new int[nF];

            for (int i = 0; i < CVal.Count; i++)
            {
                //int sum = 0;
                for (int j = 0; j < nF; j++)
                {

                    xn[j] = (j < 8) ? 1 : 0;
                }

                Object xx = new Object(CVal[i], GVal[i], xn);
                attr_values.Add(xx);
            }
            return attr_values;
        }

    
        /// <summary>
        /// Move all fireflies toward brighter ones
        /// </summary>
        public void ffa_move(double[] Lightn, Object[] fireflies0, double[] Lighto, double alpha, double gamma, List<Object> fireflies,
                              Problem prob, Parameter param, List<double> avgAcc, List<int> changedIndex)
        {
           
            int nj = fireflies0.Length;
            double rC, rG, rF; //rC -> distance for C value, rG-> distance for Gamma value, rF - distance for the feature mask
            double beta0;
            double beta; // beta -> attrativeness value for C and G, betaF -> attrativeness for the feature mask
            
            //specifying the ranges for C and Gamma
            double minC = Math.Pow(2, MIN_C); // minimum value for C
            double maxC = Math.Pow(2, MAX_C); // maximum value for C
            double minG = Math.Pow(2, MIN_G); // minimum value for G
            double maxG = Math.Pow(2, MAX_G); // maximum value for G

            int nF = fireflies[0].Attribute_Values.Count(); //nF -> number of features
            
            double[] CBackup = new double[fireflies.Count]; //back up array for C value
            double[] GammaBackup = new double[fireflies.Count]; ////back up array for Gamma value
            
            Random rnd = new Random(); Random rx = new Random(); Random ry = new Random();
            
            duplicateValue(fireflies, CBackup, GammaBackup);
            for (int i = 0; i < nj; i++)
            {
                for (int j = 0; j < nj; j++)
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
                    
                        //changing firefly i position for the continuous values - i.e C and Gamma value respectively
                        fireflies[i].cValue = ((double)fireflies[i].cValue * (1 - beta)) + (CBackup[j] * beta) + (alpha * (rnd.NextDouble() - 0.5));
                        fireflies[i].GValue = ((double)fireflies[i].GValue * (1 - beta)) + (GammaBackup[j] * beta) + (alpha * (rnd.NextDouble() - 0.5));
                        
                        findrange(fireflies[i], minC, maxC, minG, maxG); //restrict the values of C and Gamma to the specified range

                    }
                }
                if ((double)fireflies[i].cValue != CBackup[i] || (double)fireflies[i].GValue != GammaBackup[i])
                  changedIndex.Add(i); //saving the index of the firefly that has been moved for the purpose of accuracy calculation
            }

            //calculate the new accuracy for the newly updated C and Gamma value
            ParameterSelection.Grid(prob, param, fireflies, changedIndex, avgAcc, CBackup, GammaBackup, NFOLD);
        }

        /// <summary>
        /// Create a duplicate of C and Gamma Values
        /// </summary>
        public void duplicateValue(List<Object> fireflies, double[] CBackup, double[] GammaBackup)
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
        public void findrange(Object fireflies, double minC, double maxC, double minG, double maxG)
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
    }
}
