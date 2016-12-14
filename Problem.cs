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
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SVM
{
    /// <summary>
    /// Encapsulates a problem, or set of vectors which must be classified.
    /// </summary>
	[Serializable]
	public class Problem
	{
        private int _count;
        private double[] _Y;
        private Node[][] _X;
        private Node[] _oneInstance;
        private int _maxIndex;
        private List<Node[]> _NN;
        private int _K;
        private List<double> _labels;
        private List<double> _dist;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="k">Number of nearest neighbour</param>
        /// /// <param name="dist">distances of the nearest neighbours</param>
        /// <param name="nn">Nearest Neighbour</param>
        /// <param name="x">Vector data.</param>
        /// <param name="labels">The class labels</param>
        public Problem(int k, List<double> dist, List<Node[]> nn, Node[] x, List<double> labels)
        {
            _K = k;
            _dist = dist;
            _NN = nn;
            _oneInstance = x;
            _labels = labels;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="count">Number of vectors</param>
        /// <param name="y">The class labels</param>
        /// <param name="x">Vector data.</param>
        /// <param name="maxIndex">Maximum index for a vector</param>
        public Problem(int count, double[] y, Node[][] x, int maxIndex)
        {
            _count = count;
            _Y = y;
            _X = x;
            _maxIndex = maxIndex;
        }

        /// <summary>
        /// Empty Constructor.  Nothing is initialized.
        /// </summary>
        public Problem()
        {
        }
        /// <summary>
        /// Class labels
        /// </summary>
        public List<double> Labels
        {
            get
            {
                return _labels;
            }
            set
            {
                _labels = value;
            }
        }
        /// <summary>
        /// Distance between nearest neighbours
        /// </summary>
        public List<double> Distance
        {
            get
            {
                return _dist;
            }
            set
            {
                _dist = value;
            }
        }
        /// <summary>
        /// Number of vectors.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
            }
        }
        /// <summary>
        /// Class labels.
        /// </summary>
        public double[] Y
        {
            get
            {
                return _Y;
            }
            set
            {
                _Y = value;
            }
        }
        /// <summary>
        /// Vector data.
        /// </summary>
        public Node[][] X
        {
            get
            {
                return _X;
            }
            set
            {
                _X = value;
            }
        }
        /// <summary>
        /// Vector data.
        /// </summary>
        public Node[] oneInstance
        {
            get
            {
                return _oneInstance;
            }
            set
            {
                _oneInstance = value;
            }
        }
        /// <summary>
        /// Maximum index for a vector.
        /// </summary>
        public int MaxIndex
        {
            get
            {
                return _maxIndex;
            }
            set
            {
                _maxIndex = value;
            }
        }
        /// <summary>
        /// Nearest neighbours.
        /// </summary>
        public List<Node[]> NN
        {
            get
            {
                return _NN;
            }
            set
            {
                _NN = value;
            }
        }
        /// <summary>
        /// Number of nearest neighbour.
        /// </summary>
        public int K
        {
            get
            {
                return _K;
            }
            set
            {
                _K = value;
            }
        }
        /// <summary>
        /// Reads a problem from a stream.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <returns>The problem</returns>
        public static Problem Read(Stream stream)
        {
            TemporaryCulture.Start();
            //string a = "+-dog --goat";
            char[] delimiterChars = { '-', '+' };
            StreamReader input = new StreamReader(stream);

            List<double> vy = new List<double>();
            List<Node[]> vx = new List<Node[]>();
            int max_index = 0;
            while (input.Peek() > -1)
            {
                string[] parts = input.ReadLine().Trim().Split();

                vy.Add(double.Parse(parts[0]));
                int m = parts.Length - 1;
                Node[] x = new Node[m];
                for (int j = 0; j < m; j++)
                {
                    x[j] = new Node();
                    string[] nodeParts = parts[j + 1].Split(':');
                    x[j].Index = int.Parse(nodeParts[0]);
                    x[j].Value = double.Parse(nodeParts[1]);
                }
                if (m > 0)
                    max_index = Math.Max(max_index, x[m - 1].Index);
                vx.Add(x);
            }
            TemporaryCulture.Stop();

            return new Problem(vy.Count, vy.ToArray(), vx.ToArray(), max_index);
        }

        /// <summary>
        /// Writes a problem to a stream.
        /// </summary>
        /// <param name="stream">The stream to write the problem to.</param>
        /// <param name="problem">The problem to write.</param>
        public static void Write(Stream stream, Problem problem)
        {
            TemporaryCulture.Start();

            StreamWriter output = new StreamWriter(stream);
            for (int i = 0; i < problem.Count; i++)
            {
                output.Write(problem.Y[i]);
                for (int j = 0; j < problem.X[i].Length; j++)
                    output.Write(" {0}:{1}", problem.X[i][j].Index, problem.X[i][j].Value);
                output.WriteLine();
            }
            output.Flush();

            TemporaryCulture.Stop();
        }

        /// <summary>
        /// Reads a Problem from a file.
        /// </summary>
        /// <param name="filename">The file to read from.</param>
        /// <returns>the Probem</returns>
        public static Problem Read(string filename)
        {
            FileStream input = File.OpenRead(filename);
            try
            {
                return Read(input);
            }
            finally
            {
                input.Close();
            }
        }

        /// <summary>
        /// Writes a problem to a file.   This will overwrite any previous data in the file.
        /// </summary>
        /// <param name="filename">The file to write to</param>
        /// <param name="problem">The problem to write</param>
        public static void Write(string filename, Problem problem)
        {
            FileStream output = File.Open(filename, FileMode.Create);
            try
            {
                Write(output, problem);
            }
            finally
            {
                output.Close();
            }
        }
    }
}