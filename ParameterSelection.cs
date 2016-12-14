/*
 * SVM.NET Library
 * Copyright (C) 2008 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SVM
{
    /// <summary>
    /// This class is an extension class for the existing Random class
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// This function (i.e NextDouble) generate random numbers between two range of double numbers
        /// </summary>
        public static double NextDouble(this Random random, double minValue, double maxValue)
        {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }
    }

    /// <summary>
    /// This class contains routines which perform parameter selection for a model which uses C-SVC and
    /// an RBF kernel.
    /// </summary>
    public class ParameterSelection
    {
        /// <summary>
        /// Default number of times to divide the data.
        /// </summary>
        public const int NFOLD = 5;
        /// <summary>
        /// Default minimum power of 2 for the C value (-5)
        /// </summary>
        public const int MIN_C = -5;
        //public const double MIN_C = 0.01;
        //public const int MIN_C = -2;
        /// <summary>
        /// Default maximum power of 2 for the C value (15)
        /// </summary>
        //public const int MAX_C = 1000000;
        public const int MAX_C = 15;
        //public const int MAX_C = 6;
        /// <summary>
        /// Default power iteration step for the C value (2)
        /// </summary>
        public const int C_STEP = 2;
        /// <summary>
        /// Default minimum power of 2 for the Gamma value (-15)
        /// </summary>
        public const int MIN_G = -15;
        //public const double MIN_G = 0.0001;
        //public const int MIN_G = -5;
        /// <summary>
        /// Default maximum power of 2 for the Gamma Value (3)
        /// </summary>
        //public const double MAX_G = 1000;
        public const int MAX_G = 3;
        //public const int MAX_G = 4;
        /// <summary>
        /// Default power iteration step for the Gamma value (2)
        /// </summary>
        public const int G_STEP = 2;


        public static int cMinPower;
        public static int cMaxPower;
        public static int gMinPower;
        public static int gMaxPower;
        public static double CVAccuracy;
        public string filePath = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedResult.txt"); //'ExtractedResult.txt' the C,Gamma and accuracy for each iteration.
        public string filePath2 = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedSupportVectors.txt"); //'ExtractedResult.txt' the C,Gamma and accuracy for each iteration.
        public string filePath3 = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedSupportVectorsCount.txt"); //'ExtractedResult.txt' the C,Gamma and accuracy for each iteration.
        public string SVAccuracyFilePath = String.Format(Environment.CurrentDirectory + "\\{0}", "SVAccuracy.txt");
        /// <summary>
        /// Returns a list of exponentially growing sequence of C and Gamma Values using the provided iteration size.
        /// </summary>
        /// <param name="minPower">The minimum power of 2</param>
        /// <param name="maxPower">The maximum power of 2</param>
        /// <param name="iteration">The iteration size to use in powers</param>
        /// <returns></returns>
        public static List<double> GetList(double minPower, double maxPower, double iteration)
        //public static List<double> GetList(double minPower, double maxPower)
        {
            //Random r = new Random(); List<int> pow = new List<int>();
            List<double> list = new List<double>();

            for (double d = minPower; d <= maxPower; d += iteration)
                list.Add(Math.Pow(2, d));


            /*
            List<double> list = new List<double>();

            for (double d = minPower; d <= maxPower; d++)
                list.Add(Math.Pow(10, d));*/


            return list;
        }


        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  The default ranges of C and Gamma values are used.  Use this method if there is no validation data available, and it will
        /// divide it 5 times to allow 5-fold validation (training on 4/5 and validating on 1/5, 5 times).
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="parameters">The parameters to use when optimizing</param>
        /// <param name="outputFile">Output file for the parameter results.</param>
        /// <param name="C">The optimal C value will be put into this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be put into this variable</param>
        public static void Grid(
            Problem problem,
            Parameter parameters,
            string outputFile,
            out double C,
            out double Gamma)
        {
            Random r = new Random();
            int randStart = r.Next(1, 11); //randomly select the power to use
            int n = 20; //n=> number of exponential sequence to be generated

            //generating the min and max power for 20 exponentially growing sequence of c and gamma values starting from the randomly generated value in 'randStart'
            //e.g, if n=11, c=2^-5,2^-3,....2^-15, Gamma=2^-15,2^-13,....2^-3
            cMinPower = -randStart;
            cMaxPower = cMinPower + ((n - 1) * 2);
            gMinPower = -cMaxPower;
            gMaxPower = gMinPower + ((n - 1) * 2);

            //Grid(problem, parameters, GetList(cMinPower, cMaxPower, C_STEP), GetList(gMinPower, gMaxPower, G_STEP), outputFile, NFOLD, out C, out Gamma);
            Grid(problem, parameters, GetList(MIN_C, MAX_C, C_STEP), GetList(MIN_G, MAX_G, G_STEP), outputFile, NFOLD, out C, out Gamma);
            //Grid(problem, parameters, GetList(MIN_C, MAX_C), GetList(MIN_G, MAX_G), outputFile, NFOLD, out C, out Gamma);
        }

        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  Use this method if there is no validation data available, and it will
        /// divide it 5 times to allow 5-fold validation (training on 4/5 and validating on 1/5, 5 times).
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="parameters">The parameters to use when optimizing</param>
        /// <param name="CValues">The set of C values to use</param>
        /// <param name="GammaValues">The set of Gamma values to use</param>
        /// <param name="outputFile">Output file for the parameter results.</param>
        /// <param name="C">The optimal C value will be put into this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be put into this variable</param>
        public static void Grid(
            Problem problem,
            Parameter parameters,
            List<double> CValues,
            List<double> GammaValues,
            string outputFile,
            out double C,
            out double Gamma)
        {
            Grid(problem, parameters, CValues, GammaValues, outputFile, NFOLD, out C, out Gamma);
        }

        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  Use this method if validation data isn't available, as it will
        /// divide the training data and train on a portion of it and test on the rest.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="parameters">The parameters to use when optimizing</param>
        /// <param name="CValues">The set of C values to use</param>
        /// <param name="GammaValues">The set of Gamma values to use</param>
        /// <param name="outputFile">Output file for the parameter results.</param>
        /// <param name="nrfold">The number of times the data should be divided for validation</param>
        /// <param name="C">The optimal C value will be placed in this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be placed in this variable</param>
        public static void Grid(
            Problem problem,
            Parameter parameters,
            List<double> CValues,
            List<double> GammaValues,
            string outputFile,
            int nrfold,
            out double C,
            out double Gamma)
        {
            C = 0;
            Gamma = 0;
            List<double> avgAcc = new List<double>(); //avgAcc->average accuracy; it stores the average accuracies for all the C and Gamma values
            List<double> CVal = new List<double>(); //store the C values with high accuracies
            List<double> GVal = new List<double>(); //store the C values with high accuracies
            double crossValidation = double.MinValue;
            StreamWriter output = null;
            List<double> nCVal = new List<double>(); //nCVal -> randomly selected new C Values
            List<double> nGVal = new List<double>(); //nGVal -> randomly selected new Gamma Values
            List<double> nAvgAcc = new List<double>(); //nAvgAcc -> randomly selected new average accuracies
            string outputValues = ""; // outputValues -> hold the C,Gamma and accuracy for each iteration. This is for to derive a pattern and formula
            //List<string> outputValues = new List<string>(); // outputValues -> hold the C,Gamma and accuracy for each iteration. This is for to derive a pattern and formula

            ParameterSelection ps = new ParameterSelection();
            FireFly ff = new FireFly();
            int nFF = CValues.Count * GammaValues.Count; //nFF -> number of fireflies
            Random r = new Random();
            int nValues = CValues.Count - 1;
            //int nValues = CValues.Count;

            if (outputFile != null)
                output = new StreamWriter(outputFile);

            //****Firefly Optimized SVM
            //for (int i = 0; i < GammaValues.Count; i++)
            //{
            //    parameters.C = CValues[nValues--];
            //    parameters.Gamma = GammaValues[i];
            //    double test = Training.PerformCrossValidation(problem, parameters, nrfold);

            //    //avgAcc.Add(test);
            //    //CVal.Add(parameters.C);
            //    //GVal.Add(parameters.Gamma);

            //    Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, test);

            //    outputValues = parameters.C.ToString() + " " + parameters.Gamma.ToString() + " " + test.ToString();
            //    File.AppendAllText(ps.filePath, outputValues);
            //    File.AppendAllText(ps.filePath, Environment.NewLine);

            //    if (output != null)
            //        output.WriteLine("{0} {1} {2}", parameters.C, parameters.Gamma, test);
            //    if (test > crossValidation)
            //    {
            //        C = parameters.C;
            //        Gamma = parameters.Gamma;
            //        crossValidation = test;
            //        Console.WriteLine(" New Maximum!");

            //        //break from loop if the cross validation rate is equal to 1 (i.e. 100%)
            //        /*if (crossValidation == 1.0)
            //        {
            //           proceed = true;
            //           break;
            //        }*/

            //    }
            //    else
            //        Console.WriteLine();
            //}
            //Object selectedFirefly = ff.firefly_simple(avgAcc, CVal, GVal, problem, parameters);
            //C = (double)selectedFirefly.cValue;
            //Gamma = (double)selectedFirefly.GValue;

            //Standard SVM Optimization
            //for (int i = 0; i < CValues.Count; i++)
            //    for (int j = 0; j < GammaValues.Count; j++)
            //    {
            //        parameters.C = CValues[i];
            //        parameters.Gamma = GammaValues[j];
            //        double test = Training.PerformCrossValidation(problem, parameters, nrfold);
            //        Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, test);
            //        if (output != null)
            //            output.WriteLine("{0} {1} {2}", parameters.C, parameters.Gamma, test);
            //        if (test > crossValidation)
            //        {
            //            C = parameters.C;
            //            Gamma = parameters.Gamma;
            //            crossValidation = test;
            //            Console.WriteLine(" New Maximum!");
            //        }
            //        else Console.WriteLine();
            //    }

            for (int i = 0; i < CValues.Count; i++)
            {
                double test = new double();
                for (int j = 0; j < GammaValues.Count; j++)
                {
                    parameters.C = CValues[i];
                    parameters.Gamma = GammaValues[j];
                    test = Training.PerformCrossValidation(problem, parameters, nrfold);

                    File.AppendAllText(ps.filePath2, Environment.NewLine); //insert double line to file at the end of each parameter evaluation
                    File.AppendAllText(ps.filePath2, Environment.NewLine); //insert double line to file at the end of each parameter evaluation
                    
                    Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, test);

                    outputValues = parameters.C.ToString() + " " + parameters.Gamma.ToString() + " " + test.ToString();
                    File.AppendAllText(ps.filePath, outputValues);
                    File.AppendAllText(ps.filePath, Environment.NewLine);
                    if (output != null)
                        output.WriteLine("{0} {1} {2}", parameters.C, parameters.Gamma, test);
                    if (test > crossValidation)
                    {
                        C = parameters.C;
                        Gamma = parameters.Gamma;
                        crossValidation = test;
                        Console.WriteLine(" New Maximum!");

                        if (test == 1)
                        {
                            CVAccuracy = test;
                            break;
                        }
                        else
                            CVAccuracy = test;
                        //outputValues.Add(C.ToString()); outputValues.Add(Gamma.ToString()); outputValues.Add(test.ToString());
                        //outputValues = C.ToString() + " " + Gamma.ToString() + " " + test.ToString();
                        //File.AppendAllText(ps.filePath, outputValues);
                        //File.AppendAllText(ps.filePath, Environment.NewLine);

                    }
                    else Console.WriteLine();
                }
                if (test == 1)
                {
                    CVAccuracy = test;
                    break;
                }
                else
                    CVAccuracy = test;
            }

            //File.AppendAllText(ps.filePath, Environment.NewLine);
            if (output != null)
                output.Close();
        }

        /// <summary>
        /// sort and retain array index
        /// return retained array index
        /// </summary>
        public static List<int> sortRetainIndex(List<double> array)
        {
            //Ranking the fireflies by their light intensity - this sort nethod also preserves the original index of the sorted array values for future reference
            var sorted = array.Select((x, index) => new KeyValuePair<double, int>(x, index)).OrderBy(x => x.Key).ToList();

            List<double> Sorted = sorted.Select(x => x.Key).ToList(); //select the sorted list of zn values
            List<int> preservedindex = sorted.Select(x => x.Value).ToList(); //select the original index of the zn values

            return preservedindex;
        }

        /// <summary>
        /// Performs a Grid parameter selection for firefly optimized parameters. Try all possible combinations of the two 
        /// lists and returning the combination which performed best.  Use this method if validation data isn't available, 
        /// as it will divide the training data and train on a portion of it and test on the rest.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="parameters">The parameters to use when optimizing</param>
        /// <param name="CValues">The C value to use</param>
        /// <param name="GammaValues">The Gamma value to use</param>
        /// <param name="nrfold">The number of times the data should be divided for validation</param>
        public static void Grid(
            Problem problem,
            Parameter parameters,
            List<Object> fireflies,
            List<int> changedIndex,
            List<double> avgAccr,
            double[] CBackup,
            double[] GBackup,
            int nrfold)
        {
            Console.WriteLine("Firefly optimized parameters...!");
            double crossValidation = double.MinValue;
            foreach (int ci in changedIndex)
            {
                parameters.C = (double)fireflies[ci].cValue;
                parameters.Gamma = (double)fireflies[ci].GValue;
                double test = Training.PerformCrossValidation(problem, parameters, nrfold);
                avgAccr[ci] = test;

                /****
                //update the accuracy only if the new accuracy is better than the previous one
                if (test > avgAccr[ci])
                    avgAccr[ci] = test;
                else //dont update the accuracy; also revert back to the previous updated C and Gamma values 
                {
                    test = avgAccr[ci];

                    fireflies[ci].cValue = CBackup[ci]; //change the firefly updated C value to the previous one
                    fireflies[ci].GValue = GBackup[ci]; //change the firefly updated Gamma values to the previous one

                    parameters.C = (double)fireflies[ci].cValue;
                    parameters.Gamma = (double)fireflies[ci].GValue;
                }
                *****/
                //avgAccr[ci] = (test > avgAccr[ci]) ? test : avgAccr[ci]; //updating the accuracy only if the new accuracy is better than the previous one

                Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, test);
                if (test > crossValidation)
                {
                    crossValidation = test;
                    Console.WriteLine(" New Maximum!");
                }
                else Console.WriteLine();
            }
        }

        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  Uses the default values of C and Gamma.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="validation">The validation data</param>
        /// <param name="parameters">The parameters to use when optimizing</param>
        /// <param name="outputFile">The output file for the parameter results</param>
        /// <param name="C">The optimal C value will be placed in this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be placed in this variable</param>
        public static void Grid(
            Problem problem,
            Problem validation,
            Parameter parameters,
            string outputFile,
            out double C,
            out double Gamma)
        {
            //Grid(problem, parameters, GetList(cMinPower, cMaxPower, C_STEP), GetList(gMinPower, gMaxPower, G_STEP), outputFile, NFOLD, out C, out Gamma);
            //Grid(problem, parameters, GetList(cMinPower, cMaxPower), GetList(gMinPower, gMaxPower), outputFile, NFOLD, out C, out Gamma);
            Grid(problem, validation, parameters, GetList(MIN_C, MAX_C, C_STEP), GetList(MIN_G, MAX_G, G_STEP), outputFile, out C, out Gamma);
            //Grid(problem, validation, parameters, GetList(MIN_C, MAX_C), GetList(MIN_G, MAX_G), outputFile, out C, out Gamma);
        }
        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="validation">The validation data</param>
        /// <param name="parameters">The parameters to use when optimizing</param>
        /// <param name="CValues">The C values to use</param>
        /// <param name="GammaValues">The Gamma values to use</param>
        /// <param name="outputFile">The output file for the parameter results</param>
        /// <param name="C">The optimal C value will be placed in this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be placed in this variable</param>
        public static void Grid(
            Problem problem,
            Problem validation,
            Parameter parameters,
            List<double> CValues,
            List<double> GammaValues,
            string outputFile,
            out double C,
            out double Gamma)
        {
            C = 0;
            Gamma = 0;
            double maxScore = double.MinValue;
            StreamWriter output = null;
            if (outputFile != null)
                output = new StreamWriter(outputFile);
            for (int i = 0; i < CValues.Count; i++)
                for (int j = 0; j < GammaValues.Count; j++)
                {
                    parameters.C = CValues[i];
                    parameters.Gamma = GammaValues[j];
                    Model model = Training.Train(problem, parameters);
                    double test = Prediction.Predict(validation, "tmp.txt", model, false);
                    Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, test);
                    if (output != null)
                        output.WriteLine("{0} {1} {2}", parameters.C, parameters.Gamma, test);
                    if (test > maxScore)
                    {
                        C = parameters.C;
                        Gamma = parameters.Gamma;
                        maxScore = test;
                        Console.WriteLine(" New Maximum!");
                    }
                    else Console.WriteLine();
                }
            if (output != null)
                output.Close();
        }
    }
}
